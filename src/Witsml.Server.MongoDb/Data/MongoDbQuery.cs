using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using log4net;
using MongoDB.Driver;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Encloses MongoDb query method and its helper methods
    /// </summary>
    /// <typeparam name="T">The type of queried data object.</typeparam>
    public class MongoDbQuery<T>
    {
        private static readonly XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

        private readonly IMongoCollection<T> _collection;
        private readonly WitsmlQueryParser _parser;    
        private List<string> _fields;
        private List<string> _ignored;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbQuery{T}"/> class.
        /// </summary>
        /// <param name="collection">The Mongo database collection.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="fields">The fields of the data object to be selected.</param>
        public MongoDbQuery(IMongoCollection<T> collection, WitsmlQueryParser parser, List<string> fields, List<string> ignored = null)
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
                if (OptionsIn.ReturnElements.All.Equals(returnElements))
                {
                    entities.AddRange(results.ToList());
                }
                else if (OptionsIn.ReturnElements.IdOnly.Equals(returnElements) ||
                         OptionsIn.ReturnElements.HeaderOnly.Equals(returnElements) ||
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
                ? Builders<T>.Filter.Eq("CommonData.PrivateGroupOnly", privateGroupOnly)
                : Builders<T>.Filter.Ne("CommonData.PrivateGroupOnly", !privateGroupOnly);

            filters.Add(privateGroupOnlyFilter);

            var filter = BuildFilterForAnElement(element, typeof(T), null);

            if (filter != null)
                filters.Add(filter);

            var resultFilter = Builders<T>.Filter.And(filters);
            var filterJson = resultFilter.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);

            if (Logger.IsDebugEnabled)
            {
                filterJson = resultFilter.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
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
                    return null;

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
                        return BuildFilterForAnElementType(underlyingType, element, fieldName); ;
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

                if (uomProperty != null)
                {
                    ValidateMeasureUom(element, uomProperty, element.Value);
                }

                return BuildFilterForProperty(fieldType, fieldName, element.Value);
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
                try
                {
                    return Builders<T>.Filter.Eq(propertyPath, Enum.Parse(propertyType, propertyValue));
                }
                catch (Exception ex)
                {
                    Logger.WarnFormat("Error parsing query filter for enum type: {0}; value: {1}; Error: {2}", propertyType, propertyValue, ex);
                    return null;
                }
            }
            else if (typeof(DateTime).IsAssignableFrom(propertyType))
            {
                try
                {
                    return Builders<T>.Filter.Eq(propertyPath, DateTime.Parse(propertyValue));
                }
                catch (Exception ex)
                {
                    Logger.WarnFormat("Error parsing query filter for date type: {0}; value: {1}; Error: {2}", propertyType, propertyValue, ex);
                    return null;
                }
            }
            else
            {
                return Builders<T>.Filter.Eq(propertyPath, propertyValue);
            }
        }

        private IEnumerable<PropertyInfo> GetPropertyInfo(Type t)
        {
            return t.GetProperties().Where(p => !p.IsDefined(typeof(XmlIgnoreAttribute), false));
        }

        private Type GetConcreteType(XElement element, Type propType)
        {
            var xsiType = element.Attributes()
                .Where(x => x.Name == Xsi("type"))
                .Select(x => x.Value.Split(':'))
                .FirstOrDefault();

            var @namespace = element.Attributes()
                .Where(x => x.Name == Xmlns(xsiType.FirstOrDefault()))
                .Select(x => x.Value)
                .FirstOrDefault();

            var typeName = xsiType.LastOrDefault();

            return propType.Assembly.GetTypes()
                .FirstOrDefault(t =>
                {
                    var xmlType = t.GetCustomAttribute<XmlTypeAttribute>();
                    return xmlType != null && xmlType.TypeName == typeName && xmlType.Namespace == @namespace;
                });
        }

        private void ValidateMeasureUom(XElement element, PropertyInfo uomProperty, string measureValue)
        {
            var xmlAttribute = uomProperty.GetCustomAttribute<XmlAttributeAttribute>();

            // validation not needed if uom attribute is not defined
            if (xmlAttribute == null)
                return;

            var uomValue = element.Attributes()
                .Where(x => x.Name.LocalName == xmlAttribute.AttributeName)
                .Select(x => x.Value)
                .FirstOrDefault();

            // uom is required when a measure value is specified
            if (!string.IsNullOrWhiteSpace(measureValue) && string.IsNullOrWhiteSpace(uomValue))
            {
                throw new WitsmlException(ErrorCodes.MissingUnitForMeasureData);
            }

            var enumType = uomProperty.PropertyType;
            var hasXmlEnum = enumType.GetMembers().Any(x =>
            {
                var xmlEnumAttrib = x.GetCustomAttribute<XmlEnumAttribute>();
                return xmlEnumAttrib != null && xmlEnumAttrib.Name == uomValue;
            });

            // uom must be a valid enumeration member
            if (enumType.IsEnum && !enumType.IsEnumDefined(uomValue) && !hasXmlEnum)
            {
                throw new WitsmlException(ErrorCodes.InvalidUnitOfMeasure);
            }
        }

        /// <summary>
        /// Builds the projection for the query.
        /// </summary>
        /// <param name="parser">The parser.</param>
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
                return Builders<T>.Projection.Include(string.Empty);
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

        private XName Xmlns(string attributeName)
        {
            return XNamespace.Xmlns.GetName(attributeName);
        }

        private XName Xsi(string attributeName)
        {
            return xsi.GetName(attributeName);
        }
    }
}
