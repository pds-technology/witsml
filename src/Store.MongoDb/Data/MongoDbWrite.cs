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
using System.Xml.Linq;
using Energistics.DataAccess;
using MongoDB.Driver;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// An abstract class that encloses methods to update Mongo collection and its helper methods
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Data.DataObjectNavigator{MongoDbUpdateContext}" />
    public abstract class MongoDbWrite<T> : DataObjectNavigator<MongoDbUpdateContext<T>>
    {
        /// <summary>
        /// The collection
        /// </summary>
        protected readonly IMongoCollection<T> Collection;

        /// <summary>
        /// The Witsml query parser
        /// </summary>
        protected readonly WitsmlQueryParser Parser;

        /// <summary>
        /// The identifier property name
        /// </summary>
        protected readonly string IdPropertyName;

        /// <summary>
        /// The entity.
        /// </summary>
        protected T Entity;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbUpdate{T}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="collection">The collection.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="idPropertyName">Name of the identifier property.</param>
        /// <param name="ignored">The ignored.</param>
        public MongoDbWrite(IContainer container, IMongoCollection<T> collection, WitsmlQueryParser parser, string idPropertyName = "Uid", List<string> ignored = null) : base(container, new MongoDbUpdateContext<T>())
        {
            Logger.Verbose("Instance created.");
            Context.Ignored = ignored;

            Collection = collection;
            Parser = parser;
            IdPropertyName = idPropertyName;
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
                    var element = elementList.First();

                    ValidateArrayElement(propertyInfo, propertyInfo.PropertyType, element, propertyPath);

                    var childValue = ParseNestedElement(propertyInfo.PropertyType, element);
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
        /// Handles the byte array value.
        /// </summary>void HandleStringValue
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected override void HandleByteArrayValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            SetProperty(propertyInfo, propertyPath, Convert.FromBase64String(propertyValue));
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
        /// Handles the NaN value.
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
        /// Pushes the property information.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        protected virtual void PushPropertyInfo(PropertyInfo propertyInfo)
        {
            var parentValue = Context.PropertyValues.LastOrDefault();

            object propertyValue;

            if (Entity == null)
            {
                propertyValue = null;
            }
            else
            {
                propertyValue = Context.PropertyValues.Count == 0
                    ? propertyInfo.GetValue(Entity)
                    : parentValue != null
                    ? propertyInfo.GetValue(parentValue)
                    : null;
            }

            PushPropertyInfo(propertyInfo, propertyValue);
        }

        /// <summary>
        /// Pushes the property information.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyValue">The property value.</param>
        protected virtual void PushPropertyInfo(PropertyInfo propertyInfo, object propertyValue)
        {
            Context.PropertyInfos.Add(propertyInfo);
            Context.PropertyValues.Add(propertyValue);
        }

        /// <summary>
        /// Pops the property information.
        /// </summary>
        protected virtual void PopPropertyInfo()
        {
            Context.PropertyInfos.Remove(Context.PropertyInfos.Last());
            Context.PropertyValues.Remove(Context.PropertyValues.Last());
        }

        /// <summary>
        /// Gets the element identifier.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="idField">The identifier field.</param>
        /// <returns>The element id value.</returns>
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
        /// Gets the "identifier, items" collection
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="idField">The identifier field.</param>
        /// <param name="idList">The identifier list.</param>
        /// <returns>The "identifier, items" collection</returns>
        protected virtual IDictionary<string, object> GetItemsById(IEnumerable items, IList<PropertyInfo> properties, string idField, List<string> idList)
        {
            var idProp = GetPropertyInfoForAnElement(properties, idField);

            return (items ?? Enumerable.Empty<object>())
                .Cast<object>()
                .ToDictionary(x =>
                {
                    var id = GetItemId(x, properties, idProp);
                    idList.Add(id);
                    return id;
                });
        }

        /// <summary>
        /// Gets the identified for the specified recurring item.
        /// </summary>
        /// <param name="item">The recurring item.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="idPropertyInfo">The identifier property information.</param>
        /// <returns></returns>
        protected virtual string GetItemId(object item, IList<PropertyInfo> properties, PropertyInfo idPropertyInfo)
        {
            return idPropertyInfo?.GetValue(item)?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Validates the array element.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="type">The type.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The property path.</param>
        protected virtual void ValidateArrayElement(PropertyInfo propertyInfo, Type type, XElement element, string propertyPath)
        {
            var validator = new MongoDbUpdate<T>(Container, Collection, Parser, IdPropertyName, Context.Ignored);
            validator.Context.ValidationOnly = true;
            validator.NavigateElementType(propertyInfo, type, element, propertyPath);
        }

        /// <summary>
        /// Validates the array element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="isAdd">if set to <c>true</c> [is add].</param>
        /// <exception cref="WitsmlException">The Witsml exception with specified error code.</exception>
        protected virtual void ValidateArrayElement(XElement element, IList<PropertyInfo> properties, bool isAdd = true)
        {
            Logger.Debug($"Validating array element: {element.Name.LocalName}");

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

        /// <summary>
        /// Creates the list.
        /// </summary>
        /// <param name="listType">Type of the list.</param>
        /// <param name="item">The item.</param>
        /// <returns>The list of specified type.</returns>
        protected virtual IList CreateList(Type listType, object item)
        {
            var list = Activator.CreateInstance(listType) as IList;
            list?.Add(item);
            return list;
        }

        /// <summary>
        /// Sets the property.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected abstract void SetProperty<TValue>(PropertyInfo propertyInfo, string propertyPath, TValue propertyValue);

        /// <summary>
        /// Unsets the property.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyPath">The property path.</param>
        protected abstract void UnsetProperty(PropertyInfo propertyInfo, string propertyPath);

        /// <summary>
        /// Updates the array elements.
        /// </summary>
        /// <param name="elements">The elements.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="type">The type.</param>
        /// <param name="parentPath">The parent path.</param>
        protected abstract void UpdateArrayElements(List<XElement> elements, PropertyInfo propertyInfo, object propertyValue, Type type, string parentPath);
    }
}
