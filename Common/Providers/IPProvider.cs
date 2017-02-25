using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Serverless.Common.Providers
{
    public static class IPProvider
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        
        // Please don't judge me for this...
        // TODO: Find better way of getting current IP in Azure.
        public static async Task<string> GetPublicIP()
        {
            var ipAddress = await IPProvider.HttpClient
                .GetStringAsync(requestUri: "http://www.icanhazip.com/")
                .ConfigureAwait(continueOnCapturedContext: false);

            return ipAddress.Trim();
        }
    }
}
