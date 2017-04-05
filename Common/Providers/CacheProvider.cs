using System;
using System.Threading.Tasks;
using Serverless.Common.Configuration;
using Serverless.Common.Extensions;
using StackExchange.Redis;

namespace Serverless.Common.Providers
{
    public static class CacheProvider
    {
        private static readonly ConnectionMultiplexer RedisConnection = ConnectionMultiplexer.Connect(ServerlessConfiguration.RedisConnectionString);

        public static Task Enqueue<T>(string queueName, T message) where T : class
        {
            return RedisConnection
                .GetDatabase()
                .ListLeftPushAsync(
                    key: queueName,
                    value: message.ToJson());
        }

        public static async Task<T> Dequeue<T>(string queueName) where T : class
        {
            var message = await RedisConnection
                .GetDatabase()
                .ListLeftPopAsync(key: queueName)
                .ConfigureAwait(continueOnCapturedContext: false);

            return message.ToString().FromJson<T>();
        }

        public static Task<bool> QueueExists(string queueName)
        {
            return RedisConnection
                .GetDatabase()
                .KeyExistsAsync(key: queueName);
        }
    }
}
