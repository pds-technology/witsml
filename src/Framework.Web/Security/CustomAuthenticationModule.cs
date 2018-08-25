//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2018.3
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
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;

namespace PDS.WITSMLstudio.Framework.Web.Security
{
    /// <summary>
    /// Provides custom authentication to the module.
    /// </summary>
    /// <seealso cref="System.Web.IHttpModule" />
    public class CustomAuthenticationModule : IHttpModule
    {
        private const string AuthorizationHeaderKey = "Authorization";
        private const string AuthenticateHeaderKey = "WWW-Authenticate";
        private const string AuthenticationModeBasic = "Basic";
        private const string AuthenticationModeBearer = "Bearer";
        private const string GrantTypeKey = "grant_type";
        private const string GrantTypeUsername = "username";
        private const string GrantTypePassword = "password";

        /// <summary>
        /// Initializes a module and prepares it to handle requests.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpApplication" /> that provides access to the methods, properties, and events common to all application objects within an ASP.NET application</param>
        public void Init(HttpApplication context)
        {
            // Register event handlers
            context.AuthenticateRequest += OnApplicationAuthenticateRequest;
            context.EndRequest += OnApplicationEndRequest;
        }

        /// <summary>
        /// Called when application authenticate request.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected virtual void OnApplicationAuthenticateRequest(object sender, EventArgs e)
        {
            var request = HttpContext.Current.Request;
            var authHeader = request.QueryString[AuthorizationHeaderKey] ?? request.Headers[AuthorizationHeaderKey];

            // Support username and password sent via payload for a JWT request
            if (string.IsNullOrWhiteSpace(authHeader) && GrantTypePassword.EqualsIgnoreCase(request.Form[GrantTypeKey]))
            {
                var username = request.Form[GrantTypeUsername];
                var password = request.Form[GrantTypePassword];

                authHeader = ConvertToBasicAuthentication(username, password);
            }

            if (string.IsNullOrWhiteSpace(authHeader))
            {
                DenyAccess();
            }

            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
            VerifyAuthenticationHeader(authHeaderVal);
        }

        /// <summary>
        /// Called when application end request. If the request was unauthorized, the WWW-Authenticate header is added to the response.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
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

        /// <summary>
        /// Denies the access.
        /// </summary>
        protected virtual void DenyAccess()
        {
            var context = HttpContext.Current;
            context.Response.StatusCode = 401;
            context.Response.End();
        }

        /// <summary>
        /// Sets the principal.
        /// </summary>
        /// <param name="principal">The principal.</param>
        protected virtual void SetPrincipal(IPrincipal principal)
        {
            Thread.CurrentPrincipal = principal;

            if (HttpContext.Current != null)
            {
                HttpContext.Current.User = principal;
            }
        }

        /// <summary>
        /// Verifies the authentication header.
        /// </summary>
        /// <param name="authHeaderVal">The authentication header value.</param>
        protected virtual void VerifyAuthenticationHeader(AuthenticationHeaderValue authHeaderVal)
        {
            if (string.IsNullOrWhiteSpace(authHeaderVal.Parameter))
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

        /// <summary>
        /// Verifies the basic authentication.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        protected virtual void VerifyBasicAuthentication(string parameter)
        {
            DenyAccess();
        }

        /// <summary>
        /// Verifies the bearer authentication.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        protected virtual void VerifyBearerAuthentication(string parameter)
        {
            DenyAccess();
        }

        /// <summary>
        /// Converts the supplied credentials to a Basic authentication header.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A Basic authentication header.</returns>
        protected virtual string ConvertToBasicAuthentication(string username, string password)
        {
            var credentials = string.IsNullOrWhiteSpace(username)
                ? string.Empty
                : string.Concat(username, ":", password);

            return string.Concat(AuthenticationModeBasic, " ", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
        }

        /// <summary>
        /// Disposes of the resources (other than memory) used by the module.
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
