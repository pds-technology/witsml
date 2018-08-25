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
using System.Configuration;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Security.OAuth;
using Owin;
using PDS.WITSMLstudio.Store.Security;

[assembly: OwinStartup(typeof(PDS.WITSMLstudio.Store.Configuration.SecurityConfig))]

namespace PDS.WITSMLstudio.Store.Configuration
{
    /// <summary>
    /// Configures JSON Web Token (JWT) authorization.
    /// </summary>
    public class SecurityConfig
    {
        /// <summary>
        /// Configures security for the specified application.
        /// </summary>
        /// <param name="app">The application.</param>
        public void Configuration(IAppBuilder app)
        {
            var config = GlobalConfiguration.Configuration;

            app.UseOAuthAuthorizationServer(GetServerOptions());
            app.UseCors(CorsOptions.AllowAll);
            app.UseWebApi(config);
        }

        private OAuthAuthorizationServerOptions GetServerOptions()
        {
            var settings = ConfigurationManager.AppSettings;
            var endpoint = settings["jwt.auth.path"];
            var audience = settings["jwt.audience"];
            var issuer = settings["jwt.issuer"];
            var secret = settings["jwt.secret"];

            return new OAuthAuthorizationServerOptions()
            {
                AllowInsecureHttp = true,
                TokenEndpointPath = new PathString(endpoint),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(30),
                AccessTokenFormat = new EtpJwtFormat(issuer, audience, secret),
                Provider = new EtpAuthorizationServerProvider(),
            };
        }
    }
}
