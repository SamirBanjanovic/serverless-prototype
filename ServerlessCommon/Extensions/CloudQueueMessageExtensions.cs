using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace Serverless.Common.Extensions
{
    public static class CloudQueueMessageExtensions
    {
        public static T FromJson<T>(this CloudQueueMessage message) where T : class
        {
            if (message == null)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<T>(value: message.AsString);
        }
    }
}
