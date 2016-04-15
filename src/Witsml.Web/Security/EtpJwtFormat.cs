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
using System.IdentityModel.Tokens;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler.Encoder;
using Thinktecture.IdentityModel.Tokens;

namespace PDS.Witsml.Web.Security
{
    public class EtpJwtFormat : ISecureDataFormat<AuthenticationTicket>
    {
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _secret;

        public EtpJwtFormat(string issuer, string audience, string secret)
        {
            _issuer = issuer;
            _audience = audience;
            _secret = secret;
        }

        public string Protect(AuthenticationTicket data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            var keyByteArray = TextEncodings.Base64Url.Decode(_secret);
            var signingKey = new HmacSigningCredentials(keyByteArray);

            var issued = data.Properties.IssuedUtc.Value;
            var expires = data.Properties.ExpiresUtc.Value;

            var token = new JwtSecurityToken(_issuer, _audience, data.Identity.Claims, issued.UtcDateTime, expires.UtcDateTime, signingKey);
            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(token);
        }

        public AuthenticationTicket Unprotect(string protectedText)
        {
            throw new NotImplementedException();
        }
    }
}
