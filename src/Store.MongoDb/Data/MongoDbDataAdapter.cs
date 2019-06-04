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
using Witsml141 = Energistics.DataAccess.WITSML141;
using Energistics.Etp.Common.Datatypes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.ChangeLogs;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data.ChangeLogs;
using PDS.WITSMLstudio.Store.Data.Transactions;
using PDS.WITSMLstudio.Store.MongoDb;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// MongoDb data adapter that encapsulates CRUD functionality for WITSML objects.
    /// </summary>
    /// <typeparam name="T">Type of the data object</typeparam>
    /// <seealso cref="Data.WitsmlDataAdapter{T}" />
    public abstract class MongoDbDataAdapter<T> : WitsmlDataAdapter<T>
    {
        private static readonly bool _isDbAuditHistoryEnabled = Settings.Default.IsDbAuditHistoryEnabled;
        private DbAuditHistoryDataAdapter _auditHistoryAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbDataAdapter{T}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="dbCollectionName">The database collection name.</param>
        /// <param name="idPropertyName">The name of the identifier property.</param>
        /// <param name="namePropertyName">The name of the object name property</param>
        protected MongoDbDataAdapter(IContainer container, IDatabaseProvider databaseProvider, string dbCollectionName, string idPropertyName = ObjectTypes.Uid, string namePropertyName = ObjectTypes.NameProperty)
            : base(container)
        {
            DatabaseProvider = databaseProvider;
            DbCollectionName = dbCollectionName;
            IdPropertyName = idPropertyName;
            NamePropertyName = namePropertyName;
        }

        /// <summary>
        /// Gets the server sort order.
        /// </summary>
        public override string ServerSortOrder => ObjectTypes.Uid.EqualsIgnoreCase(IdPropertyName) ? ObjectTypes.NameProperty : ObjectTypes.DefaultSortOrder;

        /// <summary>
        /// Gets the database provider used for accessing MongoDb.
        /// </summary>
        /// <value>The database provider.</value>
        protected IDatabaseProvider DatabaseProvider { get; }

        /// <summary>
        /// Gets the audit history adapter.
        /// </summary>
        /// <value>The audit history adapter.</value>
        protected DbAuditHistoryDataAdapter AuditHistoryAdapter
        {
            get { return _auditHistoryAdapter ?? (_auditHistoryAdapter = Container.Resolve<IWitsmlDataAdapter<DbAuditHistory>>() as DbAuditHistoryDataAdapter); }            
        }

        /// <summary>
        /// Gets a reference to the current <see cref="MongoTransaction"/> instance.
        /// </summary>
        protected MongoTransaction Transaction => WitsmlOperationContext.Current.Transaction as MongoTransaction;

        /// <summary>
        /// Gets the database collection name for the data object.
        /// </summary>
        /// <value>The database collection name.</value>
        protected string DbCollectionName { get; }

        /// <summary>
        /// Gets the name of the identifier property.
        /// </summary>
        /// <value>The name of the identifier property.</value>
        protected string IdPropertyName { get; }

        /// <summary>
        /// Gets the name of the Name property.
        /// </summary>
        /// <value>The name of the Name property.</value>
        protected string NamePropertyName { get; }

        /// <summary>
        /// Gets a data object by the specified UUID.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="fields">The requested fields.</param>
        /// <returns>The data object instance.</returns>
        public override T Get(EtpUri uri, params string[] fields)
        {
            return GetEntity(uri, fields);
        }

        /// <summary>
        /// Retrieves data objects from the data store using the specified parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <param name="context">The response context.</param>
        /// <returns>
        /// A collection of data objects retrieved from the data store.
        /// </returns>
        public override List<T> Query(WitsmlQueryParser parser, ResponseContext context)
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
            using (var transaction = GetTransaction())
            {
                var uri = GetUri(dataObject);
                transaction.SetContext(uri);

                InsertEntity(dataObject);
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
            var uri = GetUri(dataObject);
            using (var transaction = GetTransaction())
            {
                transaction.SetContext(uri);
                UpdateEntity(parser, uri);
                ValidateUpdatedEntity(Functions.UpdateInStore, uri);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Replaces a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be replaced.</param>
        public override void Replace(WitsmlQueryParser parser, T dataObject)
        {
            var uri = GetUri(dataObject);
            using (var transaction = GetTransaction())
            {
                transaction.SetContext(uri);
                ReplaceEntity(dataObject, uri);
                ValidateUpdatedEntity(Functions.PutObject, uri);
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

            if (parser.HasElements())
            {
                using (var transaction = GetTransaction())
                {
                    transaction.SetContext(uri);
                    PartialDeleteEntity(parser, uri);
                    transaction.Commit();
                }
            }
            else
            {
                Delete(uri);
            }
        }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        public override void Delete(EtpUri uri)
        {
            using (var transaction = GetTransaction())
            {
                transaction.SetContext(uri);

                if (WitsmlOperationContext.Current.IsCascadeDelete)
                {
                    DeleteAll(uri);
                }

                DeleteEntity(uri);
                transaction.Commit();
            }
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
            return GetEntity<TObject>(uri, dbCollectionName, IdPropertyName) != null;
        }

        /// <summary>
        /// Gets the <see cref="IMongoDatabase"/> associated with the current transaction, if available; 
        /// otherwise, a new <see cref="IMongoDatabase"/> instance is requested.
        /// </summary>
        /// <returns>An <see cref="IMongoDatabase"/> instance.</returns>
        protected IMongoDatabase GetDatabase()
        {
            return Transaction?.Database ?? DatabaseProvider.GetDatabase();
        }

        /// <summary>
        /// Gets the default collection.
        /// </summary>
        /// <returns>An <see cref="IMongoCollection{T}"/> instance.</returns>
        protected IMongoCollection<T> GetCollection()
        {
            return GetCollection<T>(DbCollectionName);
        }

        /// <summary>
        /// Gets the collection having the specified name.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="dbCollectionName">Name of the database collection.</param>
        /// <returns>An <see cref="IMongoCollection{TObject}"/> instance.</returns>
        protected IMongoCollection<TObject> GetCollection<TObject>(string dbCollectionName)
        {
            return GetDatabase().GetCollection<TObject>(dbCollectionName);
        }

        /// <summary>
        /// Gets an <see cref="IQueryable{T}"/> instance for the default collection.
        /// </summary>
        /// <returns>An executable query.</returns>
        protected IMongoQueryable<T> GetQuery()
        {
            return GetQuery<T>(DbCollectionName);
        }

        /// <summary>
        /// Gets an <see cref="IQueryable{TObject}"/> instance for the specified collection.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="dbCollectionName">Name of the database collection.</param>
        /// <returns>An executable query.</returns>
        protected IMongoQueryable<TObject> GetQuery<TObject>(string dbCollectionName)
        {
            return GetCollection<TObject>(dbCollectionName).AsQueryable();
        }

        /// <summary>
        /// Gets an object from the data store by uid
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="fields">The requested fields.</param>
        /// <returns>The object represented by the UID.</returns>
        protected T GetEntity(EtpUri uri, params string[] fields)
        {
            return GetEntity<T>(uri, DbCollectionName, fields);
        }

        /// <summary>
        /// Gets an object from the data store by uid
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="dbCollectionName">The naame of the database collection.</param>
        /// <param name="fields">The requested fields.</param>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <returns>The entity represented by the indentifier.</returns>
        protected TObject GetEntity<TObject>(EtpUri uri, string dbCollectionName, params string[] fields)
        {
            try
            {
                Logger.DebugFormat("Querying {0} MongoDb collection; {1}: {2}", dbCollectionName, IdPropertyName, uri.ObjectId);

                var filter = GetEntityFilter<TObject>(uri, IdPropertyName);

                // If the field list is specified use projection to filter the results
                if (fields.Any())
                {
                    var projection = Builders<TObject>.Projection.Include(fields.First());

                    foreach (var field in fields.Skip(1))
                        projection = projection.Include(field);

                    return GetCollection<TObject>(dbCollectionName)
                        .Find(filter)
                        .Project<TObject>(projection)
                        .Limit(1)
                        .FirstOrDefault();
                }

                // Otherwise retrieve the full document
                return GetCollection<TObject>(dbCollectionName)
                    .Find(filter)
                    .Limit(1)
                    .FirstOrDefault();
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error querying {0} MongoDb collection:{1}{2}", dbCollectionName, Environment.NewLine, ex);
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Gets the entity filter for the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>The entity filter.</returns>
        protected virtual FilterDefinition<T> GetEntityFilter(EtpUri uri)
        {
            return GetEntityFilter<T>(uri, IdPropertyName);
        }

        /// <summary>
        /// Gets the entity filter for the specified URI.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="idPropertyName">Name of the identifier property.</param>
        /// <returns>The entity filter.</returns>
        protected virtual FilterDefinition<TObject> GetEntityFilter<TObject>(EtpUri uri, string idPropertyName)
        {
            return MongoDbUtility.GetEntityFilter<TObject>(uri, idPropertyName);
        }

        /// <summary>
        /// Gets the entities having the specified URIs.
        /// </summary>
        /// <param name="uris">The uris.</param>
        /// <returns>The query results.</returns>
        protected IEnumerable<T> GetEntities(IEnumerable<EtpUri> uris)
        {
            return GetEntities<T>(uris, DbCollectionName);
        }

        /// <summary>
        /// Gets the entities having the supplied URIs found in the specified collection.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="uris">The uris.</param>
        /// <param name="dbCollectionName">Name of the database collection.</param>
        /// <returns>The query results.</returns>
        protected IEnumerable<TObject> GetEntities<TObject>(IEnumerable<EtpUri> uris, string dbCollectionName)
        {
            var list = uris.ToList();

            Logger.DebugFormat("Querying {0} MongoDb collection by URIs: {1}{2}",
                dbCollectionName,
                Environment.NewLine,
                Logger.IsDebugEnabled ? string.Join(Environment.NewLine, list) : null);

            if (!list.Any())
            {
                return GetCollection<TObject>(dbCollectionName)
                    .Find("{}")
                    .ToEnumerable();
            }

            var filters = list.Select(x => MongoDbUtility.GetEntityFilter<TObject>(x, IdPropertyName));

            return GetCollection<TObject>(dbCollectionName)
                .Find(Builders<TObject>.Filter.Or(filters))
                .ToEnumerable();
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
                var query = new MongoDbQuery<T>(Container, GetCollection(), parser, fields, ignored);
                return FilterRecurringElements(query);
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error querying {0} MongoDb collection: {1}", DbCollectionName, ex);
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Filters the recurring elements within each data object returned by the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The query results collection.</returns>
        protected virtual List<T> FilterRecurringElements(MongoDbQuery<T> query)
        {
            // NOTE: this method can be overridden to include additional context
            return query.FilterRecurringElements(query.Execute());
        }

        /// <summary>
        /// Inserts an object into the data store.
        /// </summary>
        /// <param name="entity">The object to be inserted.</param>
        protected void InsertEntity(T entity)
        {
            InsertEntity(entity, DbCollectionName, GetUri(entity));
        }

        /// <summary>
        /// Inserts an object into the data store.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="entity">The object to be inserted.</param>
        /// <param name="dbCollectionName">The name of the database collection.</param>
        /// <param name="uri">The data object URI.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void InsertEntity<TObject>(TObject entity, string dbCollectionName, EtpUri uri)
        {
            try
            {
                Logger.DebugFormat("Inserting into {0} MongoDb collection.", dbCollectionName);

                var collection = GetCollection<TObject>(dbCollectionName);
                collection.InsertOne(entity);

                var transaction = Transaction;
                transaction.Attach(MongoDbAction.Add, dbCollectionName, IdPropertyName, null, uri);
                transaction.Save();

                AuditInsert(uri);
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error inserting into {0} MongoDb collection:{1}{2}", dbCollectionName, Environment.NewLine, ex);
                throw new WitsmlException(ErrorCodes.ErrorAddingToDataStore, ex);
            }
        }

        /// <summary>
        /// Audits the insert operation.
        /// </summary>
        /// <param name="uri">The URI.</param>
        protected virtual void AuditInsert(EtpUri uri)
        {
            AuditEntity(uri, Witsml141.ReferenceData.ChangeInfoType.add);
        }

        /// <summary>
        /// Updates an object in the data store.
        /// </summary>
        /// <param name="parser">The WITSML query parser.</param>
        /// <param name="uri">The data object URI.</param>
        protected void UpdateEntity(WitsmlQueryParser parser, EtpUri uri)
        {
            UpdateEntity<T>(DbCollectionName, parser, uri);
        }

        /// <summary>
        /// Updates an object in the data store.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="dbCollectionName">The name of the database collection.</param>
        /// <param name="parser">The WITSML query parser.</param>
        /// <param name="uri">The data object URI.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void UpdateEntity<TObject>(string dbCollectionName, WitsmlQueryParser parser, EtpUri uri)
        {
            try
            {
                Logger.DebugFormat("Updating {0} MongoDb collection", dbCollectionName);

                var collection = GetCollection<TObject>(dbCollectionName);
                var current = GetEntity<TObject>(uri, dbCollectionName);
                var updates = MongoDbUtility.CreateUpdateFields<TObject>();
                var ignores = MongoDbUtility.CreateIgnoreFields<TObject>(GetIgnoredElementNamesForUpdate(parser));

                var update = new MongoDbUpdate<TObject>(Container, collection, parser, IdPropertyName, ignores);
                update.Update(current, uri, updates);

                var transaction = Transaction;
                transaction.Attach(MongoDbAction.Update, dbCollectionName, IdPropertyName, current.ToBsonDocument(), uri);
                transaction.Save();

                AuditUpdate(uri);
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error updating {0} MongoDb collection: {1}", dbCollectionName, ex);
                throw new WitsmlException(ErrorCodes.ErrorUpdatingInDataStore, ex);
            }
        }

        /// <summary>
        /// Audits the update operation.
        /// </summary>
        /// <param name="uri">The URI.</param>
        protected virtual void AuditUpdate(EtpUri uri)
        {
            AuditEntity(uri, Witsml141.ReferenceData.ChangeInfoType.update);
        }

        /// <summary>
        /// Replaces an object in the data store.
        /// </summary>
        /// <param name="entity">The object to be replaced.</param>
        /// <param name="uri">The data object URI.</param>
        /// <param name="ignoreServerProperties">if set to <c>true</c> ignores server properties.</param>
        protected void ReplaceEntity(T entity, EtpUri uri, bool ignoreServerProperties = true)
        {
            ReplaceEntity(DbCollectionName, entity, uri, ignoreServerProperties);
        }

        /// <summary>
        /// Replaces an object in the data store.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="dbCollectionName">The name of the database collection.</param>
        /// <param name="entity">The object to be replaced.</param>
        /// <param name="uri">The data object URI.</param>
        /// <param name="ignoreServerProperties">if set to <c>true</c> ignores server properties.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void ReplaceEntity<TObject>(string dbCollectionName, TObject entity, EtpUri uri, bool ignoreServerProperties = true)
        {
            try
            {
                Logger.DebugFormat("Replacing {0} MongoDb collection", dbCollectionName);

                var collection = GetCollection<TObject>(dbCollectionName);
                var current = GetEntity<TObject>(uri, dbCollectionName);
                //var updates = MongoDbUtility.CreateUpdateFields<TObject>();
                var ignores = MongoDbUtility.CreateIgnoreFields<TObject>(null, true);

                if (ignoreServerProperties)
                {
                    ignores.AddRange(GetIgnoredElementNamesForUpdate(null) ?? Enumerable.Empty<string>());
                }

                var mapper = new DataObjectMapper<TObject>(Container, null, ignores);
                mapper.Map(current, entity);

                // Update Last Change Date
                AuditHistoryAdapter.SetDateTimeLastChange(entity);

                var filter = GetEntityFilter<TObject>(uri, IdPropertyName);
                collection.ReplaceOne(filter, entity);

                var transaction = Transaction;
                transaction.Attach(MongoDbAction.Update, dbCollectionName, IdPropertyName, current.ToBsonDocument(), uri);
                transaction.Save();

                AuditUpdate(uri);
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error replacing {0} MongoDb collection: {1}", dbCollectionName, ex);
                throw new WitsmlException(ErrorCodes.ErrorReplacingInDataStore, ex);
            }
        }

        /// <summary>
        /// Merges the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="parser">The WITSML query parser.</param>
        /// <param name="mergeDelete">Indicate if it is partial delete.</param>
        protected void MergeEntity(T entity, WitsmlQueryParser parser, bool mergeDelete = false)
        {
            MergeEntity(DbCollectionName, entity, parser, mergeDelete);
        }

        /// <summary>
        /// Merges the entity.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="dbCollectionName">Name of the database collection.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="parser">The WITSML query parser.</param>
        /// <param name="mergeDelete">Indicate if it is partial delete.</param>
        private void MergeEntity<TObject>(string dbCollectionName, TObject entity, WitsmlQueryParser parser, bool mergeDelete = false)
        {
            try
            {
                Logger.Debug($"Merging {dbCollectionName} MongoDb collection");

                var collection = GetCollection<TObject>(dbCollectionName);
                var ignored = GetIgnoredElementNamesForUpdate(parser);
                var merge = new MongoDbMerge<TObject>(Container, collection, parser, IdPropertyName, ignored);
                merge.MergeDelete = mergeDelete;
                merge.Merge(entity);
            }
            catch (WitsmlException)
            {
                throw;
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error replacing {0} MongoDb collection: {1}", dbCollectionName, ex);
                throw new WitsmlException(ErrorCodes.ErrorReplacingInDataStore, ex);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Error replacing {0} MongoDb collection: {1}", dbCollectionName, ex);
                throw new WitsmlException(ErrorCodes.ErrorReplacingInDataStore, ex);
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
                var current = GetEntity<TObject>(uri, dbCollectionName);

                // Check to make sure the document exists in the database
                if (current == null) return;

                // Audit before delete operation to capture current state
                AuditDelete(uri);

                var transaction = Transaction;
                if (transaction != null)
                {
                    //var document = MongoDbUtility.GetDocumentId(current);
                    transaction.Attach(MongoDbAction.Delete, dbCollectionName, IdPropertyName, null, uri);
                    transaction.Save();
                }
                else
                {
                    var filter = MongoDbUtility.GetEntityFilter<TObject>(uri, IdPropertyName);
                    collection.DeleteOne(filter);
                }
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error deleting from {0} MongoDb collection: {1}", dbCollectionName, ex);
                throw new WitsmlException(ErrorCodes.ErrorDeletingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Audits the delete operation.
        /// </summary>
        /// <param name="uri">The URI.</param>
        protected virtual void AuditDelete(EtpUri uri)
        {
            AuditEntity(uri, Witsml141.ReferenceData.ChangeInfoType.delete);
        }

        /// <summary>
        /// Partials the delete entity.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <param name="uri">The URI.</param>
        protected void PartialDeleteEntity(WitsmlQueryParser parser, EtpUri uri)
        {
            PartialDeleteEntity<T>(DbCollectionName, parser, uri);
        }

        /// <summary>
        /// Partials the delete entity.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="dbCollectionName">Name of the database collection.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="uri">The URI.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void PartialDeleteEntity<TObject>(string dbCollectionName, WitsmlQueryParser parser, EtpUri uri)
        {
            try
            {
                Logger.DebugFormat("Partial Deleting {0} MongoDb collection", dbCollectionName);

                var collection = GetCollection<TObject>(dbCollectionName);
                var current = GetEntity<TObject>(uri, dbCollectionName);
                var updates = MongoDbUtility.CreateUpdateFields<TObject>();
                var ignores = MongoDbUtility.CreateIgnoreFields<TObject>(GetIgnoredElementNamesForUpdate(parser));

                var partialDelete = new MongoDbDelete<TObject>(Container, collection, parser, IdPropertyName, ignores);
                partialDelete.PartialDelete(current, uri, updates);

                var transaction = Transaction;
                transaction.Attach(MongoDbAction.Update, dbCollectionName, IdPropertyName, current.ToBsonDocument(), uri);
                transaction.Save();

                AuditPartialDelete(uri);
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error partial deleting {0} MongoDb collection: {1}", dbCollectionName, ex);
                throw new WitsmlException(ErrorCodes.ErrorUpdatingInDataStore, ex);
            }
        }

        /// <summary>
        /// Audits the partial delete operation.
        /// </summary>
        /// <param name="uri">The URI.</param>
        protected virtual void AuditPartialDelete(EtpUri uri)
        {
            AuditEntity(uri, Witsml141.ReferenceData.ChangeInfoType.update);
        }

        /// <summary>
        /// Audits the entity.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="changeType">Type of the change.</param>
        protected virtual void AuditEntity(EtpUri uri, Witsml141.ReferenceData.ChangeInfoType changeType)
        {
            // Ensure change log support has not been disabled
            if (!_isDbAuditHistoryEnabled) return;

            if (AuditHistoryAdapter == null || ObjectTypes.ChangeLog.Equals(uri.ObjectType)) return;

            var current = GetEntity(uri); //, GetAuditProjectionPropertyNames());
            var auditHistory = AuditHistoryAdapter.GetAuditHistory(uri, current, changeType);
            var isNewEntry = string.IsNullOrWhiteSpace(auditHistory.Uid);

            if (isNewEntry)
            {
                auditHistory.Uid = auditHistory.NewUid();
                auditHistory.Name = auditHistory.Uid;
            }

            AuditEntity(current, auditHistory, isNewEntry);
        }

        /// <summary>
        /// Audits the entity. Override this method to adjust the audit record
        /// before it is submitted to the database or to prevent the audit.
        /// </summary>
        /// <param name="entity">The changed entity.</param>
        /// <param name="auditHistory">The audit history.</param>
        /// <param name="isNewEntry">if set to <c>true</c> add a new entry.</param>
        protected virtual void AuditEntity(T entity, DbAuditHistory auditHistory, bool isNewEntry)
        {
            // Ensure change log support has not been disabled
            if (!_isDbAuditHistoryEnabled) return;

            if (isNewEntry)
            {
                AuditHistoryAdapter?.InsertEntity(auditHistory);
            }
            else
            {
                AuditHistoryAdapter?.ReplaceEntity(auditHistory, auditHistory.GetUri());
            }

            var dataObject = entity as IDataObject;
            if (dataObject != null)
            {
                var collection = dataObject.CreateCollection();
                AuditHistoryAdapter?.QueueNotification(collection, auditHistory);
            }
            else
            {
                AuditHistoryAdapter?.QueueNotification(entity, auditHistory);
            }
        }

        /// <summary>
        /// Gets a list of the property names to project during a query.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of property names.</returns>
        protected override List<string> GetProjectionPropertyNames(WitsmlQueryParser parser)
        {
            if (OptionsIn.ReturnElements.IdOnly.Equals(parser.ReturnElements()))
            {
                if (typeof(IWellboreObject).IsAssignableFrom(typeof(T)))
                    return new List<string> { IdPropertyName, NamePropertyName, "UidWell", "NameWell", "UidWellbore", "NameWellbore" };

                if (typeof(IWellObject).IsAssignableFrom(typeof(T)))
                    return new List<string> { IdPropertyName, NamePropertyName, "UidWell", "NameWell" };

                return new List<string> { IdPropertyName, NamePropertyName };
            }

            return null;
        }

        /// <summary>
        /// Gets the property names for projection of entity fields needed by DbAuditHistory.
        /// </summary>
        /// <returns></returns>
        protected virtual string[] GetAuditProjectionPropertyNames()
        {
            return new[] {"Uid", "Uuid", "UidWell", "UidWellbore", "Name", "NameWell", "NameWellbore", "CommonData", "Citation"};
        }
    }
}
