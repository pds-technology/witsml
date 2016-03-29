using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Xml.Linq;
using Energistics.DataAccess;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using PDS.Framework;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// MongoDb data adapter that encapsulates CRUD functionality for WITSML objects.
    /// </summary>
    /// <typeparam name="T">Type of the object</typeparam>
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
        public MongoDbDataAdapter(IDatabaseProvider databaseProvider, string dbCollectionName, string idPropertyName = ObjectTypes.Uid, string namePropertyName = ObjectTypes.NameProperty)
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
        /// Parses the specified XML string.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <returns>An instance of <see cref="!:T" />.</returns>
        public override T Parse(WitsmlQueryParser parser)
        {
            Logger.DebugFormat("Validating {0} input template.", DbCollectionName);
            var inputValidator = new MongoDbQuery<T>(GetCollection(), parser, null);
            inputValidator.Validate();

            return Parse(parser.Context.Xml);
        }

        /// <summary>
        /// Gets a data object by the specified UUID.
        /// </summary>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <returns>The data object instance.</returns>
        public override T Get(DataObjectId dataObjectId)
        {
            return GetEntity(dataObjectId);
        }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <returns>A WITSML result.</returns>
        public override WitsmlResult Delete(DataObjectId dataObjectId)
        {
            DeleteEntity(dataObjectId);
            return new WitsmlResult(ErrorCodes.Success);
        }

        /// <summary>
        /// Determines whether the entity exists in the data store.
        /// </summary>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <returns>true if the entity exists; otherwise, false</returns>
        public override bool Exists(DataObjectId dataObjectId)
        {
            return Exists<T>(dataObjectId, DbCollectionName);
        }

        /// <summary>
        /// Determines whether the entity exists in the data store.
        /// </summary>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <param name="dbCollectionName">The name of the database collection.</param>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <returns>true if the entity exists; otherwise, false</returns>
        protected bool Exists<TObject>(DataObjectId dataObjectId, string dbCollectionName)
        {
            try
            {
                return GetEntityById<TObject>(dataObjectId, dbCollectionName) != null;
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
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <returns>The object represented by the UID.</returns>
        protected T GetEntity(DataObjectId dataObjectId)
        {
            return GetEntity<T>(dataObjectId, DbCollectionName);
        }

        /// <summary>
        /// Gets an object from the data store by uid
        /// </summary>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <param name="dbCollectionName">The naame of the database collection.</param>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <returns>The entity represented by the indentifier.</returns>
        protected TObject GetEntity<TObject>(DataObjectId dataObjectId, string dbCollectionName)
        {
            try
            {
                Logger.DebugFormat("Querying {0} MongoDb collection; uid: {1}", dbCollectionName, dataObjectId);
                return GetEntityById<TObject>(dataObjectId, dbCollectionName);
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error querying {0} MongoDb collection:{1}{2}", dbCollectionName, Environment.NewLine, ex);
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Gets the entity by it's data object identifier.
        /// </summary>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <returns>The entity represented by the indentifier.</returns>
        protected T GetEntityById(DataObjectId dataObjectId)
        {
            return GetEntityById<T>(dataObjectId, DbCollectionName);
        }

        /// <summary>
        /// Gets the entity by it's data object identifier.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <param name="dbCollectionName">Name of the database collection.</param>
        /// <returns></returns>
        protected TObject GetEntityById<TObject>(DataObjectId dataObjectId, string dbCollectionName)
        {
            var filter = GetEntityFilter<TObject>(dataObjectId);

            return GetCollection<TObject>(dbCollectionName)
                .Find(filter)
                .FirstOrDefault();
        }

        protected List<T> GetEntities(IEnumerable<DataObjectId> dataObjectIds)
        {
            return GetEntities<T>(dataObjectIds, DbCollectionName);
        }

        protected List<TObject> GetEntities<TObject>(IEnumerable<DataObjectId> dataObjectIds, string dbCollectionName)
        {
            var filters = dataObjectIds.Select(x => GetEntityFilter<TObject>(x));

            return GetCollection<TObject>(dbCollectionName)
                .Find(Builders<TObject>.Filter.Or(filters))
                .ToList();
        }

        /// <summary>
        /// Queries the data store with Mongo Bson filter and projection.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <returns>The query results collection.</returns>
        protected List<T> QueryEntities(WitsmlQueryParser parser, List<string> fields, List<string> ignored = null)
        {
            try
            {
                if (OptionsIn.RequestObjectSelectionCapability.True.Equals(parser.RequestObjectSelectionCapability()))
                {
                    Logger.DebugFormat("Requesting {0} query template.", DbCollectionName);
                    var queryTemplate = CreateQueryTemplate();
                    return queryTemplate.AsList();
                }

                Logger.DebugFormat("Querying {0} MongoDb collection.", DbCollectionName);
                var query = new MongoDbQuery<T>(GetCollection(), parser, fields, ignored);
                return query.Execute();
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error querying {0} MongoDb collection:{1}{2}", DbCollectionName, Environment.NewLine, ex);
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
        /// <param name="entity">The object to be updated.</param>
        /// <param name="parser">The WITSML query parser.</param>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <param name="ignored">The list of ignored elements.</param>
        protected void UpdateEntity(T entity, WitsmlQueryParser parser, DataObjectId dataObjectId, string[] ignored = null)
        {
            UpdateEntity<T>(entity, DbCollectionName, parser, dataObjectId, ignored);
        }

        /// <summary>
        /// Updates an object in the data store.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="entity">The object to be updated.</param>
        /// <param name="dbCollectionName">The name of the database collection.</param>
        /// <param name="parser">The WITSML query parser.</param>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <param name="ignored">The list of ignored elements.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void UpdateEntity<TObject>(TObject entity, string dbCollectionName, WitsmlQueryParser parser, DataObjectId dataObjectId, string[] ignored = null)
        {
            try
            {
                Logger.DebugFormat("Updating {0} MongoDb collection", dbCollectionName);

                var update = new MongoDbUpdate<TObject>(GetCollection<TObject>(dbCollectionName), parser, IdPropertyName, ignored);
                update.Update(entity, dataObjectId);
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
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void DeleteEntity(DataObjectId dataObjectId)
        {
            DeleteEntity<T>(dataObjectId, DbCollectionName);
        }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <typeparam name="TObject">The type of data object.</typeparam>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <param name="dbCollectionName">The name of the database collection.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void DeleteEntity<TObject>(DataObjectId dataObjectId, string dbCollectionName)
        {
            try
            {
                Logger.DebugFormat("Deleting from {0} MongoDb collection", dbCollectionName);

                var collection = GetCollection<TObject>(dbCollectionName);
                var filter = GetEntityFilter<TObject>(dataObjectId);
                var result = collection.DeleteOne(filter);
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error deleting from {0} MongoDb collection:{1}{2}", dbCollectionName, Environment.NewLine, ex);
                throw new WitsmlException(ErrorCodes.ErrorDeletingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Creates a filter that can be used to find the unique entity represented by the specified <see cref="DataObjectId"/>.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <returns>The filter definition instance.</returns>
        protected FilterDefinition<TObject> GetEntityFilter<TObject>(DataObjectId dataObjectId)
        {
            var builder = Builders<TObject>.Filter;
            var filters = new List<FilterDefinition<TObject>>();

            filters.Add(builder.EqIgnoreCase(IdPropertyName, dataObjectId.Uid));

            if (dataObjectId is WellObjectId)
            {
                filters.Add(builder.EqIgnoreCase("UidWell", ((WellObjectId)dataObjectId).UidWell));
            }
            if (dataObjectId is WellboreObjectId)
            {
                filters.Add(builder.EqIgnoreCase("UidWellbore", ((WellboreObjectId)dataObjectId).UidWellbore));
            }

            return builder.And(filters);
        }

        /// <summary>
        /// Initializes a new UID value if one was not supplied.
        /// </summary>
        /// <param name="uid">The supplied UID (default value null).</param>
        /// <returns>The supplied UID if not null; otherwise, a generated UID.</returns>
        protected string NewUid(string uid = null)
        {
            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString();
            }

            return uid;
        }
    }
}
