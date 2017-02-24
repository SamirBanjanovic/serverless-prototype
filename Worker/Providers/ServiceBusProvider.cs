using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp;

namespace Serverless.Worker.Providers
{
    public static class ServiceBusProvider
    {
        private static readonly NamespaceManager NamespaceManager = NamespaceManager.CreateFromConnectionString(connectionString: ConfigurationProvider.ServiceBusConnectionString);

        private static readonly MessagingFactory MessagingFactory = ServiceBusProvider.CreateMessagingFactory().Result;

        private static readonly ConcurrentDictionary<string, QueueClient> QueueClients = new ConcurrentDictionary<string, QueueClient>();

        private static Task<MessagingFactory> CreateMessagingFactory()
        {
            return MessagingFactory.CreateAsync(
                address: ConfigurationProvider.ServiceBusEndpoint,
                factorySettings: new MessagingFactorySettings
                {
                    TransportType = TransportType.Amqp,
                    AmqpTransportSettings = new AmqpTransportSettings
                    {
                        BatchFlushInterval = TimeSpan.Zero
                    },
                    TokenProvider = ServiceBusProvider.NamespaceManager.Settings.TokenProvider
                });
        }

        private static async Task CreateQueueIfNotExists(string path)
        {
            var queueExists = await ServiceBusProvider.NamespaceManager
                .QueueExistsAsync(path: path)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (!queueExists)
            {
                var queueDescription = new QueueDescription(path: path)
                {
                    EnableBatchedOperations = false,
                    EnablePartitioning = true
                };

                await ServiceBusProvider.NamespaceManager
                    .CreateQueueAsync(description: queueDescription)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private static QueueClient CreateQueueClient(string path)
        {
            return ServiceBusProvider.MessagingFactory.CreateQueueClient(path: path);
        }

        public static async Task<QueueClient> GetQueueClient(string path)
        {
            if (ServiceBusProvider.QueueClients.ContainsKey(key: path))
            {
                return ServiceBusProvider.QueueClients[path];
            }
            else
            {
                await ServiceBusProvider
                    .CreateQueueIfNotExists(path: path)
                    .ConfigureAwait(continueOnCapturedContext: false);

                return ServiceBusProvider.QueueClients.GetOrAdd(
                    key: path,
                    valueFactory: ServiceBusProvider.CreateQueueClient);
            }
        }

        public static Task CloseQueueClient(string path)
        {
            QueueClient client;
            if (ServiceBusProvider.QueueClients.TryRemove(path, out client))
            {
                return client.CloseAsync();
            }

            return null;
        }
    }
}
