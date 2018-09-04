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

namespace PDS.WITSMLstudio.Store.Data.GrowingObjects
{
    /// <summary>
    /// Metadata on the growing status of WITSML growing objects.
    /// </summary>
    [Serializable]
    public class DbGrowingObject
    {
        /// <summary>
        /// Gets or sets the URI of the parent data object.
        /// </summary>
        /// <value>The parent data object URI.</value>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the type of the growing object.
        /// </summary>
        /// <value>
        /// The type of the object.
        /// </value>
        public string ObjectType { get; set; }

        /// <summary>
        /// Gets or sets the uid of the wellbore.
        /// </summary>
        /// <value>
        /// The uid wellbore.
        /// </value>
        public string WellboreUri { get; set; }

        /// <summary>
        /// Gets or sets the date time of the last data append.
        /// </summary>
        /// <value>
        /// The last append date time.
        /// </value>
        public DateTime LastAppendDateTime { get; set; }
    }
}
