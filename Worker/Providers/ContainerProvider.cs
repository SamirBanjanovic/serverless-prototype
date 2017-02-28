using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serverless.Common.Async;
using Serverless.Common.Configuration;
using Serverless.Common.Providers;
using Serverless.Worker.Entities;
using Serverless.Worker.Models;

namespace Serverless.Worker.Providers
{
    public static class ContainerProvider
    {
        private static readonly ConcurrentDictionary<string, Container> Containers = new ConcurrentDictionary<string, Container>();

        private static readonly Dictionary<string, ContainerExecutionState> ExecutionStates = new Dictionary<string, ContainerExecutionState>();

        private static readonly ConcurrentDictionary<string, Task> ExpirationTasks = new ConcurrentDictionary<string, Task>();

        private static readonly ConcurrentDictionary<string, Task> QueueWatchTasks = new ConcurrentDictionary<string, Task>();

        private static readonly Dictionary<string, TaskCompletionSource<bool>> DeletionTasks = new Dictionary<string, TaskCompletionSource<bool>>();

        private static readonly AsyncLock Lock = new AsyncLock();

        public static async Task<bool> CreateContainerIfNotExists(string containerName, string functionId, int memorySize)
        {
            using (await ContainerProvider.Lock.WaitAsync().ConfigureAwait(continueOnCapturedContext: false))
            {
                if (ContainerProvider.ExecutionStates.ContainsKey(containerName))
                {
                    return false;
                }

                ContainerProvider.ExecutionStates[containerName] = ContainerExecutionState.Creating;
            }

            var container = await Container
                .Create(
                    name: containerName,
                    functionId: functionId,
                    memorySize: memorySize)
                .ConfigureAwait(continueOnCapturedContext: false);

            await MemoryProvider
                .AdjustReservation(
                    containerName: containerName,
                    memorySize: memorySize)
                .ConfigureAwait(continueOnCapturedContext: false);

            ContainerProvider.Containers[containerName] = container;

            ContainerProvider.ExpirationTasks[containerName] = ContainerProvider.ExpireContainer(containerName: containerName);
            ContainerProvider.QueueWatchTasks[containerName] = ContainerProvider.WatchContainerQueue(
                queueName: functionId,
                containerName: containerName);

            using (await ContainerProvider.Lock.WaitAsync().ConfigureAwait(continueOnCapturedContext: false))
            {
                if (ContainerProvider.DeletionTasks.ContainsKey(containerName))
                {
                    ContainerProvider.DeletionTasks[containerName].SetResult(result: true);
                    ContainerProvider.ExecutionStates[containerName] = ContainerExecutionState.Deleted;
                }
                else
                {
                    ContainerProvider.ExecutionStates[containerName] = ContainerExecutionState.Ready;
                }
            }

            return true;
        }

        public static async Task DeleteContainerIfExists(string containerName)
        {
            var tcs = new TaskCompletionSource<bool>();

            using (await ContainerProvider.Lock.WaitAsync().ConfigureAwait(continueOnCapturedContext: false))
            {
                if (ContainerProvider.ExecutionStates[containerName] == ContainerExecutionState.Ready)
                {
                    ContainerProvider.ExecutionStates[containerName] = ContainerExecutionState.Deleted;
                    tcs.SetResult(result: true);
                }
                else if (ContainerProvider.ExecutionStates[containerName] == ContainerExecutionState.Deleted)
                {
                    return;
                }
                else
                {
                    ContainerProvider.DeletionTasks[containerName] = tcs;
                }
            }

            await tcs.Task.ConfigureAwait(continueOnCapturedContext: false);

            await ContainerProvider
                .DeleteContainer(containerName: containerName)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task<Container> TryReserve(string containerName)
        {
            using (await ContainerProvider.Lock.WaitAsync().ConfigureAwait(continueOnCapturedContext: false))
            {
                if (ContainerProvider.ExecutionStates[containerName] == ContainerExecutionState.Ready)
                {
                    ContainerProvider.ExecutionStates[containerName] = ContainerExecutionState.Busy;
                    return ContainerProvider.Containers[containerName];
                }
                else
                {
                    return null;
                }
            }
        }

        public static async Task ReleaseContainer(string containerName)
        {
            using (await ContainerProvider.Lock.WaitAsync().ConfigureAwait(continueOnCapturedContext: false))
            {
                if (ContainerProvider.DeletionTasks.ContainsKey(containerName))
                {
                    ContainerProvider.DeletionTasks[containerName].SetResult(result: true);
                    ContainerProvider.ExecutionStates[containerName] = ContainerExecutionState.Deleted;
                }
                else
                {
                    ContainerProvider.ExecutionStates[containerName] = ContainerExecutionState.Ready;
                }
            }
        }

        private static async Task ExpireContainer(string containerName)
        {
            while (true)
            {
                var timeSinceLastExecution = DateTime.UtcNow - ContainerProvider.Containers[containerName].LastExecutionTime;
                var expirationTimeSpan = TimeSpan.FromMinutes(ServerlessConfiguration.ContainerLifeInMinutes);

                if (timeSinceLastExecution >= expirationTimeSpan)
                {
                    var delete = false;
                    using (await ContainerProvider.Lock.WaitAsync().ConfigureAwait(continueOnCapturedContext: false))
                    {
                        if (ContainerProvider.ExecutionStates[containerName] == ContainerExecutionState.Ready)
                        {
                            ContainerProvider.ExecutionStates[containerName] = ContainerExecutionState.Deleted;
                            delete = true;
                        }
                        else if (ContainerProvider.ExecutionStates[containerName] == ContainerExecutionState.Deleted)
                        {
                            return;
                        }
                    }

                    if (delete)
                    {
                        await ContainerProvider
                            .DeleteContainer(containerName: containerName)
                            .ConfigureAwait(continueOnCapturedContext: false);

                        return;
                    }
                }

                await Task
                    .Delay(delay: expirationTimeSpan - timeSinceLastExecution)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private static async Task WatchContainerQueue(string queueName, string containerName)
        {
            while (true)
            {
                var exists = await QueueProvider
                    .QueueExists(queueName: queueName)
                    .ConfigureAwait(continueOnCapturedContext: false);

                if (!exists)
                {
                    await ContainerProvider
                        .DeleteContainerIfExists(containerName: containerName)
                        .ConfigureAwait(continueOnCapturedContext: false);
                }

                await Task
                    .Delay(delay: TimeSpan.FromSeconds(5))
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private static async Task DeleteContainer(string containerName)
        {
            await ContainerProvider.Containers[containerName]
                .Delete()
                .ConfigureAwait(continueOnCapturedContext: false);

            Container expiredContainer;
            ContainerProvider.Containers.TryRemove(containerName, out expiredContainer);

            Task expirationTask;
            ContainerProvider.ExpirationTasks.TryRemove(containerName, out expirationTask);

            Task queueWatchTask;
            ContainerProvider.QueueWatchTasks.TryRemove(containerName, out queueWatchTask);

            if (ContainerProvider.DeletionTasks.ContainsKey(containerName))
            {
                ContainerProvider.DeletionTasks.Remove(containerName);
            }

            await MemoryProvider
                .ReclaimReservation(containerName: containerName)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}