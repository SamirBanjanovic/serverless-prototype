using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Serverless.Common.Async;
using Serverless.Common.Configuration;

namespace Serverless.Worker.Providers
{
    public static class CodeProvider
    {
        private static readonly Dictionary<string, bool> Functions = new Dictionary<string, bool>();

        private static readonly AsyncLock Lock = new AsyncLock();

        public static async Task<bool> DownloadIfNotExists(string functionId, string blobUri)
        {
            using (await CodeProvider.Lock.WaitAsync().ConfigureAwait(continueOnCapturedContext: false))
            {
                if (CodeProvider.Functions.ContainsKey(functionId))
                {
                    return false;
                }

                CodeProvider.Functions[functionId] = true;
            }

            var codeBlob = new CloudBlockBlob(blobAbsoluteUri: new Uri(blobUri));
            var codeDirectoryRoot = string.Format(
                format: ServerlessConfiguration.CodeDirectoryTemplate,
                arg0: ServerlessConfiguration.DriveName);
            var codeDirectory = Path.Combine(codeDirectoryRoot, functionId);
            var codePath = Path.Combine(codeDirectory, codeBlob.Name);

            if (!Directory.Exists(path: codeDirectory))
            {
                Directory.CreateDirectory(codeDirectory);

                await codeBlob
                    .DownloadToFileAsync(
                        path: codePath,
                        mode: FileMode.Create)
                    .ConfigureAwait(continueOnCapturedContext: false);

                ZipFile.ExtractToDirectory(
                    sourceArchiveFileName: codePath,
                    destinationDirectoryName: codeDirectory);

                return true;
            }

            return false;
        }
    }
}