using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Serverless.Web
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;

            // Web API configuration and services
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));

            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "FunctionsCRUD",
                routeTemplate: "functions/{functionId}",
                defaults: new { controller = "Functions", functionId = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "Invoke",
                routeTemplate: "functions/{functionId}/invoke",
                defaults: new { controller = "Invoke" }
            );

            config.Routes.MapHttpRoute(
                name: "Respond",
                routeTemplate: "executions/{executionId}/respond",
                defaults: new { controller = "Respond" }
            );
        }
    }
}
