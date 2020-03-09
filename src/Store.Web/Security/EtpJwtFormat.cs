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
using System.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler.Encoder;
using Thinktecture.IdentityModel.Tokens;

namespace PDS.WITSMLstudio.Store.Security
{
    /// <summary>
    /// Defines the JSON Web Token (JWT) format used for ETP.
    /// </summary>
    /// <seealso cref="Microsoft.Owin.Security.ISecureDataFormat{AuthenticationTicket}" />
    public class EtpJwtFormat : ISecureDataFormat<AuthenticationTicket>
    {
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _secret;

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpJwtFormat"/> class.
        /// </summary>
        /// <param name="issuer">The issuer.</param>
        /// <param name="audience">The audience.</param>
        /// <param name="secret">The secret.</param>
        public EtpJwtFormat(string issuer, string audience, string secret)
        {
            _issuer = issuer;
            _audience = audience;
            _secret = secret;
        }

        /// <summary>
        /// Encrypts the authentication data in JSON Web Token format.
        /// </summary>
        /// <param name="data">The token data.</param>
        /// <returns>The formatted JSON Web Token.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public string Protect(AuthenticationTicket data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var keyByteArray = TextEncodings.Base64Url.Decode(_secret);
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyByteArray);
            var signingKey = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            var issued = data.Properties.IssuedUtc.GetValueOrDefault(DateTimeOffset.Now);
            var expires = data.Properties.ExpiresUtc.GetValueOrDefault(issued.AddDays(30));

            var token = new JwtSecurityToken(_issuer, _audience, data.Identity.Claims, issued.UtcDateTime, expires.UtcDateTime, signingKey);
            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(token);
        }

        /// <summary>
        /// Decrypts the specified authentication token.
        /// </summary>
        /// <param name="protectedText">The protected token.</param>
        /// <returns>An <see cref="AuthenticationTicket"/> containing authentication data.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public AuthenticationTicket Unprotect(string protectedText)
        {
            throw new NotImplementedException();
        }
    }
}
