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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using PDS.Witsml.Server.Data.Transactions;

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
        /// Retrieves data objects from the data store using the specified parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <returns>
        /// A collection of data objects retrieved from the data store.
        /// </returns>
        public override List<T> Query(WitsmlQueryParser parser)
        {
            return QueryEntities(parser);
        }

        /// <summary>
        /// Adds a data object to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be added.</param>
        public override void Add(WitsmlQueryParser parser, T dataObject)
        {
            using (var transaction = DatabaseProvider.BeginTransaction())
            {
                InsertEntity(dataObject, transaction);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be updated.</param>
        public override void Update(WitsmlQueryParser parser, T dataObject)
        {
            using (var transaction = DatabaseProvider.BeginTransaction())
            {
                var uri = GetUri(dataObject);
                UpdateEntity(parser, uri, transaction);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Deletes or partially updates the specified object by uid.
        /// </summary>
        /// <param name="parser">The query parser that specifies the object.</param>
        public override void Delete(WitsmlQueryParser parser)
        {
            var uri = parser.GetUri<T>();
            Delete(uri);
        }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        public override void Delete(EtpUri uri)
        {
            using (var transaction = DatabaseProvider.BeginTransaction())
            {
                DeleteEntity(uri, transaction);
                transaction.Commit();
            }
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

                var fields = GetProjectionPropertyNames(parser);
                var ignored = GetIgnoredElementNamesForQuery(parser);

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
        /// <param name="transaction">The transaction.</param>
        protected void InsertEntity(T entity, MongoTransaction transaction = null)
        {
            InsertEntity(entity, DbCollectionName, transaction);
        }

        /// <summary>
        /// Inserts an object into the data store.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="entity">The object to be inserted.</param>
        /// <param name="dbCollectionName">The name of the database collection.</param>
        /// <param name="transaction">The transaction.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void InsertEntity<TObject>(TObject entity, string dbCollectionName, MongoTransaction transaction = null)
        {
            try
            {
                Logger.DebugFormat("Inserting into {0} MongoDb collection.", dbCollectionName);

                var collection = GetCollection<TObject>(dbCollectionName);
                collection.InsertOne(entity);
                
                if (transaction != null)
                {
                    var document = MongoDbUtility.GetDocumentId(entity);
                    transaction.Attach(MongoDbAction.Add, dbCollectionName, document);
                    transaction.Save();
                }               
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
        /// <param name="transaction">The transaction.</param>
        protected void UpdateEntity(WitsmlQueryParser parser, EtpUri uri, MongoTransaction transaction = null)
        {
            UpdateEntity<T>(DbCollectionName, parser, uri, transaction);
        }

        /// <summary>
        /// Updates an object in the data store.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="dbCollectionName">The name of the database collection.</param>
        /// <param name="parser">The WITSML query parser.</param>
        /// <param name="uri">The data object URI.</param>
        /// <param name="transaction">The transaction.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void UpdateEntity<TObject>(string dbCollectionName, WitsmlQueryParser parser, EtpUri uri, MongoTransaction transaction = null)
        {
            try
            {
                Logger.DebugFormat("Updating {0} MongoDb collection", dbCollectionName);

                var collection = GetCollection<TObject>(dbCollectionName);
                var current = GetEntity<TObject>(uri, dbCollectionName);
                var updates = MongoDbUtility.CreateUpdateFields<TObject>();
                var ignores = MongoDbUtility.CreateIgnoreFields<TObject>(GetIgnoredElementNamesForUpdate(parser));

                var update = new MongoDbUpdate<TObject>(collection, parser, IdPropertyName, ignores);
                update.Update(current, uri, updates);

                if (transaction != null)
                {
                    transaction.Attach(MongoDbAction.Update, dbCollectionName, current.ToBsonDocument());
                    transaction.Save();
                }                  
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
        /// <param name="transaction">The transaction.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void DeleteEntity(EtpUri uri, MongoTransaction transaction = null)
        {
            DeleteEntity<T>(uri, DbCollectionName, transaction);
        }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <typeparam name="TObject">The type of data object.</typeparam>
        /// <param name="uri">The data object URI.</param>
        /// <param name="dbCollectionName">The name of the database collection.</param>
        /// <param name="transaction">The transaction.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void DeleteEntity<TObject>(EtpUri uri, string dbCollectionName, MongoTransaction transaction = null)
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
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of property names.</returns>
        protected override List<string> GetProjectionPropertyNames(WitsmlQueryParser parser)
        {
            return OptionsIn.ReturnElements.IdOnly.Equals(parser.ReturnElements())
                ? new List<string> { IdPropertyName, NamePropertyName }
                : null;
        }
    }
}
