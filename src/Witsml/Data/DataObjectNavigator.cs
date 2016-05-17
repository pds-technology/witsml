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
        /// Determines whether the specified element name is ignored.
        /// </summary>
        /// <param name="elementName">Name of the element.</param>
        /// <returns></returns>
        protected virtual bool IsIgnored(string elementName)
        {
            return Context.Ignored != null && Context.Ignored.Contains(elementName);
        }

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
            NavigateElement(element, Context.DataObjectType);
        }

        protected void NavigateElement(XElement element, Type type, string parentPath = null)
        {
            if (IsIgnored(element.Name.LocalName)) return;

            var properties = GetPropertyInfo(type);
            var groupings = element.Elements().GroupBy(e => e.Name.LocalName);

            foreach (var group in groupings)
            {
                if (IsIgnored(group.Key)) continue;

                var propertyInfo = GetPropertyInfoForAnElement(properties, group.Key);
                NavigateElementGroup(propertyInfo, group, parentPath);
            }

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Name == Xsi("nil") || attribute.Name == Xsi("type"))
                    continue;

                var attributeProp = GetPropertyInfoForAnElement(properties, attribute.Name.LocalName);
                NavigateAttribute(attributeProp, attribute, parentPath);
            }
        }

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

        protected virtual void NavigateRecurringElements(List<XElement> elements, Type childType, string propertyPath, PropertyInfo propertyInfo)
        {
            foreach (var value in elements)
            {
                NavigateElementType(childType, value, propertyPath);
            }
        }

        protected virtual void NavigateArrayElementType(List<XElement> elements, Type childType, XElement element, string propertyPath, PropertyInfo propertyInfo)
        {
            NavigateElementType(childType, element, propertyPath);
        }

        protected virtual void NavigateNullableElementType(Type elementType, XElement element, string propertyPath, PropertyInfo propertyInfo)
        {
            NavigateElementType(elementType, element, propertyPath);
        }     

        protected void NavigateElementType(Type elementType, XElement element, string propertyPath)
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

                    NavigateUomAttribute(element.Attribute("uom"), uomProperty.PropertyType, uomPath, element.Value, uomValue);
                }

                NavigateProperty(element, propertyType, propertyName, element.Value);
            }
            else if (element.HasElements || element.HasAttributes)
            {
                NavigateElement(element, elementType, propertyPath);
            }
            else
            {
                NavigateProperty(element, elementType, propertyPath, element.Value);
            }
        }

        protected virtual void NavigateUomAttribute(XObject xmlObject, Type propertyType, string propertyPath, string measureValue, string uomValue)
        {
            // By default, ignore the uomValue if there is no measureValue provided
            if (string.IsNullOrWhiteSpace(measureValue) || measureValue.EqualsIgnoreCase("NaN")) return;

            NavigateProperty(xmlObject, propertyType, propertyPath, uomValue);
        }

        protected void NavigateAttribute(PropertyInfo propertyInfo, XAttribute attribute, string parentPath = null)
        {
            var propertyPath = GetPropertyPath(parentPath, propertyInfo.Name);
            var propertyType = propertyInfo.PropertyType;

            NavigateProperty(attribute, propertyType, propertyPath, attribute.Value);
        }

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

        protected virtual void HandleStringValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
        }

        protected virtual void HandleDateTimeValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, DateTime dateTimeValue)
        {
        }

        protected virtual void HandleTimestampValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, Timestamp timestampValue)
        {
        }

        protected virtual void HandleObjectValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, object objectValue)
        {
        }

        protected virtual void HandleNaNValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
        }

        protected virtual void HandleNullValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
        }

        protected virtual void InitializeRecurringElementHandler(string propertyPath)
        {
        }

        protected virtual void HandleRecurringElements(string propertyPath)
        {
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
    }
}
