using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Serverless.Common.Configuration;

namespace Serverless.Web.Providers
{
    public static class BlobProvider
    {
        public static async Task<string> CreateDeploymentBlob(string deploymentId, string zipFile)
        {
            var deploymentBlob = await BlobProvider
                .GetDeploymentBlob(deploymentId: deploymentId)
                .ConfigureAwait(continueOnCapturedContext: false);

            var buffer = Convert.FromBase64String(zipFile);

            await deploymentBlob
                .UploadFromByteArrayAsync(
                    buffer: buffer,
                    index: 0,
                    count: buffer.Length)
                .ConfigureAwait(continueOnCapturedContext: false);

            var sasToken = deploymentBlob.GetSharedAccessSignature(policy: new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTimeOffset.MinValue,
                SharedAccessExpiryTime = DateTimeOffset.MaxValue
            });

            return deploymentBlob.Uri + sasToken;
        }

        public static async Task DeleteDeploymentBlob(string deploymentId)
        {
            var deploymentBlob = await BlobProvider
                .GetDeploymentBlob(deploymentId: deploymentId)
                .ConfigureAwait(continueOnCapturedContext: false);

            await deploymentBlob
                .DeleteIfExistsAsync()
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        private static async Task<CloudBlockBlob> GetDeploymentBlob(string deploymentId)
        {
            var blobClient = ServerlessConfiguration.StorageAccount.CreateCloudBlobClient();

            var deploymentsContainer = blobClient.GetContainerReference(containerName: "deployments");

            await deploymentsContainer
                .CreateIfNotExistsAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            return deploymentsContainer.GetBlockBlobReference(blobName: deploymentId);
        }
    }
}