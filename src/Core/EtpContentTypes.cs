//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using Energistics.Etp.Common.Datatypes;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Defines the supported list of ETP content types.
    /// </summary>
    public static class EtpContentTypes
    {
        /// <summary>
        /// The <see cref="EtpContentType"/> for prodml200
        /// </summary>
        public static readonly EtpContentType Prodml200 = new EtpContentType("application/x-prodml+xml;version=2.0");

        /// <summary>
        /// The <see cref="EtpContentType"/> for resqml200
        /// </summary>
        public static readonly EtpContentType Resqml200 = new EtpContentType("application/x-resqml+xml;version=2.0");

        /// <summary>
        /// The <see cref="EtpContentType"/> for resqml201
        /// </summary>
        public static readonly EtpContentType Resqml201 = new EtpContentType("application/x-resqml+xml;version=2.0.1");

        /// <summary>
        /// The <see cref="EtpContentType"/> for resqml210
        /// </summary>
        public static readonly EtpContentType Resqml210 = new EtpContentType("application/x-resqml+xml;version=2.1");

        /// <summary>
        /// The <see cref="EtpContentType"/> for witsml131
        /// </summary>
        public static readonly EtpContentType Witsml131 = new EtpContentType("application/x-witsml+xml;version=1.3.1.1");

        /// <summary>
        /// The <see cref="EtpContentType"/> for witsml141
        /// </summary>
        public static readonly EtpContentType Witsml141 = new EtpContentType("application/x-witsml+xml;version=1.4.1.1");

        /// <summary>
        /// The <see cref="EtpContentType"/> for witsml200
        /// </summary>
        public static readonly EtpContentType Witsml200 = new EtpContentType("application/x-witsml+xml;version=2.0");

        /// <summary>
        /// The <see cref="EtpContentType"/> for eml210
        /// </summary>
        public static readonly EtpContentType Eml210 = new EtpContentType("application/x-eml+xml;version=2.1");

        /// <summary>
        /// Gets the ETP content type for the specified data object type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>An <see cref="EtpContentType"/> instance.</returns>
        public static EtpContentType GetContentType(Type type)
        {
            var uri = EtpUris.GetUriFamily(type);
            var objectType = ObjectTypes.GetObjectType(type);
            return uri.ContentType.For(objectType);
        }
    }
}
