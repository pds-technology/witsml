//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
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
using PDS.Witsml.Server.Security;

[assembly: OwinStartup(typeof(PDS.Witsml.Server.Configuration.SecurityConfig))]

namespace PDS.Witsml.Server.Configuration
{
    public class SecurityConfig
    {
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
