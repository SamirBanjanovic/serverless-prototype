using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serverless.Common.Configuration;
using Serverless.Worker.Entities;
using Serverless.Worker.Models;

namespace Serverless.Worker.Providers
{
    public static class ContainerProvider
    {
        private static readonly ConcurrentDictionary<string, Container> Containers = new ConcurrentDictionary<string, Container>();

        private static readonly Dictionary<string, ContainerExecutionState> ExecutionStates = new Dictionary<string, ContainerExecutionState>();

        private static readonly ConcurrentDictionary<string, Task> MonitorTasks = new ConcurrentDictionary<string, Task>();

        private static readonly Dictionary<string, TaskCompletionSource<bool>> DeletionTasks = new Dictionary<string, TaskCompletionSource<bool>>();

        public static async Task<bool> CreateContainerIfNotExists(string containerName, string functionId, int memorySize)
        {
            lock (ContainerProvider.ExecutionStates)
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

            MemoryProvider.AdjustReservation(
                containerName: containerName,
                memorySize: memorySize);

            ContainerProvider.Containers[containerName] = container;

            ContainerProvider.MonitorTasks[containerName] = ContainerProvider.MonitorContainer(containerName: containerName);

            lock (ContainerProvider.ExecutionStates)
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

            lock (ContainerProvider.ExecutionStates)
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

        public static bool TryReserve(string containerName, out Container container)
        {
            var success = false;
            lock (ContainerProvider.ExecutionStates)
            {
                if (ContainerProvider.ExecutionStates[containerName] == ContainerExecutionState.Ready)
                {
                    ContainerProvider.ExecutionStates[containerName] = ContainerExecutionState.Busy;
                    success = true;
                }
            }

            if (success)
            {
                container = ContainerProvider.Containers[containerName];
            }
            else
            {
                container = null;
            }

            return success;
        }

        public static void ReleaseContainer(string containerName)
        {
            lock (ContainerProvider.ExecutionStates)
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

        private static async Task MonitorContainer(string containerName)
        {
            while (true)
            {
                var timeSinceLastExecution = DateTime.UtcNow - ContainerProvider.Containers[containerName].LastExecutionTime;
                var expirationTimeSpan = TimeSpan.FromMinutes(ServerlessConfiguration.ContainerLifeInMinutes);

                if (timeSinceLastExecution >= expirationTimeSpan)
                {
                    var delete = false;
                    lock (ContainerProvider.ExecutionStates)
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

        private static async Task DeleteContainer(string containerName)
        {
            await ContainerProvider.Containers[containerName]
                .Delete()
                .ConfigureAwait(continueOnCapturedContext: false);

            Container expiredContainer;
            ContainerProvider.Containers.TryRemove(containerName, out expiredContainer);

            Task thisMonitor;
            ContainerProvider.MonitorTasks.TryRemove(containerName, out thisMonitor);

            if (ContainerProvider.DeletionTasks.ContainsKey(containerName))
            {
                ContainerProvider.DeletionTasks.Remove(containerName);
            }

            MemoryProvider.ReclaimReservation(containerName: containerName);
        }
    }
}