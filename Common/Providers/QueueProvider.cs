using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
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
            var queue = await QueueProvider
                .GetQueue(queueName: queueName)
                .ConfigureAwait(continueOnCapturedContext: false);

            await queue
                .AddMessageAsync(message: new CloudQueueMessage(content: message.ToJson()))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task<CloudQueueMessage> GetMessage(string queueName)
        {
            var queue = await QueueProvider
                .GetQueue(queueName: queueName)
                .ConfigureAwait(continueOnCapturedContext: false);

            return await queue
                .GetMessageAsync()
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task SetMessageVisibilityTimeout(string queueName, CloudQueueMessage message, TimeSpan visibilityTimeout)
        {
            var queue = await QueueProvider
                .GetQueue(queueName: queueName)
                .ConfigureAwait(continueOnCapturedContext: false);

            await queue
                .UpdateMessageAsync(
                    message: message,
                    visibilityTimeout: visibilityTimeout,
                    updateFields: MessageUpdateFields.Visibility)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task DeleteMessage(string queueName, CloudQueueMessage message)
        {
            var queue = await QueueProvider
                .GetQueue(queueName: queueName)
                .ConfigureAwait(continueOnCapturedContext: false);

            await queue
                .DeleteMessageAsync(message: message)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static Task DeleteQueue(string queueName)
        {
            return ServerlessConfiguration.StorageAccount
                .CreateCloudQueueClient()
                .GetQueueReference(queueName: queueName)
                .DeleteIfExistsAsync();
        }

        private static async Task<CloudQueue> GetQueue(string queueName)
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

            return queue;
        }
    }
}
