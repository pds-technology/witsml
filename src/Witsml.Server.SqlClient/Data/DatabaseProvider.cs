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

using System.ComponentModel.Composition;
using PetaPoco;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Provides a connection to a SQL database.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.IDatabaseProvider" />
    [Export(typeof(IDatabaseProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DatabaseProvider : IDatabaseProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseProvider" /> class.
        /// </summary>
        /// <param name="mapper">The SQL schema mapper.</param>
        [ImportingConstructor]
        public DatabaseProvider(SqlSchemaMapper mapper)
        {
            SchemaMapper = mapper;
            SchemaMapper.Configure();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseProvider" /> class.
        /// </summary>
        /// <param name="mapper">The SQL schema mapper.</param>
        /// <param name="connectionString">The connection string.</param>
        internal DatabaseProvider(SqlSchemaMapper mapper, string connectionString) : this(mapper)
        {
            SchemaMapper.Schema.Database.ConnectionString = connectionString;
        }

        /// <summary>
        /// Gets the SQL schema mapper.
        /// </summary>
        /// <value>The SQL schema mapper.</value>
        public SqlSchemaMapper SchemaMapper { get; }

        /// <summary>
        /// Gets a reference to a new <see cref="IDatabase" /> instance.
        /// </summary>
        /// <returns>A <see cref="IDatabase" /> instance.</returns>
        public IDatabase GetDatabase()
        {
            return new Database(
                SchemaMapper.Schema.Database.ConnectionString,
                SchemaMapper.Schema.Database.ProviderName);
        }
    }
}
