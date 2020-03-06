//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using Energistics.DataAccess;
using Energistics.DataAccess.Validation;
using log4net;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Compatibility;

namespace PDS.WITSMLstudio.Data
{
    /// <summary>
    /// Provides a framework for navigating a WITSML document while providing additional data object type information.
    /// </summary>
    /// <typeparam name="TContext">The type of the context.</typeparam>
    public abstract class DataObjectNavigator<TContext> where TContext : DataObjectNavigationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataObjectNavigator{TContext}"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="container">The composition container.</param>
        protected DataObjectNavigator(IContainer container, TContext context)
        {
            Logger = LogManager.GetLogger(GetType());
            Container = container;
            Context = context;
        }

        /// <summary>
        /// Gets the context.
        /// </summary>
        /// <value>The context.</value>
        public TContext Context { get; }

        /// <summary>
        /// Gets or sets the composition container used for dependency injection.
        /// </summary>
        protected IContainer Container { get; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILog Logger { get; }

        /// <summary>
        /// Creates an <see cref="XName"/> for the xmlns namespace using the specified local name.
        /// </summary>
        /// <param name="attributeName">The attribute name.</param>
        /// <returns>
        /// An <see cref="XName"/> created from the xmlns namepsace and the specified local name.
        /// </returns>
        protected static XName Xmlns(string attributeName)
        {
            return XNamespace.Xmlns.GetName(attributeName);
        }

        /// <summary>
        /// Creates an <see cref="XName"/> for the xsi namespace using the specified local name.
        /// </summary>
        /// <param name="attributeName">The attribute name.</param>
        /// <returns>
        /// An <see cref="XName"/> created from the xsi namepsace and the specified local name.
        /// </returns>
        protected static XName Xsi(string attributeName)
        {
            return XmlUtil.Xsi.GetName(attributeName);
        }

        /// <summary>
        /// Configures the context.
        /// </summary>
        protected virtual void ConfigureContext()
        {
        }

        /// <summary>
        /// Navigates the specified element.
        /// </summary>
        /// <param name="element">The XML element.</param>
        protected virtual void Navigate(XElement element)
        {
            Logger.DebugFormat("Navigating XML element: {0}", element?.Name.LocalName);

            ConfigureContext();
            NavigateElement(element, Context.DataObjectType);
        }

        /// <summary>
        /// Navigates the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="type">The type.</param>
        /// <param name="parentPath">The parent path.</param>
        protected virtual void NavigateElement(XElement element, Type type, string parentPath = null)
        {
            if (IsIgnored(element.Name.LocalName)) return;

            var properties = GetPropertyInfo(type, element);
            var groupings = element.Elements().GroupBy(e => e.Name.LocalName);

            foreach (var group in groupings)
            {
                var propertyInfo = GetPropertyInfoForAnElement(properties, group.Key);

                if (IsIgnored(group.Key, GetPropertyPath(parentPath, propertyInfo?.Name ?? group.Key))) continue;

                if (propertyInfo != null)
                {
                    NavigateElementGroup(propertyInfo, group, parentPath);
                }
                else
                {
                    HandleInvalidElementGroup(group.Key);
                }
            }

            NavigateAttributes(element, parentPath, properties);
        }

        /// <summary>
        /// Navigates the element group.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elements">The elements.</param>
        /// <param name="parentPath">The parent path.</param>
        protected virtual void NavigateElementGroup(PropertyInfo propertyInfo, IGrouping<string, XElement> elements, string parentPath)
        {
            if (propertyInfo == null) return;

            var propertyPath = GetPropertyPath(parentPath, propertyInfo.Name);
            var propertyType = propertyInfo.PropertyType;
            var elementList = elements.ToList();

            if (HandleSpecialCase(propertyInfo, elementList, parentPath, elements.Key))
                return;

            if (elementList.Count == 1)
            {
                var element = elementList.FirstOrDefault();
                NavigateElementGroupItem(propertyInfo, elementList, element, propertyPath);
            }
            else
            {
                var args = propertyType.GetGenericArguments();
                var childType = args.FirstOrDefault() ?? propertyType.GetElementType();

                // Handle duplicate elements which are not recurring elements
                if (childType == null && CompatibilitySettings.AllowDuplicateNonRecurringElements)
                {
                    foreach (var element in elementList)
                    {
                        NavigateElementGroupItem(propertyInfo, elementList, element, propertyPath);
                    }
                    return;
                }

                InitializeRecurringElementHandler(propertyInfo, propertyPath);

                NavigateRecurringElements(propertyInfo, elementList, childType, propertyPath);

                HandleRecurringElements(propertyInfo, propertyPath);
            }
        }

        /// <summary>
        /// Navigates the element group item.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elementList">The element list.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The property path.</param>
        protected virtual void NavigateElementGroupItem(PropertyInfo propertyInfo, List<XElement> elementList, XElement element, string propertyPath)
        {
            var propertyType = propertyInfo.PropertyType;

            if (propertyType.IsGenericType)
            {
                var genericType = propertyType.GetGenericTypeDefinition();

                if (genericType == typeof(Nullable<>))
                {
                    var underlyingType = Nullable.GetUnderlyingType(propertyType);
                    NavigateNullableElementType(propertyInfo, underlyingType, element, propertyPath);
                }
                else if (genericType == typeof(List<>))
                {
                    var childType = propertyType.GetGenericArguments()[0];
                    NavigateArrayElementType(propertyInfo, elementList, childType, element, propertyPath);
                }
                else
                {
                    NavigateElementType(propertyInfo, propertyType, element, propertyPath);
                }
            }
            else if (propertyType.IsAbstract)
            {
                var concreteType = GetConcreteType(element, propertyType);
                NavigateElementType(propertyInfo, concreteType, element, propertyPath);
            }
            else
            {
                NavigateElementType(propertyInfo, propertyType, element, propertyPath);
            }
        }

        /// <summary>
        /// Navigates the recurring elements.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elements">The elements.</param>
        /// <param name="childType">Type of the child.</param>
        /// <param name="propertyPath">The property path.</param>
        protected virtual void NavigateRecurringElements(PropertyInfo propertyInfo, List<XElement> elements, Type childType, string propertyPath)
        {
            foreach (var value in elements)
            {
                NavigateElementType(propertyInfo, childType, value, propertyPath);
            }
        }

        /// <summary>
        /// Navigates the array element.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elements">The elements.</param>
        /// <param name="childType">Type of the child.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The property path.</param>
        protected virtual void NavigateArrayElementType(PropertyInfo propertyInfo, List<XElement> elements, Type childType, XElement element, string propertyPath)
        {
            var arrayItem = XmlAttributeCache<XmlArrayItemAttribute>.GetCustomAttribute(propertyInfo);

            // Special case when the property has both XmlArrayAttribute and XamlArrayItemAttribute
            // This usually indicates that XML type of the list or array has not been translated into a .NET type so
            // there is a mismatch between the XML type hierarchy and the .NET one.
            // In this case, a pseudo-property may need to be navigated, which represents the array items as a "property" on the .NET list or array type.
            if (arrayItem != null)
            {
                if (element.HasElements)
                {
                    var childPropertyInfo = new NavigatorArrayItemPropertyInfo(propertyInfo.PropertyType, childType, arrayItem.ElementName);
                    foreach (var childElement in element.Elements())
                        NavigateElementType(childPropertyInfo, childType, childElement, GetPropertyPath(propertyPath, arrayItem.ElementName));
                }
                else
                    NavigateElementType(propertyInfo, propertyInfo.PropertyType, element, propertyPath);
            }
            else
                NavigateElementType(propertyInfo, childType, element, propertyPath);
        }

        /// <summary>
        /// Navigates the nullable element.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elementType">Type of the element.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The property path.</param>
        protected virtual void NavigateNullableElementType(PropertyInfo propertyInfo, Type elementType, XElement element, string propertyPath)
        {
            NavigateElementType(propertyInfo, elementType, element, propertyPath);
        }

        /// <summary>
        /// Navigates the element.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elementType">Type of the element.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The property path.</param>
        protected virtual void NavigateElementType(PropertyInfo propertyInfo, Type elementType, XElement element, string propertyPath)
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

                NavigateProperty(textProperty, element, propertyType, propertyName, element.Value);
            }
            else
            {
                NavigateElementTypeWithoutXmlText(propertyInfo, elementType, element, propertyPath);
            }
        }

        /// <summary>
        /// Navigates the element without processing <see cref="XmlTextAttribute"/>.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elementType">Type of the element.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The property path.</param>
        protected virtual void NavigateElementTypeWithoutXmlText(PropertyInfo propertyInfo, Type elementType, XElement element, string propertyPath)
        {
            RemoveInvalidChildElementsAndAttributes(propertyInfo, elementType, element);

            if (element.HasElements || (element.HasAttributes && !element.Attributes().All(IsIgnoredAttribute)))
            {
                NavigateElement(element, elementType, propertyPath);
            }
            else
            {
                if (element.Attributes().Any(a => a.Name == Xsi("type")))
                {
                    var dataType = GetConcreteType(element, propertyInfo.PropertyType);
                    elementType = dataType ?? elementType;
                }

                NavigateProperty(propertyInfo, element, elementType, propertyPath, element.Value);
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
        protected virtual void NavigateUomAttribute(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string measureValue, string uomValue)
        {
            if (measureValue.EqualsIgnoreCase("NaN")) return;

            NavigateProperty(propertyInfo, xmlObject, propertyType, propertyPath, uomValue);
        }

        /// <summary>
        /// Navigates the attributes.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="parentPath">The parent path.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="skipUom">if set to <c>true</c> skip uom.</param>
        protected virtual void NavigateAttributes(XElement element, string parentPath, IList<PropertyInfo> properties, bool skipUom = false)
        {
            foreach (var attribute in element.Attributes())
            {
                if (IsIgnoredAttribute(attribute) || (skipUom && attribute.Name.LocalName == "uom"))
                    continue;

                var attributeProp = GetPropertyInfoForAnElement(properties, attribute.Name.LocalName);

                if (attributeProp != null)
                {
                    NavigateAttribute(attributeProp, attribute, parentPath);
                }
                else
                {
                    HandleInvalidAttribute(attribute, false);
                }
            }
        }

        /// <summary>
        /// Navigates the attribute.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="attribute">The attribute.</param>
        /// <param name="parentPath">The parent path.</param>
        protected virtual void NavigateAttribute(PropertyInfo propertyInfo, XAttribute attribute, string parentPath = null)
        {
            var propertyPath = GetPropertyPath(parentPath, propertyInfo.Name);
            var propertyType = propertyInfo.PropertyType;

            NavigateProperty(propertyInfo, attribute, propertyType, propertyPath, attribute.Value);
        }

        /// <summary>
        /// Navigates the property.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <exception cref="WitsmlException"></exception>
        protected virtual void NavigateProperty(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            if (string.IsNullOrWhiteSpace(propertyValue))
            {
                HandleNullValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue);
            }
            else if (propertyType == typeof(string))
            {
                HandleStringValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue);
            }
            else if (propertyType == typeof(byte[]))
            {
                HandleByteArrayValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue);
            }
            else if (propertyType.IsEnum)
            {
                var value = ParseEnum(propertyType, propertyValue);
                HandleObjectValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue, value);
            }
            else if (propertyType == typeof(bool))
            {
                propertyValue = propertyValue == "1"
                    ? bool.TrueString
                    : propertyValue == "0"
                        ? bool.FalseString
                        : propertyValue;

                var value = Convert.ChangeType(propertyValue, propertyType, CultureInfo.InvariantCulture);
                HandleObjectValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue, value);
            }
            else if (propertyType == typeof(DateTime))
            {
                DateTime value;

                if (!DateTime.TryParse(propertyValue, out value))
                    throw new WitsmlException(Context.Function.GetNonConformingErrorCode());

                HandleDateTimeValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue, value);
            }
            else if (propertyType == typeof(Timestamp))
            {
                DateTimeOffset value;

                if (!DateTimeOffset.TryParse(propertyValue, out value))
                    throw new WitsmlException(Context.Function.GetNonConformingErrorCode());

                HandleTimestampValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue, new Timestamp(value));
            }
            else if (propertyValue.EqualsIgnoreCase("NaN") && IsNumeric(propertyType))
            {
                HandleNaNValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue);
            }
            else if (typeof(IConvertible).IsAssignableFrom(propertyType))
            {
                var value = Convert.ChangeType(propertyValue, propertyType, CultureInfo.InvariantCulture);
                HandleObjectValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue, value);
            }
            else
            {
                HandleStringValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue);
            }
        }

        /// <summary>
        /// Parses the nested element to the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="element">The element.</param>
        /// <returns>The parsed object.</returns>
        /// <exception cref="WitsmlException"></exception>
        protected virtual object ParseNestedElement(Type type, XElement element)
        {
            if (element.DescendantsAndSelf().Any(e => (!e.HasAttributes && string.IsNullOrWhiteSpace(e.Value)) || e.Attributes().Any(a => string.IsNullOrWhiteSpace(a.Value))))
                throw new WitsmlException(ErrorCodes.EmptyNewElementsOrAttributes);

            var clone = element.UpdateRootElementName(type);

            try
            {
                return WitsmlParser.Parse(type, clone);
            }
            catch
            {
                // Try again without the default namespace specified
                clone = new XElement(clone) { Name = clone.Name.LocalName };
                return WitsmlParser.Parse(type, clone);
            }
        }


        /// <summary>
        /// Handles the <see cref="string" /> value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected virtual void HandleStringValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
        }

        /// <summary>
        /// Handles the byte array value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected virtual void HandleByteArrayValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
        }

        /// <summary>
        /// Handles the <see cref="DateTime" /> value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="dateTimeValue">The date time value.</param>
        protected virtual void HandleDateTimeValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, DateTime dateTimeValue)
        {
        }

        /// <summary>
        /// Handles the <see cref="Timestamp" /> value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="timestampValue">The timestamp value.</param>
        protected virtual void HandleTimestampValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, Timestamp timestampValue)
        {
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
        protected virtual void HandleObjectValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, object objectValue)
        {
        }

        /// <summary>
        /// Handles the NaN value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected virtual void HandleNaNValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
        }

        /// <summary>
        /// Handles the null value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected virtual void HandleNullValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            if (IsComplexType(propertyType))
                return;

            var element = xmlObject as XElement;
            if (element == null) return;

            var nil = Xsi("nil");
            var attribute = element.Attribute(nil);
            if (attribute != null) return;

            element.Add(new XAttribute(nil, "true"));
        }

        /// <summary>
        /// Initializes the recurring element handler.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyPath">The property path.</param>
        protected virtual void InitializeRecurringElementHandler(PropertyInfo propertyInfo, string propertyPath)
        {
        }

        /// <summary>
        /// Handles the recurring elements.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyPath">The property path.</param>
        protected virtual void HandleRecurringElements(PropertyInfo propertyInfo, string propertyPath)
        {
        }

        /// <summary>
        /// Handles the special case.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elementList">The element list.</param>
        /// <param name="parentPath">The parent path.</param>
        /// <param name="elementName">Name of the element.</param>
        /// <returns>true if the special case was handled, false otherwise</returns>
        protected virtual bool HandleSpecialCase(PropertyInfo propertyInfo, List<XElement> elementList, string parentPath, string elementName)
        {
            return false;
        }

        /// <summary>
        /// Handles the invalid element group.
        /// </summary>
        /// <param name="element">The name of the element.</param>
        protected virtual void HandleInvalidElementGroup(string element)
        {
            HandleInvalidElementOrAttribute(null, element, nameof(element), false);
        }

        /// <summary>
        /// Handles the invalid element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="remove">if set to <c>true</c> the element will be removed.</param>
        protected virtual void HandleInvalidElement(XElement element, bool remove = true)
        {
            HandleInvalidElementOrAttribute(element, element.Name.LocalName, nameof(element), remove);
        }

        /// <summary>
        /// Handles the invalid attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="remove">if set to <c>true</c> the attribute will be removed.</param>
        protected virtual void HandleInvalidAttribute(XAttribute attribute, bool remove = true)
        {
            HandleInvalidElementOrAttribute(attribute, attribute.Name.LocalName, nameof(attribute), remove);
        }

        /// <summary>
        /// Handles and optionally removes the invalid element or attribute.
        /// </summary>
        /// <param name="element">The XML element or attribute.</param>
        /// <param name="name">The XML element or attribute name.</param>
        /// <param name="type">The XML object type.</param>
        /// <param name="remove">if set to <c>true</c> the element or attribute will be removed.</param>
        protected virtual void HandleInvalidElementOrAttribute(XObject element, string name, string type, bool remove = true)
        {
            var message = $"Invalid {type} found: {name}";

            if (CompatibilitySettings.UnknownElementSetting == UnknownElementSetting.Ignore || Context.IgnoreUnknownElements)
            {
                Logger.Debug(message);
            }
            else if (CompatibilitySettings.UnknownElementSetting == UnknownElementSetting.Warn)
            {
                Logger.Warn(message);

                Context.Warnings.Add(new WitsmlValidationResult(
                    (short)ErrorCodes.SuccessWithWarnings,
                    message,
                    new[] { type }));
            }
            else
            {
                throw new WitsmlException(Context.Function.GetNonConformingErrorCode(), message);
            }

            if (remove)
            {
                Remove(element);
            }
        }

        /// <summary>
        /// Determines whether the specified element name is ignored.
        /// </summary>
        /// <param name="elementName">Name of the element.</param>
        /// <param name="parentPath">Parent path of the element.</param>
        /// <returns></returns>
        protected virtual bool IsIgnored(string elementName, string parentPath = null)
        {
            return Context.Ignored != null && Context.Ignored.Contains(elementName);
        }

        /// <summary>
        /// Removes the specified XML object from it's parent.
        /// </summary>
        /// <param name="xmlObject">The XML object.</param>
        protected virtual void Remove(XObject xmlObject)
        {
            var attribute = xmlObject as XAttribute;
            attribute?.Remove();

            var node = xmlObject as XNode;
            node?.Remove();
        }

        /// <summary>
        /// Gets the property information for an element.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <param name="name">The name of the property.</param>
        /// <returns>The property info for the element.</returns>
        protected virtual PropertyInfo GetPropertyInfoForAnElement(IList<PropertyInfo> properties, string name)
        {
            foreach (var prop in properties)
            {
                var elementAttribute = XmlAttributeCache<XmlElementAttribute>.GetCustomAttribute(prop);
                if (elementAttribute != null)
                {
                    if (elementAttribute.ElementName.EqualsIgnoreCase(name))
                        return prop;
                }

                var arrayAttribute = XmlAttributeCache<XmlArrayAttribute>.GetCustomAttribute(prop);
                if (arrayAttribute != null)
                {
                    if (arrayAttribute.ElementName.EqualsIgnoreCase(name))
                        return prop;
                }

                var attributeAttribute = XmlAttributeCache<XmlAttributeAttribute>.GetCustomAttribute(prop);
                if (attributeAttribute != null)
                {
                    if (attributeAttribute.AttributeName.EqualsIgnoreCase(name))
                        return prop;
                }

                // If no matches on Attributes try to match by property name
                if (prop.Name.EqualsIgnoreCase(name))
                    return prop;
            }
            return null;
        }

        /// <summary>
        /// Validates the uom/value pair for the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="uomProperty">The uom property.</param>
        /// <param name="measureValue">The measure value.</param>
        /// <returns>The uom value if valid.</returns>
        /// <exception cref="WitsmlException"></exception>
        protected virtual string ValidateMeasureUom(XElement element, PropertyInfo uomProperty, string measureValue)
        {
            var xmlAttribute = XmlAttributeCache<XmlAttributeAttribute>.GetCustomAttribute(uomProperty);
            var isRequired = IsRequired(uomProperty) && Context.Function != Functions.GetFromStore;

            // validation not needed if uom attribute is not defined
            if (xmlAttribute == null)
                return null;

            var uomValue = element.Attributes()
                .Where(x => x.Name.LocalName == xmlAttribute.AttributeName)
                .Select(x => x.Value)
                .FirstOrDefault();

            // uom is required when a measure value is specified
            if (isRequired && !string.IsNullOrWhiteSpace(measureValue) && string.IsNullOrWhiteSpace(uomValue))
            {
                throw new WitsmlException(Context.Function.GetMissingUomValueErrorCode());
            }

            return uomValue;
        }

        /// <summary>
        /// Gets the concrete type of the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="propType">Type of the property.</param>
        /// <returns>The concrete type</returns>
        protected virtual Type GetConcreteType(XElement element, Type propType)
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

            var dataType = propType.Assembly.GetTypes()
                .FirstOrDefault(t =>
                {
                    var xmlType = XmlAttributeCache<XmlTypeAttribute>.GetCustomAttribute(t);
                    return ((xmlType != null && xmlType.TypeName == typeName) &&
                        (string.IsNullOrWhiteSpace(@namespace) || xmlType.Namespace == @namespace));
                });

            return dataType ?? GetClrType(typeName);
        }

        /// <summary>
        /// Gets the the CLR type for the specified XML data type.
        /// </summary>
        /// <param name="xmlDataType">The XML data type.</param>
        /// <returns>The CLR type.</returns>
        /// <remarks>See: https://docs.microsoft.com/en-us/dotnet/standard/data/xml/mapping-xml-data-types-to-clr-types </remarks>
        protected virtual Type GetClrType(string xmlDataType)
        {
            switch (xmlDataType)
            {
                case "base64Binary":
                case "hexBinary":
                    return typeof(byte[]);

                case "bool":
                case "boolean":
                    return typeof(bool);

                case "byte":
                    return typeof(sbyte);

                case "date":
                case "datetime":
                case "time":
                    return typeof(DateTime);

                case "decimal":
                case "integer":
                case "positiveInteger":
                case "negativeInteger":
                case "nonNegativeInteger":
                case "nonPositiveInteger":
                    return typeof(decimal);

                case "double":
                    return typeof(double);

                case "duration":
                case "dayTimeDuration":
                case "yearMonthDuration":
                    return typeof(TimeSpan);

                case "float":
                    return typeof(float);

                case "int":
                    return typeof(int);

                case "long":
                    return typeof(long);

                case "short":
                    return typeof(short);

                case "unsignedByte":
                    return typeof(byte);

                case "unsignedInt":
                    return typeof(uint);

                case "unsignedLong":
                    return typeof(ulong);

                case "unsignedShort":
                    return typeof(ushort);

                default:
                    return typeof(string);
            }
        }

        /// <summary>
        /// Parses the enum.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="enumValue">The enum value.</param>
        /// <returns></returns>
        /// <exception cref="WitsmlException"></exception>
        protected virtual object ParseEnum(Type enumType, string enumValue)
        {
            try
            {
                return enumType.ParseEnum(enumValue);
            }
            catch (ArgumentException ex)
            {
                var errorCode = enumType.Name.IndexOf("Uom", StringComparison.InvariantCultureIgnoreCase) > -1 ||
                                enumType.Name.IndexOf("Unit", StringComparison.InvariantCultureIgnoreCase) > -1
                    ? ErrorCodes.InvalidUnitOfMeasure
                    : ErrorCodes.InputTemplateNonConforming;

                throw new WitsmlException(errorCode, ex);
            }
        }

        /// <summary>
        /// Gets the property information.
        /// </summary>
        /// <param name="type">The property type.</param>
        /// <param name="element">An optional element containing type information.</param>
        /// <returns>A collection of <see cref="PropertyInfo"/> objects.</returns>
        protected virtual IList<PropertyInfo> GetPropertyInfo(Type type, XElement element = null)
        {
            if (type.IsAbstract && element != null)
                type = GetConcreteType(element, type);

            return type.GetProperties()
                .Where(p => !XmlAttributeCache<XmlIgnoreAttribute>.IsDefined(p))
                .ToList();
        }

        /// <summary>
        /// Gets the full path for the property.
        /// </summary>
        /// <param name="parentPath">The parent path.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The full path for the property.</returns>
        protected virtual string GetPropertyPath(string parentPath, string propertyName)
        {
            var prefix = string.IsNullOrEmpty(parentPath) ? string.Empty : string.Format("{0}.", parentPath);
            return string.Format("{0}{1}", prefix, propertyName.ToPascalCase());
        }

        /// <summary>
        /// Determines whether the element has any attribute with non-empty value.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>
        ///   <c>true</c> if the element has any attribute with non-empty value; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool HasAttributesWithValues(XElement element)
        {
            return element.Attributes().Any(a => !a.IsNamespaceDeclaration && !string.IsNullOrWhiteSpace(a.Value));
        }

        /// <summary>
        /// Determines whether the specified type has a uid property.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the type defines a uid property; otherwise, <c>false</c>.</returns>
        protected virtual bool HasUidProperty(Type type)
        {
            return type.GetProperty("Uid") != null;
        }

        /// <summary>
        /// Determines whether the specified type is a complex type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the type defines a complex type; otherwise, <c>false</c>.</returns>
        protected virtual bool IsComplexType(Type type)
        {
            return type != typeof(string) && !type.IsValueType && !type.IsEnum;
        }

        /// <summary>
        /// Determines whether the specified property information is required.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <returns><c>true</c> if the propertyInfo has the custom attribute RequiredAttribute; /// otherwise, <c>false</c>.</returns>
        protected virtual bool IsRequired(PropertyInfo propertyInfo)
        {
            return XmlAttributeCache<RequiredAttribute>.GetCustomAttribute(propertyInfo) != null;
        }

        /// <summary>
        /// Determines whether the specified property is a special case.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <returns>true if the property is a special case, false otherwise.</returns>
        protected virtual bool IsSpecialCase(PropertyInfo propertyInfo)
        {
            var propertyType = propertyInfo.PropertyType;
            var args = propertyType.GetGenericArguments();
            var childType = args.FirstOrDefault() ?? propertyType.GetElementType();

            if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(byte[]) &&
                childType != null && !HasUidProperty(childType))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the property with the <see cref="XmlTextAttribute"/> attribute defined, if any.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The <see cref="PropertyInfo"/> instance.</returns>
        protected virtual PropertyInfo GetXmlTextProperty(Type type)
        {
            return type.GetProperties().FirstOrDefault(XmlAttributeCache<XmlTextAttribute>.IsDefined);
        }

        /// <summary>
        /// Determines whether the specified type has simple content.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the type defines a type with simple content; otherwise, <c>false</c>.</returns>
        protected virtual bool HasSimpleContent(Type type)
        {
            return type.GetProperties().Any(XmlAttributeCache<XmlTextAttribute>.IsDefined);
        }

        /// <summary>
        /// Determines whether the specified type supports any XML element.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the type supports any XML elements; otherwise, <c>false</c>.</returns>
        protected virtual bool HasXmlAnyElement(Type type)
        {
            return type.GetProperties().Any(XmlAttributeCache<XmlAnyElementAttribute>.IsDefined);
        }

        /// <summary>
        /// Determines whether the specified property type is numeric.
        /// </summary>
        /// <param name="propertyType">The property type.</param>
        /// <returns>
        ///   <c>true</c> if the specified property type is numeric; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool IsNumeric(Type propertyType)
        {
            var type = propertyType;

            if (propertyType.IsClass)
            {
                var propertyInfo = propertyType.GetProperty("Value");
                if (propertyInfo != null)
                {
                    type = propertyInfo.PropertyType;
                }
                else
                {
                    return false;
                }
            }

            return type.IsNumeric();
        }

        /// <summary>
        /// Determines whether the specified attribute is an ignored attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns>
        ///   <c>true</c> if the specified attribute is ignored; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool IsIgnoredAttribute(XAttribute attribute)
        {
            return attribute.IsNamespaceDeclaration || attribute.Name == Xsi("nil") || attribute.Name == Xsi("type");
        }

        /// <summary>
        /// Removes invalid child elements and attributes from the specified element.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elementType">The element type.</param>
        /// <param name="element">The XML element.</param>
        protected virtual void RemoveInvalidChildElementsAndAttributes(PropertyInfo propertyInfo, Type elementType, XElement element)
        {
            // Ignore list properties that declare child elements using XmlArrayItem
            if (XmlAttributeCache<XmlArrayItemAttribute>.GetCustomAttribute(propertyInfo) != null) return;

            var properties = GetPropertyInfo(elementType, element);

            foreach (var attribute in element.Attributes())
            {
                if (IsIgnoredAttribute(attribute)) continue;

                var attributeInfo = GetPropertyInfoForAnElement(properties, attribute.Name.LocalName);

                if (attributeInfo == null)
                {
                    HandleInvalidAttribute(attribute);
                }
            }

            // Ignore CustomData child elements
            if (ObjectTypes.CustomData.EqualsIgnoreCase(ObjectTypes.GetObjectTypeFromGroup(element)))
                return;

            foreach (var child in element.Elements())
            {
                var childInfo = GetPropertyInfoForAnElement(properties, child.Name.LocalName);

                if (childInfo == null)
                {
                    HandleInvalidElement(child);
                }
            }
        }
    }
}
