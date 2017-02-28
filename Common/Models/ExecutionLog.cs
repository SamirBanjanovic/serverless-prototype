using System;

namespace Serverless.Common.Models
{
    public class ExecutionLog
    {
        public string Name { get; set; }

        public long Duration { get; set; }

        public ExecutionLog[] SubLogs { get; set; }
    }
}
