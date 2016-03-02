using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using System.Xml.Serialization;
using log4net;
using MongoDB.Bson;
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
        /// Initializes a new instance of the <see cref="MongoDbDataAdapter{T}" /> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="dbCollectionName">The database collection name.</param>
        /// <param name="idPropertyName">The name of the identifier property.</param>
        public MongoDbDataAdapter(IDatabaseProvider databaseProvider, string dbCollectionName, string idPropertyName = ObjectTypes.Uid)
        {
            DatabaseProvider = databaseProvider;
            DbCollectionName = dbCollectionName;
            IdPropertyName = idPropertyName;
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
        /// Gets a data object by the specified UUID.
        /// </summary>
        /// <param name="uuid">The UUID.</param>
        /// <returns>The data object instance.</returns>
        public override T Get(string uuid)
        {
            return GetEntity(uuid);
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
                .Where(IdPropertyName + " = @0", uid);

            return query;
        }

        /// <summary>
        /// Queries the first entity.
        /// </summary>
        /// <returns>The first entity found.</returns>
        protected T QueryFirstEntity()
        {
            return GetFirstEntity<T>(DbCollectionName);
        }

        protected TObject GetFirstEntity<TObject>(string dbCollectionName)
        {
            return GetQuery<TObject>(dbCollectionName).FirstOrDefault() ;
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
        /// Queries the data store with Mongo Bson filter.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <param name="tList">List of query for object T.</param>
        /// <returns></returns>
        protected List<T> QueryEntities(WitsmlQueryParser parser, List<T> tList)
        {
            var entities = new List<T>();
            var database = DatabaseProvider.GetDatabase();
            var collection = database.GetCollection<T>(DbCollectionName);

            foreach (var entity in tList)
            {
                var filter = BuildFilter(parser, entity);

                var results = collection.Find(filter ?? "{}").ToList();
                entities.AddRange(results.ToList());
            }

            return entities;
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

        /// <summary>
        /// Builds the query filter.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <param name="entity">The entity to be queried.</param>
        /// <returns></returns>
        private FilterDefinition<T> BuildFilter(WitsmlQueryParser parser, T entity)
        {
            var properties = GetPropertyInfo(entity);
            var filters = new List<FilterDefinition<T>>();

            foreach (var property in properties)
            {
                var filter = BuildFilterForAProperty(property, entity);
                if (filter != null)
                    filters.Add(filter);
            }

            if (filters.Count > 0)
                return Builders<T>.Filter.And(filters);
            else
                return null;
        }

        /// <summary>
        /// Builds the query filter for a property.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="obj">The object that contains the property.</param>
        /// <param name="path">The path of the property to the entity to be queried in the data store.</param>
        /// <returns></returns>
        private FilterDefinition<T> BuildFilterForAProperty(PropertyInfo propertyInfo, object obj, string path = null)
        {
            var propertyValue = propertyInfo.GetValue(obj);
            if (propertyValue == null)
                return null;

            var fieldName = propertyInfo.Name;
            if (!string.IsNullOrEmpty(path))
                fieldName = string.Format("{0}.{1}", path, fieldName);
            var properties = GetPropertyInfo(propertyValue).ToList();
            var filters = new List<FilterDefinition<T>>();

            if (properties.Count > 0)
            {
                foreach (var property in properties)
                {
                    var filter = BuildFilterForAProperty(property, propertyValue, fieldName);
                    if (filter != null)
                        filters.Add(filter);
                }
            }
            else
            {
                var propertyType = propertyInfo.PropertyType;

                if (propertyType == typeof(string))
                {
                    var strValue = propertyValue.ToString();
                    if (string.IsNullOrEmpty(strValue))
                        return null;

                    return Builders<T>.Filter.Regex(fieldName, new BsonRegularExpression("/^" + strValue + "$/i"));
                }
                else if (propertyValue is IEnumerable)
                {
                    var listFilters = new List<FilterDefinition<T>>();
                    var list = (IEnumerable)propertyValue;              
                    foreach (var item in list)
                    {
                        var itemFilters = new List<FilterDefinition<T>>();
                        var itemProperties = GetPropertyInfo(item);
                        foreach (var itemProperty in itemProperties)
                        {
                            var itemFilter = BuildFilterForAProperty(itemProperty, item, fieldName);
                            if (itemFilter != null)
                                itemFilters.Add(itemFilter);
                        }
                        if (itemFilters.Count > 0)
                            listFilters.Add(Builders<T>.Filter.And(itemFilters));
                    }
                    if (listFilters.Count > 0)
                        filters.Add(Builders<T>.Filter.Or(listFilters));
                }
                else
                {
                    return Builders<T>.Filter.Eq(fieldName, propertyValue);
                }
            }

            if (filters.Count > 0)
                return Builders<T>.Filter.And(filters);
            else
                return null;
        }

        private IEnumerable<PropertyInfo> GetPropertyInfo(object obj)
        {
            return obj.GetType().GetProperties().Where(p => p.IsDefined(typeof(XmlElementAttribute), false) || p.IsDefined(typeof(XmlAttributeAttribute), false));
        }
    }
}
