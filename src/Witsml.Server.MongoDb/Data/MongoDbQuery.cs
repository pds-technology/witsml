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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Energistics.DataAccess;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Data;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Encloses MongoDb query method and its helper methods
    /// </summary>
    /// <typeparam name="T">The type of queried data object.</typeparam>
    public class MongoDbQuery<T> : DataObjectNavigator<MongoDbQueryContext<T>>
    {
        private readonly IMongoCollection<T> _collection;
        private readonly WitsmlQueryParser _parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbQuery{T}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="collection">The Mongo database collection.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="fields">The fields of the data object to be selected.</param>
        /// <param name="ignored">The fields of the data object to be ignored.</param>
        public MongoDbQuery(IContainer container, IMongoCollection<T> collection, WitsmlQueryParser parser, List<string> fields, List<string> ignored = null) : base(container, new MongoDbQueryContext<T>())
        {
            Context.Fields = fields;
            Context.Ignored = ignored;

            _collection = collection;
            _parser = parser;
        }

        /// <summary>
        /// Gets a value indicating whether the current property is a recurring element.
        /// </summary>
        /// <value><c>true</c> if the current property is a recurring element; otherwise, <c>false</c>.</value>
        private bool IsRecurringElement => Context.ParentRecurringFilters.Any();
        
        /// <summary>
        /// Executes this MongoDb query.
        /// </summary>
        /// <returns>The list of queried data object.</returns>
        public List<T> Execute()
        {
            Logger.DebugFormat("Executing query for {0}", _parser.ObjectType);

            var returnElements = _parser.ReturnElements();

            Navigate(returnElements);

            // Build Mongo filter
            var filter = BuildFilter();
            var results = _collection.Find(filter ?? "{}");
            var entities = new List<T>();

            // Format response using MongoDb projection, i.e. selecting specified fields only
            if (OptionsIn.ReturnElements.All.Equals(returnElements) ||
                OptionsIn.ReturnElements.HeaderOnly.Equals(returnElements) ||
                OptionsIn.ReturnElements.LatestChangeOnly.Equals(returnElements))
            {
                entities.AddRange(results.ToList());
            }
            else if (Context.IsProjection)
            {
                var projection = BuildProjection();

                if (projection != null)
                {
                    results = results.Project<T>(projection);
                }

                entities.AddRange(results.ToList());
            }

            Logger.DebugFormat("Executed query for {0}; Count: {1}", _parser.ObjectType, entities.Count);
            WitsmlOperationContext.Current.Warnings.AddRange(Context.Warnings);

            return entities;
        }

        /// <summary>
        /// Filters the recurring elements.
        /// </summary>
        /// <param name="entities">The entities.</param>
        public List<T> FilterRecurringElements(List<T> entities)
        {
            // Skip if no recurring element filters were detected
            if (!Context.RecurringElementFilters.Any())
                return entities;

            if (Logger.IsDebugEnabled)
            {
                var expression = string.Join(" AND ", Context.RecurringElementFilters.Select(x => x.Expression));
                Logger.Debug("Detected recurring element filters: " + expression);
            }

            entities.ForEach(FilterRecurringElements);

            return entities;
        }

        /// <summary>
        /// Navigates the root element.
        /// </summary>
        /// <param name="returnElements">The return elements.</param>
        internal void Navigate(string returnElements)
        {
            // Check if to project fields
            Context.IsProjection = OptionsIn.ReturnElements.IdOnly.Equals(returnElements) ||
                OptionsIn.ReturnElements.Requested.Equals(returnElements) ||
                OptionsIn.ReturnElements.DataOnly.Equals(returnElements) ||
                OptionsIn.ReturnElements.StationLocationOnly.Equals(returnElements);

            if (Context.Fields == null)
                Context.Fields = new List<string>();

            var element = _parser.Element();

            // Navigate the root element and create filter and projection fields
            Navigate(element);
        }

        /// <summary>
        /// Navigates the uom attribute.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="measureValue">The measure value.</param>
        /// <param name="uomValue">The uom value.</param>
        protected override void NavigateUomAttribute(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string measureValue, string uomValue)
        {
            // TODO: may need to adjust this method override when unit conversion is implemented
            // NOTE: not applying uom filter, only using attribute for projection
            //base.NavigateUomAttribute(propertyInfo, xmlObject, propertyType, propertyPath, measureValue, uomValue);
            AddProjectionProperty(propertyPath);
        }

        /// <summary>
        /// Navigates the array element.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elements">The elements.</param>
        /// <param name="childType">Type of the child.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The property path.</param>
        protected override void NavigateArrayElementType(PropertyInfo propertyInfo, List<XElement> elements, Type childType, XElement element, string propertyPath)
        {
            InitializeRecurringElementFilters(propertyPath);

            base.NavigateArrayElementType(propertyInfo, elements, childType, element, propertyPath);

            HandleRecurringElementFilters(propertyPath);
        }

        /// <summary>
        /// Initializes the recurring element handler.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyPath">The property path.</param>
        protected override void InitializeRecurringElementHandler(PropertyInfo propertyInfo, string propertyPath)
        {
            Context.ParentFilters[propertyPath] = Context.Filters;
            Context.Filters = new List<FilterDefinition<T>>();

            if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                InitializeRecurringElementFilters(propertyPath, true);
            }
        }

        /// <summary>
        /// Navigates the recurring elements.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elements">The elements.</param>
        /// <param name="childType">Type of the child.</param>
        /// <param name="propertyPath">The property path.</param>
        protected override void NavigateRecurringElements(PropertyInfo propertyInfo, List<XElement> elements, Type childType, string propertyPath)
        {
            elements.ForEach((value, index) =>
            {
                var propertyKey = $"{propertyPath}.{index}";
                InitializeRecurringElementFilters(propertyKey);

                NavigateElementType(propertyInfo, childType, value, propertyPath);

                HandleRecurringElementFilters(propertyKey);
            });
        }

        /// <summary>
        /// Handles the recurring elements.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyPath">The property path.</param>
        protected override void HandleRecurringElements(PropertyInfo propertyInfo, string propertyPath)
        {
            var filters = Context.ParentFilters[propertyPath];

            if (Context.Filters.Any())
            {
                filters.Add(Builders<T>.Filter.Or(Context.Filters));
            }

            HandleRecurringElementFilters(propertyPath, true);

            Context.Filters = filters;
            Context.ParentFilters.Remove(propertyPath);
        }

        /// <summary>
        /// Handles the string value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected override void HandleStringValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            if (IsRecurringElement)
            {
                Context.RecurringElementFilters.Add(new RecurringElementFilter(propertyPath, $"EqualsIgnoreCase({propertyValue})",
                    (dataObject, instance) => $"{propertyInfo.GetValue(instance)}".EqualsIgnoreCase(propertyValue)));
            }

            Context.Filters.Add(Builders<T>.Filter.EqIgnoreCase(propertyPath, propertyValue));
            AddProjectionProperty(propertyPath);
        }

        /// <summary>
        /// Handles the date time value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="dateTimeValue">The date time value.</param>
        protected override void HandleDateTimeValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, DateTime dateTimeValue)
        {
            if (propertyPath.EndsWith(".DateTimeCreation") || propertyPath.EndsWith(".DateTimeLastChange") || propertyPath.EndsWith(".DateTimeChange"))
            {
                if (IsRecurringElement)
                {
                    Context.RecurringElementFilters.Add(new RecurringElementFilter(propertyPath, $"GreaterThan({propertyValue})",
                        (dataObject, instance) =>
                        {
                            var value = (DateTime?) propertyInfo.GetValue(instance);
                            return value.HasValue && value.Value > dateTimeValue;
                        }));
                }

                Context.Filters.Add(Builders<T>.Filter.Gt(propertyPath, propertyValue));
            }
            else
            {
                if (IsRecurringElement)
                {
                    Context.RecurringElementFilters.Add(new RecurringElementFilter(propertyPath, $"Equals({propertyValue})",
                        (dataObject, instance) =>
                        {
                            var value = (DateTime?) propertyInfo.GetValue(instance);
                            return value.HasValue && value.Value.Equals(dateTimeValue);
                        }));
                }

                Context.Filters.Add(Builders<T>.Filter.Eq(propertyPath, dateTimeValue));
            }

            AddProjectionProperty(propertyPath);
        }

        /// <summary>
        /// Handles the timestamp value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="timestampValue">The timestamp value.</param>
        protected override void HandleTimestampValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, Timestamp timestampValue)
        {
            if (propertyPath.EndsWith(".DateTimeCreation") || propertyPath.EndsWith(".DateTimeLastChange") || propertyPath.EndsWith(".DateTimeChange"))
            {
                if (IsRecurringElement)
                {
                    Context.RecurringElementFilters.Add(new RecurringElementFilter(propertyPath, $"GreaterThan({propertyValue})",
                        (dataObject, instance) =>
                        {
                            var value = (Timestamp?) propertyInfo.GetValue(instance);
                            return value.HasValue && value.Value > timestampValue;
                        }));
                }

                Context.Filters.Add(Builders<T>.Filter.Gt(propertyPath, propertyValue));
            }
            else
            {
                if (IsRecurringElement)
                {
                    Context.RecurringElementFilters.Add(new RecurringElementFilter(propertyPath, $"Equals({propertyValue})",
                        (dataObject, instance) =>
                        {
                            var value = (Timestamp?) propertyInfo.GetValue(instance);
                            return value.HasValue && value.Value.Equals(timestampValue);
                        }));
                }

                Context.Filters.Add(Builders<T>.Filter.Eq(propertyPath, timestampValue));
            }

            AddProjectionProperty(propertyPath);
        }

        /// <summary>
        /// Handles the object value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="objectValue">The object value.</param>
        protected override void HandleObjectValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, object objectValue)
        {
            if (IsRecurringElement)
            {
                Context.RecurringElementFilters.Add(new RecurringElementFilter(propertyPath, $"Equals({propertyValue})",
                    (dataObject, instance) => Equals(propertyInfo.GetValue(instance), objectValue)));
            }

            Context.Filters.Add(Builders<T>.Filter.Eq(propertyPath, objectValue));
            AddProjectionProperty(propertyPath);
        }

        /// <summary>
        /// Handles the null value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected override void HandleNullValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            AddProjectionProperty(propertyPath);
        }

        /// <summary>
        /// Handles the NaN value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected override void HandleNaNValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            AddProjectionProperty(propertyPath);
        }

        /// <summary>
        /// Determines whether the specified element name is ignored.
        /// </summary>
        /// <param name="elementName">Name of the element.</param>
        /// <param name="parentPath">Parent path of the element.</param>
        /// <returns></returns>
        protected override bool IsIgnored(string elementName, string parentPath = null)
        {
            var ignored = base.IsIgnored(elementName, parentPath);
            if (ignored && (OptionsIn.ReturnElements.Requested.Equals(_parser.ReturnElements()) || OptionsIn.ReturnElements.DataOnly.Equals(_parser.ReturnElements())))
                Context.Fields.Add(parentPath);

            return ignored;
        }

        /// <summary>
        /// Builds the query filter.
        /// </summary>
        /// <returns>The filter object that for the selection criteria for the queried entity.</returns>
        private FilterDefinition<T> BuildFilter()
        {
            Logger.DebugFormat("Building filter criteria for entity: {0}", _parser.ObjectType);

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
                Logger.Debug($"Detected query filters: {filterJson}");
            }

            return resultFilter;
        }

        /// <summary>
        /// Builds the projection for the query.
        /// </summary>
        /// <returns>The projection object that contains the fields to be selected.</returns>
        private ProjectionDefinition<T> BuildProjection()
        {
            Logger.DebugFormat("Building projection fields for entity: {0}", _parser.ObjectType);

            if (Context.Fields.Count == 0)
            {
                Logger.Warn("No fields projected.  Projection field count should never be zero.");
                var requested = OptionsIn.ReturnElements.Requested.Equals(_parser.ReturnElements());
                return requested ? Builders<T>.Projection.Include(string.Empty) : null;
            }
            else
            {
                var projection = Builders<T>.Projection.Include(Context.Fields.First());

                foreach (var field in Context.Fields.Skip(1))
                    projection = projection.Include(field);

                if (Logger.IsDebugEnabled)
                {
                    var projectionJson = projection.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                    Logger.Debug($"Detected query projection: {projectionJson}");
                }

                return projection;
            }
        }

        /// <summary>
        /// Adds the projection property.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        private void AddProjectionProperty(string propertyPath)
        {
            if (!Context.IsProjection || Context.Fields.Contains(propertyPath))
                return;

            Context.Fields.Add(propertyPath);
        }

        /// <summary>
        /// Initializes the recurring element filters.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="isRecurringCriteria">if set to <c>true</c> is recurring criteria.</param>
        private void InitializeRecurringElementFilters(string propertyPath, bool isRecurringCriteria = false)
        {
            Context.ParentRecurringFilters[propertyPath] = Context.RecurringElementFilters;
            Context.RecurringElementFilters = new List<RecurringElementFilter>();
        }

        /// <summary>
        /// Handles the recurring element filters.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="isRecurringCriteria">if set to <c>true</c> is recurring criteria.</param>
        private void HandleRecurringElementFilters(string propertyPath, bool isRecurringCriteria = false)
        {
            var recurringFilters = Context.ParentRecurringFilters[propertyPath];

            if (Context.RecurringElementFilters.Any())
            {
                var filters = Context.RecurringElementFilters.ToArray();
                var filter = new RecurringElementFilter(propertyPath, isRecurringCriteria, filters);

                recurringFilters.Add(filter);
            }

            Context.RecurringElementFilters = recurringFilters;
            Context.ParentRecurringFilters.Remove(propertyPath);
        }

        /// <summary>
        /// Filters the recurring elements.
        /// </summary>
        /// <param name="entity">The entity.</param>
        private void FilterRecurringElements(T entity)
        {
            var filters = Context.RecurringElementFilters;
            filters.ForEach(filter => FilterRecurringElements(entity, entity, filter));
        }

        /// <summary>
        /// Filters the recurring elements.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="parentPath">The parent path.</param>
        private bool FilterRecurringElements(object dataObject, object instance, RecurringElementFilter filter, string parentPath = null)
        {
            var propertyPath = !string.IsNullOrWhiteSpace(parentPath)
                ? filter.PropertyPath.Substring(parentPath.Length + 1)
                : filter.PropertyPath;

            var type = instance.GetType();
            var paths = propertyPath.Split(new[] {'.'}, 2);
            var propertyName = paths.First();
            var nestedPath = paths.Skip(1).FirstOrDefault() ?? string.Empty;

            var propertyInfo = type.GetProperty(propertyName);
            var propertyValue = propertyInfo.GetValue(instance);
            var listValue = propertyValue as IList;

            // Update parent path with current property name
            parentPath = GetPropertyPath(parentPath, propertyName);

            // Check if processing nested complex type
            if (listValue == null)
            {
                if (propertyValue != null && !string.IsNullOrWhiteSpace(nestedPath) && !HasSimpleContent(propertyInfo.PropertyType))
                {
                    return FilterRecurringElements(dataObject, propertyValue, filter, parentPath);
                }

                return filter.Predicate(dataObject, propertyValue);
            }

            // Process recurring element property
            var filtered = new ArrayList();

            foreach (var item in listValue)
            {
                if (item == null) continue;

                // Check if nested properties need to be filtered
                if ((filter.Filters != null && !string.IsNullOrWhiteSpace(nestedPath)) || nestedPath.Contains('.'))
                {
                    if (!FilterRecurringElements(dataObject, item, filter, parentPath))
                        filtered.Add(item);

                    continue;
                }

                // Check if recurring element doesn't meet criteria
                if (!filter.Predicate(dataObject, item))
                    filtered.Add(item);
            }

            // Remove filtered items from original list
            foreach (var item in filtered)
            {
                listValue.Remove(item);
            }

            return true;
        }
    }
}
