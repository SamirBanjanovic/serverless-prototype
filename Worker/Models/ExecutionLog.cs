using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serverless.Worker.Models
{
    public class ExecutionLog
    {
        public string Name { get; set; }

        public long Duration { get; set; }
    }
}
