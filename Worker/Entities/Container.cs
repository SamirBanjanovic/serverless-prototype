using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json.Linq;
using Serverless.Common.Configuration;
using Serverless.Common.Extensions;
using Serverless.Common.Models;
using Serverless.Worker.Models;

namespace Serverless.Worker.Entities
{
    public class Container
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        private static readonly DockerClient DockerClient = Container.CreateDockerClient();

        private static DockerClient CreateDockerClient()
        {
            var dockerUri = new Uri(ServerlessConfiguration.DockerUri);
            return new DockerClientConfiguration(endpoint: dockerUri).CreateClient();
        }

        private string Id { get; set; }

        private string Name { get; set; }

        private Uri Uri { get; set; }

        public DateTime LastExecutionTime { get; private set; }

        public Container(string id, string name, string ipAddress)
        {
            this.Id = id;
            this.Name = name;
            this.Uri = new Uri(string.Format(
                format: ServerlessConfiguration.ContainerUriTemplate,
                arg0: ipAddress));
            this.LastExecutionTime = DateTime.UtcNow;
        }

        public async Task<ExecutionResponse> Execute(ExecutionRequest request)
        {
            this.LastExecutionTime = DateTime.UtcNow;

            var response = await Container.HttpClient
                .PostAsync(
                    requestUri: this.Uri,
                    content: request.Input)
                .ConfigureAwait(continueOnCapturedContext: false);

            var output = await response.Content
                .FromJson<JToken>()
                .ConfigureAwait(continueOnCapturedContext: false);

            return new ExecutionResponse
            {
                Output = output
            };
        }

        public async Task Delete()
        {
            // Keep an eye on this, it looks like a Docker Engine bug on remove.
            try
            {
                await Container.DockerClient.Containers
                    .RemoveContainerAsync(
                        id: this.Id,
                        parameters: new ContainerRemoveParameters
                        {
                            Force = true
                        })
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            catch (DockerApiException exception)
            {
                if (exception.Message.Contains("windowsfilter"))
                {
                    Console.WriteLine(exception.Message);
                    Console.WriteLine(exception.StackTrace);
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<Container> Create(string name, string functionId, int memorySize)
        {
            var createResponse = await Container.DockerClient.Containers
                .CreateContainerAsync(parameters: new CreateContainerParameters
                {
                    Image = "serverless-node",
                    HostConfig = new HostConfig
                    {
                        Memory = memorySize,
                        CPUShares = memorySize,
                        Binds = new List<string>
                    {
                        "C:/functions/" + functionId + ":C:/function:ro"
                    }
                    },
                    Volumes = new Dictionary<string, object>
                    {
                        { "C:/function", new { } }
                    }
                })
                .ConfigureAwait(continueOnCapturedContext: false);

            await Container.DockerClient.Containers
                .StartContainerAsync(
                    id: createResponse.ID,
                    parameters: new ContainerStartParameters { })
                .ConfigureAwait(continueOnCapturedContext: false);

            string startMessage = string.Empty;
            do
            {
                var logStream = await Container.DockerClient.Containers
                    .GetContainerLogsAsync(
                        id: createResponse.ID,
                        parameters: new ContainerLogsParameters
                        {
                            ShowStdout = true,
                            Tail = "1"
                        },
                        cancellationToken: new CancellationToken())
                    .ConfigureAwait(continueOnCapturedContext: false);

                startMessage = new StreamReader(stream: logStream).ReadToEnd();
            }
            while (!startMessage.Contains("started"));

            var inspectResponse = await Container.DockerClient.Containers
                .InspectContainerAsync(id: createResponse.ID)
                .ConfigureAwait(continueOnCapturedContext: false);

            return new Container(
                id: inspectResponse.ID,
                name: name,
                ipAddress: inspectResponse.NetworkSettings.Networks[ServerlessConfiguration.DockerNetworkName].IPAddress);
        }
    }
}
