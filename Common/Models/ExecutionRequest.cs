using System;
using Newtonsoft.Json.Linq;

namespace Serverless.Common.Models
{
    public class ExecutionRequest
    {
        public FunctionModel Function { get; set; }

        public JToken Input { get; set; }
    }
}