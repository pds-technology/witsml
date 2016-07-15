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
using System.Collections.Generic;
using System.Reflection;
using MongoDB.Driver;
using PDS.Witsml.Data;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Encapsulates common properties used for updating the data store.
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    /// <seealso cref="PDS.Witsml.Data.DataObjectNavigationContext" />
    public class MongoDbUpdateContext<T> : DataObjectNavigationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbUpdateContext{T}"/> class.
        /// </summary>
        public MongoDbUpdateContext()
        {
            DataObjectType = typeof(T);
            Function = Functions.UpdateInStore;
            PropertyInfos = new List<PropertyInfo>();
            PropertyValues = new List<object>();
            Update = null;           
        }

        /// <summary>
        /// Gets the type of the data object.
        /// </summary>
        /// <value>The type of the data object.</value>
        public override Type DataObjectType { get; }

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
        /// Gets or sets the update definition.
        /// </summary>
        /// <value>The update definition.</value>
        public UpdateDefinition<T> Update { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [validation only].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [validation only]; otherwise, <c>false</c>.
        /// </value>
        public bool ValidationOnly { get; set; }
    }
}
