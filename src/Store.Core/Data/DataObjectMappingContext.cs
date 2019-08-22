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
using PDS.WITSMLstudio.Data;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Encapsulates common properties for mapping data objects.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Data.DataObjectNavigationContext" />
    public class DataObjectMappingContext<T> : DataObjectNavigationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataObjectMappingContext{T}"/> class.
        /// </summary>
        public DataObjectMappingContext()
        {
            DataObjectType = typeof(T);
        }

        /// <summary>
        /// Gets or sets the list of properties.
        /// </summary>
        /// <value>The list of properties.</value>
        public List<string> Properties { get; set; }

        /// <summary>
        /// Gets or sets the source data object.
        /// </summary>
        /// <value>The source data object.</value>
        public T Source { get; set; }

        /// <summary>
        /// Gets or sets the target data object.
        /// </summary>
        /// <value>The target data object.</value>
        public T Target { get; set; }
    }
}
