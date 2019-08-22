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
using System.Reflection;
using MongoDB.Driver;
using PDS.WITSMLstudio.Data;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Encapsulates common properties used for partial-deleting the data store.
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Data.DataObjectNavigationContext" />
    public class MongoDbDeleteContext<T> : DataObjectNavigationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbDeleteContext{T}"/> class.
        /// </summary>
        public MongoDbDeleteContext()
        {
            DataObjectType = typeof(T);
            Function = Functions.DeleteFromStore;
            PropertyInfos = new List<PropertyInfo>();
            PropertyValues = new List<object>();
            Update = null;
        }

        /// <summary>
        /// Gets the property information list.
        /// </summary>
        /// <value>The property information list.</value>
        public List<PropertyInfo> PropertyInfos { get; }

        /// <summary>
        /// Gets the property value list.
        /// </summary>
        /// <value>The property value list.</value>
        public List<object> PropertyValues { get; }

        /// <summary>
        /// Gets or sets the update definition for partial delete.
        /// </summary>
        /// <value>The update definition.</value>
        public UpdateDefinition<T> Update { get; set; }
    }
}
