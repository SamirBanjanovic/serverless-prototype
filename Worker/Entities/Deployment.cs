using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Serverless.Worker.Managers;
using Serverless.Worker.Models;
using Serverless.Worker.Providers;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Serverless.Worker.Entities
{
    public class Deployment
    {
        public QueueClient DeploymentQueueClient { get; private set; }

        private ExecutionManager ExecutionManager { get; set; }

        public Function Function { get; private set; }

        private ConcurrentDictionary<string, Container> Containers { get; set; }

        private ConcurrentStack<string> ContainerStack { get; set; }

        private ConcurrentDictionary<Container, DeploymentWatchdog> Watchdogs { get; set; }

        private Task InitializationTask { get; set; }

        public Task ManagementTask { get; private set; }

        public Deployment(Function function, ExecutionManager executionManager, CancellationToken cancellationToken)
        {
            this.Function = function;

            this.DeploymentQueueClient = QueueClient.CreateFromConnectionString(
                connectionString: ConfigurationProvider.ServiceBusConnectionString,
                path: this.Function.DeploymentId);

            this.ExecutionManager = executionManager;

            this.Containers = new ConcurrentDictionary<string, Container>();
            this.ContainerStack = new ConcurrentStack<string>();
            this.Watchdogs = new ConcurrentDictionary<Container, DeploymentWatchdog>();

            this.InitializationTask = this.Initialize(cancellationToken: cancellationToken);
            this.ManagementTask = this.ManageContainers(cancellationToken: cancellationToken);
        }

        private async Task Initialize(CancellationToken cancellationToken)
        {
            var codeBlob = new CloudBlockBlob(blobAbsoluteUri: new Uri(this.Function.BlobUri));
            var codeDirectoryRoot = string.Format(
                format: ConfigurationProvider.CodeDirectoryTemplate,
                arg0: ConfigurationProvider.DriveName);
            var codeDirectory = Path.Combine(codeDirectoryRoot, this.Function.DeploymentId);
            var codePath = Path.Combine(codeDirectory, codeBlob.Name);

            if (!Directory.Exists(path: codeDirectory))
            {
                Directory.CreateDirectory(codeDirectory);

                await codeBlob
                    .DownloadToFileAsync(
                        path: codePath,
                        mode: FileMode.Create,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                ZipFile.ExtractToDirectory(
                    sourceArchiveFileName: codePath,
                    destinationDirectoryName: codeDirectory);
            }
        }

        private async Task ManageContainers(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var lastExecutionTime = await this
                    .RemoveExpiredContainers()
                    .ConfigureAwait(continueOnCapturedContext: false);

                await Task
                    .Delay(
                        delay: TimeSpan.FromMinutes(ConfigurationProvider.InstanceCacheMinutes) - (DateTime.UtcNow - lastExecutionTime),
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            await this.InitializationTask.ConfigureAwait(continueOnCapturedContext: false);

            await Task
                .WhenAll(this.Watchdogs.Values.Select(watchdog => watchdog.CancelAsync()))
                .ConfigureAwait(continueOnCapturedContext: false);

            await Task
                .WhenAll(this.Containers.Values.Select(container => container.Delete()))
                .ConfigureAwait(continueOnCapturedContext: false);

            var codeDirectoryRoot = string.Format(
                format: ConfigurationProvider.CodeDirectoryTemplate,
                arg0: ConfigurationProvider.DriveName);
            var codeDirectory = Path.Combine(codeDirectoryRoot, this.Function.DeploymentId);

            Directory.Delete(
                path: codeDirectory,
                recursive: true);
        }

        private async Task<DateTime> RemoveExpiredContainers()
        {
            var lastExecutionTime = DateTime.UtcNow;
            foreach (var container in this.Containers.Values)
            {
                if (DateTime.UtcNow - container.LastExecutionTime >= TimeSpan.FromMinutes(ConfigurationProvider.InstanceCacheMinutes))
                {
                    await this.Watchdogs[container]
                        .CancelAsync()
                        .ConfigureAwait(continueOnCapturedContext: false);

                    lock (this.ExecutionManager)
                    {
                        this.ExecutionManager.AvailableMemory += this.Function.MemorySize;
                    }

                    Container removedContainer;
                    if (this.Containers.TryRemove(container.Id, out removedContainer))
                    {
                        // Keep an eye on this, it looks like an Engine API bug on remove.
                        try
                        {
                            await removedContainer
                                .Delete()
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
                }
                else if (container.LastExecutionTime < lastExecutionTime)
                {
                    lastExecutionTime = container.LastExecutionTime;
                }
            }

            return lastExecutionTime;
        }

        public async Task<ExecutionResponse> Execute(ExecutionRequest request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            await this.InitializationTask.ConfigureAwait(continueOnCapturedContext: false);

            Container container = null;
            while (container == null && !cancellationToken.IsCancellationRequested)
            {
                string containerId;
                if (!this.ContainerStack.TryPop(result: out containerId) ||
                    !this.Containers.TryRemove(key: containerId, value: out container))
                {
                    container = await Container
                        .Create(
                            deploymentId: this.Function.DeploymentId,
                            memorySize: this.Function.MemorySize,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false);
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var response = await container
                .Execute(
                    request: request,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            this.Containers[container.Id] = container;
            this.ContainerStack.Push(item: container.Id);

            this.Watchdogs.GetOrAdd(
                key: container,
                valueFactory: key =>
                {
                    lock (this.ExecutionManager)
                    {
                        this.ExecutionManager.AvailableMemory -= this.Function.MemorySize;
                    }

                    return new DeploymentWatchdog(deployment: this);
                });

            return response;
        }

        public async Task Delete()
        {
            foreach (var container in this.Containers.Values)
            {
                container.LastExecutionTime = DateTime.MinValue;
            }

            await this
                .RemoveExpiredContainers()
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
