//----------------------------------------------------------------------- 
// PDS.Framework, 2016.1
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
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Threading;
using System.Web;

namespace PDS.Framework.Web.Security
{
    public class CustomAuthenticationModule : IHttpModule
    {
        private const string AuthorizationHeaderKey = "Authorization";
        private const string AuthenticateHeaderKey = "WWW-Authenticate";
        private const string AuthenticationModeBasic = "Basic";
        private const string AuthenticationModeBearer = "Bearer";

        public void Init(HttpApplication context)
        {
            // Register event handlers
            context.AuthenticateRequest += OnApplicationAuthenticateRequest;
            context.EndRequest += OnApplicationEndRequest;
        }

        protected virtual void OnApplicationAuthenticateRequest(object sender, EventArgs e)
        {
            var request = HttpContext.Current.Request;
            var authHeader = request.QueryString[AuthorizationHeaderKey] ?? request.Headers[AuthorizationHeaderKey];

            if (authHeader == null)
            {
                DenyAccess();
            }

            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
            VerifyAuthenticationHeader(authHeaderVal);
        }

        // If the request was unauthorized, add the WWW-Authenticate header to the response.
        protected virtual void OnApplicationEndRequest(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            var response = context.Response;

            if (response.StatusCode == 401)
            {
                var realm = context.Request.Url.Host;
                response.Headers.Add(AuthenticateHeaderKey, string.Format("{0} realm=\"{1}\"", AuthenticationModeBasic, realm));
            }
        }

        protected virtual void DenyAccess()
        {
            var context = HttpContext.Current;
            context.Response.StatusCode = 401;
            context.Response.End();
        }

        protected virtual void SetPrincipal(IPrincipal principal)
        {
            Thread.CurrentPrincipal = principal;

            if (HttpContext.Current != null)
            {
                HttpContext.Current.User = principal;
            }
        }

        protected virtual void VerifyAuthenticationHeader(AuthenticationHeaderValue authHeaderVal)
        {
            if (authHeaderVal.Parameter == null)
            {
                DenyAccess();
            }

            // RFC 2617 sec 1.2, "scheme" name is case-insensitive
            if (authHeaderVal.Scheme.Equals(AuthenticationModeBasic, StringComparison.InvariantCultureIgnoreCase))
            {
                VerifyBasicAuthentication(authHeaderVal.Parameter);
            }
            else if (authHeaderVal.Scheme.Equals(AuthenticationModeBearer, StringComparison.InvariantCultureIgnoreCase))
            {
                VerifyBearerAuthentication(authHeaderVal.Parameter);
            }
            else
            {
                DenyAccess();
            }
        }

        protected virtual void VerifyBasicAuthentication(string parameter)
        {
            DenyAccess();
        }

        protected virtual void VerifyBearerAuthentication(string parameter)
        {
            DenyAccess();
        }

        public virtual void Dispose()
        {
        }
    }
}
