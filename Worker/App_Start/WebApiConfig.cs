using System;
using System.Net.Http.Headers;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Serverless.Worker
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));

            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "Containers",
                routeTemplate: "containers/{containerName}",
                defaults: new { controller = "Containers" }
            );

            config.Routes.MapHttpRoute(
                name: "Execute",
                routeTemplate: "containers/{containerName}/execute",
                defaults: new { controller = "Execute" }
            );
        }
    }
}
