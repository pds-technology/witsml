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
using System.Linq;
using Energistics.DataAccess;
using Witsml200 = Energistics.DataAccess.WITSML200;
using MongoDB.Driver;
using PDS.Framework;
using Energistics.Datatypes;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Utility class that encapsulates helper methods for parsing element in query and update
    /// </summary>
    public static class MongoDbUtility
    {
        /// <summary>
        /// Gets the entity filter using the specified id field.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="idPropertyName">Name of the identifier property.</param>
        /// <returns>The entity filter with the specified id field</returns>
        public static FilterDefinition<T> GetEntityFilter<T>(EtpUri uri, string idPropertyName = "Uid")
        {
            var builder = Builders<T>.Filter;
            var filters = new List<FilterDefinition<T>>();

            var objectIds = uri.GetObjectIds()
                .ToDictionary(x => x.ObjectType, x => x.ObjectId);

            filters.Add(builder.EqIgnoreCase(idPropertyName, uri.ObjectId));

            if (!ObjectTypes.Well.EqualsIgnoreCase(uri.ObjectType) && objectIds.ContainsKey(ObjectTypes.Well))
            {
                filters.Add(builder.EqIgnoreCase("UidWell", objectIds[ObjectTypes.Well]));
            }
            if (!ObjectTypes.Wellbore.EqualsIgnoreCase(uri.ObjectType) && objectIds.ContainsKey(ObjectTypes.Wellbore))
            {
                filters.Add(builder.EqIgnoreCase("UidWellbore", objectIds[ObjectTypes.Wellbore]));
            }

            return builder.And(filters.Where(f => f != null));
        }

        /// <summary>
        /// Creates a dictionary of common object property paths to update.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Dictionary<string, object> CreateUpdateFields<T>()
        {
            if (typeof(IDataObject).IsAssignableFrom(typeof(T)))
            {
                return new Dictionary<string, object> {
                    { "CommonData.DateTimeLastChange", DateTimeOffset.UtcNow.ToString("o") }
                };
            }

            if (typeof(Witsml200.AbstractObject).IsAssignableFrom(typeof(T)))
            {
                return new Dictionary<string, object> {
                    { "Citation.LastUpdate", DateTime.UtcNow.ToString("o") }
                };
            }

            return new Dictionary<string, object>(0);
        }

        /// <summary>
        /// Creates a list of common element names to ignore during an update.
        /// </summary>
        /// <typeparam name="T">The data object type</typeparam>
        /// <param name="ignored">A custom list of elements to ignore.</param>
        /// <returns></returns>
        public static List<string> CreateIgnoreFields<T>(IEnumerable<string> ignored)
        {
            var creationTime = typeof(IDataObject).IsAssignableFrom(typeof(T))
                ? new List<string> { "dTimCreation", "dTimLastChange" }
                : new List<string> { "Creation", "LastUpdate" };

            return ignored == null ? creationTime : creationTime.Union(ignored).ToList();
        }

        /// <summary>
        /// Builds the filter for a MongoDb field.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="field">The MongoDb field.</param>
        /// <param name="value">The value.</param>
        /// <returns>The filter.</returns>
        public static FilterDefinition<T> BuildFilter<T>(string field, object value)
        {
            if (value is string)
                return Builders<T>.Filter.EqIgnoreCase(field, value.ToString());

            return Builders<T>.Filter.Eq(field, value);
        }

        /// <summary>
        /// Builds the update for a MongoDb field.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="updates">The updates.</param>
        /// <param name="field">The MongoDb field.</param>
        /// <param name="value">The value.</param>
        /// <returns>The update.</returns>
        public static UpdateDefinition<T> BuildUpdate<T>(UpdateDefinition<T> updates, string field, object value)
        {
            if (updates == null)
                return Builders<T>.Update.Set(field, value);

            return updates.Set(field, value);
        }

        /// <summary>
        /// Looks up identifier field.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="defaultField">The default field.</param>
        /// <returns>The identifier field.</returns>
        public static string LookUpIdField(Type type, string defaultField = "Uid")
        {
            var idField = defaultField;
            var classMap = BsonClassMap.LookupClassMap(type);

            if (classMap != null && classMap.IdMemberMap != null)
                idField = classMap.IdMemberMap.MemberName;

            return idField;
        }

        /// <summary>
        /// Gets the identifier in BsonDocument format.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>The identifier in BsonDocument format.</returns>
        public static BsonDocument GetDocumentId<T>(T entity)
        {
            var copy = Activator.CreateInstance<T>();
            if (entity is IDataObject)
            {
                ((IDataObject)copy).Uid = ((IDataObject)entity).Uid;
            }
            if (entity is IWellObject)
            {
                ((IWellObject)copy).UidWell = ((IWellObject)entity).UidWell;
            }
            if (entity is IWellboreObject)
            {
                ((IWellboreObject)copy).UidWellbore = ((IWellboreObject)entity).UidWellbore;
            }

            return copy.ToBsonDocument();
        }
    }
}
