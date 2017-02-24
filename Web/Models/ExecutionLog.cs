using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Serverless.Web.Models
{
    public class ExecutionLog
    {
        public string Name { get; set; }

        public long Duration { get; set; }

        public ExecutionLog[] SubLogs { get; set; }
    }
}