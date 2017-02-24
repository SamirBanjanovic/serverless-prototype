using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Configuration;
using Microsoft.WindowsAzure.Storage;

namespace Serverless.Web.Providers
{
    public static class ConfigurationProvider
    {
        public static readonly CloudStorageAccount StorageAccount = ConfigurationProvider.ParseStorageAccount();

        public static readonly NamespaceManager NamespaceManager = ConfigurationProvider.ParseNamespaceManager();

        public static readonly string ExecutionQueueName = CloudConfigurationManager.GetSetting(name: "QueueName");

        private static readonly ConcurrentDictionary<string, QueueClient> QueueClients = new ConcurrentDictionary<string, QueueClient>();

        private static readonly string ServiceBusConnectionString = CloudConfigurationManager.GetSetting(name: "ServiceBusConnectionString");

        private static readonly string ServiceBusEndpoint = CloudConfigurationManager.GetSetting(name: "ServiceBusEndpoint");

        private static readonly MessagingFactory MessagingFactory = MessagingFactory.Create(
            address: ConfigurationProvider.ServiceBusEndpoint,
            factorySettings: new MessagingFactorySettings
            {
                TransportType = TransportType.Amqp,
                AmqpTransportSettings = new AmqpTransportSettings
                {
                    BatchFlushInterval = TimeSpan.Zero
                },
                TokenProvider = ConfigurationProvider.NamespaceManager.Settings.TokenProvider
            });

        private static CloudStorageAccount ParseStorageAccount()
        {
            var connectionString = CloudConfigurationManager.GetSetting(name: "StorageConnectionString");
            return CloudStorageAccount.Parse(connectionString: connectionString);
        }

        private static NamespaceManager ParseNamespaceManager()
        {
            var connectionString = CloudConfigurationManager.GetSetting(name: "ServiceBusConnectionString");
            return NamespaceManager.CreateFromConnectionString(connectionString: connectionString);
        }

        public static async Task<QueueClient> GetQueueClient(string path)
        {
            if (ConfigurationProvider.QueueClients.ContainsKey(key: path))
            {
                return ConfigurationProvider.QueueClients[path];
            }
            else
            {
                var queueExists = await ConfigurationProvider.NamespaceManager
                    .QueueExistsAsync(path: path)
                    .ConfigureAwait(continueOnCapturedContext: false);

                if (!queueExists)
                {
                    var queueDescription = new QueueDescription(path: path)
                    {
                        EnableBatchedOperations = false,
                        EnablePartitioning = true
                    };
                    
                    await ConfigurationProvider.NamespaceManager
                        .CreateQueueAsync(description: queueDescription)
                        .ConfigureAwait(continueOnCapturedContext: false);
                }

                return ConfigurationProvider.QueueClients.GetOrAdd(
                    key: path,
                    valueFactory: ConfigurationProvider.CreateQueueClient);
            }
        }

        private static QueueClient CreateQueueClient(string path)
        {
            return ConfigurationProvider.MessagingFactory.CreateQueueClient(path: path);
        }
    }
}