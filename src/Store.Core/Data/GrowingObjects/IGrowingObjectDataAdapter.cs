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

using System.Collections.Generic;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.Object;

namespace PDS.WITSMLstudio.Store.Data.GrowingObjects
{
    /// <summary>
    /// Defines methods specific to growing object data adapters.
    /// </summary>
    public interface IGrowingObjectDataAdapter
    {
        /// <summary>
        /// Updates the objectGrowing flag for a growing object.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="isGrowing">The objectGrowing flag of a growing object is set to this value.</param>
        void UpdateObjectGrowing(EtpUri uri, bool isGrowing);

        /// <summary>
        /// Determines whether this instance can save the data portion of the growing object.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can save the data portion of the growing object; otherwise, <c>false</c>.
        /// </returns>
        bool CanSaveData();

        /// <summary>
        /// Gets the growing part having the specified UID for a growing object.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="uid">The growing part's uid.</param>
        /// <returns></returns>
        IDataObject GetGrowingPart(IEtpAdapter etpAdapter, EtpUri uri, string uid);

        /// <summary>
        /// Gets the growing parts for a growing object within the specified index range.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <returns></returns>
        List<IDataObject> GetGrowingParts(IEtpAdapter etpAdapter, EtpUri uri, object startIndex, object endIndex);

        /// <summary>
        /// Puts the growing part for a growing object.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="data">The data.</param>
        void PutGrowingPart(IEtpAdapter etpAdapter, EtpUri uri, string contentType, byte[] data);

        /// <summary>
        /// Deletes the growing part having the specified UID for a growing object.
        /// </summary>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="uid">The growing part's uid.</param>
        void DeleteGrowingPart(EtpUri uri, string uid);

        /// <summary>
        /// Deletes the growing parts for a growing object within the specified index range.
        /// </summary>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        void DeleteGrowingParts(EtpUri uri, object startIndex, object endIndex);
    }
}
