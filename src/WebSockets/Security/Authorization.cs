//----------------------------------------------------------------------- 
// ETP DevKit, 1.0
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
using System.Collections.Generic;
using System.Text;

namespace Energistics.Security
{
    /// <summary>
    /// Provides methods that can be used to create a dictionary containing an Authorization header.
    /// </summary>
    public static class Authorization
    {
        public const string Header = "Authorization";

        /// <summary>
        /// Creates a dictionary containing an Authorization header for the specified username and password.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A dictionary containing an Authorization header.</returns>
        public static IDictionary<string, string> Basic(string username, string password)
        {
            return GetAuthorizationHeader("Basic", string.IsNullOrWhiteSpace(username) ? string.Empty : string.Concat(username, ":", password));
        }

        /// <summary>
        /// Creates a dictionary containing an Authorization header for the specified JSON web token.
        /// </summary>
        /// <param name="token">The JSON web token.</param>
        /// <returns>A dictionary containing an Authorization header.</returns>
        public static IDictionary<string, string> Bearer(string token)
        {
            return GetAuthorizationHeader("Bearer", string.IsNullOrWhiteSpace(token) ? string.Empty : token);
        }

        /// <summary>
        /// Creates a dictionary containing an Authorization header for the specified schema and encoded string.
        /// </summary>
        /// <param name="schema">The schema.</param>
        /// <param name="encodedString">The encoded string.</param>
        /// <returns>A dictionary containing an Authorization header.</returns>
        private static IDictionary<string, string> GetAuthorizationHeader(string schema, string encodedString)
        {
            var encoded = Convert.ToBase64String(Encoding.Default.GetBytes(encodedString));
            var headers = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(encoded))
            {
                headers[Header] = string.Concat(schema, " ", encoded);
            }

            return headers;
        }
    }
}
