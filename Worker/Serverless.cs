using System;
using System.ServiceProcess;
using Serverless.Common.Configuration;
using Serverless.Worker.Managers;

namespace Serverless.Worker
{
    public static class Serverless
    {
        public static readonly ExecutionManager ExecutionManager = new ExecutionManager();

        public class Service : ServiceBase
        {
            public Service()
            {
                this.ServiceName = ServerlessConfiguration.ServiceName;
            }

            protected override void OnStart(string[] args)
            {
                Serverless.Start(args: args);
            }

            protected override void OnStop()
            {
                Serverless.Stop();
            }
        }

        public static void Main(string[] args)
        {
            if (!Environment.UserInteractive)
            {
                using (var service = new Service())
                {
                    ServiceBase.Run(service: service);
                }
            }
            else
            {
                Serverless.Start(args: args);

                Console.WriteLine(value: "Press any key to stop...");
                Console.ReadKey(intercept: true);

                Serverless.Stop();
            }
        }

        public static void Start(string[] args)
        {
            Serverless.ExecutionManager.Start();
        }

        public static void Stop()
        {
            Serverless.ExecutionManager.Stop();
        }
    }
}
