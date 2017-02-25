using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Serverless.Common.Models;
using Serverless.Web.Providers;
using Serverless.Web.Models;

namespace Serverless.Web.Entities
{
    public class Function : TableEntity
    {
        public string Id { get; set; }

        public string BlobUri { get; set; }

        public int MemorySize { get; set; }

        public FunctionModel ToModel()
        {
            return new FunctionModel
            {
                Id = this.Id,
                BlobUri = this.BlobUri,
                MemorySize = this.MemorySize
            };
        }

        public static async Task<Function> FromUpload(FunctionUpload upload, string functionId = null)
        {
            functionId = functionId ?? Guid.NewGuid().ToString();
            var deploymentId = Guid.NewGuid().ToString();

            var blobUri = await BlobProvider
                .CreateDeploymentBlob(
                    deploymentId: deploymentId,
                    zipFile: upload.ZipFile)
                .ConfigureAwait(continueOnCapturedContext: false);

            return new Function
            {
                PartitionKey = "functions",
                RowKey = functionId,
                ETag = "*",
                Id = functionId,
                BlobUri = blobUri,
                MemorySize = upload.MemorySize
            };
        }

        public static Function FromId(string functionId)
        {
            return new Function
            {
                PartitionKey = "functions",
                RowKey = functionId,
                ETag = "*",
                Id = functionId
            };
        }
    }
}