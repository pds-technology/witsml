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
using Energistics.Datatypes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// MongoDb data adapter that encapsulates CRUD functionality for WITSML objects.
    /// </summary>
    /// <typeparam name="T">Type of the data object</typeparam>
    /// <seealso cref="Data.WitsmlDataAdapter{T}" />
    public abstract class MongoDbDataAdapter<T> : WitsmlDataAdapter<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbDataAdapter{T}" /> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="dbCollectionName">The database collection name.</param>
        /// <param name="idPropertyName">The name of the identifier property.</param>
        /// <param name="namePropertyName">The name of the object name property</param>
        protected MongoDbDataAdapter(IDatabaseProvider databaseProvider, string dbCollectionName, string idPropertyName = ObjectTypes.Uid, string namePropertyName = ObjectTypes.NameProperty)
        {
            DatabaseProvider = databaseProvider;
            DbCollectionName = dbCollectionName;
            IdPropertyName = idPropertyName;
            NamePropertyName = namePropertyName;
        }

        /// <summary>
        /// Gets the database provider used for accessing MongoDb.
        /// </summary>
        /// <value>The database provider.</value>
        protected IDatabaseProvider DatabaseProvider { get; private set; }

        /// <summary>
        /// Gets the database collection name for the data object.
        /// </summary>
        /// <value>The database collection name.</value>
        protected string DbCollectionName { get; private set; }

        /// <summary>
        /// Gets the name of the identifier property.
        /// </summary>
        /// <value>The name of the identifier property.</value>
        protected string IdPropertyName { get; private set; }

        /// <summary>
        /// Gets the name of the Name property.
        /// </summary>
        /// <value>The name of the Name property.</value>
        protected string NamePropertyName { get; private set; }

        /// <summary>
        /// Gets a data object by the specified UUID.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>The data object instance.</returns>
        public override T Get(EtpUri uri)
        {
            return GetEntity(uri);
        }

        /// <summary>
        /// Adds a data object to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be added.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Add(WitsmlQueryParser parser, T dataObject)
        {
            var uri = GetUri(dataObject);
            InsertEntity(dataObject);

            return new WitsmlResult(ErrorCodes.Success, uri.ObjectId);
        }

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be updated.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Update(WitsmlQueryParser parser, T dataObject)
        {
            var uri = GetUri(dataObject);
            UpdateEntity(parser, uri);

            return new WitsmlResult(ErrorCodes.Success);
        }

        /// <summary>
        /// Deletes or partially updates the specified object by uid.
        /// </summary>
        /// <param name="parser">The query parser that specifies the object.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Delete(WitsmlQueryParser parser)
        {
            var uri = parser.GetUri<T>();
            return Delete(uri);
        }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>A WITSML result.</returns>
        public override WitsmlResult Delete(EtpUri uri)
        {
            DeleteEntity(uri);
            return new WitsmlResult(ErrorCodes.Success);
        }

        /// <summary>
        /// Validates the input template using the specified parser.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        public override void Validate(WitsmlQueryParser parser)
        {
            Logger.DebugFormat("Validating {0} input template.", DbCollectionName);

            var inputValidator = new MongoDbQuery<T>(GetCollection(), parser, null);
            inputValidator.Validate();
        }

        /// <summary>
        /// Determines whether the entity exists in the data store.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>true if the entity exists; otherwise, false</returns>
        public override bool Exists(EtpUri uri)
        {
            return Exists<T>(uri, DbCollectionName);
        }

        /// <summary>
        /// Determines whether the entity exists in the data store.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="dbCollectionName">The name of the database collection.</param>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <returns>true if the entity exists; otherwise, false</returns>
        protected bool Exists<TObject>(EtpUri uri, string dbCollectionName)
        {
            try
            {
                return GetEntity<TObject>(uri, dbCollectionName) != null;
            }
            catch (MongoException ex)
            {
                Logger.Error("Error querying " + dbCollectionName, ex);
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, ex);
            }
        }

        protected IMongoCollection<T> GetCollection()
        {
            return GetCollection<T>(DbCollectionName);
        }

        protected IMongoCollection<TObject> GetCollection<TObject>(string dbCollectionName)
        {
            var database = DatabaseProvider.GetDatabase();
            return database.GetCollection<TObject>(dbCollectionName);
        }

        protected IMongoQueryable<T> GetQuery()
        {
            return GetQuery<T>(DbCollectionName);
        }

        protected IMongoQueryable<TObject> GetQuery<TObject>(string dbCollectionName)
        {
            return GetCollection<TObject>(dbCollectionName).AsQueryable();
        }

        /// <summary>
        /// Gets an object from the data store by uid
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>The object represented by the UID.</returns>
        protected T GetEntity(EtpUri uri)
        {
            return GetEntity<T>(uri, DbCollectionName);
        }

        /// <summary>
        /// Gets an object from the data store by uid
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="dbCollectionName">The naame of the database collection.</param>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <returns>The entity represented by the indentifier.</returns>
        protected TObject GetEntity<TObject>(EtpUri uri, string dbCollectionName)
        {
            try
            {
                Logger.DebugFormat("Querying {0} MongoDb collection; uid: {1}", dbCollectionName, uri.ObjectId);

                var filter = MongoDbUtility.GetEntityFilter<TObject>(uri, IdPropertyName);

                return GetCollection<TObject>(dbCollectionName)
                    .Find(filter)
                    .FirstOrDefault();
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error querying {0} MongoDb collection:{1}{2}", dbCollectionName, Environment.NewLine, ex);
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, ex);
            }
        }

        protected List<T> GetEntities(IEnumerable<EtpUri> uris)
        {
            return GetEntities<T>(uris, DbCollectionName);
        }

        protected List<TObject> GetEntities<TObject>(IEnumerable<EtpUri> uris, string dbCollectionName)
        {
            var list = uris.ToList();

            if (!list.Any())
            {
                return new List<TObject>(0);
            }

            var filters = list.Select(x => MongoDbUtility.GetEntityFilter<TObject>(x, IdPropertyName));

            return GetCollection<TObject>(dbCollectionName)
                .Find(Builders<TObject>.Filter.Or(filters))
                .ToList();
        }

        /// <summary>
        /// Queries the data store with Mongo Bson filter and projection.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <returns>The query results collection.</returns>
        /// <exception cref="WitsmlException"></exception>
        protected List<T> QueryEntities(WitsmlQueryParser parser)
        {
            try
            {
                if (OptionsIn.RequestObjectSelectionCapability.True.Equals(parser.RequestObjectSelectionCapability()))
                {
                    Logger.DebugFormat("Requesting {0} query template.", DbCollectionName);
                    var queryTemplate = CreateQueryTemplate();
                    return queryTemplate.AsList();
                }

                var returnElements = parser.ReturnElements();
                Logger.DebugFormat("Querying with return elements '{0}'", returnElements);

                var fields = GetProjectionPropertyNames(returnElements);
                var ignored = GetIgnoredElementNames();

                Logger.DebugFormat("Querying {0} MongoDb collection.", DbCollectionName);
                var query = new MongoDbQuery<T>(GetCollection(), parser, fields, ignored);
                return query.Execute();
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error querying {0} MongoDb collection: {1}", DbCollectionName, ex);
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Inserts an object into the data store.
        /// </summary>
        /// <param name="entity">The object to be inserted.</param>
        protected void InsertEntity(T entity)
        {
            InsertEntity(entity, DbCollectionName);
        }

        /// <summary>
        /// Inserts an object into the data store.
        /// </summary>
        /// <param name="entity">The object to be inserted.</param>
        /// <param name="dbCollectionName">The name of the database collection.</param>
        /// <typeparam name="TObject">The data object type.</typeparam>
        protected void InsertEntity<TObject>(TObject entity, string dbCollectionName)
        {
            try
            {
                Logger.DebugFormat("Inserting into {0} MongoDb collection.", dbCollectionName);

                var collection = GetCollection<TObject>(dbCollectionName);
                collection.InsertOne(entity);
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error inserting into {0} MongoDb collection:{1}{2}", dbCollectionName, Environment.NewLine, ex);
                throw new WitsmlException(ErrorCodes.ErrorAddingToDataStore, ex);
            }
        }

        /// <summary>
        /// Updates an object in the data store.
        /// </summary>
        /// <param name="parser">The WITSML query parser.</param>
        /// <param name="uri">The data object URI.</param>
        /// <param name="ignored">The list of ignored elements.</param>
        protected void UpdateEntity(WitsmlQueryParser parser, EtpUri uri, string[] ignored = null)
        {
            UpdateEntity<T>(DbCollectionName, parser, uri, ignored);
        }

        /// <summary>
        /// Updates an object in the data store.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="dbCollectionName">The name of the database collection.</param>
        /// <param name="parser">The WITSML query parser.</param>
        /// <param name="uri">The data object URI.</param>
        /// <param name="ignored">The list of ignored elements.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void UpdateEntity<TObject>(string dbCollectionName, WitsmlQueryParser parser, EtpUri uri, string[] ignored = null)
        {
            try
            {
                Logger.DebugFormat("Updating {0} MongoDb collection", dbCollectionName);

                var collection = GetCollection<TObject>(dbCollectionName);
                var current = GetEntity<TObject>(uri, dbCollectionName);
                var updates = MongoDbUtility.CreateUpdateFields<TObject>();
                var ignores = MongoDbUtility.CreateIgnoreFields<TObject>(ignored);

                var update = new MongoDbUpdate<TObject>(collection, parser, IdPropertyName, ignores);
                update.Update(current, uri, updates);
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error updating {0} MongoDb collection: {1}", dbCollectionName, ex);
                throw new WitsmlException(ErrorCodes.ErrorUpdatingInDataStore, ex);
            }
        }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void DeleteEntity(EtpUri uri)
        {
            DeleteEntity<T>(uri, DbCollectionName);
        }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <typeparam name="TObject">The type of data object.</typeparam>
        /// <param name="uri">The data object URI.</param>
        /// <param name="dbCollectionName">The name of the database collection.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void DeleteEntity<TObject>(EtpUri uri, string dbCollectionName)
        {
            try
            {
                Logger.DebugFormat("Deleting from {0} MongoDb collection", dbCollectionName);

                var collection = GetCollection<TObject>(dbCollectionName);
                var filter = MongoDbUtility.GetEntityFilter<TObject>(uri, IdPropertyName);
                collection.DeleteOne(filter);
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error deleting from {0} MongoDb collection: {1}", dbCollectionName, ex);
                throw new WitsmlException(ErrorCodes.ErrorDeletingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Gets a list of the property names to project during a query.
        /// </summary>
        /// <param name="returnElements">The return elements.</param>
        /// <returns>A list of property names.</returns>
        protected virtual List<string> GetProjectionPropertyNames(string returnElements)
        {
            return OptionsIn.ReturnElements.IdOnly.Equals(returnElements)
                ? new List<string> { IdPropertyName, NamePropertyName }
                : null;
        }

        /// <summary>
        /// Gets a list of the element names to ignore during a query.
        /// </summary>
        /// <returns>A list of element names.</returns>
        protected virtual List<string> GetIgnoredElementNames()
        {
            return null;
        }
    }
}
