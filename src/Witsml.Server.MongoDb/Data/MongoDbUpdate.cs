using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using Energistics.DataAccess;
using log4net;
using MongoDB.Driver;

namespace PDS.Witsml.Server.Data
{
    public class MongoDbUpdate<T>
    {
        private readonly IMongoCollection<T> _collection;
        private readonly WitsmlQueryParser _parser;
        private readonly string _idPropertyName;
        private readonly string[] _ignored;

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

        public void Update(T entity, DataObjectId dataObjectId)
        {
            var dataObj = entity as IDataObject;
            var abstractObj = entity as Energistics.DataAccess.WITSML200.ComponentSchemas.AbstractObject;
            var uidValue = abstractObj == null ? dataObj.Uid : abstractObj.Uuid;

            var filter = MongoDbFieldHelper.GetEntityFilter<T>(dataObjectId, _idPropertyName);
            var update = Builders<T>.Update.Set(_idPropertyName, uidValue);

            var element = _parser.Element();
            update = BuildUpdate(update, element, entity);

            if (Logger.IsDebugEnabled)
            {
                var updateJson = update.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                Logger.DebugFormat("Detected update elements: {0}", updateJson);
            }

            var result = _collection.UpdateOne(filter, update);
        }

        public void Update(Dictionary<string, T> replacements, string field = null)
        {
            foreach (var key in replacements.Keys)
            {
                var filter = BuildIdFilter(field ?? _idPropertyName, key);
                _collection.ReplaceOne(filter, replacements[key]);
            }
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
            return BuildUpdateForAnElement(update, element, typeof(T));
        }

        private UpdateDefinition<T> BuildUpdateForAnElement(UpdateDefinition<T> update, XElement element, Type type, string parentPath = null)
        {
            if (_ignored.Contains(element.Name.LocalName))
                return update;

            var properties = MongoDbFieldHelper.GetPropertyInfo(type);
            var groupings = element.Elements().GroupBy(e => e.Name.LocalName);

            foreach (var group in groupings)
            {
                if (_ignored.Contains(group.Key))
                    continue;

                var propertyInfo = MongoDbFieldHelper.GetPropertyInfoForAnElement(properties, group.Key);  
                update = BuildUpdateForAnElementGroup(update, propertyInfo, group, parentPath);
            }

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Name == MongoDbFieldHelper.Xsi("nil") || attribute.Name == MongoDbFieldHelper.Xsi("type"))
                    continue;

                var attributeProp = MongoDbFieldHelper.GetPropertyInfoForAnElement(properties, attribute.Name.LocalName);
                update = BuildUpdateForAttribute(update, attributeProp, attribute, parentPath);
            }

            return update;
        }

        private UpdateDefinition<T> BuildUpdateForAnElementGroup(UpdateDefinition<T> update, PropertyInfo propertyInfo, IEnumerable<XElement> elements, string parentPath = null)
        {
            if (propertyInfo == null)
                return update;

            var fieldName = MongoDbFieldHelper.GetPropertyPath(parentPath, propertyInfo.Name);
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
                        update = BuildUpdateForAnElementType(update, underlyingType, element, fieldName);

                        // set the *Specified property when updating nullable elements
                        if (propertyInfo.DeclaringType.GetProperty(propertyInfo.Name + "Specified") != null)
                            update = update.Set(fieldName + "Specified", true);

                        return update;
                    }
                    else if (genericType == typeof(List<>))
                    {
                        var childType = propType.GetGenericArguments()[0];
                        return BuildUpdateForAnElementType(update, childType, element, fieldName);
                    }
                }
                else if (propType.IsAbstract)
                {
                    var concreteType = MongoDbFieldHelper.GetConcreteType(element, propType);
                    return BuildUpdateForAnElementType(update, concreteType, element, fieldName);
                }
                else
                {
                    return BuildUpdateForAnElementType(update, propType, element, fieldName);
                }
            }
            else
            {
                var childType = propType.GetGenericArguments()[0];

                foreach (var value in values)
                {
                    //BuildUpdateForAnElementType(builder, childType, value, fieldName);
                }

                return update;
            }

            return update;
        }

        private UpdateDefinition<T> BuildUpdateForAnElementType(UpdateDefinition<T> update, Type elementType, XElement element, string propertyPath)
        {
            var textProperty = elementType.GetProperties().FirstOrDefault(x => x.IsDefined(typeof(XmlTextAttribute), false));

            if (textProperty != null)
            {
                var uomProperty = elementType.GetProperty("Uom");
                var fieldName = MongoDbFieldHelper.GetPropertyPath(propertyPath, textProperty.Name);
                var fieldType = textProperty.PropertyType;
                var filters = new List<FilterDefinition<T>>();

                if (uomProperty != null)
                {
                    var uomPath = MongoDbFieldHelper.GetPropertyPath(propertyPath, uomProperty.Name);
                    var uomValue = MongoDbFieldHelper.ValidateMeasureUom(element, uomProperty, element.Value);
                    update = BuildUpdateForProperty(update, uomProperty.PropertyType, uomPath, uomValue);
                }

                return BuildUpdateForProperty(update, fieldType, fieldName, element.Value);             
            }
            else if (element.HasElements || element.HasAttributes)
            {
                return BuildUpdateForAnElement(update, element, elementType, propertyPath);
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
                var value = MongoDbFieldHelper.ParseEnum(propertyType, propertyValue);
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
            var propertyPath = MongoDbFieldHelper.GetPropertyPath(parentPath, propertyInfo.Name);
            var propertyType = propertyInfo.PropertyType;

            return BuildUpdateForProperty(update, propertyType, propertyPath, attribute.Value);
        }
    }
}
