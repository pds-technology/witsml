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
using PDS.WITSMLstudio.Data;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Encloses MongoDb partial delete method and its helper methods
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Data.DataObjectNavigator{MongoDbDeleteContext}" />
    public class MongoDbDelete<T> : DataObjectNavigator<MongoDbDeleteContext<T>>
    {
        private readonly IMongoCollection<T> _collection;
        private readonly WitsmlQueryParser _parser;
        private readonly string _idPropertyName;
        private readonly Dictionary<int, List<UpdateOneModel<T>>> _pullUpdates;

        private FilterDefinition<T> _entityFilter;
        private T _entity;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbDelete{T}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="collection">The collection.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="idPropertyName">Name of the identifier property.</param>
        /// <param name="ignored">The ignored.</param>
        public MongoDbDelete(IContainer container, IMongoCollection<T> collection, WitsmlQueryParser parser, string idPropertyName = "Uid", List<string> ignored = null) : base(container, new MongoDbDeleteContext<T>())
        {
            Logger.Verbose("Instance created.");
            Context.Ignored = ignored;

            _collection = collection;
            _parser = parser;
            _idPropertyName = idPropertyName;
            _pullUpdates = new Dictionary<int, List<UpdateOneModel<T>>>();
        }

        /// <summary>
        /// Executes partial delete.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="updates">The updates.</param>
        public virtual void PartialDelete(T entity, EtpUri uri, Dictionary<string, object> updates)
        {
            Logger.Debug($"Partial deleting data object: {uri}");

            _entityFilter = MongoDbUtility.GetEntityFilter<T>(uri, _idPropertyName);
            _entity = entity;

            Context.Update = Update(updates, uri.ObjectId);
            BuildPartialDelete(_parser.Element());

            LogUpdateFilter(_entityFilter, Context.Update);
            _collection.UpdateOne(_entityFilter, Context.Update);

            // Remove recurring elements after all update because of the position being used in field path
            RemoveArrayElementsByDepth();

            WitsmlOperationContext.Current.Warnings.AddRange(Context.Warnings);
        }

        /// <summary>
        /// Updates the specified updates.
        /// </summary>
        /// <param name="updates">The updates.</param>
        /// <param name="uidValue">The uid value.</param>
        /// <returns></returns>
        public virtual UpdateDefinition<T> Update(Dictionary<string, object> updates, string uidValue)
        {
            var update = Builders<T>.Update.Set(_idPropertyName, uidValue);

            foreach (var pair in updates)
                update = update.Set(pair.Key, pair.Value);

            return update;
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

            base.NavigateElementGroup(propertyInfo, elements, parentPath);

            PopPropertyInfo();
        }

        /// <summary>
        /// Navigates the element.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elementType">Type of the element.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The property path.</param>
        protected override void NavigateElementType(PropertyInfo propertyInfo, Type elementType, XElement element, string propertyPath)
        {
            NavigateElementTypeWithoutXmlText(propertyInfo, elementType, element, propertyPath);
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
            PartialDeleteArrayElements(elements, propertyInfo, Context.PropertyValues.Last(), childType, propertyPath);
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
                PartialDeleteXmlAnyElements(elementList, propertyInfo, propertyValue, parentPath);
                return true;
            }
            if (!IsSpecialCase(propertyInfo))
            {
                return base.HandleSpecialCase(propertyInfo, elementList, parentPath, elementName);
            }

            var items = Context.PropertyValues.Last() as IEnumerable;
            var propertyPath = GetPropertyPath(parentPath, propertyInfo.Name);

            if (items != null && elementList.Any(e => string.IsNullOrWhiteSpace(e.Value)))
            {
                UnsetProperty(propertyInfo, propertyPath);
            }

            return true;
        }

        /// <summary>
        /// Builds the partial delete definition.
        /// </summary>
        /// <param name="element">The XML element.</param>
        protected virtual void BuildPartialDelete(XElement element)
        {
            NavigateElement(element, Context.DataObjectType);
        }

        /// <summary>
        /// Pushes the property information.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        protected virtual void PushPropertyInfo(PropertyInfo propertyInfo)
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

        /// <summary>
        /// Pushes the property information.
        /// </summary>
        /// <param name="propertyInfo">The property information</param>
        /// <param name="propertyValue">The property value.</param>
        protected virtual void PushPropertyInfo(PropertyInfo propertyInfo, object propertyValue)
        {
            Context.PropertyInfos.Add(propertyInfo);
            Context.PropertyValues.Add(propertyValue);
        }

        /// <summary>
        /// Removes the property information.
        /// </summary>
        protected virtual void PopPropertyInfo()
        {
            Context.PropertyInfos.Remove(Context.PropertyInfos.Last());
            Context.PropertyValues.Remove(Context.PropertyValues.Last());
        }

        //protected virtual void SetProperty<TValue>(PropertyInfo propertyInfo, string propertyPath, TValue propertyValue)
        //{
        //    Context.Update = Context.Update.Set(propertyPath, propertyValue);
        //    SetSpecifiedProperty(propertyInfo, propertyPath, true);
        //}

        /// <summary>
        /// Unsets the property.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyPath">The property path.</param>
        protected virtual void UnsetProperty(PropertyInfo propertyInfo, string propertyPath)
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
        /// Partially deletes the XML any elements.
        /// </summary>
        /// <param name="elements">The XML elements.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="parentPath">The parent path.</param>
        protected virtual void PartialDeleteXmlAnyElements(List<XElement> elements, PropertyInfo propertyInfo, object propertyValue, string parentPath)
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
                    var position = names.IndexOf(x.Name.LocalName);
                    var positionPath = propertyPath + "." + position;

                    if (position == -1)
                        return null;

                    // Set position to null
                    var update = updateBuilder.Unset(positionPath);
                    return new UpdateOneModel<T>(_entityFilter, update);
                })
                .Where(x => x != null)
                .ToList();

            if (updateList.Count > 0)
            {
                // Remove all null items
                var update = updateBuilder.PullAll(propertyPath, new object[] { null });
                updateList.Add(new UpdateOneModel<T>(_entityFilter, update));

                _collection.BulkWrite(updateList);
            }
        }

        /// <summary>
        /// Partially deletes the array elements.
        /// </summary>
        /// <param name="elements">The XML elements.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="type">The property type.</param>
        /// <param name="parentPath">The parent path.</param>
        protected virtual void PartialDeleteArrayElements(List<XElement> elements, PropertyInfo propertyInfo, object propertyValue, Type type, string parentPath)
        {
            Logger.Debug($"Partial deleting array elements: {parentPath} {propertyInfo?.Name}");

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
                    if (string.IsNullOrEmpty(elementId) || propertyInfo == null)
                        return null;

                    var filters = new List<FilterDefinition<T>>() { _entityFilter };

                    object current;
                    itemsById.TryGetValue(elementId, out current);

                    if (current == null)
                        return null;

                    var elementFilter = Builders<T>.Filter.EqIgnoreCase(filterPath, elementId);
                    filters.Add(elementFilter);
                    var filter = filterBuilder.And(filters);

                    if (element.Elements().Any())
                    {
                        var position = ids.IndexOf(elementId);
                        var positionPath = parentPath + "." + position;
                           
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
                    else
                    {
                        if (IsRequired(propertyInfo) && itemsById.Count == 1)
                        {
                            throw new WitsmlException(ErrorCodes.MustRetainOneRecurringNode);
                        }

                        var childFilter = MongoDbExtensions.EqualsIgnoreCase(type, idField, elementId);
                        var update = MongoDbExtensions.PullFilter(typeof (T), type, parentPath, childFilter) as UpdateDefinition<T>;
                        
                        if (childFilter != null && update != null)
                        {
                            //var update = updateBuilder.Pull(parentPath, current);
                            AddToPullCollection(parentPath, new UpdateOneModel<T>(filter, update));
                        }

                        return null;
                    }
                })
                .Where(x => x != null)
                .ToList();

            if (updateList.Count > 0)
                _collection.BulkWrite(updateList);
        }

        /// <summary>
        /// Gets the element's id.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <param name="idField">The id field.</param>
        /// <returns></returns>
        protected virtual string GetElementId(XElement element, string idField)
        {
            var idAttribute = element.Attributes().FirstOrDefault(a => a.Name.LocalName.EqualsIgnoreCase(idField));
            if (idAttribute != null)
                return idAttribute.Value.Trim();

            var idElement = element.Elements().FirstOrDefault(e => e.Name.LocalName.EqualsIgnoreCase(idField));
            if (idElement != null)
                return idElement.Value.Trim();

            return string.Empty;
        }

        /// <summary>
        /// Creates a dictionary of items keyed by each item's id.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="properties">The property information.</param>
        /// <param name="idField">The id field.</param>
        /// <param name="idList">The id list.</param>
        /// <returns></returns>
        protected virtual IDictionary<string, object> GetItemsById(IEnumerable items, IList<PropertyInfo> properties, string idField, List<string> idList)
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

        /// <summary>
        /// Logs the specified filter and update definitions.
        /// </summary>
        /// <param name="filter">The filter definition.</param>
        /// <param name="update">The update definition.</param>
        protected virtual void LogUpdateFilter(FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            if (!Logger.IsDebugEnabled)
                return;

            var filterJson = filter.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
            var updateJson = update.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
            Logger.Debug($"Detected partial delete parameters: {updateJson}");
            Logger.Debug($"Detected partial delete filters: {filterJson}");
        }

        /// <summary>
        /// Add a pull update to pull collection based on the depth of its field path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="pullUpdate">The pull update.</param>
        protected virtual void AddToPullCollection(string path, UpdateOneModel<T> pullUpdate)
        {
            var depth = path.Split('.').Length;

            List<UpdateOneModel<T>> updates;

            if (_pullUpdates.ContainsKey(depth))
                updates = _pullUpdates[depth];
            else
            {
                updates = new List<UpdateOneModel<T>>();
                _pullUpdates.Add(depth, updates);
            }

            updates.Add(pullUpdate);
        }

        /// <summary>
        /// Removes the array elements by depth, i.e remove the most nested array elements
        /// so that it won't cause positional error.
        /// </summary>
        protected virtual void RemoveArrayElementsByDepth()
        {
            var depths = _pullUpdates.Keys.ToList();
            depths.Sort();

            for (var i = depths.Count - 1; i > -1; i--)
            {
                var updates = _pullUpdates[depths[i]];
                _collection.BulkWrite(updates);
            }
        }
    }
}
