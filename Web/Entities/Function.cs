using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Serverless.Web.Providers;
using Serverless.Web.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace Serverless.Web.Entities
{
    public class Function : TableEntity
    {
        public string Id { get; set; }

        public string DeploymentId { get; set; }

        public string DisplayName { get; set; }

        public string BlobUri { get; set; }

        public int MemorySize { get; set; }

        public FunctionResponseModel ToResponseModel()
        {
            return new FunctionResponseModel
            {
                Id = this.Id,
                DeploymentId = this.DeploymentId,
                DisplayName = this.DisplayName,
                BlobUri = this.BlobUri,
                MemorySize = this.MemorySize
            };
        }

        public static async Task<Function> FromRequestModel(FunctionRequestModel model, string functionId = null)
        {
            functionId = functionId ?? Guid.NewGuid().ToString();
            var deploymentId = Guid.NewGuid().ToString();

            var blobUri = await BlobProvider
                .CreateDeploymentBlob(
                    deploymentId: deploymentId,
                    zipFile: model.ZipFile)
                .ConfigureAwait(continueOnCapturedContext: false);

            return new Function
            {
                PartitionKey = "functions",
                RowKey = functionId,
                ETag = "*",
                Id = functionId,
                DeploymentId = deploymentId,
                DisplayName = model.DisplayName,
                BlobUri = blobUri,
                MemorySize = model.MemorySize
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