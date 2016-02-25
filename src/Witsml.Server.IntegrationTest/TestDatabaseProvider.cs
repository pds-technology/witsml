using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using PDS.Witsml.Server.Data;

namespace PDS.Witsml.Server
{
    public class TestDatabaseProvider : IDatabaseProvider
    {
        internal static readonly string DefaultDatabaseName = "WitsmlStore";
        private Lazy<IMongoClient> _client;
        private string _connection;

        public TestDatabaseProvider(MongoDbClassMapper mapper, string connection)
        {
            _connection = connection;
            _client = new Lazy<IMongoClient>(CreateMongoClient);
            mapper.Register();
        }

        public IMongoClient Client
        {
            get { return _client.Value; }
        }

        public IMongoDatabase GetDatabase()
        {
            return Client.GetDatabase(DefaultDatabaseName);
        }

        private IMongoClient CreateMongoClient()
        {
            return new MongoClient(_connection);
        }
    }
}
