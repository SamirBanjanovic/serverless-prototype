using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Serverless.Worker.Models
{
    public class ExecutionResponse
    {
        public JToken Output { get; set; }

        public ExecutionLog Logs { get; set; }
    }
}