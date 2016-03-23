using System.Net.Http.Formatting;
using System.Web.Http;
using Energistics.Common;
using Newtonsoft.Json;
using PDS.Framework.Web.Services;

namespace PDS.Witsml.Web
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Filters.Add(new UnhandledExceptionHandler());

            config.Formatters.JsonFormatter.MediaTypeMappings.Add(new QueryStringMapping("json", "true", "application/json"));

            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new EtpContractResolver();
#if DEBUG
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
#endif
        }
    }
}
