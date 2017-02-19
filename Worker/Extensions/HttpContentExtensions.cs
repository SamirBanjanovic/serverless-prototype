using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Serverless.Worker.Extensions
{
    public static class HttpContentExtensions
    {
        public static async Task<T> FromJson<T>(this HttpContent content)
        {
            var stringContent = await content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            return JsonConvert.DeserializeObject<T>(value: stringContent);
        }
    }
}
