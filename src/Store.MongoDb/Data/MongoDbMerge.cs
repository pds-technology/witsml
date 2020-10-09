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
using System.Xml.Serialization;
using MongoDB.Driver;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Data.Common;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Encloses MongoDb merge method and its helper methods
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="MongoDbWrite{T}" />
    public class MongoDbMerge<T> : MongoDbWrite<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbMerge{T}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="collection">The collection.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="idPropertyName">Name of the identifier property.</param>
        /// <param name="ignored">The ignored.</param>
        public MongoDbMerge(IContainer container, IMongoCollection<T> collection, WitsmlQueryParser parser, string idPropertyName = "Uid", List<string> ignored = null) : 
            base(container, collection, parser,idPropertyName, ignored)
        {          
        }

        /// <summary>
        /// Gets or sets a value indicating whether to remove empty recurring elements.
        /// </summary>
        /// <value>True if it is partial delete; false otherwise.
        /// </value>
        public bool MergeDelete { get; set; }

        /// <summary>
        /// Merges the entity with the update query.
        /// </summary>
        /// <param name="entity">The entity to be merged.</param>
        public virtual void Merge(T entity)
        {
            Entity = entity;
            Merge(Parser.Element());
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
            var textProperty = GetXmlTextProperty(elementType);

            if (textProperty != null)
            {
                var uomProperty = elementType.GetProperty("Uom");
                var propertyName = GetPropertyPath(propertyPath, textProperty.Name);
                var propertyType = textProperty.PropertyType;

                if (uomProperty != null)
                {
                    var uomPath = GetPropertyPath(propertyPath, uomProperty.Name);
                    var uomValue = ValidateMeasureUom(element, uomProperty, element.Value);

                    NavigateUomAttribute(uomProperty, element.Attribute("uom"), uomProperty.PropertyType, uomPath, element.Value, uomValue);
                }

                if (element.HasAttributes)
                {
                    var properties = GetPropertyInfo(elementType, element);
                    NavigateAttributes(element, propertyPath, properties, true);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(element.Value))
                    {
                        NavigateElementTypeWithoutXmlText(propertyInfo, elementType, element, propertyPath);
                        return;
                    }
                }

                PushPropertyInfo(textProperty);
                NavigateProperty(textProperty, element, propertyType, propertyName, element.Value);
                PopPropertyInfo();
            }
            else
            {
                NavigateElementTypeWithoutXmlText(propertyInfo, elementType, element, propertyPath);
            }
        }
        
        /// <summary>
        /// Navigates the attributes.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="parentPath">The parent path.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="skipUom">if set to <c>true</c> skip uom.</param>
        protected override void NavigateAttributes(XElement element, string parentPath, IList<PropertyInfo> properties, bool skipUom = false)
        {
            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Name == Xsi("nil") || attribute.Name == Xsi("type") ||
                    (skipUom && attribute.Name.LocalName == "uom"))
                    continue;

                var attributeProp = GetPropertyInfoForAnElement(properties, attribute.Name.LocalName);

                if (attributeProp == null)
                    continue;

                PushPropertyInfo(attributeProp);
                NavigateAttribute(attributeProp, attribute, parentPath);
                PopPropertyInfo();
            }
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

            if (string.IsNullOrWhiteSpace(measureValue))
                return;

            PushPropertyInfo(propertyInfo);
            NavigateProperty(propertyInfo, xmlObject, propertyType, propertyPath, uomValue);
            PopPropertyInfo();
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
            if (Context.ValidationOnly)
                return;

            if (Context.PropertyValues.Count == 1)
            {
                propertyInfo.SetValue(Entity, propertyValue);
                SetSpecifiedProperty(propertyInfo, Entity, true);
            }              
            else
            {
                var count = Context.PropertyValues.Count;
                var parent = Context.PropertyValues[count - 2];
                if (parent == null)
                    return;
                if (!IsTypeOfEnumValueName(propertyInfo))
                    propertyInfo.SetValue(parent, propertyValue);
                SetSpecifiedProperty(propertyInfo, parent, true);
            }
        }


        /// <summary>
        /// Unsets the property.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <exception cref="WitsmlException"></exception>
        protected override void UnsetProperty(PropertyInfo propertyInfo, string propertyPath)
        {
            var propertyValue = Context.PropertyValues.LastOrDefault();
            if (propertyValue == null)
                return;

            if (propertyInfo.IsDefined(typeof(RequiredAttribute), false))
                throw new WitsmlException(ErrorCodes.MissingRequiredData);

            UnsetProperty(propertyInfo);
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
            Logger.Debug($"Merge array elements: {parentPath} {propertyInfo?.Name}");

            if (MergeDelete && RemoveAll(elements))
            {
                var list = propertyValue as IList;
                list?.Clear();
                return;
            }

            var idField = MongoDbUtility.LookUpIdField(type);
            var properties = GetPropertyInfo(type);

            var ids = new List<string>();
            var itemsById = GetItemsById((IEnumerable)propertyValue, properties, idField, ids);

            foreach (var element in elements)
            {
                var elementId = GetElementId(element, idField);
                if (string.IsNullOrEmpty(elementId) || propertyInfo == null)
                    continue;

                object current;
                itemsById.TryGetValue(elementId, out current);

                if (current == null)
                {
                    if (MergeDelete)
                        continue;

                    ValidateArrayElement(element, properties);
                    ValidateArrayElement(propertyInfo, type, element, parentPath);

                    if (Context.ValidationOnly)
                        continue;

                    var item = ParseNestedElement(type, element);
                    if (propertyValue == null)
                        continue;

                    var list = propertyValue as IList;
                    list?.Add(item);
                }
                else
                {
                    if (MergeDelete && RemoveItem(element))
                    {
                        var list = propertyValue as IList;
                        list?.Remove(current);
                    }
                    else
                    {
                        var position = ids.IndexOf(elementId);
                        var positionPath = parentPath + "." + position;
                        ValidateArrayElement(element, properties, false);

                        PushPropertyInfo(propertyInfo, current);
                        NavigateElement(element, type, positionPath);
                        PopPropertyInfo();
                    }
                }
            }
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

            if (childType != null && childType != typeof(string))
            {
                try
                {
                    var version = ObjectTypes.GetVersion(childType);
                    var family = ObjectTypes.GetFamily(childType);
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
        /// Updates array elements without uid properties.
        /// </summary>
        /// <param name="elements">The collection of XML elements.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="type">The property type.</param>
        /// <param name="parentPath">The parent path.</param>
        protected virtual void UpdateArrayElementsWithoutUid(List<XElement> elements, PropertyInfo propertyInfo, object propertyValue, Type type, string parentPath)
        {
            Logger.Debug($"Updating recurring elements without a uid: {parentPath} {propertyInfo?.Name}");

            if (MergeDelete)
                RemoveAll(elements);

            var list = propertyValue as IList;
            list?.Clear();

            foreach (var element in elements)
            {
                if (MergeDelete)
                    continue;

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
                        continue;

                    item = element.Value;
                }

                if (Context.ValidationOnly)
                    continue;

                if (propertyValue == null)
                    continue;

                list?.Add(item);
            }
        }

        /// <summary>
        /// Merges the specified XML element.
        /// </summary>
        /// <param name="element">The XML element.</param>
        protected virtual void Merge(XElement element)
        {
            NavigateElement(element, Context.DataObjectType);
        }

        /// <summary>
        /// Unsets the property and its corresponding Specified property.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        protected virtual void UnsetProperty(PropertyInfo propertyInfo)
        {
            if (Context.PropertyValues.Count == 1)
            {
                propertyInfo.SetValue(Entity, null);
                SetSpecifiedProperty(propertyInfo, Entity, false);
            }
            else
            {
                var count = Context.PropertyValues.Count;
                var parent = Context.PropertyValues[count - 2];
                if (parent == null)
                    return;

                propertyInfo.SetValue(parent, null);
                SetSpecifiedProperty(propertyInfo, parent, false);
            }
        }

        /// <summary>
        /// Sets the special Specified property value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="obj">The object instance.</param>
        /// <param name="specified">The Specified property value.</param>
        protected virtual void SetSpecifiedProperty(PropertyInfo propertyInfo, object obj, bool specified)
        {
            var property = propertyInfo.DeclaringType?.GetProperty(propertyInfo.Name + "Specified");

            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, specified);
            }
        }

        /// <summary>
        /// Determines whether the specified element should be removed.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns></returns>
        protected virtual bool RemoveItem(XElement element)
        {
            if (element.HasElements)
                return false;

            var attributes = element.Attributes().ToList();
            return attributes.Count == 1 && attributes.Any(a => a.Name.LocalName == "uid");
        }

        /// <summary>
        /// Determines if the specified elements should be removed.
        /// </summary>
        /// <param name="elements">The collection of XML elements.</param>
        /// <returns></returns>
        protected virtual bool RemoveAll(List<XElement> elements)
        {
            return elements.Count == 1 && elements.Any(e => !e.HasElements && !e.HasAttributes);
        }

        /// <summary>
        /// Determines whether the propertyInfo is a type of Energistics.DataAccess.EnumValue.EnumValue property Name.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <returns>
        ///   <c>true</c> if propertyInfo is a type of Energistics.DataAccess.EnumValue.EnumValue property Name; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool IsTypeOfEnumValueName(PropertyInfo propertyInfo)
        {
            return propertyInfo.DeclaringType != null && 
                propertyInfo.DeclaringType.IsAssignableFrom(typeof(Energistics.DataAccess.EnumValue.EnumValue)) && 
                propertyInfo.Name.Equals("Name");
        }
    }
}
