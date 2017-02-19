using System;
using Newtonsoft.Json.Linq;

namespace Serverless.Worker.Models
{
    public class ExecutionRequest
    {
        public string ExecutionId { get; set; }

        public JToken Input { get; set; }
    }
}