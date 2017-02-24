using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Serverless.Common.Configuration;
using Serverless.Common.Extensions;

namespace Serverless.Common.Providers
{
    public static class QueueProvider
    {
        private static readonly ConcurrentDictionary<string, bool> Queues = new ConcurrentDictionary<string, bool>();

        public static async Task AddMessage<T>(string queueName, T message)
        {
            var queue = ServerlessConfiguration.StorageAccount
                .CreateCloudQueueClient()
                .GetQueueReference(queueName: queueName);

            if (!QueueProvider.Queues.ContainsKey(key: queueName))
            {
                await queue
                    .CreateIfNotExistsAsync()
                    .ConfigureAwait(continueOnCapturedContext: false);

                QueueProvider.Queues[queueName] = true;
            }

            await queue
                .AddMessageAsync(message: new CloudQueueMessage(content: message.ToJson()))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task<CloudQueueMessage> GetMessage(string queueName)
        {
            var queue = ServerlessConfiguration.StorageAccount
                .CreateCloudQueueClient()
                .GetQueueReference(queueName: queueName);

            if (!QueueProvider.Queues.ContainsKey(key: queueName))
            {
                await queue
                    .CreateIfNotExistsAsync()
                    .ConfigureAwait(continueOnCapturedContext: false);

                QueueProvider.Queues[queueName] = true;
            }

            return await queue
                .GetMessageAsync()
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task<CloudQueueMessage> WaitForMessage(string queueName, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await QueueProvider
                    .GetMessage(queueName: queueName)
                    .ConfigureAwait(continueOnCapturedContext: false);

                if (message != null)
                {
                    return message;
                }
            }

            return null;
        }

        public static Task DeleteMessage(string queueName, CloudQueueMessage message)
        {
            return ServerlessConfiguration.StorageAccount
                .CreateCloudQueueClient()
                .GetQueueReference(queueName: queueName)
                .DeleteMessageAsync(message: message);
        }

        public static Task DeleteQueue(string queueName)
        {
            return ServerlessConfiguration.StorageAccount
                .CreateCloudQueueClient()
                .GetQueueReference(queueName: queueName)
                .DeleteIfExistsAsync();
        }
    }
}
