using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serverless.Common.Configuration;
using Serverless.Common.Models;
using Serverless.Common.Providers;

namespace Serverless.Worker.Providers
{
    public static class MemoryProvider
    {
        private static readonly Dictionary<string, int> Reservations = new Dictionary<string, int>();

        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Task, bool>> SendTasks = new ConcurrentDictionary<string, ConcurrentDictionary<Task, bool>>();

        private static int AvailableMemory = ServerlessConfiguration.AvailableMemory;

        public static bool ReservationExists(string containerName)
        {
            lock (MemoryProvider.Reservations)
            {
                return MemoryProvider.Reservations.ContainsKey(containerName);
            }
        }

        public static bool TryReserve(int memorySize, out string containerName)
        {
            lock (MemoryProvider.Reservations)
            {
                if (MemoryProvider.AvailableMemory >= memorySize)
                {
                    MemoryProvider.AvailableMemory -= memorySize;

                    containerName = Guid.NewGuid().ToString();
                    MemoryProvider.Reservations[containerName] = memorySize;
                    MemoryProvider.SendTasks[containerName] = new ConcurrentDictionary<Task, bool>();
                    return true;
                }
                else
                {
                    containerName = null;
                    return false;
                }
            }
        }

        public static void AdjustReservation(string containerName, int memorySize)
        {
            lock (MemoryProvider.Reservations)
            {
                MemoryProvider.AvailableMemory += MemoryProvider.Reservations[containerName] - memorySize;
                MemoryProvider.Reservations[containerName] = memorySize;
            }

            MemoryProvider.SendReservations();
        }

        public static void ReclaimReservation(string containerName)
        {
            lock (MemoryProvider.Reservations)
            {
                MemoryProvider.AvailableMemory += MemoryProvider.Reservations[containerName];
                MemoryProvider.Reservations.Remove(containerName);
                ConcurrentDictionary<Task, bool> sendTasks;
                MemoryProvider.SendTasks.TryRemove(containerName, out sendTasks);
            }

            MemoryProvider.SendReservations();
        }

        public static void SendReservations()
        {
            string containerName;
            while (MemoryProvider.TryReserve(ServerlessConfiguration.MaximumFunctionMemory, out containerName))
            {
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
                        arg0: ServerlessConfiguration.IPAddress,
                        arg1: containerName)
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