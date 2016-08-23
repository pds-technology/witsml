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
using Energistics.Datatypes;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Data;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Encloses MongoDb partial delete method and its helper methods
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    /// <seealso cref="PDS.Witsml.Data.DataObjectNavigator{MongoDbDeleteContext}" />
    public class MongoDbDelete<T> : DataObjectNavigator<MongoDbDeleteContext<T>>
    {
        private readonly IMongoCollection<T> _collection;
        private readonly WitsmlQueryParser _parser;
        private readonly string _idPropertyName;

        private FilterDefinition<T> _entityFilter;
        private T _entity;
        private Dictionary<int, List<UpdateOneModel<T>>> _pullUpdates;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbDelete{T}"/> class.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="idPropertyName">Name of the identifier property.</param>
        /// <param name="ignored">The ignored.</param>
        public MongoDbDelete(IMongoCollection<T> collection, WitsmlQueryParser parser, string idPropertyName = "Uid", List<string> ignored = null) : base(new MongoDbDeleteContext<T>())
        {
            Logger.Debug("Instance created.");
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
        public void PartialDelete(T entity, EtpUri uri, Dictionary<string, object> updates)
        {
            Logger.DebugFormat($"Partial deleting data object: {uri}");

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
        public UpdateDefinition<T> Update(Dictionary<string, object> updates, string uidValue)
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

        private void BuildPartialDelete(XElement element)
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
            if (propertyInfo.DeclaringType?.GetProperty(propertyInfo.Name + "Specified") != null)
            {
                Context.Update = Context.Update.Set(propertyPath + "Specified", specified);
            }
        }

        private void PartialDeleteArrayElements(List<XElement> elements, PropertyInfo propertyInfo, object propertyValue, Type type, string parentPath)
        {
            Logger.DebugFormat($"Partial deleting array elements: {parentPath} {propertyInfo?.Name}");

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
                        /* 
                        // FieldDefinition<T>
                        var parentField = new StringFieldDefinition<T>(parentPath);

                        // TField
                        var childPropertyType = typeof(string);
                        var childPropertyPath = idField; 
                        var childPropertyValue = elementId; 

                        // Create: FieldDefinition<TChild, TField>
                        var childFieldDefinition = typeof(FieldDefinition<,>);
                        var childFieldType = childFieldDefinition.MakeGenericType(type, childPropertyType);

                        // The following statement generates exception (constructor not find)
                        var childField = Activator.CreateInstance(childFieldType, childPropertyPath);

                        // Create: FilterDefinitionBuilder<TChild>
                        var childFilterBuilderDefinition = typeof(FilterDefinitionBuilder<>);
                        var childFilterBuilderType = childFilterBuilderDefinition.MakeGenericType(type);
                        var childFilterBuilder = Activator.CreateInstance(childFilterBuilderType);

                        // GetMethod: FilterDefinitionBuilder<TChild>.Eq
                        var eqMethod = childFilterBuilderType.GetMethod("Eq", new[] { childFieldType, childPropertyType });

                        // Invoke: FilterDefinitionBuilder<TChild>.Eq<TField>(FieldDefinition<TChild, TField>, TField) => FilterDefinition<TChild>
                        var childFilter = eqMethod.Invoke(childFilterBuilder, new[] { childField, childPropertyValue });

                        // GetMethod: UpdateDefinition<T>.PullFilter
                        var pullFilterMethod = updateBuilder.GetType().GetMethod("PullFilter", new[] { typeof(FieldDefinition<T>), childFilter.GetType() });

                        // Invoke: UpdateDefinitionBuilder<T>.PullFilter(FieldDefinition<T>, FilterDefinition<TChild>)
                        var update = pullFilterMethod.Invoke(updateBuilder, new[] { parentField, childFilter }) as UpdateDefinition<T>;
                        */

                        var update = updateBuilder.Pull(parentPath, current);
                        AddToPullCollection(parentPath, new UpdateOneModel<T>(filter, update));
                        return null;
                    }
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

        private void LogUpdateFilter(FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            if (!Logger.IsDebugEnabled)
                return;

            var filterJson = filter.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
            var updateJson = update.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
            Logger.DebugFormat($"Detected partial delete parameters: {updateJson}");
            Logger.DebugFormat($"Detected partial delete filters: {filterJson}");
        }

        /// <summary>
        /// Add a pull update to pull collection based on the depth of its field path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="pullUpdate">The pull update.</param>
        private void AddToPullCollection(string path, UpdateOneModel<T> pullUpdate)
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
        private void RemoveArrayElementsByDepth()
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
