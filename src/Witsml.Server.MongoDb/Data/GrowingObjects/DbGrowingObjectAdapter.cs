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
using System.ComponentModel.Composition;
using Energistics.Datatypes;
using MongoDB.Bson;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Server.Data.Transactions;

namespace PDS.Witsml.Server.Data.GrowingObjects
{
    /// <summary>
    /// Manages storage of DbGrowingDataObject in the Mongo Db
    /// </summary>
    [Export(typeof(IGrowingObjectDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DbGrowingObjectAdapter : MongoDbDataAdapter<DbGrowingObject>, IGrowingObjectDataProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbGrowingObjectAdapter"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public DbGrowingObjectAdapter(IContainer container, IDatabaseProvider databaseProvider) : 
            base(container, databaseProvider, "dbGrowingObject", ObjectTypes.Uri)
        {
            
        }

        /// <summary>
        /// Growings the object append.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="wellboreUri">The wellbore URI.</param>
        public void UpdateLastAppendDateTime(EtpUri uri, EtpUri wellboreUri)
        {
            uri = new EtpUri(uri.ToString().ToLowerInvariant());

            var growingObject = GetEntity(uri);
            var lastAppendDateTime = DateTime.UtcNow;

            if (growingObject == null)
            {
                growingObject = new DbGrowingObject()
                {
                    Uri = uri,
                    ObjectType = uri.ObjectType,
                    WellboreUri = wellboreUri,
                    LastAppendDateTime = lastAppendDateTime
                };

                Transaction.Attach(MongoDbAction.Add, DbCollectionName, growingObject.ToBsonDocument(), uri);
                Transaction.Save();
                InsertEntity(growingObject);
            }
            else
            {
                growingObject.LastAppendDateTime = lastAppendDateTime;
                Transaction.Attach(MongoDbAction.Update, DbCollectionName, growingObject.ToBsonDocument(), uri);
                Transaction.Save();
                ReplaceEntity(growingObject, uri);
            }
        }

        /// <summary>
        /// Expires the growing objects for the specified objectType and expiredDateTime.
        /// Any growing object of the specified type will have its objectGrowing flag set
        /// to false if its lastAppendDateTime is older than the expireDateTime.
        /// </summary>
        /// <param name="objectType">Type of the groing object.</param>
        /// <param name="expiredDateTime">The expired date time.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void ExpireGrowingObjects(string objectType, DateTime expiredDateTime)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the URI for the specified data object.
        /// </summary>
        /// <param name="instance">The data object.</param>
        /// <returns>
        /// The URI representing the data object.
        /// </returns>
        protected override EtpUri GetUri(DbGrowingObject instance)
        {
            return new EtpUri(instance.Uri);
        }

        /// <summary>
        /// Gets the entity filter for the specified URI.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="idPropertyName">Name of the identifier property.</param>
        /// <returns>
        /// The entity filter.
        /// </returns>
        protected override FilterDefinition<TObject> GetEntityFilter<TObject>(EtpUri uri, string idPropertyName)
        {
            return Builders<TObject>.Filter.Eq(IdPropertyName, uri.ToString());
        }
    }
}
