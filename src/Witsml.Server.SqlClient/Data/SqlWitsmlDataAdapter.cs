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

using System.Collections.Generic;
using System.Data.SqlClient;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// SQL data adapter that encapsulates CRUD functionality for WITSML objects.
    /// </summary>
    /// <typeparam name="T">Type of the data object.</typeparam>
    /// <seealso cref="PDS.Witsml.Server.Data.WitsmlDataAdapter{T}" />
    public abstract class SqlWitsmlDataAdapter<T> : WitsmlDataAdapter<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlWitsmlDataAdapter{T}" /> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="objectName">The data object name.</param>
        protected SqlWitsmlDataAdapter(IDatabaseProvider databaseProvider, ObjectName objectName)
        {
            DatabaseProvider = databaseProvider;
            ObjectName = objectName;
        }

        /// <summary>
        /// Gets the database provider used for accessing SQL Server.
        /// </summary>
        /// <value>The database provider.</value>
        protected IDatabaseProvider DatabaseProvider { get; }

        /// <summary>
        /// Gets the type of the data object.
        /// </summary>
        /// <value>The type of the data object.</value>
        protected ObjectName ObjectName { get; }

        /// <summary>
        /// Retrieves data objects from the data store using the specified parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <returns>
        /// A collection of data objects retrieved from the data store.
        /// </returns>
        public override List<T> Query(WitsmlQueryParser parser)
        {
            return QueryEntities(parser);
        }

        /// <summary>
        /// Queries the data store using the specified <see cref="WitsmlQueryParser"/>.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <returns>The query results collection.</returns>
        protected virtual List<T> QueryEntities(WitsmlQueryParser parser)
        {
            return QueryEntities(parser, ObjectName.Name);
        }

        /// <summary>
        /// Queries the data store using the specified <see cref="WitsmlQueryParser" />.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <param name="objectMappingKey">The object mapping key.</param>
        /// <returns>The query results collection.</returns>
        /// <exception cref="WitsmlException"></exception>
        protected virtual List<T> QueryEntities(WitsmlQueryParser parser, string objectMappingKey)
        {
            var mapping = DatabaseProvider.SchemaMapper.Schema.Mappings[objectMappingKey];

            if (OptionsIn.RequestObjectSelectionCapability.True.Equals(parser.RequestObjectSelectionCapability()))
            {
                Logger.DebugFormat("Requesting {0} query template.", mapping.Table);
                var template = CreateQueryTemplate();
                return template.AsList();
            }

            var returnElements = parser.ReturnElements();
            Logger.DebugFormat("Querying with return elements '{0}'", returnElements);

            try
            {
                var fields = GetProjectionPropertyNames(parser);
                var ignored = GetIgnoredElementNamesForQuery(parser);

                using (var db = DatabaseProvider.GetDatabase())
                {
                    Logger.DebugFormat("Querying {0} data table.", mapping.Table);
                    var query = new SqlQuery<T>(db, mapping, parser, fields, ignored);
                    return query.Execute();
                }
            }
            catch (SqlException ex)
            {
                Logger.ErrorFormat("Error querying {0} data table: {1}", mapping.Table, ex);
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, ex);
            }
        }
    }
}
