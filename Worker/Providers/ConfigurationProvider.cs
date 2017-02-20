using System;

namespace Serverless.Worker.Providers
{
    public static class ConfigurationProvider
    {
        public const string DriveName = "C";
        public const string DockerUri = "http://127.0.0.1:2375";
        public const int AvailableMemory = 4096;
        public const int MaximumFunctionMemory = 1024;
        public const string ServiceBusConnectionString = "";
        public const string ExecutionQueueName = "serverless-queue";
        public const string ServiceName = "Serverless Prototype";
        public const string ResponseTemplate = "https://ndserverless.azurewebsites.net/executions/{0}/respond";
        public const int InstanceCacheMinutes = 15;
        public const string ContainerUriTemplate = "http://{0}:8080/";
        public const string DockerNetworkName = "nat";
        public const string CodeDirectoryTemplate = @"{0}:\deployments";
    }
}
