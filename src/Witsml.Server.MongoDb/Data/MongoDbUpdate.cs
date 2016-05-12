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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using Energistics.DataAccess;
using Energistics.Datatypes;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Data;

namespace PDS.Witsml.Server.Data
{
    public class MongoDbUpdate<T> : DataObjectNavigator<MongoDbUpdateContext<T>>
    {
        private readonly IMongoCollection<T> _collection;
        private readonly WitsmlQueryParser _parser;
        private readonly string _idPropertyName;

        private FilterDefinition<T> _entityFilter;
        private T _entity;

        public MongoDbUpdate(IMongoCollection<T> collection, WitsmlQueryParser parser, string idPropertyName = "Uid", List<string> ignored = null) : base(new MongoDbUpdateContext<T>())
        {
            Context.Ignored = ignored;

            _collection = collection;
            _parser = parser;
            _idPropertyName = idPropertyName;            
        }

        public void Update(T entity, EtpUri uri, Dictionary<string, object> updates)
        {
             _entityFilter = MongoDbUtility.GetEntityFilter<T>(uri, _idPropertyName);       

            var update = Update(updates, uri.ObjectId);
            var element = _parser.Element();

            Context.Update = update;
            _entity = entity;
            BuildUpdate(element);

            if (Logger.IsDebugEnabled)
            {
                var updateJson = update.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                Logger.DebugFormat("Detected update elements: {0}", updateJson);
            }

            _collection.UpdateOne(_entityFilter, Context.Update);
        }

        public UpdateDefinition<T> Update(Dictionary<string, object> updates, string uidValue)
        {
            var update = Builders<T>.Update.Set(_idPropertyName, uidValue);

            foreach (var pair in updates)
                update = update.Set(pair.Key, pair.Value);

            return update;
        }

        public void UpdateFields(FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            if (Logger.IsDebugEnabled)
            {
                var filterJson = filter.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                var updateJson = update.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
            }
            _collection.UpdateOne(filter, update);
        }

        private void BuildUpdate(XElement element)
        {
            NavigateElement(element, Context.DataObjectType, null);
        }

        protected override void PushPropertyInfo(PropertyInfo propertyInfo)
        {
            object propertyValue;
            if (Context.PropertyValueList.Count == 0)
            {
                propertyValue = propertyInfo.GetValue(_entity);
            }
            else
            {
                propertyValue = propertyInfo.GetValue(Context.PropertyValueList.Last());
            }

            PushPropertyInfo(propertyInfo, propertyValue);
        }

        protected void PushPropertyInfo(PropertyInfo propertyInfo, object propertyValue)
        {
            Context.PropertyInfoList.Add(propertyInfo);
            Context.PropertyValueList.Add(propertyValue);
        }

        protected override void PopPropertyInfo()
        {
            Context.PropertyInfoList.Remove(Context.PropertyInfoList.Last());
            Context.PropertyValueList.Remove(Context.PropertyValueList.Last());
        }

        protected override void NavigateNullableElementType(Type elementType, XElement element, string propertyPath, PropertyInfo propertyInfo)
        {
            NavigateElementType(elementType, element, propertyPath);

            if (propertyInfo.DeclaringType.GetProperty(propertyInfo.Name + "Specified") != null)
                Context.Update = Context.Update.Set(propertyPath + "Specified", true);                
        }

        protected override void HandleStringValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            Context.Update = Context.Update.Set(propertyPath, propertyValue);
        }

