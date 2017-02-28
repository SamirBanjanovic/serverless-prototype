using System;

namespace Serverless.Web.Models
{
    public class FunctionUpload
    {
        public int MemorySize { get; set; }

        public string ZipFile { get; set; }
    }
}