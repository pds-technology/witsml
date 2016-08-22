//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using Energistics.DataAccess;
using Energistics.DataAccess.Validation;
using log4net;
using PDS.Framework;
using PDS.Witsml.Server.Compatibility;

namespace PDS.Witsml.Data
{
    /// <summary>
    /// Provides a framework for navigating a WITSML document while providing additional data object type information.
    /// </summary>
    /// <typeparam name="TContext">The type of the context.</typeparam>
    public abstract class DataObjectNavigator<TContext> where TContext : DataObjectNavigationContext
    {
        private static readonly XNamespace _xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
        private static readonly UnknownElementSetting _unknownElementSetting;

        /// <summary>
        /// Initializes the <see cref="DataObjectNavigator{TContext}"/> class.
        /// </summary>
        static DataObjectNavigator()
        {
            Enum.TryParse(Properties.Settings.Default.UnknownElementSetting, out _unknownElementSetting);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataObjectNavigator{TContext}"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        protected DataObjectNavigator(TContext context)
        {
            Logger = LogManager.GetLogger(GetType());
            Context = context;
        }

        /// <summary>
        /// Gets the context.
        /// </summary>
        /// <value>The context.</value>
        protected TContext Context { get; }

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
            return _xsi.GetName(attributeName);
        }

        /// <summary>
        /// Navigates the specified element.
        /// </summary>
        /// <param name="element">The XML element.</param>
        protected void Navigate(XElement element)
        {
            Logger.DebugFormat("Navigating XML element: {0}", element?.Name.LocalName);
            NavigateElement(element, Context.DataObjectType);
        }

