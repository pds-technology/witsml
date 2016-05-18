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

using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Security;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;

namespace PDS.Witsml.Server.Security
{
    public class EtpAuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        private const object Null = null;

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            if (context.Request.User != null && context.Request.User.Identity != null &&
                context.Request.User.Identity.IsAuthenticated)
            {
                var user = Membership.GetUser(context.Request.User.Identity.Name, false);
                if (user != null)
                {
                    context.Validated(user.Email);
                }
            }

            return Task.FromResult(Null);
        }

        public override Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] { "*" });

            var identity = new ClaimsIdentity("JWT");

            identity.AddClaim(new Claim("sub", context.Request.User.Identity.Name));
            identity.AddClaim(new Claim("email", context.ClientId));

            var ticket = new AuthenticationTicket(identity, null);
            context.Validated(ticket);

            return Task.FromResult(Null);
        }
    }
}