        protected override void HandleDateTimeValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, DateTime dateTimeValue)
        {
            Context.Update = Context.Update.Set(propertyPath, dateTimeValue);
        }

        protected override void HandleTimestampValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, Timestamp timestampValue)
        {
            Context.Update = Context.Update.Set(propertyPath, timestampValue);
        }

        protected override void HandleObjectValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, object objectValue)
        {
            Context.Update = Context.Update.Set(propertyPath, objectValue);
        }
    
        protected override void HandleNullValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            UnsetProperty(xmlObject, propertyType, propertyPath, propertyValue);
        }

        protected override void HandleNaNValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            UnsetProperty(xmlObject, propertyType, propertyPath, propertyValue);
        }

        protected override void NavigateRecurringElement(List<XElement> elements, Type childType, string propertyPath, PropertyInfo propertyInfo)
        {           
            NavigateArrayElementType(elements, childType, null, propertyPath, propertyInfo);
        }

        protected override void NavigateArrayElementType(List<XElement> elements, Type childType, XElement element, string propertyPath, PropertyInfo propertyInfo)
        {
            UpdateArrayElements(elements, propertyInfo, Context.PropertyValueList.Last(), childType, propertyPath);
        }

        private void UnsetProperty(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            if (Context.PropertyInfoList.Last().IsDefined(typeof(RequiredAttribute), false))
                throw new WitsmlException(ErrorCodes.MissingRequiredData);

            Context.Update = Context.Update.Unset(propertyPath);
        }

        private void UpdateArrayElements(List<XElement> elements, PropertyInfo propertyInfo, object propertyValue, Type type, string parentPath)
        {
            var updateBuilder = Builders<T>.Update;
            var filterBuilder = Builders<T>.Filter;
            var idField = MongoDbUtility.LookUpIdField(type);
            var filterPath = GetPropertyPath(parentPath, idField);
            var properties = GetPropertyInfo(type);
            var positionPath = parentPath + ".$";
            var filters = new List<FilterDefinition<T>>() { _entityFilter };

            foreach (var element in elements)
            {
                var elementId = GetElementId(element, idField);
                if (string.IsNullOrEmpty(elementId))
                    continue;

                var current = GetCurrentElementValue(idField, elementId, properties, propertyValue);

                if (current == null)
                {
                    ValidateArrayElement(element, properties);

                    // update element name to match XSD type
                    var xmlType = type.GetCustomAttribute<XmlTypeAttribute>();
                    element.Name = xmlType != null ? xmlType.TypeName : element.Name;

                    var item = WitsmlParser.Parse(type, element.ToString());

                    var update = updateBuilder.Push(parentPath, item);
                    _collection.UpdateOne(filterBuilder.And(filters), update);
                }
                else
                {
                    ValidateArrayElement(element, properties, false);

                    var elementFilter = Builders<T>.Filter.EqIgnoreCase(filterPath, elementId);
                    filters.Add(elementFilter);
                    var filter = filterBuilder.And(filters);

                    var update = updateBuilder.Set(GetPropertyPath(positionPath, idField), elementId);

                    var saveUpdate = Context.Update;
                    Context.Update = update;
                  
                    PushPropertyInfo(propertyInfo, current);
                    NavigateElement(element, type, positionPath);
                    PopPropertyInfo();

                    if (Logger.IsDebugEnabled)
                    {
                        var filterJson = filter.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                        var updateJson = update.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                    }

                    _collection.UpdateOne(filter, Context.Update);
                    Context.Update = saveUpdate;                   
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
                var prop = GetPropertyInfoForAnElement(properties, idField);
                var idValue = prop.GetValue(item).ToString();
                if (elementId.EqualsIgnoreCase(idValue))
                    return item;
            }
            return null;
        }

        private void ValidateArrayElement(XElement element, IList<PropertyInfo> properties, bool isAdd = true)
        {          
            if (isAdd)
            {
                WitsmlParser.RemoveEmptyElements(element);

                if (!element.HasElements)
                    throw new WitsmlException(ErrorCodes.EmptyNewElementsOrAttributes);
            }
            else
            {
                var emptyElements = element.Elements()
                    .Where(e => e.IsEmpty || string.IsNullOrWhiteSpace(e.Value))
                    .ToList();

                foreach (var child in emptyElements)
                {
                    var prop = GetPropertyInfoForAnElement(properties, child.Name.LocalName);
                    if (prop == null) continue;

                    if (prop.IsDefined(typeof(RequiredAttribute), false))
                        throw new WitsmlException(ErrorCodes.EmptyNewElementsOrAttributes);
                }

                var emptyAttributes = element.Attributes()
                    .Where(a => string.IsNullOrWhiteSpace(a.Value))
                    .ToList();

                foreach (var child in emptyAttributes)
                {
                    var prop = GetPropertyInfoForAnElement(properties, child.Name.LocalName);
                    if (prop == null) continue;

                    if (prop.IsDefined(typeof(RequiredAttribute), false))
                        throw new WitsmlException(ErrorCodes.MissingRequiredData);
                }
            }
        }
    }
}
