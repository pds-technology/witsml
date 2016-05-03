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
using System.ComponentModel.Composition;
using System.Configuration;
using Energistics.Datatypes;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Server.Data.Transactions;
using PDS.Witsml.Server.MongoDb;

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

        private readonly IContainer _container;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseProvider"/> class.
        /// </summary>
        /// <param name="mapper">The MongoDb class mapper.</param>
        [ImportingConstructor]
        public DatabaseProvider(IContainer container, MongoDbClassMapper mapper)
        {
            _container = container;
            _client = new Lazy<IMongoClient>(CreateMongoClient);
            _connectionString = GetConnectionString();
            mapper.Register();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseProvider"/> class.
        /// </summary>
        /// <param name="mapper">The MongoDb class mapper.</param>
        /// <param name="connectionString">The connection string.</param>
        internal DatabaseProvider(IContainer container, MongoDbClassMapper mapper, string connectionString) : this(container, mapper)
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
        /// Creates a transaction.
        /// </summary>
        /// <returns>The transaction</returns>
        public MongoTransaction BeginTransaction(EtpUri? uri = null)
        {
            var transaction = _container.Resolve<MongoTransaction>();

            if (uri.HasValue)
            {
                transaction.Wait(uri.Value);
            }

            return transaction;
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
