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
using System.ComponentModel.Composition;
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
using PDS.Witsml.Server.Data.Common;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Encloses MongoDb update method and its helper methods
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    /// <seealso cref="PDS.Witsml.Data.DataObjectNavigator{MongoDbUpdateContext}" />
    public class MongoDbUpdate<T> : DataObjectNavigator<MongoDbUpdateContext<T>>
    {
        private readonly IMongoCollection<T> _collection;
        private readonly WitsmlQueryParser _parser;
        private readonly string _idPropertyName;

        private FilterDefinition<T> _entityFilter;
        private T _entity;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbUpdate{T}" /> class.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="idPropertyName">Name of the identifier property.</param>
        /// <param name="ignored">The ignored.</param>
        /// <param name="container">The container.</param>
        public MongoDbUpdate(IMongoCollection<T> collection, WitsmlQueryParser parser, string idPropertyName = "Uid", List<string> ignored = null, IContainer container = null) : base(new MongoDbUpdateContext<T>())
        {
            Logger.Debug("Instance created.");
            Context.Ignored = ignored;

            _collection = collection;
            _parser = parser;
            _idPropertyName = idPropertyName;
            Container = container;
        }

        /// <summary>
        /// Gets or sets the composition container used for dependency injection.
        /// </summary>
        /// <value>The composition container.</value>
        //[Import]
        public IContainer Container { get; set; }

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="updates">The updates.</param>
        public void Update(T entity, EtpUri uri, Dictionary<string, object> updates)
        {
            Logger.DebugFormat("Updating data object: {0}", uri);

            _entityFilter = MongoDbUtility.GetEntityFilter<T>(uri, _idPropertyName);
            _entity = entity;

            Context.Update = Update(updates, uri.ObjectId);
            BuildUpdate(_parser.Element());

            LogUpdateFilter(_entityFilter, Context.Update);
            _collection.UpdateOne(_entityFilter, Context.Update);

            WitsmlOperationContext.Current.Warnings.AddRange(Context.Warnings);
        }

        /// <summary>
        /// Creates an <see cref="UpdateDefinition{T}"/> based on the supplied dictionary of name/value pairs.
        /// </summary>
        /// <param name="updates">The updates.</param>
        /// <param name="uidValue">The uid value.</param>
        /// <returns>The update definition.</returns>
        public UpdateDefinition<T> Update(Dictionary<string, object> updates, string uidValue)
        {
            var update = Builders<T>.Update.Set(_idPropertyName, uidValue);

            foreach (var pair in updates)
                update = update.Set(pair.Key, pair.Value);

            return update;
        }

        /// <summary>
        /// Updates a document using the specified filter and update definitions.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="update">The update.</param>
        public void UpdateFields(FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            LogUpdateFilter(filter, update);
            _collection.UpdateOne(filter, update);
        }

        /// <summary>
        /// Navigates the element group.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elements">The elements.</param>
        /// <param name="parentPath">The parent path.</param>
        protected override void NavigateElementGroup(PropertyInfo propertyInfo, IGrouping<string, XElement> elements, string parentPath)
        {
            PushPropertyInfo(propertyInfo);

            var parentValue = Context.PropertyValues.LastOrDefault();

            if (parentValue == null)
            {
                var elementList = elements.ToList();

                if (elementList.Count == 1 && 
                    !propertyInfo.PropertyType.FullName.StartsWith("System.") && 
                    propertyInfo.PropertyType != typeof(Timestamp))
                {
                    var propertyPath = GetPropertyPath(parentPath, propertyInfo.Name);
                    var childValue = ParseNestedElement(propertyInfo.PropertyType, elementList.First());

                    var childValidator = new MongoDbUpdate<T>(_collection, _parser, _idPropertyName, Context.Ignored);
                    childValidator.Context.ValidationOnly = true;
                    childValidator.NavigateElementType(propertyInfo, propertyInfo.PropertyType, elementList[0], propertyPath);

                    HandleObjectValue(propertyInfo, null, null, propertyPath, null, childValue);
                }
                else
                {
                    base.NavigateElementGroup(propertyInfo, elements, parentPath);
                }
            }
            else
            {
                base.NavigateElementGroup(propertyInfo, elements, parentPath);
            }

            PopPropertyInfo();
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
            NavigateArrayElementType(propertyInfo, elements, childType, null, propertyPath);
        }

        /// <summary>
        /// Navigates the array element type.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elements">The elements.</param>
        /// <param name="childType">Type of the child.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The property path.</param>
        protected override void NavigateArrayElementType(PropertyInfo propertyInfo, List<XElement> elements, Type childType, XElement element, string propertyPath)
        {
            UpdateArrayElements(elements, propertyInfo, Context.PropertyValues.Last(), childType, propertyPath);
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
        /// <exception cref="WitsmlException"></exception>
        protected override void NavigateUomAttribute(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string measureValue, string uomValue)
        {
            // Throw error -446 if uomValue is specified but measureValue is missing or NaN
            if (!string.IsNullOrWhiteSpace(uomValue) && (string.IsNullOrWhiteSpace(measureValue) || measureValue.EqualsIgnoreCase("NaN")))
                throw new WitsmlException(ErrorCodes.MissingMeasureDataForUnit);

            NavigateProperty(propertyInfo, xmlObject, propertyType, propertyPath, uomValue);
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
            SetProperty(propertyInfo, propertyPath, propertyValue);
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
            SetProperty(propertyInfo, propertyPath, dateTimeValue);
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
            SetProperty(propertyInfo, propertyPath, timestampValue);
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
            SetProperty(propertyInfo, propertyPath, objectValue);
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
            UnsetProperty(propertyInfo, propertyPath);
        }

        /// <summary>
        /// Handles the na n value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected override void HandleNaNValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            UnsetProperty(propertyInfo, propertyPath);
        }

        /// <summary>
        /// Handles the special case.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elementList">The element list.</param>
        /// <param name="parentPath">The parent path.</param>
        /// <param name="elementName">Name of the element.</param>
        /// <returns>true if the special case was handled, false otherwise</returns>
        protected override bool HandleSpecialCase(PropertyInfo propertyInfo, List<XElement> elementList, string parentPath, string elementName)
        {
            if (!IsSpecialCase(propertyInfo))
            {
                return base.HandleSpecialCase(propertyInfo, elementList, parentPath, elementName);
            }

            var items = Context.PropertyValues.Last() as IEnumerable;
            var propertyPath = GetPropertyPath(parentPath, propertyInfo.Name);
            var propertyType = propertyInfo.PropertyType;

            var args = propertyType.GetGenericArguments();
            var childType = args.FirstOrDefault() ?? propertyType.GetElementType();

            var version = ObjectTypes.GetVersion(childType);
            var validator = Container?.Resolve<IRecurringElementValidator>(new ObjectName(childType.Name, version));
            validator?.Validate(Context.Function, childType, items, elementList);

            UpdateRecurringElementsWithoutUid(elementList, propertyInfo, items, childType, propertyPath);

            return true;
        }

        private void BuildUpdate(XElement element)
        {
            NavigateElement(element, Context.DataObjectType);
        }

        private void PushPropertyInfo(PropertyInfo propertyInfo)
        {
            var parentValue = Context.PropertyValues.LastOrDefault();

            object propertyValue;

            if (_entity == null)
            {
                propertyValue = null;
            }
            else
            {
                propertyValue = Context.PropertyValues.Count == 0
                    ? propertyInfo.GetValue(_entity)
                    : parentValue != null
                    ? propertyInfo.GetValue(parentValue)
                    : null;
            }

            PushPropertyInfo(propertyInfo, propertyValue);
        }

        private void PushPropertyInfo(PropertyInfo propertyInfo, object propertyValue)
        {
            Context.PropertyInfos.Add(propertyInfo);
            Context.PropertyValues.Add(propertyValue);
        }

        private void PopPropertyInfo()
        {
            Context.PropertyInfos.Remove(Context.PropertyInfos.Last());
            Context.PropertyValues.Remove(Context.PropertyValues.Last());
        }

        private void SetProperty<TValue>(PropertyInfo propertyInfo, string propertyPath, TValue propertyValue)
        {
            Context.Update = Context.Update.Set(propertyPath, propertyValue);
            SetSpecifiedProperty(propertyInfo, propertyPath, true);
        }

        private void UnsetProperty(PropertyInfo propertyInfo, string propertyPath)
        {
            if (propertyInfo.IsDefined(typeof(RequiredAttribute), false))
                throw new WitsmlException(ErrorCodes.MissingRequiredData);

            Context.Update = Context.Update.Unset(propertyPath);
            SetSpecifiedProperty(propertyInfo, propertyPath, false);
        }

        private void SetSpecifiedProperty(PropertyInfo propertyInfo, string propertyPath, bool specified)
        {
            var property = propertyInfo.DeclaringType?.GetProperty(propertyInfo.Name + "Specified");

            if (property != null && property.CanWrite)
            {
                Context.Update = Context.Update.Set(propertyPath + "Specified", specified);
            }
        }

        private object ParseNestedElement(Type type, XElement element)
        {
            if (element.DescendantsAndSelf().Any(e => (!e.HasAttributes && string.IsNullOrWhiteSpace(e.Value)) || e.Attributes().Any(a => string.IsNullOrWhiteSpace(a.Value))))
                throw new WitsmlException(ErrorCodes.EmptyNewElementsOrAttributes);

            // update element name to match XSD type name
            var xmlType = type.GetCustomAttribute<XmlTypeAttribute>();
            var clone = new XElement(element)
            {
                Name = xmlType == null ? type.Name : xmlType.TypeName
            };

            return WitsmlParser.Parse(type, clone);
        }

        private IList CreateList(Type listType, object item)
        {
            var list = Activator.CreateInstance(listType) as IList;
            list?.Add(item);
            return list;
        }

        private void UpdateArrayElements(List<XElement> elements, PropertyInfo propertyInfo, object propertyValue, Type type, string parentPath)
        {
            Logger.DebugFormat("Updating array elements: {0} {1}", parentPath, propertyInfo?.Name);

            var updateBuilder = Builders<T>.Update;
            var filterBuilder = Builders<T>.Filter;
            var idField = MongoDbUtility.LookUpIdField(type);
            var filterPath = GetPropertyPath(parentPath, idField);
            var properties = GetPropertyInfo(type);           

            var ids = new List<string>();
            var itemsById = GetItemsById((IEnumerable)propertyValue, properties, idField, ids);

            var updateList = elements
                .Select(element =>
                {
                    var elementId = GetElementId(element, idField);
                    if (string.IsNullOrEmpty(elementId) || propertyInfo == null) return null;

                    var filters = new List<FilterDefinition<T>>() {_entityFilter};

                    object current;
                    itemsById.TryGetValue(elementId, out current);

                    if (current == null)
                    {
                        ValidateArrayElement(element, properties);

                        var item = ParseNestedElement(type, element);
                        var filter = filterBuilder.And(filters);

                        var childValidator = new MongoDbUpdate<T>(_collection, _parser, _idPropertyName, Context.Ignored);
                        childValidator.Context.ValidationOnly = true;
                        childValidator.NavigateElementType(propertyInfo, type, element, parentPath);

                        if (Context.ValidationOnly)
                            return null;

                        var update = propertyValue == null
                            ? updateBuilder.Set(parentPath, CreateList(propertyInfo.PropertyType, item))
                            : updateBuilder.Push(parentPath, item);

                        return new UpdateOneModel<T>(filter, update);
                    }
                    else
                    {
                        var position = ids.IndexOf(elementId);
                        var positionPath = parentPath + "." + position;
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

                        var model = new UpdateOneModel<T>(filter, Context.Update);
                        Context.Update = saveUpdate;
                        return model;
                    }
                })
                .Where(x => x != null)
                .ToList();

            if (updateList.Count > 0)
                _collection.BulkWrite(updateList);
        }

        private void UpdateRecurringElementsWithoutUid(List<XElement> elements, PropertyInfo propertyInfo, object propertyValue, Type type, string parentPath)
        {
            Logger.DebugFormat("Updating recurring elements without a uid: {0} {1}", parentPath, propertyInfo?.Name);

            var updateBuilder = Builders<T>.Update;
            var filterBuilder = Builders<T>.Filter;

            var updateList = elements
                .Select(element =>
                {
                    var filters = new List<FilterDefinition<T>>() { _entityFilter };

                    WitsmlParser.RemoveEmptyElements(element);

                    // TODO: Modify to handle other item types as needed.
                    object item;

                    if (IsComplexType(type))
                    {
                        item = ParseNestedElement(type, element);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(element.Value))
                            return null;

                        item = element.Value;
                    }

                    var filter = filterBuilder.And(filters);

                    //var childValidator = new MongoDbUpdate<T>(_collection, _parser, _idPropertyName, Context.Ignored);
                    //childValidator.Context.ValidationOnly = true;
                    //childValidator.NavigateElementType(propertyInfo, type, element, parentPath);

                    if (Context.ValidationOnly)
                        return null;

                    var update = propertyValue == null
                        ? updateBuilder.Set(parentPath, CreateList(propertyInfo.PropertyType, item))
                        : updateBuilder.Push(parentPath, item);

                    return new UpdateOneModel<T>(filter, update);
                })
                .Where(x => x != null)
                .ToList();

            if (updateList.Count > 0)
                _collection.BulkWrite(updateList);
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

        private IDictionary<string, object> GetItemsById(IEnumerable items, IList<PropertyInfo> properties, string idField, List<string> idList)
        {
            var idProp = GetPropertyInfoForAnElement(properties, idField);            

            return (items ?? Enumerable.Empty<object>())
                .Cast<object>()
                .ToDictionary(x =>
                {
                    var id = idProp.GetValue(x).ToString();
                    idList.Add(id);
                    return id;
                });
        }

        private void ValidateArrayElement(XElement element, IList<PropertyInfo> properties, bool isAdd = true)
        {
            Logger.DebugFormat("Validating array element: {0}", element.Name.LocalName);

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

        private void LogUpdateFilter(FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            if (Logger.IsDebugEnabled)
            {
                var filterJson = filter.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                var updateJson = update.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                Logger.DebugFormat("Detected update parameters: {0}", updateJson);
                Logger.DebugFormat("Detected update filters: {0}", filterJson);
            }
        }
    }
}
