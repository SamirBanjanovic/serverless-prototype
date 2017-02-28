using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serverless.Common.Async;
using Serverless.Common.Configuration;
using Serverless.Common.Models;
using Serverless.Common.Providers;

namespace Serverless.Worker.Providers
{
    public static class MemoryProvider
    {
        private static readonly Dictionary<string, int> Reservations = new Dictionary<string, int>();

        private static readonly AsyncLock Lock = new AsyncLock();

        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Task, bool>> SendTasks = new ConcurrentDictionary<string, ConcurrentDictionary<Task, bool>>();

        private static int AvailableMemory = ServerlessConfiguration.AvailableMemory;


        public static async Task<bool> ReservationExists(string containerName)
        {
            using (await MemoryProvider.Lock.WaitAsync().ConfigureAwait(continueOnCapturedContext: false))
            {
                return MemoryProvider.Reservations.ContainsKey(containerName);
            }
        }

        public static async Task<string> TryReserve(int memorySize)
        {
            using (await MemoryProvider.Lock.WaitAsync().ConfigureAwait(continueOnCapturedContext: false))
            {
                if (MemoryProvider.AvailableMemory >= memorySize)
                {
                    MemoryProvider.AvailableMemory -= memorySize;

                    var containerName = Guid.NewGuid().ToString();
                    MemoryProvider.Reservations[containerName] = memorySize;
                    MemoryProvider.SendTasks[containerName] = new ConcurrentDictionary<Task, bool>();
                    return containerName;
                }
                else
                {
                    return null;
                }
            }
        }

        public static async Task AdjustReservation(string containerName, int memorySize)
        {
            using (await MemoryProvider.Lock.WaitAsync().ConfigureAwait(continueOnCapturedContext: false))
            {
                MemoryProvider.AvailableMemory += MemoryProvider.Reservations[containerName] - memorySize;
                MemoryProvider.Reservations[containerName] = memorySize;
            }

            await MemoryProvider
                .SendReservations()
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task ReclaimReservation(string containerName)
        {
            using (await MemoryProvider.Lock.WaitAsync().ConfigureAwait(continueOnCapturedContext: false))
            {
                MemoryProvider.AvailableMemory += MemoryProvider.Reservations[containerName];
                MemoryProvider.Reservations.Remove(containerName);
                ConcurrentDictionary<Task, bool> sendTasks;
                MemoryProvider.SendTasks.TryRemove(containerName, out sendTasks);
            }

            await MemoryProvider
                .SendReservations()
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task SendReservations()
        {
            while (true)
            {
                var containerName = await MemoryProvider
                    .TryReserve(memorySize: ServerlessConfiguration.MaximumFunctionMemory)
                    .ConfigureAwait(continueOnCapturedContext: false);

                if (containerName == null)
                {
                    return;
                }

                MemoryProvider.SendReservation(
                    queueName: ServerlessConfiguration.ExecutionQueueName,
                    containerName: containerName);
            }
        }

        public static void SendReservation(string queueName, string containerName)
        {
            var sendTask = QueueProvider.AddMessage(
                queueName: queueName,
                message: new ExecutionAvailability
                {
                    CallbackURI = string.Format(
                        format: ServerlessConfiguration.ExecutionTemplate,
                        arg0: containerName)
                });

            MemoryProvider.SendTasks[containerName][sendTask] = true;

            foreach (var task in MemoryProvider.SendTasks[containerName].Keys.Where(task => task.IsCompleted))
            {
                bool boolean;
                MemoryProvider.SendTasks[containerName].TryRemove(task, out boolean);
            }
        }
    }
}