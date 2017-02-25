using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Serverless.Common.Configuration;
using Serverless.Common.Models;

namespace Serverless.Worker.Providers
{
    public static class CodeProvider
    {
        private static readonly Dictionary<string, bool> Functions = new Dictionary<string, bool>();

        public static async Task DownloadIfNotExists(string functionId, string blobUri)
        {
            lock (CodeProvider.Functions)
            {
                if (CodeProvider.Functions.ContainsKey(functionId))
                {
                    return;
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
            }
        }
    }
}