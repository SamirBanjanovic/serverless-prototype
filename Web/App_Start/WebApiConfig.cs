using System;
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
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

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
        }
    }
}
