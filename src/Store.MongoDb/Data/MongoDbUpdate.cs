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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Energistics.Etp.Common.Datatypes;
using MongoDB.Driver;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Data.Common;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Encloses MongoDb update method and its helper methods
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    /// <seealso cref="MongoDbWrite{T}" />
    public class MongoDbUpdate<T> : MongoDbWrite<T>
    {
        private FilterDefinition<T> _entityFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbUpdate{T}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="collection">The collection.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="idPropertyName">Name of the identifier property.</param>
        /// <param name="ignored">The ignored.</param>
        public MongoDbUpdate(IContainer container, IMongoCollection<T> collection, WitsmlQueryParser parser, string idPropertyName = "Uid", List<string> ignored = null) 
            : base(container, collection, parser, idPropertyName, ignored)
        {
            Logger.Verbose("Instance created.");           
        }

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="updates">The updates.</param>
        public virtual void Update(T entity, EtpUri uri, Dictionary<string, object> updates)
        {
            Logger.Debug($"Updating data object: {uri}");

            _entityFilter = BuildEntityFilter(entity, uri);
            Entity = entity;

            var entityId = GetEntityId(entity, uri);
            Context.Update = Update(updates, entityId);
            BuildUpdate(Parser.Element());

            LogUpdateFilter(_entityFilter, Context.Update);
            Collection.UpdateOne(_entityFilter, Context.Update);

            WitsmlOperationContext.Current.Warnings.AddRange(Context.Warnings);
        }

        /// <summary>
        /// Updates a document using the specified filter and update definitions.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="update">The update.</param>
        public virtual void UpdateFields(FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            LogUpdateFilter(filter, update);
            Collection.UpdateOne(filter, update);
        }

        /// <summary>
        /// Creates an <see cref="UpdateDefinition{T}"/> based on the supplied dictionary of name/value pairs.
        /// </summary>
        /// <param name="updates">The updates.</param>
        /// <param name="idValue">The id value.</param>
        /// <returns>The update definition.</returns>
        protected virtual UpdateDefinition<T> Update(Dictionary<string, object> updates, string idValue)
        {
            var update = Builders<T>.Update.Set(IdPropertyName, idValue);

            foreach (var pair in updates)
                update = update.Set(pair.Key, pair.Value);

            return update;
        }

        /// <summary>
        /// Builds the update based on the specified xml element.
        /// </summary>
        /// <param name="element">The xml element.</param>
        protected virtual void BuildUpdate(XElement element)
        {
            NavigateElement(element, Context.DataObjectType);
        }

        /// <summary>
        /// Gets the entity id for the specified data object.
        /// </summary>
        /// <param name="entity">The data object entity.</param>
        /// <param name="uri">The data object uri.</param>
        /// <returns></returns>
        protected virtual string GetEntityId(T entity, EtpUri uri)
        {
            return uri.ObjectId;
        }

        /// <summary>
        /// Builds the entity filter from the specified uri.
        /// </summary>
        /// <param name="entity">The data object entity.</param>
        /// <param name="uri">The data object uri.</param>
        /// <returns>The filter definition.</returns>
        protected virtual FilterDefinition<T> BuildEntityFilter(T entity, EtpUri uri)
        {
            return MongoDbUtility.GetEntityFilter<T>(uri, IdPropertyName);
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

            if (!string.IsNullOrWhiteSpace(measureValue))
                NavigateProperty(propertyInfo, xmlObject, propertyType, propertyPath, uomValue);
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
            if (HasXmlAnyElement(propertyInfo.PropertyType))
            {
                var propertyValue = Context.PropertyValues.Last();
                UpdateXmlAnyElements(elementList, propertyInfo, propertyValue, parentPath);
                return true;
            }
            if (!IsSpecialCase(propertyInfo))
            {
                return base.HandleSpecialCase(propertyInfo, elementList, parentPath, elementName);
            }

            var items = Context.PropertyValues.Last() as IEnumerable;
            var propertyPath = GetPropertyPath(parentPath, propertyInfo.Name);
            var propertyType = propertyInfo.PropertyType;

            var args = propertyType.GetGenericArguments();
            var childType = args.FirstOrDefault() ?? propertyType.GetElementType();

            if (childType != null && childType != typeof(string))
            {
                try
                {
                    var version = ObjectTypes.GetVersion(childType);
                    var family = ObjectTypes.GetVersion(childType);
                    var validator = Container.Resolve<IRecurringElementValidator>(new ObjectName(childType.Name, family, version));
                    validator?.Validate(Context.Function, childType, items, elementList);
                }
                catch (ContainerException)
                {
                    Logger.DebugFormat("{0} not configured for type: {1}", typeof(IRecurringElementValidator).Name, childType);
                }
            }

            UpdateArrayElementsWithoutUid(elementList, propertyInfo, items, childType, propertyPath);

            return true;
        }

        /// <summary>
        /// Updates the array elements.
        /// </summary>
        /// <param name="elements">The elements.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="type">The type.</param>
        /// <param name="parentPath">The parent path.</param>
        protected override void UpdateArrayElements(List<XElement> elements, PropertyInfo propertyInfo, object propertyValue, Type type, string parentPath)
        {
            Logger.Debug($"Updating array elements: {parentPath} {propertyInfo?.Name}");

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
                        ValidateArrayElement(propertyInfo, type, element, parentPath);

                        if (Context.ValidationOnly)
                            return null;

                        var item = ParseNestedElement(type, element);
                        var filter = filterBuilder.And(filters);

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
                Collection.BulkWrite(updateList);
        }

        /// <summary>
        /// Updates the array elements which don't contain a uid property.
        /// </summary>
        /// <param name="elements">The elements.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="type">The type.</param>
        /// <param name="parentPath">The parent path.</param>
        protected virtual void UpdateArrayElementsWithoutUid(List<XElement> elements, PropertyInfo propertyInfo, object propertyValue, Type type, string parentPath)
        {
            Logger.Debug($"Updating recurring elements without a uid: {parentPath} {propertyInfo?.Name}");

            var updateBuilder = Builders<T>.Update;
            var filterBuilder = Builders<T>.Filter;

            var updateList = elements
                .Select((element, index) =>
                {
                    var filters = new List<FilterDefinition<T>>() { _entityFilter };

                    WitsmlParser.RemoveEmptyElements(element);

                    // TODO: Modify to handle other item types as needed.
                    object item;

                    if (IsComplexType(type))
                    {
                        var complexType = type.IsAbstract
                            ? GetConcreteType(element, type)
                            : type;

                        item = ParseNestedElement(complexType, element);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(element.Value))
                            return null;

                        item = element.Value;
                    }

                    if (Context.ValidationOnly)
                        return null;

                    var update = propertyValue == null || index == 0
                        ? updateBuilder.Set(parentPath, CreateList(propertyInfo.PropertyType, item))
                        : updateBuilder.Push(parentPath, item);

                    var filter = filterBuilder.And(filters);
                    return new UpdateOneModel<T>(filter, update);
                })
                .Where(x => x != null)
                .ToList();

            if (updateList.Count > 0)
                Collection.BulkWrite(updateList);
        }

        /// <summary>
        /// Updates the xml any elements.
        /// </summary>
        /// <param name="elements">The elements.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="parentPath">The parent path.</param>
        protected virtual void UpdateXmlAnyElements(List<XElement> elements, PropertyInfo propertyInfo, object propertyValue, string parentPath)
        {
            if (elements.Count < 1 || propertyInfo == null) return;
            var element = elements.First();

            if (!element.HasElements)
            {
                UnsetProperty(propertyInfo, parentPath);
                return;
            }

            Logger.Debug($"Updating XmlAnyElements: {parentPath} {propertyInfo?.Name}");

            var anyPropertyInfo = propertyInfo.PropertyType.GetProperties()
                .First(x => x.IsDefined(typeof(XmlAnyElementAttribute), false));

            var propertyPath = GetPropertyPath(GetPropertyPath(parentPath, propertyInfo.Name), anyPropertyInfo.Name);
            var items = (IList<XmlElement>)anyPropertyInfo.GetValue(propertyValue);
            var names = items.Select(x => x.LocalName).ToList();

            var updateBuilder = Builders<T>.Update;

            var updateList = element
                .Elements()
                .Select(x =>
                {
                    if (Context.ValidationOnly)
                        return null;

                    var position = names.IndexOf(x.Name.LocalName);
                    var positionPath = propertyPath + "." + position;
                    var item = x.ToXmlElement();

                    var update = position > -1
                        ? updateBuilder.Set(positionPath, item)
                        : updateBuilder.Push(propertyPath, item);

                    return new UpdateOneModel<T>(_entityFilter, update);
                })
                .Where(x => x != null)
                .ToList();

            if (updateList.Count > 0)
                Collection.BulkWrite(updateList);
        }

        /// <summary>
        /// Sets the property.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected override void SetProperty<TValue>(PropertyInfo propertyInfo, string propertyPath, TValue propertyValue)
        {
            Context.Update = Context.Update.Set(propertyPath, propertyValue);
            SetSpecifiedProperty(propertyInfo, propertyPath, true);
        }

        /// <summary>
        /// Unsets the property.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <exception cref="WitsmlException">The Witsml exception with specified error code.</exception>
        protected override void UnsetProperty(PropertyInfo propertyInfo, string propertyPath)
        {
            if (propertyInfo.IsDefined(typeof(RequiredAttribute), false))
                throw new WitsmlException(ErrorCodes.MissingRequiredData);

            Context.Update = Context.Update.Unset(propertyPath);
            SetSpecifiedProperty(propertyInfo, propertyPath, false);
        }

        /// <summary>
        /// Sets the special Specified property.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="specified">The specified value.</param>
        protected virtual void SetSpecifiedProperty(PropertyInfo propertyInfo, string propertyPath, bool specified)
        {
            var property = propertyInfo.DeclaringType?.GetProperty(propertyInfo.Name + "Specified");

            if (property != null && property.CanWrite)
            {
                Context.Update = Context.Update.Set(propertyPath + "Specified", specified);
            }
        }

        /// <summary>
        /// Logs the update filter.
        /// </summary>
        /// <param name="filter">The filter definition.</param>
        /// <param name="update">The update definition.</param>
        protected virtual void LogUpdateFilter(FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            if (!Logger.IsDebugEnabled)
                return;

            var filterJson = filter.Render(Collection.DocumentSerializer, Collection.Settings.SerializerRegistry);
            var updateJson = update.Render(Collection.DocumentSerializer, Collection.Settings.SerializerRegistry);
            Logger.Debug($"Detected update parameters: {updateJson}");
            Logger.Debug($"Detected update filters: {filterJson}");
        }
    }
}
