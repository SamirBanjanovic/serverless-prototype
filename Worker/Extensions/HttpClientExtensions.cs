using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Serverless.Worker.Extensions
{
    public static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> PostAsync<T>(this HttpClient httpClient, string requestUri, T content, CancellationToken cancellationToken)
        {
            return httpClient.PostAsync<T>(
                requestUri: new Uri(requestUri),
                content: content,
                cancellationToken: cancellationToken);
        }

        public static async Task<HttpResponseMessage> PostAsync<T>(this HttpClient httpClient, Uri requestUri, T content, CancellationToken cancellationToken)
        {
            var stringContent = new StringContent(
                content: JsonConvert.SerializeObject(value: content),
                encoding: Encoding.UTF8,
                mediaType: "application/json");

            return await httpClient
                .PostAsync(
                    requestUri: requestUri,
                    content: stringContent,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
