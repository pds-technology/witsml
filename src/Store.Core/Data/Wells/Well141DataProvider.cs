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
using Energistics.DataAccess.WITSML141;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Wells
{
    /// <summary>
    /// Data provider that implements support for WITSML API functions for <see cref="Well"/>.
    /// </summary>
    public partial class Well141DataProvider
    {
        /// <summary>
        /// Sets the additional default values.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        partial void SetAdditionalDefaultValues(Well dataObject)
        {
            if (string.IsNullOrWhiteSpace(dataObject.TimeZone))
                dataObject.TimeZone = GetTimeZoneOffset();
        }

        /// <summary>
        /// Sets additional default values for the specified data object and URI.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="uri">The data object URI.</param>
        partial void SetAdditionalDefaultValues(Well dataObject, EtpUri uri)
        {
            dataObject.TimeZone = GetTimeZoneOffset();
        }

        /// <summary>
        /// Gets the time zone offset from Witsml settings.
        /// </summary>
        /// <returns>The time zone offset.</returns>
        private string GetTimeZoneOffset()
        {
            return WitsmlSettings.DefaultTimeZone;
        }
    }
}
