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
using System.Linq;
using PDS.Witsml.Server.Models;
using PetaPoco;

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
        /// Gets a value indicating whether an object mapping is available for the current data object type.
        /// </summary>
        /// <value>
        /// <c>true</c> if an object mapping is available; otherwise, <c>false</c>.
        /// </value>
        protected bool IsObjectMappingAvailable
        {
            get
            {
                var schema = DatabaseProvider.SchemaMapper.Schema;
                return schema.Version == ObjectName.Version && schema.Mappings.ContainsKey(ObjectName.Name);
            }
        }

        /// <summary>
        /// Retrieves data objects from the data store using the specified parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <returns>
        /// A collection of data objects retrieved from the data store.
        /// </returns>
        public override List<T> Query(WitsmlQueryParser parser)
        {
            if (OptionsIn.RequestObjectSelectionCapability.True.Equals(parser.RequestObjectSelectionCapability()))
            {
                return CreateQueryTemplateList();
            }

            var mapping = DatabaseProvider.SchemaMapper.Schema.Mappings[ObjectName.Name];
            var sql = Select(mapping);

            From(sql, mapping);
            Where(sql, mapping, parser);
            OrderBy(sql, mapping);

            using (var db = DatabaseProvider.GetDatabase())
            {
                return db.Fetch<T>(sql);
            }
        }

        protected virtual Sql Select(ObjectMapping mapping)
        {
            // Select
            var sql = Sql.Builder.Append("SELECT");
            var count = 0;

            foreach (var column in mapping.Columns)
            {
                sql.Append(count > 0 ? ", " : " ");
                sql.Append(Combine(column.Column, column.Alias));
                count++;
            }

            return sql;
        }

        protected virtual void From(Sql sql, ObjectMapping mapping)
        {
            // From
            sql.From(Combine(mapping.Table, mapping.Alias));

            // Join
            foreach (var join in mapping.Joins)
            {
                sql.InnerJoin(Combine(join.Table, join.Alias)).On(join.Filter);
            }
        }

        protected virtual void Where(Sql sql, ObjectMapping mapping, WitsmlQueryParser parser)
        {
            // Where
            sql.Where(mapping.Filter ?? "1=1");

            foreach (var column in mapping.Columns.Where(x => x.Selection))
            {
                var name = ToCamelCase(column.Alias);

                if (parser.HasAttribute(name))
                    sql.Append("AND " + column.Column + " = @0", parser.Attribute(name));

                else if (parser.Contains(name))
                    sql.Append("AND " + column.Column + " = @0", parser.PropertyValue(name));
            }
        }

        protected virtual void OrderBy(Sql sql, ObjectMapping mapping)
        {
            var nameColumn = mapping.Columns.FirstOrDefault(x => x.IsName);

            // OrderBy
            if (!string.IsNullOrWhiteSpace(nameColumn?.Column))
                sql.OrderBy(nameColumn.Column);
        }

        protected string Combine(string name, string alias)
        {
            return string.Concat(name, " ", alias);
        }

        protected string ToCamelCase(string alias)
        {
            if (alias.Contains('.'))
                alias = alias.Split('.').Last();

            return Inflector.Instance.Camelise(alias);
        }

        protected abstract List<T> CreateQueryTemplateList();
    }
}
