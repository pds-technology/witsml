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
using Energistics.Datatypes;

namespace PDS.Witsml.Server.Data.GrowingObjects
{
    /// <summary>
    /// Represents a data provider that supports growing object operations.
    /// </summary>
    public interface IGrowingObjectDataProvider
    {
        /// <summary>
        /// Gets a value indicating whether this instance is expiring growing objects.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is expiring growing objects; otherwise, <c>false</c>.
        /// </value>
        bool IsExpiringGrowingObjects { get; }

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
        void ExpireGrowingObjects(string objectType, DateTime expiredDateTime);
    }
}
