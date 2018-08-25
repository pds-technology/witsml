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
using System.Collections.Generic;
using Energistics.Etp.Common.Datatypes;

namespace PDS.WITSMLstudio.Store.Data.GrowingObjects
{
    /// <summary>
    /// Represents a data provider that supports growing object operations.
    /// </summary>
    public interface IGrowingObjectDataProvider
    {
        /// <summary>
        /// Updates the last append date time for a growing object for the specified uri.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="wellboreUri">The wellbore URI.</param>
        void UpdateLastAppendDateTime(EtpUri uri, EtpUri wellboreUri);

        /// <summary>
        /// Expires the growing objects for the specified objectType and expiredDateTime.
        /// Any growing object of the specified type will have its objectGrowing flag set
        /// to false if its lastAppendDateTime is older than the expireDateTime.
        /// </summary>
        /// <param name="objectType">Type of the groing object.</param>
        /// <param name="expiredDateTime">The expired date time.</param>
        /// <returns>A list of wellbore uri of expired growing objects.</returns>
        List<string> ExpireGrowingObjects(string objectType, DateTime expiredDateTime);

        /// <summary>
        /// Sets isActive flag of wellbore to false if none of its children are growing
        /// </summary>
        /// <param name="wellboreUris">The list of wellbore uris to check for growing children objects.</param>
        void ExpireWellboreObjects(List<string> wellboreUris);

        /// <summary>
        /// Checks for the existance of a dbGrowingObject for the specified uri.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>true of a dbGrowingObject exists for the specified uri, false otherwise.</returns>
        bool Exists(EtpUri uri);

        /// <summary>
        /// Deletes a dbGrowingObject for the specified identifier.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        void Delete(EtpUri uri);
    }
}
