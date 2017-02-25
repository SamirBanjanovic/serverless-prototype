using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serverless.Common.Models
{
    public class FunctionModel
    {
        public string Id { get; set; }

        public string BlobUri { get; set; }

        public int MemorySize { get; set; }
    }
}
