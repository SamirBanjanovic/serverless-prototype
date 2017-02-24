using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serverless.Common.Configuration;
using Serverless.Common.Extensions;
using Serverless.Common.Providers;
using Serverless.Common.Models;
using Serverless.Worker.Extensions;

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

            await this.WatchTask
                .ConfigureAwait(continueOnCapturedContext: false);

            await this.Deployment
                .Delete()
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        private async Task Watch(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var executionRequestMessage = await QueueProvider
                    .WaitForMessage(
                        queueName: this.Deployment.Function.DeploymentId,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var stopwatch = Stopwatch.StartNew();

                var executionRequest = executionRequestMessage.FromJson<ExecutionRequest>();

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
                            format: ServerlessConfiguration.ResponseTemplate,
                            arg0: executionRequest.ExecutionId),
                        content: executionResponse,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                await QueueProvider
                    .DeleteMessage(
                        queueName: this.Deployment.Function.DeploymentId,
                        message: executionRequestMessage)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }


    }
}
