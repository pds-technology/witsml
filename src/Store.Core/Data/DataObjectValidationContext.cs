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

using PDS.WITSMLstudio.Data;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Encapsulates common properties used for validating WITSML documents.
    /// </summary>
    /// <typeparam name="T">The type of the data object.</typeparam>
    public class DataObjectValidationContext<T> : DataObjectNavigationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataObjectValidationContext{T}"/> class.
        /// </summary>
        public DataObjectValidationContext()
        {
            DataObjectType = typeof(T);
        }

        /// <summary>
        /// Gets or sets a value indicating whether NaN elements should be removed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if NaN elements should be removed; otherwise, <c>false</c>.
        /// </value>
        public bool RemoveNaNElements { get; set; }
    }
}
