using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            var stopwatch = Stopwatch.StartNew();
            var logs = new List<ExecutionLog>();

            CloudQueueMessage queueMessage;

            do
            {
                queueMessage = await QueueProvider
                    .GetMessage(queueName: request.Function.Id)
                    .ConfigureAwait(continueOnCapturedContext: false);

                logs.Add(new ExecutionLog
                {
                    Name = "GetWarmMessage",
                    Duration = stopwatch.ElapsedMilliseconds - (logs.Count > 0 ? logs.Last().Duration : 0)
                });

                if (queueMessage != null)
                {
                    var httpResponse = await HttpClient
                        .PostAsJsonAsync(
                            requestUri: queueMessage.FromJson<ExecutionAvailability>().CallbackURI,
                            value: request)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    logs.Add(new ExecutionLog
                    {
                        Name = "ExecuteWarm",
                        Duration = stopwatch.ElapsedMilliseconds - logs.Last().Duration
                    });

                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        var response = await httpResponse.Content
                            .FromJson<ExecutionResponse>()
                            .ConfigureAwait(continueOnCapturedContext: false);

                        logs.Add(response.Logs);

                        await QueueProvider
                            .SetMessageVisibilityTimeout(
                                queueName: request.Function.Id,
                                message: queueMessage,
                                visibilityTimeout: TimeSpan.Zero)
                            .ConfigureAwait(continueOnCapturedContext: false);

                        logs.Add(new ExecutionLog
                        {
                            Name = "SetWarmVisibilityTimeout",
                            Duration = stopwatch.ElapsedMilliseconds - logs.Last().Duration
                        });

                        response.Logs = new ExecutionLog
                        {
                            Name = "ExecutionProvider.Warm",
                            Duration = stopwatch.ElapsedMilliseconds,
                            SubLogs = logs.ToArray()
                        };

                        return response;
                    }
                    else if (httpResponse.StatusCode == HttpStatusCode.Gone)
                    {
                        await QueueProvider
                            .DeleteMessage(
                                queueName: request.Function.Id,
                                message: queueMessage)
                            .ConfigureAwait(continueOnCapturedContext: false);

                        logs.Add(new ExecutionLog
                        {
                            Name = "DeleteWarmMessage",
                            Duration = stopwatch.ElapsedMilliseconds - logs.Last().Duration
                        });
                    }
                }
            }
            while (queueMessage != null);

            do
            {
                queueMessage = await QueueProvider
                    .GetMessage(queueName: ServerlessConfiguration.ExecutionQueueName)
                    .ConfigureAwait(continueOnCapturedContext: false);

                logs.Add(new ExecutionLog
                {
                    Name = "GetColdMessage",
                    Duration = stopwatch.ElapsedMilliseconds - logs.Last().Duration
                });

                if (queueMessage != null)
                {
                    var httpResponse = await HttpClient
                        .PostAsJsonAsync(
                            requestUri: queueMessage.FromJson<ExecutionAvailability>().CallbackURI,
                            value: request)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    logs.Add(new ExecutionLog
                    {
                        Name = "ExecuteCold",
                        Duration = stopwatch.ElapsedMilliseconds - logs.Last().Duration
                    });

                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        var response = await httpResponse.Content
                            .FromJson<ExecutionResponse>()
                            .ConfigureAwait(continueOnCapturedContext: false);

                        logs.Add(response.Logs);

                        await QueueProvider
                            .DeleteMessage(
                                queueName: ServerlessConfiguration.ExecutionQueueName,
                                message: queueMessage)
                            .ConfigureAwait(continueOnCapturedContext: false);

                        logs.Add(new ExecutionLog
                        {
                            Name = "DeleteColdMessage",
                            Duration = stopwatch.ElapsedMilliseconds - logs.Last().Duration
                        });

                        response.Logs = new ExecutionLog
                        {
                            Name = "ExecutionProvider.Cold",
                            Duration = stopwatch.ElapsedMilliseconds,
                            SubLogs = logs.ToArray()
                        };

                        return response;
                    }
                }
            }
            while (queueMessage != null);

            return null;
        }
    }
}