using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;
using Serverless.Common.Configuration;
using Serverless.Common.Extensions;
using Serverless.Common.Models;
using Serverless.Common.Providers;

namespace Serverless.Web.Providers
{
    public static class ExecutionProvider
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task<ExecutionResponse> Execute(ExecutionRequest request)
        {
            CloudQueueMessage queueMessage;

            do
            {
                queueMessage = await QueueProvider
                    .GetMessage(queueName: request.Function.Id)
                    .ConfigureAwait(continueOnCapturedContext: false);

                if (queueMessage != null)
                {
                    var httpResponse = await HttpClient
                        .PostAsJsonAsync(
                            requestUri: queueMessage.FromJson<ExecutionAvailability>().CallbackURI,
                            value: request)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        var response = await httpResponse.Content
                            .FromJson<ExecutionResponse>()
                            .ConfigureAwait(continueOnCapturedContext: false);

                        await QueueProvider
                            .SetMessageVisibilityTimeout(
                                queueName: request.Function.Id,
                                message: queueMessage,
                                visibilityTimeout: TimeSpan.Zero)
                            .ConfigureAwait(continueOnCapturedContext: false);

                        return response;
                    }
                    else if (httpResponse.StatusCode == HttpStatusCode.Gone)
                    {
                        await QueueProvider
                            .DeleteMessage(
                                queueName: request.Function.Id,
                                message: queueMessage)
                            .ConfigureAwait(continueOnCapturedContext: false);
                    }
                }
            }
            while (queueMessage != null);

            do
            {
                queueMessage = await QueueProvider
                    .GetMessage(queueName: ServerlessConfiguration.ExecutionQueueName)
                    .ConfigureAwait(continueOnCapturedContext: false);

                if (queueMessage != null)
                {
                    var httpResponse = await HttpClient
                        .PostAsJsonAsync(
                            requestUri: queueMessage.FromJson<ExecutionAvailability>().CallbackURI,
                            value: request)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        var response = await httpResponse.Content
                            .FromJson<ExecutionResponse>()
                            .ConfigureAwait(continueOnCapturedContext: false);

                        await QueueProvider
                            .DeleteMessage(
                                queueName: ServerlessConfiguration.ExecutionQueueName,
                                message: queueMessage)
                            .ConfigureAwait(continueOnCapturedContext: false);

                        return response;
                    }
                }
            }
            while (queueMessage != null);

            return null;
        }
    }
}