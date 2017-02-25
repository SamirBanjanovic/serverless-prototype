using System;

namespace Serverless.Worker.Models
{
    public enum ContainerExecutionState
    {
        Creating,

        Ready,

        Busy,

        Deleted
    }
}