using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Serverless.Web.Models
{
    public class ExecutionResponse
    {
        public JToken Output { get; set; }
    }
}