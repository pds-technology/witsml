using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using log4net;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace PDS.Witsml.Server.Data
{
    public abstract class MongoDbDataAdapter<T> : WitsmlDataAdapter<T>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MongoDbDataAdapter<T>));

        public MongoDbDataAdapter(IDatabaseProvider databaseProvider)
        {
            DatabaseProvider = databaseProvider;
        }

        protected IDatabaseProvider DatabaseProvider { get; private set; }

        public T GetEntity(string uid, string dbCollectionName)
        {
            try
            {
                _log.DebugFormat("Query WITSML object: {0}; uid: {1}", dbCollectionName, uid);
                var entities = new List<T>();
                var database = DatabaseProvider.GetDatabase();
                var collection = database.GetCollection<T>(dbCollectionName);

                // Default to return all entities
                var query = collection.AsQueryable()
                    .Where(string.Format("Uid = \"{0}\"", uid));

                return query.FirstOrDefault();
            }
            catch (MongoQueryException ex)
            {
                _log.ErrorFormat("Error querying {0}{1}{2}", dbCollectionName, Environment.NewLine, ex.Message);
                throw;
            }
        }

        protected List<T> QueryEntities(WitsmlQueryParser parser, string dbCollectionName, List<string> names)
        {
            var entities = new List<T>();
            var database = DatabaseProvider.GetDatabase();
            var collection = database.GetCollection<T>(dbCollectionName);

            // Default to return all entities
            var query = collection.AsQueryable();

            // Find a unique entity by Uid if one was provided
            var uid = parser.Attribute("uid");
            if (!string.IsNullOrEmpty(uid))
            {
                query = (IMongoQueryable<T>)query.Where(string.Format("Uid = \"{0}\"", uid));
            }
            else
            {
                //... else, filter by unique name list if values for 
                //... each name can be parsed from the Witsml query.
                query = (IMongoQueryable<T>)FilterQuery(parser, query, names);
                entities = query.ToList();
            }
            entities = query.ToList();

            return entities;
        }

        protected void CreateEntity(T entity, string dbCollectionName)
        {
            if (entity != null)
            {
                try
                {
                    _log.DebugFormat("Insert WITSML object: {0}", dbCollectionName);
                    var database = DatabaseProvider.GetDatabase();
                    var collection = database.GetCollection<T>(dbCollectionName);

                    collection.InsertOne(entity);
                }
                catch (MongoWriteException ex)
                {
                    _log.ErrorFormat("Error inserting {0}{1}{2}", dbCollectionName, Environment.NewLine, ex.Message);
                    throw;
                }
            }
        }

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
