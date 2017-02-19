using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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

        public FunctionModel ToModel()
        {
            return new FunctionModel
            {
                Id = this.Id,
                DeploymentId = this.DeploymentId,
                DisplayName = this.DisplayName,
                BlobUri = this.BlobUri,
                MemorySize = this.MemorySize
            };
        }

        public static Function FromModel(FunctionModel model)
        {
            return new Function
            {
                PartitionKey = "functions",
                RowKey = model.Id,
                ETag = "*",
                Id = model.Id,
                DeploymentId = model.DeploymentId,
                DisplayName = model.DisplayName,
                BlobUri = model.BlobUri,
                MemorySize = model.MemorySize
            };
        }
    }
}