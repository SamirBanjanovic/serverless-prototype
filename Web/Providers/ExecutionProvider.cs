using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Serverless.Common.Configuration;
using Serverless.Common.Providers;
using Serverless.Common.Models;
using Serverless.Web.Entities;

namespace Serverless.Web.Providers
{
    public static class ExecutionProvider
    {
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<ExecutionResponse>> Responses = new ConcurrentDictionary<string, TaskCompletionSource<ExecutionResponse>>();

        public static async Task<ExecutionResponse> Execute(Function function, ExecutionRequest request)
        {
            ExecutionProvider.Responses[request.ExecutionId] = new TaskCompletionSource<ExecutionResponse>();

            var stopwatch = Stopwatch.StartNew();

            await QueueProvider
                .AddMessage(
                    queueName: function.DeploymentId,
                    message: request)
                .ConfigureAwait(continueOnCapturedContext: false);

            var deploymentMessageLog = new ExecutionLog
            {
                Name = "ExecutionProvider.Execute.DeploymentMessage",
                Duration = stopwatch.ElapsedMilliseconds
            };

            var executionMessageTask = Task
                .Delay(delay: TimeSpan.FromMilliseconds(100))
                .ContinueWith(task => QueueProvider.AddMessage(
                    queueName: ServerlessConfiguration.ExecutionQueueName,
                    message: function.ToResponseModel()));

            var responseTask = ExecutionProvider.Responses[request.ExecutionId].Task;

            await Task
                .WhenAny(executionMessageTask, responseTask)
                .ConfigureAwait(continueOnCapturedContext: false);

            ExecutionResponse response;
            if (responseTask.IsCompleted)
            {
                var responseLog = new ExecutionLog
                {
                    Name = "ExecutionProvider.Execute.AwaitResponse",
                    Duration = stopwatch.ElapsedMilliseconds - deploymentMessageLog.Duration
                };

                response = responseTask.Result;
                response.Logs = new ExecutionLog
                {
                        Name = "ExecutionProvider.Execute",
                        Duration = stopwatch.ElapsedMilliseconds,
                        SubLogs = new[]
                        {
                            deploymentMessageLog,
                            responseLog,
                            response.Logs
                        }
                };
            }
            else
            {
                var executionMessageLog = new ExecutionLog
                {
                    Name = "ExecutionProvider.Execute.ExecutionMessage",
                    Duration = stopwatch.ElapsedMilliseconds - deploymentMessageLog.Duration
                };

                response = await responseTask.ConfigureAwait(continueOnCapturedContext: false);

                var responseLog = new ExecutionLog
                {
                    Name = "ExecutionProvider.Execute.AwaitResponse",
                    Duration = stopwatch.ElapsedMilliseconds - executionMessageLog.Duration
                };

                response.Logs = new ExecutionLog
                {
                    Name = "ExecutionProvider.Execute",
                    Duration = stopwatch.ElapsedMilliseconds,
                    SubLogs = new[]
                    {
                        deploymentMessageLog,
                        executionMessageLog,
                        responseLog,
                        response.Logs
                    }
                };
            }

            return response;
        }

        public static void Respond(string executionId, ExecutionResponse response)
        {
            ExecutionProvider.Responses[executionId].TrySetResult(result: response);
        }
    }
}