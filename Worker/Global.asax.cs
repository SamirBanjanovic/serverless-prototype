using System;
using System.Net;
using System.Web.Http;
using Serverless.Worker.Providers;

namespace Serverless.Worker
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;

            GlobalConfiguration.Configure(WebApiConfig.Register);

            MemoryProvider.SendReservations();
        }
    }
}
