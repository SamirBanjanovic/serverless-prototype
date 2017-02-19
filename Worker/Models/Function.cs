using System;

namespace Serverless.Worker.Models
{
    public class Function
    {
        public string Id { get; set; }

        public string DeploymentId { get; set; }

        public string BlobUri { get; set; }

        public int MemorySize { get; set; }
    }
}