using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

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
            var blobClient = ConfigurationProvider.StorageAccount.CreateCloudBlobClient();

            var deploymentsContainer = blobClient.GetContainerReference(containerName: "deployments");

            await deploymentsContainer
                .CreateIfNotExistsAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            return deploymentsContainer.GetBlockBlobReference(blobName: deploymentId);
        }
    }
}