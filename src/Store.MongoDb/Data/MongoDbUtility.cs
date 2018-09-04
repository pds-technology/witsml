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
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using Witsml200 = Energistics.DataAccess.WITSML200;
using MongoDB.Driver;
using PDS.WITSMLstudio.Framework;
using Energistics.Etp.Common.Datatypes;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;

namespace PDS.WITSMLstudio.Store.Data
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

            if (ObjectTypes.Uri.EqualsIgnoreCase(idPropertyName) || !uri.IsValid)
            {
                return builder.EqIgnoreCase(idPropertyName, uri.Uri);
            }

            // Uuid filter
            if (ObjectTypes.Uuid.Equals(idPropertyName))
            {
                return builder.EqIgnoreCase(idPropertyName, uri.ObjectId);
            }

            // Create dictionary with case-insensitive keys
            var objectIds = uri.GetObjectIds()
                .ToDictionary(x => x.ObjectType, x => x.ObjectId, StringComparer.CurrentCultureIgnoreCase);

            // Uid filter
            var filters = new List<FilterDefinition<T>>
            {
                builder.EqIgnoreCase(idPropertyName, uri.ObjectId)
            };

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
        /// <typeparam name="T">The data object type.</typeparam>
        /// <returns>A dictionary of name/value pairs.</returns>
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
        /// Creates a dictionary of the object growing property path to update.
        /// </summary>
        /// <typeparam name="T">The data object type.</typeparam>
        /// <param name="isGrowing">The value to set the object growing flag.</param>
        /// <returns>A dictionary of name/value pairs.</returns>
        public static Dictionary<string, object> CreateObjectGrowingFields<T>(bool isGrowing)
        {
            if (typeof(IDataObject).IsAssignableFrom(typeof(T)))
            {
                return new Dictionary<string, object> {
                    { "ObjectGrowing", isGrowing }
                };
            }

            if (typeof(Witsml200.AbstractObject).IsAssignableFrom(typeof(T)))
            {
                return new Dictionary<string, object> {
                    { "GrowingStatus", isGrowing ? Witsml200.ReferenceData.ChannelStatus.active : Witsml200.ReferenceData.ChannelStatus.inactive }
                };
            }

            return new Dictionary<string, object>(0);
        }

        /// <summary>
        /// Creates a list of common element names to ignore during an update.
        /// </summary>
        /// <typeparam name="T">The data object type</typeparam>
        /// <param name="ignored">A custom list of elements to ignore.</param>
        /// <param name="qualified">if set to <c>true</c> use qualified names.</param>
        /// <returns></returns>
        public static List<string> CreateIgnoreFields<T>(IEnumerable<string> ignored, bool qualified = false)
        {
            var prefixCommonData = qualified ? "CommonData." : string.Empty;
            var prefixCitation = qualified ? "Citation." : string.Empty;

            var creationTime = typeof(IDataObject).IsAssignableFrom(typeof(T))
                ? new List<string> { $"{prefixCommonData}dTimCreation", $"{prefixCommonData}dTimLastChange" }
                : new List<string> { $"{prefixCitation}Creation", $"{prefixCitation}LastUpdate" };

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
        /// <returns>The update definition.</returns>
        public static UpdateDefinition<T> BuildUpdate<T>(UpdateDefinition<T> updates, string field, object value)
        {
            if (updates == null)
                return Builders<T>.Update.Set(field, value);

            return updates.Set(field, value);
        }

        /// <summary>
        /// Builds the update for a collection of MongoDb fields.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="updates">The updates.</param>
        /// <param name="values">The dictionary of property paths and values.</param>
        /// <returns>The update definition.</returns>
        public static UpdateDefinition<T> BuildUpdate<T>(UpdateDefinition<T> updates, IDictionary<string, object> values)
        {
            values.ForEach(item => updates = BuildUpdate(updates, item.Key, item.Value));
            return updates;
        }

        /// <summary>
        /// Builds the push for a MongoDb array field.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="updates">The updates.</param>
        /// <param name="field">The MongoDb field.</param>
        /// <param name="value">The value.</param>
        /// <returns>The update.</returns>
        public static UpdateDefinition<T> BuildPush<T>(UpdateDefinition<T> updates, string field, object value)
        {
            if (updates == null)
                return Builders<T>.Update.Push(field, value);

            return updates.Push(field, value);
        }

        /// <summary>
        /// Builds the push for a MongoDb array field.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TChild">The type of the child.</typeparam>
        /// <param name="updates">The updates.</param>
        /// <param name="field">The MongoDb field.</param>
        /// <param name="values">The values.</param>
        /// <returns>The update.</returns>
        public static UpdateDefinition<T> BuildPushEach<T, TChild>(UpdateDefinition<T> updates, string field, IEnumerable<TChild> values)
        {
            if (updates == null)
                return Builders<T>.Update.PushEach(field, values);

            return updates.PushEach(field, values);
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

            var obj = entity as Witsml200.AbstractObject;
            if (obj != null)
            {
                var copyObj = copy as Witsml200.AbstractObject;
                if (copyObj != null)
                    copyObj.Uuid = obj.Uuid;
            }

            return copy.ToBsonDocument();
        }

        /// <summary>
        /// Gets the list of URI by object type.
        /// </summary>
        /// <param name="uris">The URI list.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>the list of URI specified by the object type.</returns>
        public static List<EtpUri> GetObjectUris(IEnumerable<EtpUri> uris, string objectType)
        {
            return uris.Where(u => u.ObjectType.EqualsIgnoreCase(objectType)).ToList();
        }
    }
}
