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
using Energistics.DataAccess.Validation;
using PDS.Framework;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;

namespace PDS.Witsml.Data
{
    /// <summary>
    /// Provides a method of generating blank XML data object templates.
    /// </summary>
    public class DataObjectTemplate
    {
        private static readonly IList<Type> _excludeTypes = new List<Type>();
        private readonly List<string> _ignored;

        static DataObjectTemplate()
        {
            //Exclude<Witsml131.ComponentSchemas.CustomData>();
            Exclude<Witsml131.ComponentSchemas.DocumentInfo>();

            //Exclude<Witsml141.ComponentSchemas.CustomData>();
            Exclude<Witsml141.ComponentSchemas.DocumentInfo>();
            //Exclude<Witsml141.ComponentSchemas.ExtensionAny>();
            //Exclude<Witsml141.ComponentSchemas.ExtensionNameValue>();
        }

        private static void Exclude<TExclude>()
        {
            _excludeTypes.Add(typeof(TExclude));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataObjectTemplate" /> class.
        /// </summary>
        /// <param name="ignored">The list of ignored elements or properties.</param>
        public DataObjectTemplate(IEnumerable<string> ignored = null)
        {
            _ignored = ignored?.ToList() ?? new List<string>();
        }

        /// <summary>
        /// Creates a blank XML template for the specified type.
        /// </summary>
        /// <typeparam name="T">The data object type.</typeparam>
        /// <returns>An <see cref="XDocument"/> template.</returns>
        public XDocument Create<T>()
        {
            return Create(typeof(T));
        }

        /// <summary>
        /// Creates the specified type.
        /// </summary>
        /// <param name="type">The data object type.</param>
        /// <returns>An <see cref="XDocument"/> template.</returns>
        public XDocument Create(Type type)
        {
            var xmlRoot = type.GetCustomAttribute<XmlRootAttribute>();
            var xmlType = type.GetCustomAttribute<XmlTypeAttribute>();
            var ns = XNamespace.Get(xmlType?.Namespace ?? xmlRoot.Namespace);

            var objectType = ObjectTypes.GetObjectType(type);
            var version = ObjectTypes.GetVersion(type);
            var attribute = "version";

            if (OptionsIn.DataVersion.Version200.Equals(version))
            {
                objectType = objectType.ToPascalCase();
                attribute = "schemaVersion";
            }
            else
            {
                objectType = ObjectTypes.SingleToPlural(objectType);
            }

            var document = new XDocument(new XElement(ns + objectType));

            CreateTemplate(type, ns, document.Root);

            document.Root?.SetAttributeValue(attribute, version);

            return document;
        }

        private void CreateTemplate(Type objectType, XNamespace ns, XElement parent)
        {
            if (objectType == null || _excludeTypes.Contains(objectType))
            {
                return;
            }

            foreach (var property in objectType.GetProperties())
            {
                var xmlAttribute = property.GetCustomAttribute<XmlAttributeAttribute>();
                var xmlElement = property.GetCustomAttribute<XmlElementAttribute>();

                if ((xmlAttribute == null && xmlElement == null) ||
                    _excludeTypes.Contains(property.PropertyType) ||
                    _ignored.Contains(xmlAttribute?.AttributeName) ||
                    _ignored.Contains(xmlElement?.ElementName) ||
                    IsIgnored(property))
                    continue;

                // Attributes
                if (xmlAttribute != null)
                {
                    var attribute = new XAttribute(xmlAttribute.AttributeName, string.Empty);
                    parent.Add(attribute);
                    continue;
                }

                // Elements
                var element = new XElement(ns + xmlElement.ElementName);
                parent.Add(element);

                var xmlComponent = property.GetCustomAttribute<ComponentElementAttribute>();
                var xmlRecurring = property.GetCustomAttribute<RecurringElementAttribute>();

                // Stop processing if not a complex type or recurring element
                if (xmlComponent == null && xmlRecurring == null)
                    continue;

                var propertyType = property.PropertyType;

                if (propertyType.IsGenericType)
                {
                    var genericDefinition = propertyType.GetGenericTypeDefinition();

                    if (genericDefinition == typeof(Nullable<>))
                    {
                        propertyType = Nullable.GetUnderlyingType(propertyType);
                    }
                    else if (genericDefinition == typeof(List<>))
                    {
                        propertyType = propertyType.GetGenericArguments()[0];
                    }
                }
                else if (objectType.IsAbstract)
                {
                    propertyType = objectType.Assembly.GetTypes()
                        .FirstOrDefault(x => !x.IsAbstract && objectType.IsAssignableFrom(x));
                }

                CreateTemplate(propertyType, ns, element);
            }
        }

        private bool IsIgnored(MemberInfo property)
        {
            return property.GetCustomAttributes<XmlIgnoreAttribute>().Any()
                || _ignored.Contains(property.Name);
        }
    }
}
