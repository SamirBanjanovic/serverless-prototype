using System;
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

        private QueueClient DeploymentQueueClient { get; set; }

        private Deployment Deployment { get; set; }

        private CancellationTokenSource CancellationTokenSource { get; set; }

        private Task WatchTask { get; set; }

        public DeploymentWatchdog(Deployment deployment)
        {
            this.Deployment = deployment;

            this.HttpClient = new HttpClient();
            this.DeploymentQueueClient = QueueClient.CreateFromConnectionString(
                connectionString: ConfigurationProvider.ServiceBusConnectionString,
                path: this.Deployment.Function.DeploymentId);

            this.CancellationTokenSource = new CancellationTokenSource();
            this.WatchTask = this.Watch(cancellationToken: this.CancellationTokenSource.Token);
        }

        public async Task CancelAsync()
        {
            this.CancellationTokenSource.Cancel();

            this.HttpClient.CancelPendingRequests();

            await this
                .DeploymentQueueClient.CloseAsync()
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
                    executionRequestMessage = await this.DeploymentQueueClient
                        .ReceiveAsync(serverWaitTime: TimeSpan.MaxValue)
                        .ConfigureAwait(continueOnCapturedContext: false);
                }
                catch (MessagingEntityNotFoundException) { }

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
