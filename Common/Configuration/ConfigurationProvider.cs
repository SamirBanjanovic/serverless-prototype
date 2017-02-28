using System;
using Microsoft.WindowsAzure.Storage;
using Serverless.Common.Providers;

namespace Serverless.Common.Configuration
{
    public static class ServerlessConfiguration
    {
        public const string DriveName = "C";
        public const string DockerUri = "http://127.0.0.1:2375";
        public const int AvailableMemory = 6144;
        public const int MaximumFunctionMemory = 1024;
        public const string ExecutionQueueName = "serverless-queue";
        public const string ServiceName = "Serverless Prototype";
        public const int ContainerLifeInMinutes = 15;
        public const string ContainerUriTemplate = "http://{0}:8080/";
        public const string DockerNetworkName = "nat";
        public const string CodeDirectoryTemplate = @"{0}:\functions";

#if DEBUG
        public static string ExecutionTemplate = "http://localhost:44027/containers/{0}/execute";
        public const string StorageConnectionString = "UseDevelopmentStorage=true";
#else
        public static string ExecutionTemplate = string.Format(
            format: "http://{0}:44027/containers/{1}/execute",
            arg0: IPProvider.GetPublicIP().Result,
            arg1: "{0}");
        public const string StorageConnectionString = "";
#endif

        public static readonly CloudStorageAccount StorageAccount = ServerlessConfiguration.ParseStorageAccount();

        private static CloudStorageAccount ParseStorageAccount()
        {
            return CloudStorageAccount.Parse(connectionString: ServerlessConfiguration.StorageConnectionString);
        }
    }
}