        /// <summary>
        /// Navigates the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="type">The type.</param>
        /// <param name="parentPath">The parent path.</param>
        protected void NavigateElement(XElement element, Type type, string parentPath = null)
        {
            if (IsIgnored(element.Name.LocalName)) return;

            var properties = GetPropertyInfo(type);
            var groupings = element.Elements().GroupBy(e => e.Name.LocalName);

            foreach (var group in groupings)
            {
                if (IsIgnored(group.Key, GetPropertyPath(parentPath, group.Key))) continue;

                var propertyInfo = GetPropertyInfoForAnElement(properties, group.Key);

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

            if (elementList.Count == 1)
            {
                var element = elementList.FirstOrDefault();

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
            else
            {
                InitializeRecurringElementHandler(propertyInfo, propertyPath);

                var args = propertyType.GetGenericArguments();
                var childType = args.FirstOrDefault() ?? propertyType.GetElementType();
           
                NavigateRecurringElements(propertyInfo, elementList, childType, propertyPath);

                HandleRecurringElements(propertyInfo, propertyPath);
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
            var textProperty = elementType.GetProperties().FirstOrDefault(x => x.IsDefined(typeof(XmlTextAttribute), false));

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
                    var properties = GetPropertyInfo(elementType);
                    NavigateAttributes(element, propertyPath, properties, true);
                }

                NavigateProperty(propertyInfo, element, propertyType, propertyName, element.Value);
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

            if (element.HasElements || element.HasAttributes)
            {
                NavigateElement(element, elementType, propertyPath);
            }
            else
            {
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
                if (attribute.IsNamespaceDeclaration || attribute.Name == Xsi("nil") || attribute.Name == Xsi("type") ||
                    (skipUom && attribute.Name.LocalName == "uom"))
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
        protected void NavigateAttribute(PropertyInfo propertyInfo, XAttribute attribute, string parentPath = null)
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
        protected void NavigateProperty(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            if (string.IsNullOrWhiteSpace(propertyValue))
            {
                HandleNullValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue);
            }
            else if (propertyType == typeof(string))
            {
                HandleStringValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue);
            }
            else if (propertyType.IsEnum)
            {
                var value = ParseEnum(propertyType, propertyValue);
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
                var value = Convert.ChangeType(propertyValue, propertyType);
                HandleObjectValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue, value);
            }
            else
            {
                HandleStringValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue);
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
        private void HandleInvalidElementOrAttribute(XObject element, string name, string type, bool remove = true)
        {
            var message = $"Invalid {type} found: {name}";

            if (_unknownElementSetting == UnknownElementSetting.Ignore || Context.IgnoreUnknownElements)
            {
                Logger.Debug(message);
            }
            else if (_unknownElementSetting == UnknownElementSetting.Warn)
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
        protected void Remove(XObject xmlObject)
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
        protected PropertyInfo GetPropertyInfoForAnElement(IList<PropertyInfo> properties, string name)
        {
            foreach (var prop in properties)
            {
                var elementAttribute = prop.GetCustomAttribute<XmlElementAttribute>();
                if (elementAttribute != null)
                {
                    if (elementAttribute.ElementName.EqualsIgnoreCase(name))
                        return prop;
                }

                var arrayAttribute = prop.GetCustomAttribute<XmlArrayAttribute>();
                if (arrayAttribute != null)
                {
                    if (arrayAttribute.ElementName.EqualsIgnoreCase(name))
                        return prop;
                }

                var attributeAttribute = prop.GetCustomAttribute<XmlAttributeAttribute>();
                if (attributeAttribute != null)
                {
                    if (attributeAttribute.AttributeName.EqualsIgnoreCase(name))
                        return prop;
                }
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
        protected string ValidateMeasureUom(XElement element, PropertyInfo uomProperty, string measureValue)
        {
            var xmlAttribute = uomProperty.GetCustomAttribute<XmlAttributeAttribute>();
            var isRequired = IsRequired(uomProperty);

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
                throw new WitsmlException(ErrorCodes.MissingUnitForMeasureData);
            }

            return uomValue;
        }

        /// <summary>
        /// Gets the concrete type of the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="propType">Type of the property.</param>
        /// <returns>The concrete type</returns>
        protected Type GetConcreteType(XElement element, Type propType)
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

            return propType.Assembly.GetTypes()
                .FirstOrDefault(t =>
                {
                    var xmlType = t.GetCustomAttribute<XmlTypeAttribute>();
                    return ((xmlType != null && xmlType.TypeName == typeName) &&
                        (string.IsNullOrWhiteSpace(@namespace) || xmlType.Namespace == @namespace));
                });
        }

        /// <summary>
        /// Parses the enum.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="enumValue">The enum value.</param>
        /// <returns></returns>
        /// <exception cref="WitsmlException"></exception>
        protected object ParseEnum(Type enumType, string enumValue)
        {
            if (Enum.IsDefined(enumType, enumValue))
            {
                return Enum.Parse(enumType, enumValue);
            }

            var enumMember = enumType.GetMembers().FirstOrDefault(x =>
            {
                if (x.Name.EqualsIgnoreCase(enumValue))
                    return true;

                var xmlEnumAttrib = x.GetCustomAttribute<XmlEnumAttribute>();
                return xmlEnumAttrib != null && xmlEnumAttrib.Name.EqualsIgnoreCase(enumValue);
            });

            // must be a valid enumeration member
            if (!enumType.IsEnum || enumMember == null)
            {
                throw new WitsmlException(ErrorCodes.InvalidUnitOfMeasure);
            }

            return Enum.Parse(enumType, enumMember.Name);
        }

        /// <summary>
        /// Gets the property information.
        /// </summary>
        /// <param name="t">The property type.</param>
        /// <returns></returns>
        protected IList<PropertyInfo> GetPropertyInfo(Type t)
        {
            return t.GetProperties()
                .Where(p => !p.IsDefined(typeof(XmlIgnoreAttribute), false))
                .ToList();
        }

        /// <summary>
        /// Gets the full path for the property.
        /// </summary>
        /// <param name="parentPath">The parent path.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The full path for the property.</returns>
        protected string GetPropertyPath(string parentPath, string propertyName)
        {
            var prefix = string.IsNullOrEmpty(parentPath) ? string.Empty : string.Format("{0}.", parentPath);
            return string.Format("{0}{1}", prefix, propertyName.ToPascalCase());
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
            return !(type == typeof(string)) && !type.IsValueType && !type.IsEnum;
        }

        /// <summary>
        /// Determines whether the specified property information is required.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <returns><c>true</c> if the propertyInfo has the custom attribute RequiredAttribute; /// otherwise, <c>false</c>.</returns>
        protected virtual bool IsRequired(PropertyInfo propertyInfo)
        {
            return propertyInfo?.GetCustomAttribute<RequiredAttribute>() != null;
        }

        /// <summary>
        /// Determines whether the specified type has simple content.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the type defines a type with simple content; otherwise, <c>false</c>.</returns>
        protected virtual bool HasSimpleContent(Type type)
        {
            return type.GetProperties().Any(x => x.IsDefined(typeof(XmlTextAttribute), false));
        }

        private bool IsNumeric(Type propertyType)
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

        private void RemoveInvalidChildElementsAndAttributes(PropertyInfo propertyInfo, Type elementType, XElement element)
        {
            // Ignore list properties that declare child elements using XmlArrayItem
            if (propertyInfo.GetCustomAttribute<XmlArrayItemAttribute>() != null) return;

            var properties = GetPropertyInfo(elementType);

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Name == Xsi("nil") || attribute.Name == Xsi("type"))
                    continue;

                var attributeInfo = GetPropertyInfoForAnElement(properties, attribute.Name.LocalName);

                if (attributeInfo == null)
                {
                    HandleInvalidAttribute(attribute);
                }
            }

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
