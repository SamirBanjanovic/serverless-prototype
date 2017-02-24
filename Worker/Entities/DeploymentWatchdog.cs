using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serverless.Worker.Extensions;
using Serverless.Worker.Providers;
using Serverless.Worker.Models;
using Microsoft.ServiceBus.Messaging;

namespace Serverless.Worker.Entities
{
    public class DeploymentWatchdog
    {
        private HttpClient HttpClient { get; set; }

        private Deployment Deployment { get; set; }

        private CancellationTokenSource CancellationTokenSource { get; set; }

        private Task WatchTask { get; set; }

        public DeploymentWatchdog(Deployment deployment)
        {
            this.Deployment = deployment;

            this.HttpClient = new HttpClient();

            this.CancellationTokenSource = new CancellationTokenSource();
            this.WatchTask = this.Watch(cancellationToken: this.CancellationTokenSource.Token);
        }

        public async Task CancelAsync()
        {
            this.CancellationTokenSource.Cancel();

            this.HttpClient.CancelPendingRequests();

            await ServiceBusProvider
                .CloseQueueClient(path: this.Deployment.Function.DeploymentId)
                .ConfigureAwait(continueOnCapturedContext: false);

            await this.WatchTask.ConfigureAwait(continueOnCapturedContext: false);
        }

        private async Task Watch(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                BrokeredMessage executionRequestMessage = null;
                try
                {
                    var deploymentQueueClient = await ServiceBusProvider
                        .GetQueueClient(path: this.Deployment.Function.DeploymentId)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    executionRequestMessage = await deploymentQueueClient
                        .ReceiveAsync(serverWaitTime: TimeSpan.MaxValue)
                        .ConfigureAwait(continueOnCapturedContext: false);
                }
                catch (MessagingEntityNotFoundException) { }

                var stopwatch = Stopwatch.StartNew();

                if (executionRequestMessage == null)
                {
                    this.Deployment.Delete();

                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var executionRequest = await executionRequestMessage
                    .ParseBody<ExecutionRequest>()
                    .ConfigureAwait(continueOnCapturedContext: false);

                var executionResponse = await this.Deployment
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
                    Name = "DeploymentWatchdog.Watch",
                    Duration = stopwatch.ElapsedMilliseconds
                };

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
        }


    }
}
