using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web;
using Microsoft.Azure;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;

namespace Serverless.Web.Providers
{
    public static class ConfigurationProvider
    {
        public static readonly CloudStorageAccount StorageAccount = ConfigurationProvider.ParseStorageAccount();

        public static readonly NamespaceManager NamespaceManager = ConfigurationProvider.ParseNamespaceManager();

        public static readonly string ExecutionQueueName = CloudConfigurationManager.GetSetting(name: "QueueName");

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

        public static QueueClient ParseQueueClient(string path)
        {
            var connectionString = CloudConfigurationManager.GetSetting(name: "ServiceBusConnectionString");
            return QueueClient.CreateFromConnectionString(connectionString: connectionString, path: path);
        }
    }
}