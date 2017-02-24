using System;
using Newtonsoft.Json.Linq;

namespace Serverless.Common.Models
{
    public class ExecutionResponse
    {
        public JToken Output { get; set; }

        public ExecutionLog Logs { get; set; }
    }
}