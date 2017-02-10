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
using PDS.Witsml.Data;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Encapsulates common properties for mapping data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Data.DataObjectNavigationContext" />
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
        /// Gets the type of the data object.
        /// </summary>
        /// <value>The type of the data object.</value>
        public override Type DataObjectType { get; }

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
