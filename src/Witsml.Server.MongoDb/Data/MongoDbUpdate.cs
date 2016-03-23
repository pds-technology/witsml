using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using log4net;
using MongoDB.Driver;

namespace PDS.Witsml.Server.Data
{
    public class MongoDbUpdate<T>
    {
        private readonly IMongoCollection<T> _collection;
        private readonly WitsmlQueryParser _parser;
        private readonly string DefaultIdField = "Uid";

        public MongoDbUpdate(IMongoCollection<T> collection, WitsmlQueryParser parser, List<string> fields, List<string> ignored = null)
        {
            Logger = LogManager.GetLogger(GetType());

            _collection = collection;
            _parser = parser;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILog Logger { get; private set; }

        public void Update()
        {
            var element = _parser.Element();
        }

        public void Update(Dictionary<string, T> replacements, string field = null)
        {
            foreach (var key in replacements.Keys)
            {
                var filter = BuildIdFilter(field ?? DefaultIdField, key);
                _collection.ReplaceOne(filter, replacements[key]);
            }
        }

        private FilterDefinition<T> BuildIdFilter(XElement element)
        {
            return null;
        }

        private FilterDefinition<T> BuildIdFilter(string field, string value)
        {
            return Builders<T>.Filter.Eq(field, value); ;
        }

        private UpdateDefinition<T> BuildUpdate(XElement element)
        {
            return null;
        }
    }
}
