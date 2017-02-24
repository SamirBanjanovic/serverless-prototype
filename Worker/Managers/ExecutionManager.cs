using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serverless.Common.Configuration;
using Serverless.Common.Extensions;
using Serverless.Common.Providers;
using Serverless.Common.Models;
using Serverless.Worker.Entities;
using Serverless.Worker.Extensions;
using Serverless.Worker.Models;

namespace Serverless.Worker.Managers
{
    public class ExecutionManager
    {
        public HttpClient HttpClient { get; set; }

        public int AvailableMemory { get; set; }

        private CancellationTokenSource CancellationTokenSource { get; set; }

        private HashSet<Task> ExecutionTasks { get; set; }

        private Dictionary<string, Deployment> Deployments { get; set; }

        private Task ManagementTask { get; set; }

        public void Start()
        {
            this.HttpClient = new HttpClient();

            this.CancellationTokenSource = new CancellationTokenSource();

            this.ExecutionTasks = new HashSet<Task>();

            this.Deployments = new Dictionary<string, Deployment>();

            this.AvailableMemory = ServerlessConfiguration.AvailableMemory;

            this.ManagementTask = this.ManageExecutions(cancellationToken: this.CancellationTokenSource.Token);
        }

        public void Stop()
        {
            this.CancellationTokenSource.Cancel();

            this.HttpClient.CancelPendingRequests();

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
                        this.AvailableMemory += ServerlessConfiguration.MaximumFunctionMemory;
                    }
                }

                while (this.AvailableMemory >= ServerlessConfiguration.MaximumFunctionMemory)
                {
                    this.ExecutionTasks.Add(this.Execute(cancellationToken: cancellationToken));

                    lock (this)
                    {
                        this.AvailableMemory -= ServerlessConfiguration.MaximumFunctionMemory;
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
            var functionMessage = await QueueProvider
                .WaitForMessage(
                    queueName: ServerlessConfiguration.ExecutionQueueName,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            var function = functionMessage.FromJson<Function>();

            lock (this.Deployments)
            {
                if (!this.Deployments.ContainsKey(key: function.DeploymentId))
                {
                    this.Deployments[function.DeploymentId] = new Deployment(
                        function: function,
                        executionManager: this,
                        cancellationToken: cancellationToken);
                }
            }

            var executionRequestMessage = await QueueProvider
                .GetMessage(queueName: function.DeploymentId)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (executionRequestMessage != null)
            {
                var executionRequest = executionRequestMessage.FromJson<ExecutionRequest>();

                var deployment = this.Deployments[function.DeploymentId];

                var executionResponse = await deployment
                    .Execute(
                        request: executionRequest,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                executionResponse.Logs = new ExecutionLog
                {
                    Name = "ExecutionManager.Execute",
                    Duration = stopwatch.ElapsedMilliseconds
                };

                var response = await this.HttpClient
                    .PostAsync<ExecutionResponse>(
                        requestUri: String.Format(
                            format: ServerlessConfiguration.ResponseTemplate,
                            arg0: executionRequest.ExecutionId),
                        content: executionResponse,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                await QueueProvider
                    .DeleteMessage(
                        queueName: ServerlessConfiguration.ExecutionQueueName,
                        message: executionRequestMessage)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            await QueueProvider
                .DeleteMessage(
                    queueName: function.DeploymentId,
                    message: functionMessage)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
