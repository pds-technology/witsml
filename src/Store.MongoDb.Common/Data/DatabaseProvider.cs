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
using System.ComponentModel.Composition;
using System.Configuration;
using MongoDB.Driver;
using PDS.WITSMLstudio.Store.MongoDb.Common;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Provides access to a MongoDb data store.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.IDatabaseProvider" />
    [Export(typeof(IDatabaseProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DatabaseProvider : IDatabaseProvider
    {
        internal static readonly string DefaultConnectionString = Settings.Default.DefaultConnectionString;
        internal static readonly string DefaultDatabaseName = Settings.Default.DefaultDatabaseName;

        private readonly Lazy<IMongoClient> _client;
        private string _connectionString;
        private string _databaseName;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseProvider" /> class.
        /// </summary>
        /// <param name="mappers">The MongoDb class mappers.</param>
        [ImportingConstructor]
        public DatabaseProvider([ImportMany] IEnumerable<IMongoDbClassMapper> mappers) : this(mappers, GetConnectionString())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseProvider"/> class.
        /// </summary>
        /// <param name="mappers">The MongoDb class mappers.</param>
        /// <param name="connectionString">The connection string.</param>
        internal DatabaseProvider(IEnumerable<IMongoDbClassMapper> mappers, string connectionString)
        {
            MongoDefaults.MaxConnectionIdleTime = TimeSpan.FromMinutes(1);
            _client = new Lazy<IMongoClient>(CreateMongoClient);

            SetConnectionString(connectionString);

            foreach (var mapper in mappers)
                mapper.Register();
        }

        /// <summary>
        /// Gets the MongoDb client interface.
        /// </summary>
        public IMongoClient Client => _client.Value;

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public string ConnectionString => _connectionString;

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        public string DatabaseName => _databaseName;

        /// <summary>
        /// Gets the MongoDb database interface.
        /// </summary>
        /// <returns>The database interface.</returns>
        public IMongoDatabase GetDatabase()
        {
            return Client.GetDatabase(_databaseName);
        }

        /// <summary>
        /// Sets the Mongo database connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public void SetConnectionString(string connectionString)
        {
            if (_client.IsValueCreated) return;

            _connectionString = connectionString;
            _databaseName = GetDatabaseName(_connectionString);
        }

        /// <summary>
        /// Creates the MongoDb client instance.
        /// </summary>
        /// <returns>The client interface.</returns>
        private IMongoClient CreateMongoClient()
        {
            return new MongoClient(_connectionString);
        }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The database name.</returns>
        private string GetDatabaseName(string connectionString)
        {
            var url = MongoUrl.Create(connectionString);

            return string.IsNullOrWhiteSpace(url?.DatabaseName)
                ? DefaultDatabaseName
                : url.DatabaseName;
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <returns>The connection string.</returns>
        private static string GetConnectionString()
        {
            var settings = ConfigurationManager.ConnectionStrings["MongoDbConnection"];
            return settings == null ? DefaultConnectionString : settings.ConnectionString;
        }
    }
}
