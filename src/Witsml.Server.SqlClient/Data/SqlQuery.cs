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
using PDS.Framework;
using PDS.Witsml.Server.Models;
using PetaPoco;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Composes and executes a SQL database query.
    /// </summary>
    /// <typeparam name="T">The type of data object.</typeparam>
    public class SqlQuery<T>
    {
        private readonly IDatabase _database;
        private readonly ObjectMapping _mapping;
        private readonly WitsmlQueryParser _parser;
        private readonly List<string> _fields;
        private readonly List<string> _ignored;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlQuery{T}" /> class.
        /// </summary>
        /// <param name="database">The database instance.</param>
        /// <param name="mapping">The object mapping.</param>
        /// <param name="parser">The query parser.</param>
        /// <param name="fields">The fields to project.</param>
        /// <param name="ignored">The ignored columns.</param>
        public SqlQuery(IDatabase database, ObjectMapping mapping, WitsmlQueryParser parser, List<string> fields, List<string> ignored)
        {
            _database = database;
            _mapping = mapping;
            _parser = parser;
            _fields = fields;
            _ignored = ignored ?? new List<string>();
        }

        /// <summary>
        /// Executes the requested query.
        /// </summary>
        /// <returns>The query results collection.</returns>
        public List<T> Execute()
        {
            var sql = Select(_mapping);

            From(sql, _mapping);
            Where(sql, _mapping, _parser);
            OrderBy(sql, _mapping);

            return _database.Fetch<T>(sql);
        }

        private Sql Select(ObjectMapping mapping)
        {
            // Select
            var sql = Sql.Builder.Append("SELECT");
            var count = 0;

            foreach (var column in mapping.Columns)
            {
                if (_fields != null && !_fields.ContainsIgnoreCase(column.GetName()))
                    continue;

                sql.Append(count > 0 ? ", " : " ");
                sql.Append(Combine(column.Column, column.Alias));
                count++;
            }

            return sql;
        }

        private void From(Sql sql, ObjectMapping mapping)
        {
            // From
            sql.From(Combine(mapping.Table, mapping.Alias));

            // Join
            foreach (var join in mapping.Joins)
            {
                sql.InnerJoin(Combine(join.Table, join.Alias)).On(join.Filter);
            }
        }

        private void Where(Sql sql, ObjectMapping mapping, WitsmlQueryParser parser)
        {
            // Where
            sql.Where(mapping.Filter ?? "1=1");

            foreach (var column in mapping.Columns.Where(x => x.Selection))
            {
                var name = Inflector.Instance.Camelise(column.GetName());

                if (_ignored.ContainsIgnoreCase(name))
                    continue;

                if (parser.HasAttribute(name))
                    sql.Append("AND " + column.Column + " = @0", parser.Attribute(name));

                else if (parser.Contains(name))
                    sql.Append("AND " + column.Column + " = @0", parser.PropertyValue(name));
            }
        }

        private void OrderBy(Sql sql, ObjectMapping mapping)
        {
            var nameColumn = mapping.Columns.FirstOrDefault(x => x.IsName);

            // OrderBy
            if (!string.IsNullOrWhiteSpace(nameColumn?.Column))
                sql.OrderBy(nameColumn.Column);
        }

        private string Combine(string name, string alias)
        {
            return string.Concat(name, " ", alias);
        }
    }
}
