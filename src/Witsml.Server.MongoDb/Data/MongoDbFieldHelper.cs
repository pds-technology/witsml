using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using Energistics.DataAccess;
using Witsml200 = Energistics.DataAccess.WITSML200;
using MongoDB.Driver;
using PDS.Framework;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Helper class that encapsulates methods to handle element parsing for update and query
    /// </summary>
    public static class MongoDbFieldHelper
    {
        private static readonly XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

        /// <summary>
        /// Gets the property information.
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>The list of property info for the type.</returns>
        public static IList<PropertyInfo> GetPropertyInfo(Type t)
        {
            return t.GetProperties()
                .Where(p => !p.IsDefined(typeof(XmlIgnoreAttribute), false))
                .ToList();
        }

        /// <summary>
        /// Gets the property path for its MongoDb field.
        /// </summary>
        /// <param name="parentPath">The parent path.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>The MongoDb field path for the property.</returns>
        public static string GetPropertyPath(string parentPath, string propertyName)
        {
            var prefix = string.IsNullOrEmpty(parentPath) ? string.Empty : string.Format("{0}.", parentPath);
            return string.Format("{0}{1}", prefix, CaptalizeString(propertyName));
        }

        /// <summary>
        /// Gets the property information for an element, for some element name is not the same as property name, i.e. Mongo field name.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <param name="element">The element.</param>
        /// <returns>The found property for the serialization element.</returns>
        public static PropertyInfo GetPropertyInfoForAnElement(IEnumerable<PropertyInfo> properties, string name)
        {
            foreach (var prop in properties)
            {
                var elementAttribute = prop.GetCustomAttribute<XmlElementAttribute>();
                if (elementAttribute != null)
                {
                    if (elementAttribute.ElementName == name)
                        return prop;
                }

                var arrayAttribute = prop.GetCustomAttribute<XmlArrayAttribute>();
                if (arrayAttribute != null)
                {
                    if (arrayAttribute.ElementName == name)
                        return prop;
                }

                var attributeAttribute = prop.GetCustomAttribute<XmlAttributeAttribute>();
                if (attributeAttribute != null)
                {
                    if (attributeAttribute.AttributeName == name)
                        return prop;
                }
            }
            return null;
        }

        /// <summary>
        /// Parses the enum.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="enumValue">The enum value.</param>
        /// <returns>The enum.</returns>
        /// <exception cref="WitsmlException"></exception>
        public static object ParseEnum(Type enumType, string enumValue)
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
        /// Validates uom/value pair for the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="uomProperty">The uom property.</param>
        /// <param name="measureValue">The measure value.</param>
        /// <returns></returns>
        /// <exception cref="WitsmlException"></exception>
        public static string ValidateMeasureUom(XElement element, PropertyInfo uomProperty, string measureValue)
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
        /// Gets the concrete type.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="propType">Type of the property.</param>
        /// <returns>The concrete type.</returns>
        public static Type GetConcreteType(XElement element, Type propType)
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

        public static string CaptalizeString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = char.ToUpper(input[0]).ToString();

            if (input.Length > 1)
                result += input.Substring(1);

            return result;
        }

        /// <summary>
        /// Get the Xml namespace value of the element.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns>The Xml namespace value.</returns>
        public static XName Xmlns(string attributeName)
        {
            return XNamespace.Xmlns.GetName(attributeName);
        }

        /// <summary>
        /// Get the XNamespace.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns>The XNamespace value.</returns>
        public static XName Xsi(string attributeName)
        {
            return xsi.GetName(attributeName);
        }

        /// <summary>
        /// Gets the entity filter by its id property.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <param name="idPropertyName">Name of the identifier property.</param>
        /// <returns>The Mongo collection filter by the id.</returns>
        public static FilterDefinition<T> GetEntityFilter<T>(DataObjectId dataObjectId, string idPropertyName = "Uid")
        {
            var builder = Builders<T>.Filter;
            var filters = new List<FilterDefinition<T>>();

            filters.Add(builder.EqIgnoreCase(idPropertyName, dataObjectId.Uid));

            if (dataObjectId is WellObjectId)
            {
                filters.Add(builder.EqIgnoreCase("UidWell", ((WellObjectId)dataObjectId).UidWell));
            }
            if (dataObjectId is WellboreObjectId)
            {
                filters.Add(builder.EqIgnoreCase("UidWellbore", ((WellboreObjectId)dataObjectId).UidWellbore));
            }

            return builder.And(filters);
        }

        /// <summary>
        /// Creates a dictionary of common object property paths to update.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Dictionary<string, object> CreateUpdateFields<T>()
        {
            if (typeof(IDataObject).IsAssignableFrom(typeof(T)))
            {
                return new Dictionary<string, object> {
                    { "CommonData.DateTimeLastChange", DateTimeOffset.UtcNow.ToString("o") }
                };
            }
            else if (typeof(Witsml200.ComponentSchemas.AbstractObject).IsAssignableFrom(typeof(T)))
            {
                return new Dictionary<string, object> {
                    { "Citation.LastUpdate", DateTime.UtcNow.ToString("o") }
                };
            }

            return new Dictionary<string, object>(0);
        }

        /// <summary>
        /// Creates a list of common element names to ignore during an update.
        /// </summary>
        /// <typeparam name="T">The data object type</typeparam>
        /// <param name="ignored">A custom list of elements to ignore.</param>
        /// <returns></returns>
        public static string[] CreateIgnoreFields<T>(string[] ignored)
        {
            var creationTime = typeof(IDataObject).IsAssignableFrom(typeof(T))
                ? new string[] { "dTimCreation", "dTimLastChange" }
                : new string[] { "Creation", "LastUpdate" };

            return ignored == null ? creationTime : creationTime.Union(ignored).ToArray();
        }
    }
}
