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
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using Energistics.DataAccess;
using log4net;
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
        private List<string> _fields;
        private List<string> _ignored;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbQuery{T}" /> class.
        /// </summary>
        /// <param name="collection">The Mongo database collection.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="fields">The fields of the data object to be selected.</param>
        /// <param name="ignored">The fields of the data object to be ignored.</param>
        public MongoDbQuery(IMongoCollection<T> collection, WitsmlQueryParser parser, List<string> fields, List<string> ignored = null) : base(new MongoDbQueryContext<T>())
        {
            Logger = LogManager.GetLogger(GetType());

            _collection = collection;
            _parser = parser;
            _fields = fields;
            _ignored = ignored;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILog Logger { get; private set; }

        /// <summary>
        /// Executes this MongoDb query.
        /// </summary>
        /// <returns>The list of queried data object.</returns>
        public List<T> Execute()
        {
            Logger.DebugFormat("Executing query for entity: {0}", _parser.Context.ObjectType);

            var returnElements = _parser.ReturnElements();
            var entities = new List<T>();

            foreach (var element in _parser.Elements())
            {
                // Build Mongo filter
                var filter = BuildFilter(element);
                var results = _collection.Find(filter ?? "{}");

                // Format response using MongoDb projection, i.e. selecting specified fields only
                if (OptionsIn.ReturnElements.All.Equals(returnElements) || 
                    OptionsIn.ReturnElements.HeaderOnly.Equals(returnElements))
                {
                    entities.AddRange(results.ToList());
                }
                else if (OptionsIn.ReturnElements.IdOnly.Equals(returnElements) ||
                         OptionsIn.ReturnElements.Requested.Equals(returnElements) ||
                         OptionsIn.ReturnElements.DataOnly.Equals(returnElements))
                {
                    var projection = BuildProjection(element);

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

        /// <summary>
        /// Builds the query filter.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The filter object that for the selection criteria for the queried entity.</returns>
        private FilterDefinition<T> BuildFilter(XElement element)
        {
            Logger.DebugFormat("Building filter criteria for entity: {0}", _parser.Context.ObjectType);

            var filters = new List<FilterDefinition<T>>();

            var privateGroupOnly = _parser.RequestPrivateGroupOnly();
            var privateGroupOnlyFilter = privateGroupOnly
                ? Builders<T>.Filter.Eq("CommonData.PrivateGroupOnly", true)
                : Builders<T>.Filter.Ne("CommonData.PrivateGroupOnly", true);

            filters.Add(privateGroupOnlyFilter);

            var filter = BuildFilterForAnElement(element, typeof(T));

            if (filter != null)
                filters.Add(filter);

            var resultFilter = Builders<T>.Filter.And(filters);

            if (Logger.IsDebugEnabled)
            {
                var filterJson = resultFilter.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                Logger.DebugFormat("Detected query filters: {0}", filterJson);
            }

            return resultFilter;
        }

        /// <summary>
        /// Builds the filter for an element of the specified runtime type.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="type">The type of the element.</param>
        /// <param name="parentPath">The parent path.</param>
        /// <returns>The filter object that for the selection criteria for the element.</returns>
        private FilterDefinition<T> BuildFilterForAnElement(XElement element, Type type, string parentPath = null)
        {
            if (_ignored != null && _ignored.Contains(element.Name.LocalName))
                return null;

            var filters = new List<FilterDefinition<T>>();
            var properties = GetPropertyInfo(type);
            var groupings = element.Elements().GroupBy(e => e.Name.LocalName);

            foreach (var group in groupings)
            {
                if (_ignored != null && _ignored.Contains(group.Key))
                    continue;

                var propertyInfo = GetPropertyInfoForAnElement(properties, group.Key);
                var propertyFilter = BuildFilterForAnElementGroup(propertyInfo, group, parentPath);

                if (propertyFilter != null)
                    filters.Add(propertyFilter);
            }

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Name == Xsi("nil") || attribute.Name == Xsi("type"))
                    continue;

                var attributeProp = GetPropertyInfoForAnElement(properties, attribute.Name.LocalName);
                var attributeFilter = BuildFilterForAttribute(attributeProp, attribute, parentPath);

                if (attributeFilter != null)
                    filters.Add(attributeFilter);
            }

            return (filters.Count > 0)
                ? Builders<T>.Filter.And(filters)
                : null;
        }

        /// <summary>
        /// Builds the filter for a group of elements.
        /// </summary>
        /// <param name="propertyInfo">The property information for the element group.</param>
        /// <param name="elements">The collection of elements.</param>
        /// <param name="parentPath">The path of the element to the entity to be queried in the data store.</param>
        /// <returns>The filter object that for the selection criteria for the group of elements.</returns>
        private FilterDefinition<T> BuildFilterForAnElementGroup(PropertyInfo propertyInfo, IEnumerable<XElement> elements, string parentPath = null)
        {
            if (propertyInfo == null)
                return null;

            var fieldName = GetPropertyPath(parentPath, propertyInfo.Name);
            var propType = propertyInfo.PropertyType;
            var values = elements.ToList();
            var count = values.Count;

            if (count == 1)
            {
                var element = values.FirstOrDefault();

                if (propType.IsGenericType)
                {
                    var genericType = propType.GetGenericTypeDefinition();

                    if (genericType == typeof(Nullable<>))
                    {
                        var underlyingType = Nullable.GetUnderlyingType(propType);
                        return BuildFilterForAnElementType(underlyingType, element, fieldName);
                    }
                    else if (genericType == typeof(List<>))
                    {
                        var childType = propType.GetGenericArguments()[0];
                        return BuildFilterForAnElementType(childType, element, fieldName);
                    }

                    return null;
                }
                else if (propType.IsAbstract)
                {
                    var concreteType = GetConcreteType(element, propType);
                    return BuildFilterForAnElementType(concreteType, element, fieldName);
                }
                else
                {
                    return BuildFilterForAnElementType(propType, element, fieldName);
                }
            }
            else
            {
                var childType = propType.GetGenericArguments()[0];
                var listFilters = new List<FilterDefinition<T>>();

                foreach (var value in values)
                {
                    var listFilter = BuildFilterForAnElementType(childType, value, fieldName);

                    if (listFilter != null)
                        listFilters.Add(listFilter);
                }

                return (listFilters.Count > 0)
                    ? Builders<T>.Filter.Or(listFilters)
                    : null;
            }
        }

        /// <summary>
        /// Builds the filter for an element of generic type.
        /// </summary>
        /// <param name="elementType">Type of the element.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The path of the property for the element.</param>
        /// <returns>The filter object that for the selection criteria for the element.</returns>
        private FilterDefinition<T> BuildFilterForAnElementType(Type elementType, XElement element, string propertyPath)
        {
            var textProperty = elementType.GetProperties().FirstOrDefault(x => x.IsDefined(typeof(XmlTextAttribute), false));

            if (textProperty != null)
            {
                var uomProperty = elementType.GetProperty("Uom");
                var fieldName = GetPropertyPath(propertyPath, textProperty.Name);
                var fieldType = textProperty.PropertyType;
                var filters = new List<FilterDefinition<T>>();

                if (uomProperty != null)
                {
                    var uomPath = GetPropertyPath(propertyPath, uomProperty.Name);
                    var uomValue = ValidateMeasureUom(element, uomProperty, element.Value);
                    var uomFilter = BuildFilterForProperty(uomProperty.PropertyType, uomPath, uomValue);

                    if (uomFilter != null)
                        filters.Add(uomFilter);
                }

                var textFilter = BuildFilterForProperty(fieldType, fieldName, element.Value);

                if (textFilter != null)
                    filters.Add(textFilter);

                return (filters.Any())
                    ? Builders<T>.Filter.And(filters)
                    : null;
            }
            else if (element.HasElements || element.HasAttributes)
            {
                return BuildFilterForAnElement(element, elementType, propertyPath);
            }

            return BuildFilterForProperty(elementType, propertyPath, element.Value);
        }

        /// <summary>
        /// Builds the filter for an attribute.
        /// </summary>
        /// <param name="propertyInfo">The property information for the attribute.</param>
        /// <param name="attribute">The attribute.</param>
        /// <param name="parentPath">The path of the attribute to the entity to be queried in the data store.</param>
        /// <returns>The filter object that for the selection criteria for the attribute.</returns>
        private FilterDefinition<T> BuildFilterForAttribute(PropertyInfo propertyInfo, XAttribute attribute, string parentPath = null)
        {
            var propertyPath = GetPropertyPath(parentPath, propertyInfo.Name);
            var propertyType = propertyInfo.PropertyType;

            return BuildFilterForProperty(propertyType, propertyPath, attribute.Value);
        }

        /// <summary>
        /// Builds the filter for a property.
        /// </summary>
        /// <param name="propertyType">The property type.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <returns></returns>
        /// <summary>
        private FilterDefinition<T> BuildFilterForProperty(Type propertyType, string propertyPath, string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyValue))
            {
                return null;
            }
            else if (propertyPath.EndsWith(".DateTimeCreation") || propertyPath.EndsWith(".DateTimeLastChange"))
            {
                return Builders<T>.Filter.Gt(propertyPath, propertyValue);
            }
            else if (propertyType == typeof(string))
            {
                return Builders<T>.Filter.EqIgnoreCase(propertyPath, propertyValue);
            }
            else if (propertyType.IsEnum)
            {
                var value = ParseEnum(propertyType, propertyValue);
                return Builders<T>.Filter.Eq(propertyPath, value);
            }
            else if (propertyType == typeof(DateTime))
            {
                DateTime value;

                if (!DateTime.TryParse(propertyValue, out value))
                    throw new WitsmlException(ErrorCodes.InputTemplateNonConforming);

                return Builders<T>.Filter.Eq(propertyPath, value);
            }
            else if (propertyType == typeof(Timestamp))
            {
                DateTimeOffset value;

                if (!DateTimeOffset.TryParse(propertyValue, out value))
                    throw new WitsmlException(ErrorCodes.InputTemplateNonConforming);

                return Builders<T>.Filter.Eq(propertyPath, new Timestamp(value));
            }
            else if (propertyValue.Equals("NaN") && propertyType.IsNumeric())
            {
                return null;
            }
            else if (typeof(IConvertible).IsAssignableFrom(propertyType))
            {
                var value = Convert.ChangeType(propertyValue, propertyType);
                return Builders<T>.Filter.Eq(propertyPath, value);
            }
            else
            {
                return Builders<T>.Filter.Eq(propertyPath, propertyValue);
            }
        }

        /// <summary>
        /// Builds the projection for the query.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns>The projection object that contains the fields to be selected.</returns>
        private ProjectionDefinition<T> BuildProjection(XElement element)
        {
            Logger.DebugFormat("Building projection fields for entity: {0}", _parser.Context.ObjectType);

            if (element == null)
                return null;

            if (_fields == null)
                _fields = new List<string>();

            BuildProjectionForAnElement(element, null, typeof(T));

            if (_fields.Count == 0)
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
                    Logger.DebugFormat("Fields projected: {0}", string.Join(",", _fields));
                }

                var projection = Builders<T>.Projection.Include(_fields[0]);

                for (var i = 1; i < _fields.Count; i++)
                    projection = projection.Include(_fields[i]);

                return projection;
            }
        }

        /// <summary>
        /// Builds the projection for an element in the QueryIn XML recursively.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="fieldPath">The field path to the top level property of the queried entity.</param>
        /// <param name="type">The object type of the element.</param>
        private void BuildProjectionForAnElement(XElement element, string fieldPath, Type type)
        {
            Logger.DebugFormat("Building projection fields for element: {0}", element.Name.LocalName);

            if (type.IsGenericType)
            {
                type = type.GetGenericArguments()[0];
                BuildProjectionForAnElement(element, fieldPath, type);
                return;
            }

            var properties = GetPropertyInfo(type);

            if (element.HasElements)
            {
                foreach (var child in element.Elements())
                {
                    var property = GetPropertyInfoForAnElement(properties, child.Name.LocalName);
                    if (property == null)
                        continue;

                    BuildProjectionForAnElement(child, fieldPath, property);
                }
            }
            if (element.HasAttributes)
            {
                foreach (var attribute in element.Attributes())
                {
                    if (attribute.IsNamespaceDeclaration)
                        continue;

                    var attributePath = GetPropertyPath(fieldPath, attribute.Name.LocalName);
                    AddProjectionProperty(attributePath);
                }
            }
        }

        /// <summary>
        /// Builds the projection for an element in the QueryIn XML recursively.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="fieldPath">The field path to the top level property of the queried entity.</param>
        /// <param name="propertyInfo">The property information for the field.</param>
        private void BuildProjectionForAnElement(XElement element, string fieldPath, PropertyInfo propertyInfo)
        {
            var path = GetPropertyPath(fieldPath, propertyInfo.Name);

            if (!element.HasElements && !element.HasAttributes)
            {
                AddProjectionProperty(path);
                return;
            }

            BuildProjectionForAnElement(element, path, propertyInfo.PropertyType);
        }

        private void AddProjectionProperty(string propertyPath)
        {
            if (_fields.Contains(propertyPath))
                return;

            _fields.Add(propertyPath);
        }
    }
}
