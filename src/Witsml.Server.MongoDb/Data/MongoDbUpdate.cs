using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using Energistics.DataAccess;
using Energistics.Datatypes;
using log4net;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using PDS.Framework;

namespace PDS.Witsml.Server.Data
{
    public class MongoDbUpdate<T>
    {
        private readonly IMongoCollection<T> _collection;
        private readonly WitsmlQueryParser _parser;
        private readonly string _idPropertyName;
        private readonly string[] _ignored;

        private FilterDefinition<T> _entityFilter;

        public MongoDbUpdate(IMongoCollection<T> collection, WitsmlQueryParser parser, string idPropertyName = "Uid", string[] ignored = null)
        {
            Logger = LogManager.GetLogger(GetType());

            _collection = collection;
            _parser = parser;
            _idPropertyName = idPropertyName;
            _ignored = ignored ?? new string[0];
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILog Logger { get; private set; }

        public void Update(T entity, EtpUri uri, Dictionary<string, object> updates)
        {
            _entityFilter = MongoDbUtility.GetEntityFilter<T>(uri, _idPropertyName);

            var update = Update(updates, uri.ObjectId);
            var element = _parser.Element();

            update = BuildUpdate(update, element, entity);

            if (Logger.IsDebugEnabled)
            {
                var updateJson = update.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                Logger.DebugFormat("Detected update elements: {0}", updateJson);
            }

            var result = _collection.UpdateOne(_entityFilter, update);
        }

        public UpdateDefinition<T> Update(Dictionary<string, object> updates, string uidValue)
        {
            var update = Builders<T>.Update.Set(_idPropertyName, uidValue);

            foreach (var pair in updates)
                update = update.Set(pair.Key, pair.Value);

            return update;
        }

        public void Update(Dictionary<string, T> replacements, string field = null)
        {
            foreach (var key in replacements.Keys)
            {
                var filter = BuildIdFilter(field ?? _idPropertyName, key);
                _collection.ReplaceOne(filter, replacements[key]);
            }
        }

        public void UpdateFields(FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            var filterJson = filter.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
            _collection.UpdateOne(filter, update);
        }

        private FilterDefinition<T> BuildIdFilter(XElement element)
        {
            return null;
        }

        private FilterDefinition<T> BuildIdFilter(string field, string value)
        {
            return Builders<T>.Filter.Eq(field, value);
        }

        private UpdateDefinition<T> BuildUpdate(UpdateDefinition<T> update, XElement element, T entity)
        {
            return BuildUpdateForAnElement(update, entity, element, typeof(T));
        }

        private UpdateDefinition<T> BuildUpdateForAnElement(UpdateDefinition<T> update, object obj, XElement element, Type type, string parentPath = null)
        {
            var properties = MongoDbUtility.GetPropertyInfo(type);

            var groupings = element.Elements().GroupBy(e => e.Name.LocalName);

            foreach (var group in groupings)
            {
                if (_ignored.Contains(group.Key))
                    continue;

                var propertyInfo = MongoDbUtility.GetPropertyInfoForAnElement(properties, group.Key);
                var propertyValue = propertyInfo.GetValue(obj);
                update = BuildUpdateForAnElementGroup(update, propertyInfo, propertyValue, group, new List<FilterDefinition<T>> { _entityFilter }, parentPath);
            }

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Name == MongoDbUtility.Xsi("nil") || attribute.Name == MongoDbUtility.Xsi("type"))
                    continue;

                var attributeProp = MongoDbUtility.GetPropertyInfoForAnElement(properties, attribute.Name.LocalName);
                update = BuildUpdateForAttribute(update, attributeProp, attribute, parentPath);
            }

            return update;
        }

