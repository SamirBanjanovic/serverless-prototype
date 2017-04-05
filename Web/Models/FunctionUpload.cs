using System;
using System.Threading.Tasks;
using Serverless.Common.Entities;
using Serverless.Web.Providers;

namespace Serverless.Web.Models
{
    public class FunctionUpload
    {
        public int MemorySize { get; set; }

        public string ZipFile { get; set; }

        public async Task<Function> ToFunction(string functionId = null)
        {
            functionId = functionId ?? Guid.NewGuid().ToString();
            var deploymentId = Guid.NewGuid().ToString();

            var blobUri = await BlobProvider
                .CreateDeploymentBlob(
                    deploymentId: deploymentId,
                    zipFile: this.ZipFile)
                .ConfigureAwait(continueOnCapturedContext: false);

            return new Function
            {
                PartitionKey = "functions",
                RowKey = functionId,
                ETag = "*",
                Id = functionId,
                BlobUri = blobUri,
                MemorySize = this.MemorySize
            };
        }
    }
}