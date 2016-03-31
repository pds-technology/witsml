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
using Energistics.Datatypes;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Utility class that encapsulates helper methods for parsing element in query and update
    /// </summary>
    public static class MongoDbUtility
    {
        private static readonly XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

        /// <summary>
        /// Gets the property information.
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>The list of properties for the type.</returns>
        public static IList<PropertyInfo> GetPropertyInfo(Type t)
        {
            return t.GetProperties()
                .Where(p => !p.IsDefined(typeof(XmlIgnoreAttribute), false))
                .ToList();
        }

        /// <summary>
        /// Gets the Mongo collection field path for the property.
        /// </summary>
        /// <param name="parentPath">The parent path.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>The Mongo collection field path for the property.</returns>
        public static string GetPropertyPath(string parentPath, string propertyName)
        {
            var prefix = string.IsNullOrEmpty(parentPath) ? string.Empty : string.Format("{0}.", parentPath);
            return string.Format("{0}{1}", prefix, CaptalizeString(propertyName));
        }

        /// <summary>
        /// Gets the property information for an element.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <param name="name">The name of the property.</param>
        /// <returns>The property info for the element.</returns>
        public static PropertyInfo GetPropertyInfoForAnElement(IEnumerable<PropertyInfo> properties, string name)
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
        /// Parses the string value and convert to enum.
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
        /// Validates the uom/value pair for the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="uomProperty">The uom property.</param>
        /// <param name="measureValue">The measure value.</param>
        /// <returns>The uom value if valid.</returns>
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
        /// Gets the concrete type of the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="propType">Type of the property.</param>
        /// <returns>The concrete type</returns>
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

        public static XName Xmlns(string attributeName)
        {
            return XNamespace.Xmlns.GetName(attributeName);
        }

        public static XName Xsi(string attributeName)
        {
            return xsi.GetName(attributeName);
        }

        /// <summary>
        /// Gets the entity filter using the specified id field.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="idPropertyName">Name of the identifier property.</param>
        /// <returns>The entity filter with the specified id field</returns>
        public static FilterDefinition<T> GetEntityFilter<T>(EtpUri uri, string idPropertyName = "Uid")
        {
            var builder = Builders<T>.Filter;
            var filters = new List<FilterDefinition<T>>();

            var objectIds = uri.GetObjectIds()
                .ToDictionary(x => x.Key, x => x.Value);

            filters.Add(builder.EqIgnoreCase(idPropertyName, uri.ObjectId));

            if (!ObjectTypes.Well.EqualsIgnoreCase(uri.ObjectType) && objectIds.ContainsKey(ObjectTypes.Well))
            {
                filters.Add(builder.EqIgnoreCase("UidWell", objectIds[ObjectTypes.Well]));
            }
            if (!ObjectTypes.Wellbore.EqualsIgnoreCase(uri.ObjectType) && objectIds.ContainsKey(ObjectTypes.Wellbore))
            {
                filters.Add(builder.EqIgnoreCase("UidWellbore", objectIds[ObjectTypes.Wellbore]));
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

        public static FilterDefinition<T> BuildFilter<T>(string field, object value)
        {
            var type = value.GetType();
            if (type == typeof(string))
                return Builders<T>.Filter.EqIgnoreCase(field, value.ToString());
            else
                return Builders<T>.Filter.Eq(field, value);
        }

        public static UpdateDefinition<T> BuildUpdate<T>(UpdateDefinition<T> updates, string field, object value)
        {
            if (updates == null)
                return Builders<T>.Update.Set(field, value);
            else
                return updates.Set(field, value);
        }
    }
}
