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

using System.Data;
using PDS.Witsml.Server.Models;

namespace PDS.Witsml.Server.Converters
{
    /// <summary>
    /// Defines methods that can be used to convert to and from provider specific data types.
    /// </summary>
    public interface IDbValueConverter
    {
        /// <summary>
        /// Converts the supplied value to a provider specific data type.
        /// </summary>
        /// <param name="propertyValue">The value to convert.</param>
        /// <returns>The converted value.</returns>
        object ConvertToDb(object propertyValue);

        /// <summary>
        /// Converts the supplied value from a provider specific data type.
        /// </summary>
        /// <param name="mapping">The data object mapping.</param>
        /// <param name="reader">The data reader.</param>
        /// <param name="columnName">The column name.</param>
        /// <param name="columnValue">The column value.</param>
        /// <returns>The converted value.</returns>
        object ConvertFromDb(ObjectMapping mapping, IDataReader reader, string columnName, object columnValue);
    }
}
