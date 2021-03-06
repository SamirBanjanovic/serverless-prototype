﻿using System;
using System.Net;
using System.Web.Http;

namespace Serverless.Web
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
