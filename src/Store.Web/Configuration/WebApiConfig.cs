//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Net.Http.Formatting;
using System.Web.Http;
using Energistics.Etp.Common;
using PDS.WITSMLstudio.Framework.Web.Services;

namespace PDS.WITSMLstudio.Store.Configuration
{
    /// <summary>
    /// Configures Web API application settings.
    /// </summary>
    public static class WebApiConfig
    {
        private const string ContentType = "application/json";

        /// <summary>
        /// Registers configuration settings for the application.
        /// </summary>
        /// <param name="config">The configuration.</param>
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

            config.Formatters.JsonFormatter.MediaTypeMappings.Add(new RequestHeaderMapping("Accept", "text/html", StringComparison.InvariantCultureIgnoreCase, true, ContentType));
            config.Formatters.JsonFormatter.MediaTypeMappings.Add(new QueryStringMapping("json", "true", ContentType));

            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new EtpContractResolver();
#if DEBUG
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
#endif
        }
    }
}
