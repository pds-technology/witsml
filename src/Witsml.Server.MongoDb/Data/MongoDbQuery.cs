using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using log4net;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Encloses MongoDb query method and its helper methods
    /// </summary>
    /// <typeparam name="TList">The type of the parent object for the list of queried data object.</typeparam>
    /// <typeparam name="T">The type of queried data object.</typeparam>
    public class MongoDbQuery<TList, T>
    {
        private readonly IMongoCollection<T> _collection;
        private readonly WitsmlQueryParser _parser;    
        private readonly string _idPropertyName;
        private List<string> _fields;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbQuery{TList, T}"/> class.
        /// </summary>
        /// <param name="collection">The Mongo database collection.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="fields">The fields of the data object to be selected.</param>
        /// <param name="idPropertyName">Name of the identifier property.</param>
        public MongoDbQuery(IMongoCollection<T> collection, WitsmlQueryParser parser, List<string> fields, string idPropertyName)
        {
            Logger = LogManager.GetLogger(GetType());

            _collection = collection;
            _parser = parser;
            _fields = fields;
            _idPropertyName = idPropertyName;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        protected ILog Logger { get; private set; }

        /// <summary>
        /// Executes this MongoDb query.
        /// </summary>
        /// <returns>The list of queried data object.</returns>
        public List<T> Execute()
        {
            Logger.DebugFormat("Executing query for entity: {0}", _parser.Context.ObjectType);

            var entities = new List<T>();
            var returnElements = _parser.ReturnElements();
            var privateGroupOnly = _parser.RequestPrivateGroupOnly();

            foreach (var element in _parser.Elements())
            {
                // Build Mongo filter
                var filter = BuildFilter(element, privateGroupOnly);
                var results = _collection.Find(filter ?? "{}");

                // Format response using MongoDb projection, i.e. selecting specified fields only
                if (OptionsIn.ReturnElements.All.Equals(returnElements))
                {
                    entities.AddRange(results.ToList());
                }
                else if (OptionsIn.ReturnElements.IdOnly.Equals(returnElements) || OptionsIn.ReturnElements.Requested.Equals(returnElements))
                {
                    var projection = BuildProjection(element, typeof(T));

                    if (projection != null)
                        results = results.Project<T>(projection);

                    entities.AddRange(results.ToList());
                }
            }

            return entities;
        }

        /// <summary>
        /// Builds the query filter.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="privateGroupOnly">Flag to indicate whether query private group only data objects.</param>
        /// <returns>The filter object that for the selection criteria for the queried entity.</returns>
        private FilterDefinition<T> BuildFilter(XElement element, bool privateGroupOnly)
        {
            var properties = GetPropertyInfo(typeof(T));
            var filters = new List<FilterDefinition<T>>();
            var privateGroupOnlyFilter = privateGroupOnly
                ? Builders<T>.Filter.Eq("CommonData.PrivateGroupOnly", privateGroupOnly)
                : Builders<T>.Filter.Ne("CommonData.PrivateGroupOnly", !privateGroupOnly);

            filters.Add(privateGroupOnlyFilter);

            var groupings = element.Elements().GroupBy(e => e.Name.LocalName);
            foreach (var group in groupings)
            {
                var propertyInfo = GetPropertyInfoForAnElement(properties, group.Key);
                var filter = BuildFilterForAnElementGroup(propertyInfo, group);
                if (filter != null)
                    filters.Add(filter);
            }
            foreach (var attribute in element.Attributes())
            {
                if (string.Compare(attribute.Name.LocalName, "nil", true) == 0)
                    continue;

                var attributeProp = GetPropertyInfoForAnElement(properties, attribute.Name.LocalName);
                var attributeFilter = BuildFilterForAttribute(attributeProp, attribute);
                if (attributeFilter != null)
                    filters.Add(attributeFilter);
            }

            var resultFilter = Builders<T>.Filter.And(filters);

            if (Logger.IsDebugEnabled)
            {
                var filterJson = resultFilter.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                Logger.DebugFormat("Detected query filters: {0}", filterJson);
            }

            return resultFilter;
        }

        /// <summary>
        /// Builds the filter for a group of elements.
        /// </summary>
        /// <param name="propertyInfo">The property information for the element group.</param>
        /// <param name="group">The list of elements.</param>
        /// <param name="path">The path of the element to the entity to be queried in the data store.</param>
        /// <returns>The filter object that for the selection criteria for the group of elements.</returns>
        private FilterDefinition<T> BuildFilterForAnElementGroup(PropertyInfo propertyInfo, IGrouping<string, XElement> group, string path = null)
        {
            var values = group.ToList();
            var count = values.Count;
            var propType = propertyInfo.PropertyType;

            var fieldName = propertyInfo.Name;
            if (!string.IsNullOrEmpty(path))
                fieldName = string.Format("{0}.{1}", path, fieldName);

            if (count == 1)
            {
                var element = values.FirstOrDefault();
                if (propType.IsGenericType)
                {
                    var genericType = propType.GetGenericTypeDefinition();
                    if (genericType == typeof(Nullable<>))
                    {
                        var underlyingType = Nullable.GetUnderlyingType(propType);
                        return BuildFilterForAnElementType(underlyingType, element, fieldName); ;
                    }
                    else if (genericType == typeof(List<>))
                    {
                        var childType = propType.GetGenericArguments()[0];
                        return BuildFilterForAnElementType(childType, element, fieldName);
                    }
                    return null;
                }
                else
                    return BuildFilterForAnElement(propertyInfo, element, path);
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
                if (listFilters.Count > 0)
                    return Builders<T>.Filter.Or(listFilters);
                else
                    return null;
            }
        }

        /// <summary>
        /// Builds the filter for an element which is not of generic type.
        /// </summary>
        /// <param name="propertyInfo">The property information for the element.</param>
        /// <param name="element">The element.</param>
        /// <param name="path">The path of the element to the entity to be queried in the data store.</param>
        /// <returns>The filter object that for the selection criteria for the element.</returns>
        private FilterDefinition<T> BuildFilterForAnElement(PropertyInfo propertyInfo, XElement element, string path = null)
        {
            var fieldName = propertyInfo.Name;
            if (!string.IsNullOrEmpty(path))
                fieldName = string.Format("{0}.{1}", path, fieldName);
            var propType = propertyInfo.PropertyType;

            if (!element.HasElements && !element.HasAttributes)
            {
                var elementValue = element.Value;
                var elementProps = GetPropertyInfo(propType).ToList();
                if (string.IsNullOrEmpty(elementValue))
                    return null;

                if (propType == typeof(string))
                    return Builders<T>.Filter.Regex(fieldName, new BsonRegularExpression("/^" + elementValue + "$/i"));
                else
                {
                    if (propertyInfo.Name == "DateTimeCreation" || propertyInfo.Name == "DateTimeLastChange")
                        return Builders<T>.Filter.Gt(fieldName, DateTime.Parse(elementValue));
                    else
                    {
                        if (propType.IsEnum)
                            return Builders<T>.Filter.Eq(fieldName, Enum.Parse(propType, elementValue));
                        else if (propType == typeof(DateTime))
                            return Builders<T>.Filter.Eq(fieldName, DateTime.Parse(elementValue));
                        return Builders<T>.Filter.Eq(fieldName, elementValue);
                    }
                }
            }
            else
            {
                var childFilters = new List<FilterDefinition<T>>();
                var childProps = GetPropertyInfo(propType).ToList();
                var childGroupings = element.Elements().GroupBy(e => e.Name.LocalName);
                foreach (var childGroup in childGroupings)
                {
                    var childPropertyInfo = GetPropertyInfoForAnElement(childProps, childGroup.Key);
                    var childFilter = BuildFilterForAnElementGroup(childPropertyInfo, childGroup, fieldName);
                    if (childFilter != null)
                        childFilters.Add(childFilter);
                }
                foreach (var attribute in element.Attributes())
                {
                    if (string.Compare(attribute.Name.LocalName, "nil", true) == 0)
                        continue;

                    var attributeProp = GetPropertyInfoForAnElement(childProps, attribute.Name.LocalName);
                    var attributeFilter = BuildFilterForAttribute(attributeProp, attribute, fieldName);
                    if (attributeFilter != null)
                        childFilters.Add(attributeFilter);
                }
                if (childFilters.Count > 0)
                    return Builders<T>.Filter.And(childFilters);
                else
                    return null;
            }
        }

        /// <summary>
        /// Builds the type of the filter for an element of generic type.
        /// </summary>
        /// <param name="propType">Type of the element.</param>
        /// <param name="element">The element.</param>
        /// <param name="fieldName">Name of the field for the element.</param>
        /// <returns>The filter object that for the selection criteria for the element.</returns>
        private FilterDefinition<T> BuildFilterForAnElementType(Type propType, XElement element, string fieldName)
        {
            if (!element.HasElements && !element.HasAttributes)
            {
                var elementValue = element.Value;
                var elementProps = GetPropertyInfo(propType).ToList();
                if (string.IsNullOrEmpty(elementValue))
                    return null;

                if (propType == typeof(string))
                    return Builders<T>.Filter.Regex(fieldName, new BsonRegularExpression("/^" + elementValue + "$/i"));
                else
                {
                    if (fieldName.EndsWith(".DateTimeCreation") || fieldName.EndsWith(".DateTimeLastChange"))
                        return Builders<T>.Filter.Gt(fieldName, DateTime.Parse(elementValue));
                    else
                    {
                        if (propType.IsEnum)
                            return Builders<T>.Filter.Eq(fieldName, Enum.Parse(propType, elementValue));
                        else if (propType == typeof(DateTime))
                            return Builders<T>.Filter.Eq(fieldName, DateTime.Parse(elementValue));
                        return Builders<T>.Filter.Eq(fieldName, elementValue);
                    }
                }
            }
            else
            {
                var childFilters = new List<FilterDefinition<T>>();
                var childProps = GetPropertyInfo(propType).ToList();
                var childGroupings = element.Elements().GroupBy(e => e.Name.LocalName);
                foreach (var childGroup in childGroupings)
                {
                    var childPropertyInfo = GetPropertyInfoForAnElement(childProps, childGroup.Key);
                    var childFilter = BuildFilterForAnElementGroup(childPropertyInfo, childGroup, fieldName);
                    if (childFilter != null)
                        childFilters.Add(childFilter);
                }
                foreach (var attribute in element.Attributes())
                {
                    if (string.Compare(attribute.Name.LocalName, "nil", true) == 0)
                        continue;

                    var attributeProp = GetPropertyInfoForAnElement(childProps, attribute.Name.LocalName);
                    var attributeFilter = BuildFilterForAttribute(attributeProp, attribute, fieldName);
                    if (attributeFilter != null)
                        childFilters.Add(attributeFilter);
                }
                if (childFilters.Count > 0)
                    return Builders<T>.Filter.And(childFilters);
                else
                    return null;
            }
        }

        /// <summary>
        /// Builds the filter for attribute.
        /// </summary>
        /// <param name="propertyInfo">The property information for the attribute.</param>
        /// <param name="attribute">The attribute.</param>
        /// <param name="path">The path of the attribute to the entity to be queried in the data store.</param>
        /// <returns>The filter object that for the selection criteria for the attribute.</returns>
        private FilterDefinition<T> BuildFilterForAttribute(PropertyInfo propertyInfo, XAttribute attribute, string path = null)
        {
            var value = attribute.Value;
            if (string.IsNullOrEmpty(value))
                return null;

            var fieldName = propertyInfo.Name;
            if (!string.IsNullOrEmpty(path))
                fieldName = string.Format("{0}.{1}", path, fieldName);

            var propType = propertyInfo.PropertyType;
            if (propType == typeof(string))
                return Builders<T>.Filter.Regex(fieldName, new BsonRegularExpression("/^" + value + "$/i"));
            else
                return Builders<T>.Filter.Eq(fieldName, value);
        }

        private IEnumerable<PropertyInfo> GetPropertyInfo(Type t)
        {
            return t.GetProperties().Where(p => p.IsDefined(typeof(XmlElementAttribute), false) || p.IsDefined(typeof(XmlAttributeAttribute), false));
        }

        /// <summary>
        /// Builds the projection for the query.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <param name="type">The object type of the element.</param>
        /// <returns>The projection object that contains the fields to be selected.</returns>
        private ProjectionDefinition<T> BuildProjection(XElement element, Type type)
        {
            Logger.DebugFormat("Building projection fields for entity: {0}", _parser.Context.ObjectType);

            if (element == null)
                return null;

            if (_fields == null)
                _fields = new List<string>();

            BuildProjectionForAnElement(element, null, type);

            if (_fields.Count == 0)
            {
                Logger.Warn("No fields projected.  Projection field count should never be zero.");
                return Builders<T>.Projection.Exclude(_idPropertyName).Include(string.Empty);
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

                if (!_fields.Contains(_idPropertyName))
                    projection = projection.Exclude(_idPropertyName);

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

            var properties = GetPropertyInfo(type);

            if (element.HasElements)
            {
                foreach (var child in element.Elements())
                {
                    var property = GetPropertyInfoForAnElement(properties, child.Name.LocalName);
                    BuildProjectionForAnElement(child, fieldPath, property);
                }
            }
            if (element.HasAttributes)
            {
                foreach (var attribute in element.Attributes())
                {
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

        /// <summary>
        /// Gets the property information for an element, for some element name is not the same as property name, i.e. Mongo field name.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <param name="element">The element.</param>
        /// <returns>The found property for the serialization element.</returns>
        private PropertyInfo GetPropertyInfoForAnElement(IEnumerable<PropertyInfo> properties, string name)
        {
            foreach (var prop in properties)
            {
                var elementAttribute = prop.GetCustomAttribute(typeof(XmlElementAttribute), false) as XmlElementAttribute;
                if (elementAttribute != null)
                {
                    if (elementAttribute.ElementName == name)
                        return prop;
                }

                var attributeAttribute = prop.GetCustomAttribute(typeof(XmlAttributeAttribute), false) as XmlAttributeAttribute;
                if (attributeAttribute != null)
                {
                    if (attributeAttribute.AttributeName == name)
                        return prop;
                }
            }
            return null;
        }

        private void AddProjectionProperty(string propertyPath)
        {
            if (_fields.Contains(propertyPath))
                return;

            _fields.Add(propertyPath);
        }

        private string GetPropertyPath(string parentPath, string propertyName)
        {
            var prefix = string.IsNullOrEmpty(parentPath) ? string.Empty : string.Format("{0}.", parentPath);
            return string.Format("{0}{1}", prefix, CaptalizeString(propertyName));
        }

        private string CaptalizeString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = char.ToUpper(input[0]).ToString();

            if (input.Length > 1)
                result += input.Substring(1);

            return result;
        }
    }
}
