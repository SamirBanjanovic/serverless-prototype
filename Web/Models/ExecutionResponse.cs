using System;
using Newtonsoft.Json.Linq;

namespace Serverless.Web.Models
{
    public class ExecutionResponse
    {
        public JToken Output { get; set; }

        public ExecutionLog Logs { get; set; }
    }
}