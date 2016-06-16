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
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using Energistics.DataAccess;
using log4net;
using PDS.Framework;

namespace PDS.Witsml.Data
{
    /// <summary>
    /// Provides a framework for navigating a WITSML document while providing additional data object type information.
    /// </summary>
    /// <typeparam name="TContext">The type of the context.</typeparam>
    public abstract class DataObjectNavigator<TContext> where TContext : DataObjectNavigationContext
    {
        private static readonly XNamespace _xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

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
        protected ILog Logger { get; private set; }

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
                var propertyInfo = GetPropertyInfoForAnElement(properties, group.Key);
                if (IsIgnored(group.Key, GetPropertyPath(parentPath, group.Key))) continue;
                
                if (propertyInfo != null)
                {
                    NavigateElementGroup(propertyInfo, group, parentPath);
                } 
                else
                {
                    Logger.DebugFormat("Invalid element '{0}' is ignored.", group.Key);                   
                }
            }

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Name == Xsi("nil") || attribute.Name == Xsi("type"))
                    continue;

                var attributeProp = GetPropertyInfoForAnElement(properties, attribute.Name.LocalName);

                if (attributeProp != null)
                {
                    NavigateAttribute(attributeProp, attribute, parentPath);
                }
                else
                {
                    Logger.DebugFormat("Invalid attribute '{0}' is ignored.", attribute.Name.LocalName);
                }
            }
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
                        NavigateNullableElementType(underlyingType, element, propertyPath, propertyInfo);
                    }
                    else if (genericType == typeof(List<>))
                    {
                        var childType = propertyType.GetGenericArguments()[0];
                        NavigateArrayElementType(elementList, childType, element, propertyPath, propertyInfo);
                    }
                }
                else if (propertyType.IsAbstract)
                {
                    var concreteType = GetConcreteType(element, propertyType);
                    NavigateElementType(concreteType, element, propertyPath);
                }
                else
                {
                    NavigateElementType(propertyType, element, propertyPath);
                }
            }
            else
            {
                InitializeRecurringElementHandler(propertyPath);

                var childType = propertyType.GetGenericArguments()[0];
           
                NavigateRecurringElements(elementList, childType, propertyPath, propertyInfo);

                HandleRecurringElements(propertyPath);
            }
        }

        /// <summary>
        /// Navigates the recurring elements.
        /// </summary>
        /// <param name="elements">The elements.</param>
        /// <param name="childType">Type of the child.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyInfo">The property information.</param>
        protected virtual void NavigateRecurringElements(List<XElement> elements, Type childType, string propertyPath, PropertyInfo propertyInfo)
        {
            foreach (var value in elements)
            {
                NavigateElementType(childType, value, propertyPath);
            }
        }

        /// <summary>
        /// Navigates the array element.
        /// </summary>
        /// <param name="elements">The elements.</param>
        /// <param name="childType">Type of the child.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyInfo">The property information.</param>
        protected virtual void NavigateArrayElementType(List<XElement> elements, Type childType, XElement element, string propertyPath, PropertyInfo propertyInfo)
        {
            NavigateElementType(childType, element, propertyPath, propertyInfo);
        }

        /// <summary>
        /// Navigates the nullable element.
        /// </summary>
        /// <param name="elementType">Type of the element.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyInfo">The property information.</param>
        protected virtual void NavigateNullableElementType(Type elementType, XElement element, string propertyPath, PropertyInfo propertyInfo)
        {
            NavigateElementType(elementType, element, propertyPath);
        }

        /// <summary>
        /// Navigates the element.
        /// </summary>
        /// <param name="elementType">Type of the element.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="parentPropertyInfo">The parent property information.</param>
        protected void NavigateElementType(Type elementType, XElement element, string propertyPath, PropertyInfo parentPropertyInfo = null)
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

                    NavigateUomAttribute(element.Attribute("uom"), uomProperty.PropertyType, uomPath, element.Value,
                        uomValue);
                }

                NavigateProperty(element, propertyType, propertyName, element.Value);
            }
            else
            {
                RemoveInvalidChildElementsAndAttributes(elementType, element, parentPropertyInfo);

                if (element.HasElements || element.HasAttributes)
                {
                    NavigateElement(element, elementType, propertyPath);
                }
                else
                {
                    NavigateProperty(element, elementType, propertyPath, element.Value);
                }
            }
        }

        /// <summary>
        /// Navigates the uom attribute.
        /// </summary>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="measureValue">The measure value.</param>
        /// <param name="uomValue">The uom value.</param>
        protected virtual void NavigateUomAttribute(XObject xmlObject, Type propertyType, string propertyPath, string measureValue, string uomValue)
        {
            if (measureValue.EqualsIgnoreCase("NaN")) return;

            NavigateProperty(xmlObject, propertyType, propertyPath, uomValue);
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

            NavigateProperty(attribute, propertyType, propertyPath, attribute.Value);
        }

        /// <summary>
        /// Navigates the property.
        /// </summary>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <exception cref="WitsmlException">
        /// </exception>
        protected void NavigateProperty(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            if (string.IsNullOrWhiteSpace(propertyValue))
            {
                HandleNullValue(xmlObject, propertyType, propertyPath, propertyValue);
            }
            else if (propertyType == typeof(string))
            {
                HandleStringValue(xmlObject, propertyType, propertyPath, propertyValue);
            }
            else if (propertyType.IsEnum)
            {
                var value = ParseEnum(propertyType, propertyValue);
                HandleObjectValue(xmlObject, propertyType, propertyPath, propertyValue, value);
            }
            else if (propertyType == typeof(DateTime))
            {
                DateTime value;

                if (!DateTime.TryParse(propertyValue, out value))
                    throw new WitsmlException(ErrorCodes.InputTemplateNonConforming);

                HandleDateTimeValue(xmlObject, propertyType, propertyPath, propertyValue, value);
            }
            else if (propertyType == typeof(Timestamp))
            {
                DateTimeOffset value;

                if (!DateTimeOffset.TryParse(propertyValue, out value))
                    throw new WitsmlException(ErrorCodes.InputTemplateNonConforming);

                HandleTimestampValue(xmlObject, propertyType, propertyPath, propertyValue, new Timestamp(value));
            }
            else if (propertyValue.EqualsIgnoreCase("NaN") && IsNumeric(propertyType))
            {
                HandleNaNValue(xmlObject, propertyType, propertyPath, propertyValue);
            }
            else if (typeof(IConvertible).IsAssignableFrom(propertyType))
            {
                var value = Convert.ChangeType(propertyValue, propertyType);
                HandleObjectValue(xmlObject, propertyType, propertyPath, propertyValue, value);
            }
            else
            {
                HandleStringValue(xmlObject, propertyType, propertyPath, propertyValue);
            }
        }

        /// <summary>
        /// Handles the <see cref="string"/> value.
        /// </summary>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected virtual void HandleStringValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
        }

        /// <summary>
        /// Handles the <see cref="DateTime"/> value.
        /// </summary>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="dateTimeValue">The date time value.</param>
        protected virtual void HandleDateTimeValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, DateTime dateTimeValue)
        {
        }

        /// <summary>
        /// Handles the <see cref="Timestamp"/> value.
        /// </summary>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="timestampValue">The timestamp value.</param>
        protected virtual void HandleTimestampValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, Timestamp timestampValue)
        {
        }

        /// <summary>
        /// Handles the object value.
        /// </summary>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="objectValue">The object value.</param>
        protected virtual void HandleObjectValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, object objectValue)
        {
        }

        /// <summary>
        /// Handles the NaN value.
        /// </summary>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected virtual void HandleNaNValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
        }

        /// <summary>
        /// Handles the null value.
        /// </summary>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected virtual void HandleNullValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
        }

        /// <summary>
        /// Initializes the recurring element handler.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        protected virtual void InitializeRecurringElementHandler(string propertyPath)
        {
        }

        /// <summary>
        /// Handles the recurring elements.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        protected virtual void HandleRecurringElements(string propertyPath)
        {
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

            // validation not needed if uom attribute is not defined
            if (xmlAttribute == null)
                return null;

            var uomValue = element.Attributes()
                .Where(x => x.Name.LocalName == xmlAttribute.AttributeName)
                .Select(x => x.Value)
                .FirstOrDefault();

            // uom is required when a measure value is specified
            if (!string.IsNullOrWhiteSpace(measureValue) && string.IsNullOrWhiteSpace(uomValue))
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

        private void RemoveInvalidChildElementsAndAttributes(Type elementType, XElement element, PropertyInfo parentPropertyInfo)
        {
            // Ignore list properties that declare child elements using XmlArrayItem
            if (parentPropertyInfo?.GetCustomAttribute<XmlArrayItemAttribute>() != null) return;

            var properties = GetPropertyInfo(elementType);

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Name == Xsi("nil") || attribute.Name == Xsi("type"))
                    continue;

                var propertyInfo = GetPropertyInfoForAnElement(properties, attribute.Name.LocalName);
                if (propertyInfo == null)
                {
                    attribute.Remove();
                    Logger.DebugFormat("Invalid attribute '{0}' is ignored.", attribute.Name.LocalName);
                }
            }

            foreach (var child in element.Elements())
            {
                var propertyInfo = GetPropertyInfoForAnElement(properties, child.Name.LocalName);
                if (propertyInfo == null)
                {
                    child.Remove();
                    Logger.DebugFormat("Invalid element '{0}' is ignored.", child.Name.LocalName);
                }
            }
        }
    }
}
