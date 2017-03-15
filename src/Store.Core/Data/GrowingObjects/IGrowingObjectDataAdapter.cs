//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.1
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

using Energistics.Datatypes;

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
    }
}
