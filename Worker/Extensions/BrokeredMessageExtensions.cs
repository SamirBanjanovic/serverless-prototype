using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace Serverless.Worker.Extensions
{
    public static class BrokeredMessageExtensions
    {
        public static async Task<T> ParseBody<T>(this BrokeredMessage message)
        {
            var reader = new StreamReader(
                stream: message.GetBody<Stream>(),
                encoding: Encoding.UTF8);

            var json = await reader
                .ReadToEndAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            return JsonConvert.DeserializeObject<T>(value: json);
        }
    }
}
