using System;

namespace Serverless.Web.Models
{
    public class FunctionResponseModel
    {
        public string Id { get; set; }

        public string DeploymentId { get; set; }

        public string DisplayName { get; set; }

        public int MemorySize { get; set; }

        public string BlobUri { get; set; }
    }
}