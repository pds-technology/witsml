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

using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Defined helper methods that can be used with MongoDB APIs.
    /// </summary>
    public static class MongoDbExtensions
    {
        /// <summary>
        /// Creates a regular expression filter to perform a case-insensitive search.
        /// </summary>
        /// <typeparam name="T">The data object type.</typeparam>
        /// <param name="filter">The filter definition builder.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <returns>The regular expression filter definition.</returns>
        public static FilterDefinition<T> EqIgnoreCase<T>(this FilterDefinitionBuilder<T> filter, string propertyPath, string propertyValue)
        {
            return filter.Regex(propertyPath, new BsonRegularExpression("^" + Regex.Escape(propertyValue) + "$", "i"));
        }
    }
}
