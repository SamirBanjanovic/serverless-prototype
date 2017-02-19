using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace Serverless.Web.Extensions
{
    public static class QueueClientExtensions
    {
        public static Task SendAsync<T>(this QueueClient queueClient, T message)
        {
            var json = JsonConvert.SerializeObject(value: message);
            var stream = new MemoryStream(buffer: Encoding.UTF8.GetBytes(json));
            var brokeredMessage = new BrokeredMessage(messageBodyStream: stream);
            return queueClient.SendAsync(message: brokeredMessage);
        }
    }
}