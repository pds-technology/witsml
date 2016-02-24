using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using log4net;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// MongoDb data adapter that encapsulates CRUD functionality for WITSML objects.
    /// </summary>
    /// <typeparam name="T">Type of the object</typeparam>
    /// <seealso cref="Data.WitsmlDataAdapter{T}" />
    public abstract class MongoDbDataAdapter<T> : WitsmlDataAdapter<T>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MongoDbDataAdapter<T>));

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbDataAdapter{T}"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="dbCollectionName">The database collection name.</param>
        public MongoDbDataAdapter(IDatabaseProvider databaseProvider, string dbCollectionName)
        {
            DatabaseProvider = databaseProvider;
            DbCollectionName = dbCollectionName;
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
        /// <param name="uid">The uid of the object.</param>
        /// <returns>The object represented by the UID.</returns>
        protected T GetEntity(string uid)
        {
            return GetEntity<T>(uid, DbCollectionName);
        }

        /// <summary>
        /// Gets an object from the data store by uid
        /// </summary>
        /// <param name="uid">The uid of the object.</param>
        /// <param name="dbCollectionName">The naame of the database collection.</param>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <returns>The object represented by the UID.</returns>
        protected TObject GetEntity<TObject>(string uid, string dbCollectionName)
        {
            try
            {
                _log.DebugFormat("Query WITSML object: {0}; uid: {1}", dbCollectionName, uid);
                return GetEntityByUidQuery<TObject>(uid, dbCollectionName).FirstOrDefault();
            }
            catch (MongoException ex)
            {
                _log.Error("Error querying " + dbCollectionName, ex);
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, ex);
            }
        }

        protected IQueryable<T> GetEntityByUidQuery(string uid)
        {
            return GetEntityByUidQuery<T>(uid, DbCollectionName);
        }

        protected IQueryable<TObject> GetEntityByUidQuery<TObject>(string uid, string dbCollectionName)
        {
            var query = GetQuery<TObject>(dbCollectionName)
                .Where("Uid = @0", uid);

            return query;
        }

        /// <summary>
        /// Determines whether the entity exists in the data store.
        /// </summary>
        /// <param name="uid">The uid.</param>
        /// <returns>true if the entity exists; otherwise, false</returns>
        public override bool Exists(string uid)
        {
            return Exists<T>(uid, DbCollectionName);
        }

        /// <summary>
        /// Determines whether the entity exists in the data store.
        /// </summary>
        /// <param name="uid">The uid.</param>
        /// <param name="dbCollectionName">The name of the database collection.</param>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <returns>true if the entity exists; otherwise, false</returns>
        protected bool Exists<TObject>(string uid, string dbCollectionName)
        {
            try
            {
                return GetEntityByUidQuery<TObject>(uid, dbCollectionName).Any();
            }
            catch (MongoException ex)
            {
                _log.Error("Error querying " + dbCollectionName, ex);
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Queries the data store.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <param name="names">The property names.</param>
        /// <returns>A collection of data objects.</returns>
        protected List<T> QueryEntities(WitsmlQueryParser parser, List<string> names)
        {
            return QueryEntities<T>(parser, DbCollectionName, names);
        }

        /// <summary>
        /// Queries the data store.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <param name="dbCollectionName">Name of the database collection.</param>
        /// <param name="names">The property names.</param>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <returns>A collection of data objects.</returns>
        protected List<TObject> QueryEntities<TObject>(WitsmlQueryParser parser, string dbCollectionName, List<string> names)
        {
            // Find a unique entity by Uid if one was provided
            var uid = parser.Attribute("uid");

            if (!string.IsNullOrEmpty(uid))
            {
                return GetEntityByUidQuery<TObject>(uid, dbCollectionName).ToList();
            }

            // Default to return all entities
            var query = GetQuery<TObject>(dbCollectionName);

            //... filter by unique name list if values for 
            //... each name can be parsed from the Witsml query.
            return FilterQuery(parser, query, names).ToList();
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
                _log.DebugFormat("Insert WITSML object: {0}", dbCollectionName);

                var collection = GetCollection<TObject>(dbCollectionName);

                collection.InsertOne(entity);
            }
            catch (MongoException ex)
            {
                _log.Error("Error inserting " + dbCollectionName, ex);
                throw new WitsmlException(ErrorCodes.ErrorAddingToDataStore, ex);
            }
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

        /// <summary>
        /// Updates the last change date/time for the object.
        /// </summary>
        /// <param name="commonData">The common data property for the object.</param>
        /// <returns>The common data with updated last change time</returns>
        protected CommonData UpdateLastChangeTime(CommonData commonData)
        {
            if (commonData == null)
            {
                commonData = new CommonData()
                {
                    DateTimeCreation = DateTime.UtcNow
                };                
            }

            commonData.DateTimeLastChange = DateTime.UtcNow;

            return commonData;
        }

        protected IQueryable<TObject> FilterQuery<TObject>(WitsmlQueryParser parser, IQueryable<TObject> query, List<string> names)
        {
            // For entity property name and its value
            var nameValues = new Dictionary<string, string>();

            // For each name pair ("<xml name>,<entity propety name>") 
            //... create a dictionary of property names and corresponding values.
            names.ForEach(n =>
            {
                // Split out the xml name and entity property names for ease of use.
                var nameAndProperty = n.Split(',');
                nameValues.Add(nameAndProperty[1], parser.PropertyValue(nameAndProperty[0]));
            });

            query = QueryByNames(query, nameValues);

            return query;
        }

        protected IQueryable<TObject> QueryByNames<TObject>(IQueryable<TObject> query, Dictionary<string, string> nameValues)
        {
            if (nameValues.Values.ToList().TrueForAll(nameValue => !string.IsNullOrEmpty(nameValue)))
            {
                nameValues.Keys.ToList().ForEach(nameKey =>
                {
                    query = query.Where(string.Format("{0} = \"{1}\"", nameKey, nameValues[nameKey]));
                });
            }

            return query;
        }
    }
}
