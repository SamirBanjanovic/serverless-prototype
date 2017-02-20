using System;

namespace Serverless.Web.Models
{
    public class FunctionRequestModel
    {
        public string DisplayName { get; set; }

        public int MemorySize { get; set; }

        public string ZipFile { get; set; }
    }
}