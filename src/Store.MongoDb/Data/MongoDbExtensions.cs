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
using System.Reflection;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Defines helper methods that can be used with MongoDB APIs.
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
            if (string.IsNullOrEmpty(propertyPath) || propertyValue == null) return null;
            return filter.Regex(propertyPath, new BsonRegularExpression("^" + Regex.Escape(propertyValue) + "$", "i"));
        }

        /// <summary>
        /// Creates a regular expression filter to perform a case-insensitive search.
        /// </summary>
        /// <param name="type">The data object type.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <returns>The regular expression filter definition.</returns>
        public static object EqualsIgnoreCase(Type type, string propertyPath, string propertyValue)
        {
            var helper = CreateType(type);
            var method = GetStaticMethod(helper, "EqualsIgnoreCase");

            return method?.Invoke(null, new object[] { propertyPath, propertyValue });
        }

        /// <summary>
        /// Creates a pull filter update definition expression.
        /// </summary>
        /// <param name="parentType">Type of the parent.</param>
        /// <param name="childType">Type of the child.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="childFilter">The child filter.</param>
        /// <returns>The update definition expression.</returns>
        public static object PullFilter(Type parentType, Type childType, string propertyPath, object childFilter)
        {
            var helper = CreateType(parentType, childType);
            var method = GetStaticMethod(helper, "PullFilter");

            return method?.Invoke(null, new[] { propertyPath, childFilter });
        }

        private static Type CreateType(Type parentType, Type childType)
        {
            var definition = typeof(MongoDbExtensions<,>);
            return definition.MakeGenericType(parentType, childType);
        }

        private static Type CreateType(Type type)
        {
            var definition = typeof(MongoDbExtensions<>);
            return definition.MakeGenericType(type);
        }

        private static MethodInfo GetStaticMethod(Type type, string methodName)
        {
            return type?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        }
    }

    /// <summary>
    /// Defines generic helper methods that can be used with MongoDB APIs.
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    public static class MongoDbExtensions<T>
    {
        /// <summary>
        /// Creates a regular expression filter to perform a case-insensitive search.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <returns></returns>
        public static FilterDefinition<T> EqualsIgnoreCase(string propertyPath, string propertyValue)
        {
            return Builders<T>.Filter.EqIgnoreCase(propertyPath, propertyValue);
        } 
    }

    /// <summary>
    /// Defines generic helper methods that can be used with MongoDB APIs.
    /// </summary>
    /// <typeparam name="TParent">The parent data object type.</typeparam>
    /// <typeparam name="TChild">The child data object type.</typeparam>
    public static class MongoDbExtensions<TParent, TChild>
    {
        /// <summary>
        /// Creates a pull filter update definition expression.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="childFilter">The child filter.</param>
        /// <returns></returns>
        public static UpdateDefinition<TParent> PullFilter(string propertyPath, FilterDefinition<TChild> childFilter)
        {
            return Builders<TParent>.Update.PullFilter(propertyPath, childFilter);
        }
    }
}
