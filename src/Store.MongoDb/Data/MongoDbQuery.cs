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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.Validation;
using MongoDB.Driver;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data;

namespace PDS.WITSMLstudio.Store.Data
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
        /// Gets a value indicating whether the current property is within a recurring element.
        /// </summary>
        /// <value><c>true</c> if the current property is within a recurring element; otherwise, <c>false</c>.</value>
        private bool IsRecurringElement => Context.RecurringFilterStack.Any();

        /// <summary>
        /// Gets date time property names for handling date time values.
        /// </summary>
        public virtual string[] DateTimeProperties => new[] { ".DateTimeCreation", ".DateTimeLastChange", ".DateTimeChange" };

        /// <summary>
        /// Executes this MongoDb query.
        /// </summary>
        /// <returns>The list of queried data object.</returns>
        public virtual List<T> Execute()
        {
            Logger.DebugFormat("Executing query for {0}", _parser.ObjectType);

            var returnElements = _parser.ReturnElements();

            Navigate(returnElements);

            // Build Mongo filter
            var filter = BuildFilter();
            var query = PrepareQuery(filter);
            var entities = new List<T>();

            // Format response using MongoDb projection, i.e. selecting specified fields only
            if (OptionsIn.ReturnElements.All.Equals(returnElements) ||
                OptionsIn.ReturnElements.HeaderOnly.Equals(returnElements) ||
                OptionsIn.ReturnElements.LatestChangeOnly.Equals(returnElements))
            {
                entities.AddRange(query.ToEnumerable());
            }
            else if (Context.IsProjection)
            {
                var projection = BuildProjection();

                if (projection != null)
                {
                    query = query.Project<T>(projection);
                }

                entities.AddRange(query.ToEnumerable());
            }

            Logger.DebugFormat("Executed query for {0}; Count: {1}", _parser.ObjectType, entities.Count);
            WitsmlOperationContext.Current.Warnings.AddRange(Context.Warnings);

            return entities;
        }

        /// <summary>
        /// Filters the recurring elements.
        /// </summary>
        /// <param name="entities">The entities.</param>
        public virtual List<T> FilterRecurringElements(List<T> entities)
        {
            // Skip if no recurring element filters were detected
            if (!Context.RecurringElementFilters.Any())
                return entities;

            if (Logger.IsDebugEnabled)
            {
                var expression = string.Join(" AND ", Context.RecurringElementFilters.Select(x => x.Expression));
                Logger.Debug("Detected recurring element filters: " + expression);
            }

            return entities
                .Where(x => !FilterRecurringElements(x))
                .ToList();
        }

        /// <summary>
        /// Navigates the root element.
        /// </summary>
        /// <param name="returnElements">The return elements.</param>
        public virtual void Navigate(string returnElements)
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
        /// Prepares the query for execution.
        /// </summary>
        /// <param name="filter">The query filter.</param>
        /// <returns>A fluent aggregation interface.</returns>
        protected virtual IAggregateFluent<T> PrepareQuery(FilterDefinition<T> filter)
        {
            //return _collection.Find(filter ?? "{}");
            return _collection.Aggregate().Match(filter ?? "{}");
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
            InitializeRecurringElementFilter(propertyPath);

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

            if (!Compatibility.CompatibilitySettings.AllowDuplicateNonRecurringElements && propertyInfo.GetCustomAttribute<RecurringElementAttribute>() == null)
                throw new WitsmlException(ErrorCodes.InputTemplateNonConforming);

            if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                InitializeRecurringElementFilter(propertyPath);
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
            foreach (var value in elements)
            {
                InitializeRecurringElementFilter(propertyPath);

                NavigateElementType(propertyInfo, childType, value, propertyPath);

                HandleRecurringElementFilters(propertyPath);
            }
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

            if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                HandleRecurringElementFilters(propertyPath, true);
            }

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
                Context.CurrentRecurringFilters.Add(new RecurringElementFilter(GetNestedPath(propertyPath), $"EqualsIgnoreCase({propertyValue})",
                    (dataObject, instance, filter) => filter.GetPropertyValue<string>(instance).EqualsIgnoreCase(propertyValue)));
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
            if (DateTimeProperties.Any(propertyPath.EndsWith))
            {
                if (IsRecurringElement)
                {
                    Context.CurrentRecurringFilters.Add(new RecurringElementFilter(GetNestedPath(propertyPath), $"GreaterThan({propertyValue})",
                        (dataObject, instance, filter) =>
                        {
                            var value = filter.GetPropertyValue<DateTime?>(instance);
                            return value.HasValue && value.Value > dateTimeValue;
                        }));
                }

                Context.Filters.Add(Builders<T>.Filter.Gt(propertyPath, dateTimeValue));
            }
            else
            {
                if (IsRecurringElement)
                {
                    Context.CurrentRecurringFilters.Add(new RecurringElementFilter(GetNestedPath(propertyPath), $"Equals({propertyValue})",
                        (dataObject, instance, filter) =>
                        {
                            var value = filter.GetPropertyValue<DateTime?>(instance);
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
            if (DateTimeProperties.Any(propertyPath.EndsWith))
            {
                if (IsRecurringElement)
                {
                    Context.CurrentRecurringFilters.Add(new RecurringElementFilter(GetNestedPath(propertyPath), $"GreaterThan({propertyValue})",
                        (dataObject, instance, filter) =>
                        {
                            var value = filter.GetPropertyValue<Timestamp?>(instance);
                            return value.HasValue && value.Value > timestampValue;
                        }));
                }

                Context.Filters.Add(Builders<T>.Filter.Gt(propertyPath, propertyValue));
            }
            else
            {
                if (IsRecurringElement)
                {
                    Context.CurrentRecurringFilters.Add(new RecurringElementFilter(GetNestedPath(propertyPath), $"Equals({propertyValue})",
                        (dataObject, instance, filter) =>
                        {
                            var value = filter.GetPropertyValue<Timestamp?>(instance);
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
                Context.CurrentRecurringFilters.Add(new RecurringElementFilter(GetNestedPath(propertyPath), $"Equals({propertyValue})",
                    (dataObject, instance, filter) =>
                    {
                        var value = filter.GetPropertyValue<object>(instance);
                        if (value is Enum && objectValue is string)
                            value = ((Enum)value).GetName();
                        return Equals(value, objectValue);
                    }));
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
        protected virtual FilterDefinition<T> BuildFilter()
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
        protected virtual ProjectionDefinition<T> BuildProjection()
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
        protected virtual void AddProjectionProperty(string propertyPath)
        {
            if (!Context.IsProjection || Context.Fields.Contains(propertyPath))
                return;

            Context.Fields.Add(propertyPath);
        }

        /// <summary>
        /// Initializes the recurring element filters.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        protected virtual void InitializeRecurringElementFilter(string propertyPath)
        {
            var filter = new RecurringElementFilter(propertyPath)
            {
                // Keep a reference to the previous list of filters
                PreviousFilters = Context.CurrentRecurringFilters
            };

            // Put the new filter on the stack
            Context.RecurringFilterStack.Push(filter);
            // Initialize the list of filters for the current recurring element
            Context.CurrentRecurringFilters = new List<RecurringElementFilter>();
        }

        /// <summary>
        /// Handles the recurring element filters.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="isRecurringCriteria">if set to <c>true</c> is recurring criteria.</param>
        protected virtual void HandleRecurringElementFilters(string propertyPath, bool isRecurringCriteria = false)
        {
            // Pull the current filter off the stack
            var filter = Context.RecurringFilterStack.Pop();

            // Peek at the next filter in the stack, if any
            var next = Context.RecurringFilterStack.Any()
                ? Context.RecurringFilterStack.Peek()
                : null;

            if (Context.CurrentRecurringFilters.Any())
            {
                var filters = Context.CurrentRecurringFilters.ToArray();

                // Recurring criteria are joined with OR
                if (isRecurringCriteria)
                {
                    var recurringFilter = new RecurringElementFilter(propertyPath, filters);
                    filter.Filters.Add(recurringFilter);
                }
                else // Otherwise join with AND
                {
                    filter.Filters.AddRange(filters);
                }
            }

            // Reset the reference for the previous list of filters
            Context.CurrentRecurringFilters = filter.PreviousFilters;
            filter.PreviousFilters = null;

            // Stop processing if no filters were included
            if (!filter.Filters.Any()) return;

            // Check if this filter should be combined with the next filter in the stack
            if (filter.PropertyPath == next?.PropertyPath)
            {
                Context.CurrentRecurringFilters.Add(filter);
            }
            else // Otherwise add as root level filters
            {
                Context.RecurringElementFilters.Add(filter);

                // Stop processing if not handling a nested recurring element
                if (next == null) return;

                // Add filter for parent recurring element when nested recurring elements are filtered
                Context.CurrentRecurringFilters.Add(new RecurringElementFilter(GetNestedPath(filter.PropertyPath), "Any()",
                    (dataObject, instance, recurringFilter) => recurringFilter.GetPropertyValue<IList>(instance)?.Count > 0));
            }
        }

        /// <summary>
        /// Filters the recurring elements.
        /// </summary>
        /// <param name="entity">The entity.</param>
        protected virtual bool FilterRecurringElements(T entity)
        {
            return Context.RecurringElementFilters
                .Aggregate(true, (current, filter) => FilterRecurringElements(entity, entity, filter, filter.PropertyPath) && current);
        }

        /// <summary>
        /// Filters the recurring elements.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="propertyPath">The property path.</param>
        protected virtual bool FilterRecurringElements(object dataObject, object instance, RecurringElementFilter filter, string propertyPath)
        {
            var propertyNames = propertyPath.Split('.');
            IList recurringElementList = null;

            // Find recurring element list(s) in property path
            for (var i=0; i < propertyNames.Length; i++)
            {
                // Stop processing if the object instance is null
                if (instance == null) return false;

                // Process nested recurring elements
                if (recurringElementList != null)
                {
                    var nestedPath = string.Join(".", propertyNames.Skip(i));

                    return recurringElementList.Cast<object>()
                        .Aggregate(true, (current, recurringItem) => FilterRecurringElements(dataObject, recurringItem, filter, nestedPath) && current);
                }

                var propertyType = instance.GetType();
                var propertyName = propertyNames[i];
                var propertyInfo = propertyType.GetProperty(propertyName);

                // Stop processing if the property was not found
                if (propertyInfo == null) return false;

                // Get the current property value
                instance = propertyInfo.GetValue(instance);
                recurringElementList = instance as IList;
            }

            // Stop processing if the list is null
            if (recurringElementList == null) return false;

            // Process recurring elements
            var filteredItems = new ArrayList();

            foreach (var recurringItem in recurringElementList)
            {
                // Check if recurring element doesn't meet criteria
                if (!filter.Predicate(dataObject, recurringItem, filter))
                    filteredItems.Add(recurringItem);
            }

            // Remove filtered items from original list
            foreach (var item in filteredItems)
            {
                recurringElementList.Remove(item);
            }

            return recurringElementList.Count <= 0;
        }

        /// <summary>
        /// Gets the nested path.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        /// <returns></returns>
        protected virtual string GetNestedPath(string propertyPath)
        {
            var filter = Context.RecurringFilterStack.Peek();

            return propertyPath.EqualsIgnoreCase(filter.PropertyPath)
                ? RecurringElementFilter.Self
                : propertyPath.StartsWith(filter.PropertyPath + ".")
                    ? propertyPath.Substring(filter.PropertyPath.Length + 1)
                    : propertyPath;
        }
    }
}
