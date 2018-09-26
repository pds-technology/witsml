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

using MongoDB.Driver;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Provides access to a MongoDb data store.
    /// </summary>
    public interface IDatabaseProvider
    {
        /// <summary>
        /// Gets the MongoDb client interface.
        /// </summary>
        /// <value>The client interface.</value>
        IMongoClient Client { get; }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        string ConnectionString{ get; }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        /// <value>
        /// The name of the database.
        /// </value>
        string DatabaseName{ get; }

        /// <summary>
        /// Gets the MongoDb database interface.
        /// </summary>
        /// <returns>The database interface.</returns>
        IMongoDatabase GetDatabase();

        /// <summary>
        /// Sets the Mongo database connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        void SetConnectionString(string connectionString);
    }
}
