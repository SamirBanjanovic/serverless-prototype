using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Serverless.Web.Models
{
    public class FunctionModel
    {
        public string Id { get; set; }

        public string DeploymentId { get; set; }

        public string DisplayName { get; set; }

        public string BlobUri { get; set; }

        public int MemorySize { get; set; }

        public FunctionRuntime Runtime { get; set; }
    }
}