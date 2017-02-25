using System;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage;
using Serverless.Common.Providers;

namespace Serverless.Common.Configuration
{
    public static class ServerlessConfiguration
    {
        public const string DriveName = "C";
        public const string DockerUri = "http://127.0.0.1:2375";
        public const int AvailableMemory = 4096;
        public const int MaximumFunctionMemory = 1024;
        public const string StorageConnectionString = "UseDevelopmentStorage=true";
        public const string ExecutionQueueName = "serverless-queue";
        public const string ServiceName = "Serverless Prototype";
        public const int ContainerLifeInMinutes = 15;
        public const string ContainerUriTemplate = "http://{0}:8080/";
        public const string DockerNetworkName = "nat";
        public const string CodeDirectoryTemplate = @"{0}:\functions";
        public const string ExecutionTemplate = "http://{0}/containers/{1}/execute";

        public static readonly string IPAddress = "localhost:44027";

        public static readonly CloudStorageAccount StorageAccount = ServerlessConfiguration.ParseStorageAccount();

        private static CloudStorageAccount ParseStorageAccount()
        {
            return CloudStorageAccount.Parse(connectionString: ServerlessConfiguration.StorageConnectionString);
        }
    }
}
