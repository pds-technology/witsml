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

using Energistics.Common;
using Energistics.Datatypes;

namespace PDS.Witsml.Server.Providers
{
    /// <summary>
    /// Defines static helper methods that can be used from any protocol handler.
    /// </summary>
    public static class EtpExtensions
    {
        /// <summary>
        /// Creates and validates the specified URI.
        /// </summary>
        /// <param name="handler">The protocol handler.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <returns>A new <see cref="EtpUri" /> instance.</returns>
        public static EtpUri CreateAndValidateUri(this EtpProtocolHandler handler, string uri, long messageId = 0)
        {
            var etpUri = new EtpUri(uri);

            if (!etpUri.IsValid)
            {
                handler.InvalidUri(uri, messageId);
            }

            return etpUri;
        }

        /// <summary>
        /// Validates URI Object Type.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="etpUri">The ETP URI.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <returns></returns>
        public static bool ValidateUriObjectType(this EtpProtocolHandler handler, EtpUri etpUri, long messageId = 0)
        {
            if (!string.IsNullOrWhiteSpace(etpUri.ObjectType))
                return true;

            handler.UnsupportedObject(null, $"{etpUri.Uri}", messageId);
            return false;
        }
    }
}
