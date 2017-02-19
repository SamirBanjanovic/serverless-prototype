using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Serverless.Web.Providers
{
    public static class CodeProvider
    {
        public static async Task CopyCode(string functionId, string codeUri)
        {
            var functionBlob = await CodeProvider
                .GetFunctionBlob(functionId: functionId)
                .ConfigureAwait(continueOnCapturedContext: false);

            var codeStream = await new HttpClient()
                .GetStreamAsync(requestUri: codeUri)
                .ConfigureAwait(continueOnCapturedContext: false);

            await functionBlob
                .UploadFromStreamAsync(source: codeStream)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task DeleteCode(string functionId)
        {
            var functionBlob = await CodeProvider
                .GetFunctionBlob(functionId: functionId)
                .ConfigureAwait(continueOnCapturedContext: false);

            await functionBlob
                .DeleteIfExistsAsync()
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        private static async Task<CloudBlockBlob> GetFunctionBlob(string functionId)
        {
            var blobClient = ConfigurationProvider.StorageAccount.CreateCloudBlobClient();

            var functionsContainer = blobClient.GetContainerReference(containerName: "functions");

            await functionsContainer
                .CreateIfNotExistsAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            return functionsContainer.GetBlockBlobReference(blobName: functionId);
        }
    }
}