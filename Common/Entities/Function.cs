using System;
using Microsoft.WindowsAzure.Storage.Table;
using Serverless.Common.Models;

namespace Serverless.Common.Entities
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