        private UpdateDefinition<T> BuildUpdateForAnElementGroup(UpdateDefinition<T> update, PropertyInfo propertyInfo, object propertyValue, IEnumerable<XElement> elements, List<FilterDefinition<T>> filters, string parentPath = null)
        {
            if (propertyInfo == null)
                return update;

            var fieldName = MongoDbUtility.GetPropertyPath(parentPath, propertyInfo.Name);
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
                        update = BuildUpdateForAnElementType(update, underlyingType, propertyValue, element, fieldName);

                        // set the *Specified property when updating nullable elements
                        if (propertyInfo.DeclaringType.GetProperty(propertyInfo.Name + "Specified") != null)
                            update = update.Set(fieldName + "Specified", true);

                        return update;
                    }
                    else if (genericType == typeof(List<>))
                    {
                        var childType = propType.GetGenericArguments()[0];
                        UpdateArrayElements(values, propertyInfo, propertyValue, childType, fieldName, filters);
                        return update;
                    }
                }
                else if (propType.IsAbstract)
                {
                    var concreteType = MongoDbUtility.GetConcreteType(element, propType);
                    return BuildUpdateForAnElementType(update, concreteType, propertyValue, element, fieldName);
                }
                else
                {
                    return BuildUpdateForAnElementType(update, propType, propertyValue, element, fieldName);
                }
            }
            else
            {
                var childType = propType.GetGenericArguments()[0];
                UpdateArrayElements(values, propertyInfo, propertyValue, childType, fieldName, filters);
                return update;
            }

            return update;
        }

        private UpdateDefinition<T> BuildUpdateForAnElementType(UpdateDefinition<T> update, Type elementType, object value, XElement element, string propertyPath)
        {
            var textProperty = elementType.GetProperties().FirstOrDefault(x => x.IsDefined(typeof(XmlTextAttribute), false));

            if (textProperty != null)
            {
                var uomProperty = elementType.GetProperty("Uom");
                var fieldName = MongoDbUtility.GetPropertyPath(propertyPath, textProperty.Name);
                var fieldType = textProperty.PropertyType;
                var filters = new List<FilterDefinition<T>>();

                if (uomProperty != null)
                {
                    var uomPath = MongoDbUtility.GetPropertyPath(propertyPath, uomProperty.Name);
                    var uomValue = MongoDbUtility.ValidateMeasureUom(element, uomProperty, element.Value);
                    update = BuildUpdateForProperty(update, uomProperty.PropertyType, uomPath, uomValue);
                }

                return BuildUpdateForProperty(update, fieldType, fieldName, element.Value);
            }
            else if (element.HasElements || element.HasAttributes)
            {
                return BuildUpdateForAnElement(update, value, element, elementType, propertyPath);
            }

            return BuildUpdateForProperty(update, elementType, propertyPath, element.Value);
        }

        private UpdateDefinition<T> BuildUpdateForProperty(UpdateDefinition<T> update, Type propertyType, string propertyPath, string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyValue))
            {
                return update.Unset(propertyPath);
            }

            if (propertyType == typeof(string))
            {
                return update.Set(propertyPath, propertyValue);
            }
            else if (propertyType.IsEnum)
            {
                var value = MongoDbUtility.ParseEnum(propertyType, propertyValue);
                return update.Set(propertyPath, value);
            }
            else if (propertyType == typeof(DateTime))
            {
                DateTime value;

                if (!DateTime.TryParse(propertyValue, out value))
                    throw new WitsmlException(ErrorCodes.InputTemplateNonConforming);

                return update.Set(propertyPath, value);
            }
            else if (propertyType == typeof(Timestamp))
            {
                DateTimeOffset value;

                if (!DateTimeOffset.TryParse(propertyValue, out value))
                    throw new WitsmlException(ErrorCodes.InputTemplateNonConforming);

                return update.Set(propertyPath, new Timestamp(value));
            }
            else if (typeof(IConvertible).IsAssignableFrom(propertyType))
            {
                var value = Convert.ChangeType(propertyValue, propertyType);
                return update.Set(propertyPath, value);
            }
            else
            {
                return update.Set(propertyPath, propertyValue);
            }
        }

        private UpdateDefinition<T> BuildUpdateForAttribute(UpdateDefinition<T> update, PropertyInfo propertyInfo, XAttribute attribute, string parentPath = null)
        {
            var propertyPath = MongoDbUtility.GetPropertyPath(parentPath, propertyInfo.Name);
            var propertyType = propertyInfo.PropertyType;

            return BuildUpdateForProperty(update, propertyType, propertyPath, attribute.Value);
        }

        private void UpdateArrayElements(List<XElement> elements, PropertyInfo propertyInfo, object propertyValue, Type type, string parentPath, List<FilterDefinition<T>> filters)
        {
            var updateBuilder = Builders<T>.Update;
            var filterBuilder = Builders<T>.Filter;
            var idField = "Uid";
            var filterPath = MongoDbUtility.GetPropertyPath(parentPath, idField);
            var properties = MongoDbUtility.GetPropertyInfo(type);
            var positionPath = parentPath + ".$";

            var classMap = BsonClassMap.LookupClassMap(type);
            if (classMap != null && classMap.IdMemberMap != null)
                idField = classMap.IdMemberMap.MemberName;

            foreach (var element in elements)
            {
                var elementId = GetElementId(element, idField);
                if (string.IsNullOrEmpty(elementId))
                    continue;

                var current = GetCurrentElementValue(idField, elementId, properties, propertyValue);

                if (current == null)
                {
                    // update element name to match XSD type
                    var xmlType = type.GetCustomAttribute<XmlTypeAttribute>();
                    element.Name = xmlType != null ? xmlType.TypeName : element.Name;

                    var item = typeof(WitsmlParser).GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(x=>x.GetGenericArguments().Any())
                        .MakeGenericMethod(type)
                        .Invoke(null, new object[] { element.ToString() });

                    var update = updateBuilder.Push(parentPath, item);
                    _collection.UpdateOne(filterBuilder.And(filters), update);
                }
                else
                {
                    var elementFilter = Builders<T>.Filter.EqIgnoreCase(filterPath, elementId);
                    filters.Add(elementFilter);
                    var filter = filterBuilder.And(filters);

                    var update = updateBuilder.Set(MongoDbUtility.GetPropertyPath(positionPath, idField), elementId);
                    update = BuildUpdateForAnElement(update, current, element, type, positionPath);

                    var filterJson = filter.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                    var updateJson = update.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);

                    _collection.UpdateOne(filter, update);
                }
            }
        }

        private string GetElementId(XElement element, string idField)
        {
            var idAttribute = element.Attributes().FirstOrDefault(a => a.Name.LocalName.EqualsIgnoreCase(idField));
            if (idAttribute != null)
                return idAttribute.Value.Trim();

            var idElement = element.Elements().FirstOrDefault(e => e.Name.LocalName.EqualsIgnoreCase(idField));
            if (idElement != null)
                return idElement.Value.Trim();

            return string.Empty;
        }

        private object GetCurrentElementValue(string idField, string elementId, IEnumerable<PropertyInfo> properties, object propertyValue)
        {
            var list = (IEnumerable)propertyValue;
            foreach (var item in list)
            {
                var prop = MongoDbUtility.GetPropertyInfoForAnElement(properties, idField);
                var idValue = prop.GetValue(item).ToString();
                if (elementId.EqualsIgnoreCase(idValue))
                    return item;
            }
            return null;
        }
    }
}
