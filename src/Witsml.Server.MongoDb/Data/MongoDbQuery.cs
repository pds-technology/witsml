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
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess;
using MongoDB.Driver;
using PDS.Witsml.Data;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Encloses MongoDb query method and its helper methods
    /// </summary>
    /// <typeparam name="T">The type of queried data object.</typeparam>
    public class MongoDbQuery<T> : DataObjectNavigator<MongoDbQueryContext<T>>
    {
        private readonly Dictionary<string, List<FilterDefinition<T>>> _filters;
        private readonly IMongoCollection<T> _collection;
        private readonly WitsmlQueryParser _parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbQuery{T}" /> class.
        /// </summary>
        /// <param name="collection">The Mongo database collection.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="fields">The fields of the data object to be selected.</param>
        /// <param name="ignored">The fields of the data object to be ignored.</param>
        public MongoDbQuery(IMongoCollection<T> collection, WitsmlQueryParser parser, List<string> fields, List<string> ignored = null) : base(new MongoDbQueryContext<T>())
        {
            Context.Fields = fields;
            Context.Ignored = ignored;

            _filters = new Dictionary<string, List<FilterDefinition<T>>>();
            _collection = collection;
            _parser = parser;
        }

        /// <summary>
        /// Executes this MongoDb query.
        /// </summary>
        /// <returns>The list of queried data object.</returns>
        public List<T> Execute()
        {
            Logger.DebugFormat("Executing query for entity: {0}", _parser.Context.ObjectType);

            var returnElements = _parser.ReturnElements();
            var entities = new List<T>();

            // Check if to project fields
            Context.Project = OptionsIn.ReturnElements.IdOnly.Equals(returnElements) ||
                OptionsIn.ReturnElements.Requested.Equals(returnElements) ||
                OptionsIn.ReturnElements.DataOnly.Equals(returnElements);

            if (Context.Fields == null)
                Context.Fields = new List<string>();

            foreach (var element in _parser.Elements())
            {
                // Navigate the root element and create filter and projection fields
                Navigate(element);

                // Build Mongo filter
                var filter = BuildFilter(element);
                var results = _collection.Find(filter ?? "{}");

                // Format response using MongoDb projection, i.e. selecting specified fields only
                if (OptionsIn.ReturnElements.All.Equals(returnElements) || 
                    OptionsIn.ReturnElements.HeaderOnly.Equals(returnElements))
                {
                    entities.AddRange(results.ToList());
                }
                else if (Context.Project)
                {
                    var projection = BuildProjection();

                    if (projection != null)
                    {
                        results = results.Project<T>(projection);
                    }

                    entities.AddRange(results.ToList());
                }
            }

            return entities;
        }

        /// <summary>
        /// Validates the values provided in the input template to catch any errors that 
        /// would be lost or hidden during data object deserialization.
        /// </summary>
        public void Validate()
        {
            Logger.DebugFormat("Validating input template for entity: {0}", _parser.Context.ObjectType);

            foreach (var element in _parser.Elements())
            {
                BuildFilter(element);
            }
        }

        protected override void HandleStringValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            Context.Filters.Add(Builders<T>.Filter.EqIgnoreCase(propertyPath, propertyValue));

            if (Context.Project)
                AddProjectionProperty(propertyPath);
        }

        protected override void HandleDateTimeValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, DateTime dateTimeValue)
        {
            if (propertyPath.EndsWith(".DateTimeCreation") || propertyPath.EndsWith(".DateTimeLastChange"))
            {
                Context.Filters.Add(Builders<T>.Filter.Gt(propertyPath, propertyValue));
            }
            else
            {
                Context.Filters.Add(Builders<T>.Filter.Eq(propertyPath, dateTimeValue));
            }

            if (Context.Project)
                AddProjectionProperty(propertyPath);
        }

        protected override void HandleTimestampValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, Timestamp timestampValue)
        {
            if (propertyPath.EndsWith(".DateTimeCreation") || propertyPath.EndsWith(".DateTimeLastChange"))
            {
                Context.Filters.Add(Builders<T>.Filter.Gt(propertyPath, propertyValue));
            }
            else
            {
                Context.Filters.Add(Builders<T>.Filter.Eq(propertyPath, timestampValue));
            }

            if (Context.Project)
                AddProjectionProperty(propertyPath);
        }

        protected override void HandleObjectValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, object objectValue)
        {
            Context.Filters.Add(Builders<T>.Filter.Eq(propertyPath, objectValue));

            if (Context.Project)
                AddProjectionProperty(propertyPath);
        }

        protected override void HandleNullValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            if (Context.Project)
                AddProjectionProperty(propertyPath);
        }

        protected override void HandleNaNValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            if (Context.Project)
                AddProjectionProperty(propertyPath);
        }

        protected override void InitializeRecurringElementHandler(string propertyPath)
        {
            _filters[propertyPath] = Context.Filters;
            Context.Filters = new List<FilterDefinition<T>>();
        }

        protected override void HandleRecurringElements(string propertyPath)
        {
            var filters = _filters[propertyPath];

            if (Context.Filters.Any())
            {
                filters.Add(Builders<T>.Filter.Or(Context.Filters));
            }

            Context.Filters = filters;
            _filters.Remove(propertyPath);
        }

        /// <summary>
        /// Builds the query filter.
        /// </summary>
        /// <returns>The filter object that for the selection criteria for the queried entity.</returns>
        private FilterDefinition<T> BuildFilter(XElement element)
        {
            Logger.DebugFormat("Building filter criteria for entity: {0}", _parser.Context.ObjectType);

            var filters = Context.Filters;

            var privateGroupOnly = _parser.RequestPrivateGroupOnly();
            var privateGroupOnlyFilter = privateGroupOnly
                ? Builders<T>.Filter.Eq("CommonData.PrivateGroupOnly", true)
                : Builders<T>.Filter.Ne("CommonData.PrivateGroupOnly", true);

            filters.Add(privateGroupOnlyFilter);

            var resultFilter = Builders<T>.Filter.And(filters);

            if (Logger.IsDebugEnabled)
            {
                var filterJson = resultFilter.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                Logger.DebugFormat("Detected query filters: {0}", filterJson);
            }

            return resultFilter;
        }

        /// <summary>
        /// Builds the projection for the query.
        /// </summary>
        /// <returns>The projection object that contains the fields to be selected.</returns>
        private ProjectionDefinition<T> BuildProjection()
        {
            Logger.DebugFormat("Building projection fields for entity: {0}", _parser.Context.ObjectType);

            if (Context.Fields.Count == 0)
            {
                Logger.Warn("No fields projected.  Projection field count should never be zero.");
                var requested = OptionsIn.ReturnElements.Requested.Equals(_parser.ReturnElements());
                return requested ? Builders<T>.Projection.Include(string.Empty) : null;
            }
            else
            {
                // Log projection fields if debug is enabled
                if (Logger.IsDebugEnabled)
                {
                    Logger.DebugFormat("Fields projected: {0}", string.Join(",", Context.Fields));
                }

                var projection = Builders<T>.Projection.Include(Context.Fields.First());

                foreach (var field in Context.Fields.Skip(1))
                    projection = projection.Include(field);

                return projection;
            }
        }

        private void AddProjectionProperty(string propertyPath)
        {
            if (Context.Fields.Contains(propertyPath))
                return;

            Context.Fields.Add(propertyPath);
        }
    }
}
