using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serverless.Worker.Entities;
using Serverless.Worker.Extensions;
using Serverless.Worker.Models;
using Serverless.Worker.Providers;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace Serverless.Worker.Managers
{
    public class ExecutionManager
    {
        public HttpClient HttpClient { get; set; }

        public QueueClient ExecutionQueueClient { get; set; }

        public int AvailableMemory { get; set; }

        private CancellationTokenSource CancellationTokenSource { get; set; }

        private HashSet<Task> ExecutionTasks { get; set; }

        private ConcurrentDictionary<string, Deployment> Deployments { get; set; }

        private Task ManagementTask { get; set; }

        public void Start()
        {
            this.HttpClient = new HttpClient();

            this.ExecutionQueueClient = QueueClient.CreateFromConnectionString(
                connectionString: ConfigurationProvider.ServiceBusConnectionString,
                path: ConfigurationProvider.ExecutionQueueName);

            this.CancellationTokenSource = new CancellationTokenSource();

            this.ExecutionTasks = new HashSet<Task>();

            this.Deployments = new ConcurrentDictionary<string, Deployment>();

            this.AvailableMemory = ConfigurationProvider.AvailableMemory;

            this.ManagementTask = this.ManageExecutions(cancellationToken: this.CancellationTokenSource.Token);
        }

        public void Stop()
        {
            this.CancellationTokenSource.Cancel();

            this.HttpClient.CancelPendingRequests();

            this.ExecutionQueueClient.Close();

            this.ManagementTask.Wait();

            Task.WhenAll(this.ExecutionTasks).Wait();

            Task.WhenAll(this.Deployments.Values.Select(deployment => deployment.ManagementTask)).Wait();
        }

        public async Task ManageExecutions(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (this.ExecutionTasks.Count > 0)
                {
                    var task = await Task
                        .WhenAny(tasks: this.ExecutionTasks)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    this.ExecutionTasks.Remove(task);

                    lock (this)
                    {
                        this.AvailableMemory += ConfigurationProvider.MaximumFunctionMemory;
                    }
                }

                while (this.AvailableMemory > ConfigurationProvider.MaximumFunctionMemory)
                {
                    this.ExecutionTasks.Add(this.Execute(cancellationToken: cancellationToken));

                    lock (this)
                    {
                        this.AvailableMemory -= ConfigurationProvider.MaximumFunctionMemory;
                    }
                }

                if (this.ExecutionTasks.Count == 0)
                {
                    await Task
                        .Delay(
                            delay: TimeSpan.FromSeconds(1),
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false);
                }
            }
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            var functionMessage = await this.ExecutionQueueClient
                .ReceiveAsync(serverWaitTime: TimeSpan.MaxValue)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            
            var function = await functionMessage
                .ParseBody<Function>()
                .ConfigureAwait(continueOnCapturedContext: false);

            var deployment = this.Deployments.GetOrAdd(
                key: function.DeploymentId,
                valueFactory: id => new Deployment(
                    function: function,
                    executionManager: this,
                    cancellationToken: cancellationToken));

            var executionRequestMessage = await deployment.DeploymentQueueClient
                .ReceiveAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (executionRequestMessage != null)
            {
                var executionRequest = await executionRequestMessage
                    .ParseBody<ExecutionRequest>()
                    .ConfigureAwait(continueOnCapturedContext: false);

                var executionResponse = await deployment
                    .Execute(
                        request: executionRequest,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var response = await this.HttpClient
                    .PostAsync<ExecutionResponse>(
                        requestUri: String.Format(
                            format: ConfigurationProvider.ResponseTemplate,
                            arg0: executionRequest.ExecutionId),
                        content: executionResponse,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                await executionRequestMessage
                    .CompleteAsync()
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            await functionMessage
                .CompleteAsync()
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
