using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Serverless.Common.Extensions
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
