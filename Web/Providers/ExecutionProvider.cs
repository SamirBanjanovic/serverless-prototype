using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Serverless.Web.Entities;
using Serverless.Web.Extensions;
using Serverless.Web.Models;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace Serverless.Web.Providers
{
    public static class ExecutionProvider
    {
        private static ConcurrentDictionary<string, TaskCompletionSource<ExecutionResponse>> Responses { get; set; }

        static ExecutionProvider()
        {
            ExecutionProvider.Responses = new ConcurrentDictionary<string, TaskCompletionSource<ExecutionResponse>>();
        }

        public static async Task<ExecutionResponse> Execute(Function function, ExecutionRequest request)
        {
            var deploymentQueue = await ConfigurationProvider
                .GetQueueClient(path: function.DeploymentId)
                .ConfigureAwait(continueOnCapturedContext: false);

            var executionQueue = await ConfigurationProvider
                .GetQueueClient(path: ConfigurationProvider.ExecutionQueueName)
                .ConfigureAwait(continueOnCapturedContext: false);

            var tcs = new TaskCompletionSource<ExecutionResponse>();

            ExecutionProvider.Responses[request.ExecutionId] = tcs;

            await deploymentQueue
                .SendAsync<ExecutionRequest>(message: request)
                .ConfigureAwait(continueOnCapturedContext: false);

            await executionQueue
                .SendAsync<FunctionResponseModel>(message: function.ToResponseModel())
                .ConfigureAwait(continueOnCapturedContext: false);

            return await tcs.Task.ConfigureAwait(continueOnCapturedContext: false);
        }

        public static void Respond(string executionId, ExecutionResponse response)
        {
            ExecutionProvider.Responses[executionId].TrySetResult(result: response);
        }

        public static Task DeleteQueue(Function function)
        {
            return ExecutionProvider.DeleteQueue(path: function.DeploymentId);
        }

        private static async Task DeleteQueue(string path)
        {
            var queueExists = await ConfigurationProvider.NamespaceManager
                .QueueExistsAsync(path: path)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (queueExists)
            {
                await ConfigurationProvider.NamespaceManager
                    .DeleteQueueAsync(path: path)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }
}