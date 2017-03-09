//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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
using System.IdentityModel.Tokens;
using log4net;
using Microsoft.Owin.Security.DataHandler.Encoder;
using PDS.Framework.Web.Security;
using Thinktecture.IdentityModel.Tokens;

namespace PDS.Witsml.Server.Security
{
    /// <summary>
    /// Provides Basic and JSON Web Token (JWT) authentication for ETP.
    /// </summary>
    /// <seealso cref="PDS.Framework.Web.Security.BasicAuthenticationModule" />
    public class EtpAuthenticationModule : BasicAuthenticationModule
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(EtpAuthenticationModule));
        private static readonly string _issuer;
        private static readonly string _audience;
        private static readonly string _secret;

        /// <summary>
        /// Initializes the <see cref="EtpAuthenticationModule"/> class.
        /// </summary>
        static EtpAuthenticationModule()
        {
            _issuer = ConfigurationManager.AppSettings["jwt.issuer"];
            _audience = ConfigurationManager.AppSettings["jwt.audience"];
            _secret = ConfigurationManager.AppSettings["jwt.secret"];
        }

        /// <summary>
        /// Verifies the Bearer authentication token.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        protected override void VerifyBearerAuthentication(string parameter)
        {
            ValidateJsonWebToken(parameter);
        }

        /// <summary>
        /// Validates the JSON Web Token (JWT).
        /// </summary>
        /// <param name="encryptedToken">The encrypted token.</param>
        private void ValidateJsonWebToken(string encryptedToken)
        {
            _log.Debug("Validating JSON web token");

            try
            {
                var decodedSecret = TextEncodings.Base64Url.Decode(_secret);
                var signingCredentials = new HmacSigningCredentials(decodedSecret);

                var parameters = new TokenValidationParameters()
                {
                    ValidAudience = _audience,
                    ValidIssuer = _issuer,
                    IssuerSigningKey = signingCredentials.SigningKey
                };

                SecurityToken validatedToken;

                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(encryptedToken, parameters, out validatedToken);
                var token = validatedToken as JwtSecurityToken;

                if (token == null)
                {
                    _log.Warn("Invalid JSON web token");
                    DenyAccess();
                }

                SetPrincipal(principal);

                _log.DebugFormat("Validated JSON web token for user: {0}", token.Payload.Sub);
            }
            catch (Exception ex)
            {
                _log.Error("Error validating JSON web token", ex);
                DenyAccess();
            }
        }
    }
}
