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
using System.Security.Principal;
using System.Text;
using System.Web.Security;
using log4net;

namespace PDS.WITSMLstudio.Framework.Web.Security
{
    /// <summary>
    /// Provides basic authentication.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Framework.Web.Security.CustomAuthenticationModule" />
    public class BasicAuthenticationModule : CustomAuthenticationModule
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(BasicAuthenticationModule));

        /// <summary>
        /// Verifies the Basic authentication token.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        protected override void VerifyBasicAuthentication(string parameter)
        {
            AuthenticateUser(parameter);
        }

        private void AuthenticateUser(string credentials)
        {
            try
            {
                var encoding = Encoding.GetEncoding("iso-8859-1");
                credentials = encoding.GetString(Convert.FromBase64String(credentials));

                int separator = credentials.IndexOf(':');
                var username = credentials.Substring(0, separator);
                var password = credentials.Substring(separator + 1);

                if (CheckPassword(username, password))
                {
                    var identity = new GenericIdentity(username);
                    SetPrincipal(new GenericPrincipal(identity, null));
                }
                else
                {
                    // Invalid username or password.
                    _log.WarnFormat("Invalid authentication attempt for user: {0}", username);
                    DenyAccess();
                }
            }
            catch (FormatException)
            {
                // Credentials were not formatted correctly.
                DenyAccess();
            }
        }

        private bool CheckPassword(string username, string password)
        {
            _log.InfoFormat("Authenticating user: {0}", username);
            return Membership.ValidateUser(username, password);
        }
    }
}
