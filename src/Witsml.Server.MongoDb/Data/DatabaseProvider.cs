using System;
using System.ComponentModel.Composition;
using System.Configuration;
using MongoDB.Driver;
using PDS.Server.MongoDb;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Provides access to a MongoDb data store.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.IDatabaseProvider" />
    [Export(typeof(IDatabaseProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DatabaseProvider : IDatabaseProvider
    {
        internal static readonly string DefaultConnectionString = Settings.Default.DefaultConnectionString;
        internal static readonly string DefaultDatabaseName = Settings.Default.DefaultDatabaseName;
        private readonly Lazy<IMongoClient> _client;
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseProvider"/> class.
        /// </summary>
        /// <param name="mapper">The MongoDb class mapper.</param>
        [ImportingConstructor]
        public DatabaseProvider(MongoDbClassMapper mapper)
        {
            _client = new Lazy<IMongoClient>(CreateMongoClient);
            _connectionString = GetConnectionString();
            mapper.Register();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseProvider"/> class.
        /// </summary>
        /// <param name="mapper">The MongoDb class mapper.</param>
        /// <param name="connectionString">The connection string.</param>
        internal DatabaseProvider(MongoDbClassMapper mapper, string connectionString) : this(mapper)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Gets the MongoDb client interface.
        /// </summary>
        /// <value>The client interface.</value>
        public IMongoClient Client
        {
            get { return _client.Value; }
        }

        /// <summary>
        /// Gets the MongoDb database interface.
        /// </summary>
        /// <returns>The database interface.</returns>
        public IMongoDatabase GetDatabase()
        {
            return Client.GetDatabase(DefaultDatabaseName);
        }

        /// <summary>
        /// Creates the MongoDb client instance.
        /// </summary>
        /// <returns>The client interface.</returns>
        private IMongoClient CreateMongoClient()
        {
            MongoDefaults.MaxConnectionIdleTime = TimeSpan.FromMinutes(1);
            return new MongoClient(_connectionString);
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <returns>The connection string.</returns>
        private string GetConnectionString()
        {
            var settings = ConfigurationManager.ConnectionStrings["MongoDbConnection"];
            return settings == null ? DefaultConnectionString : settings.ConnectionString;
        }
    }
}
