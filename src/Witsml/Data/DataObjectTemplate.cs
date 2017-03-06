//----------------------------------------------------------------------- 
// PDS.Witsml, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using Energistics.DataAccess;
using Energistics.DataAccess.Validation;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using PDS.Framework;

namespace PDS.Witsml.Data
{
    /// <summary>
    /// Provides a method of generating blank XML data object templates.
    /// </summary>
    public class DataObjectTemplate
    {
        private static readonly ConcurrentDictionary<Type, XDocument> _cache;
        private static readonly IList<Type> _excluded;
        private static readonly Regex _regex;
        private readonly List<string> _ignored;
        private XmlNamespaceManager _manager;
        private const string Prefix = "witsml";

        static DataObjectTemplate()
        {
            _regex = new Regex(@"(?<selector>[//]+)?(?<namespace>[A-z0-9_]+:)?(?<element>[\@\*A-z0-9_\[\]]+)");
            _cache = new ConcurrentDictionary<Type, XDocument>();
            _excluded = new List<Type>();

            //Exclude<Witsml131.ComponentSchemas.CustomData>();
            Exclude<Witsml131.ComponentSchemas.DocumentInfo>();

            //Exclude<Witsml141.ComponentSchemas.CustomData>();
            Exclude<Witsml141.ComponentSchemas.DocumentInfo>();
            //Exclude<Witsml141.ComponentSchemas.ExtensionAny>();
            //Exclude<Witsml141.ComponentSchemas.ExtensionNameValue>();
        }

        private static void Exclude<TExclude>()
        {
            _excluded.Add(typeof(TExclude));
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
        /// Creates a blank XML template for the specified type.
        /// </summary>
        /// <param name="type">The data object type.</param>
        /// <returns>An <see cref="XDocument"/> template.</returns>
        public XDocument Create(Type type)
        {
            var cached = _cache.GetOrAdd(type, CreateTemplate);
            return new XDocument(cached);
        }

        /// <summary>
        /// Creates a clone of the node in the document using the specified XPath expression.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="xpath">The xpath.</param>
        /// <returns>A new <see cref="XDocument"/> instance.</returns>
        public XDocument Clone(XDocument document, string xpath)
        {
            var manager = GetNamespaceManager(document.Root);
            xpath = IncludeNamespacePrefix(xpath);

            var node = document.XPathSelectElement(xpath, manager);
            return node == null ? null : new XDocument(node);
        }

        /// <summary>
        /// Sets the value of a node in the document using the specified XPath expression.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="document">The document.</param>
        /// <param name="xpath">The xpath.</param>
        /// <param name="value">The value.</param>
        /// <returns>This <see cref="DataObjectTemplate"/> instance.</returns>
        public DataObjectTemplate Set<TValue>(XDocument document, string xpath, TValue value)
        {
            var manager = GetNamespaceManager(document.Root);
            xpath = IncludeNamespacePrefix(xpath);

            var node = document.Evaluate(xpath, manager).FirstOrDefault();
            var attribute = node as XAttribute;

            if (attribute != null)
            {
                attribute.Value = $"{value}";
            }
            else
            {
                var element = node as XElement;

                if (element == null)
                    return this;

                if (value is XElement)
                {
                    element.ReplaceWith(value);
                }
                else
                {
                    element.RemoveNodes();
                    element.Add(value);
                }
            }

            return this;
        }

        /// <summary>
        /// Adds a recurring element to the document using the specified XPath expression.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="xpath">The xpath.</param>
        /// <param name="element">The element.</param>
        /// <returns>This <see cref="DataObjectTemplate"/> instance.</returns>
        public DataObjectTemplate Push(XDocument document, string xpath, XElement element)
        {
            var manager = GetNamespaceManager(document.Root);
            xpath = IncludeNamespacePrefix(xpath);

            var last = document.Evaluate(xpath, manager)
                .LastOrDefault() as XElement;

            last?.AddAfterSelf(element);

            return this;
        }

        /// <summary>
        /// Removes nodes from the document using the specified XPath expressions.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="xpaths">The XPath expressions.</param>
        /// <returns>This <see cref="DataObjectTemplate"/> instance.</returns>
        public DataObjectTemplate Remove(XDocument document, params string[] xpaths)
        {
            var manager = GetNamespaceManager(document.Root);

            xpaths
                .Select(IncludeNamespacePrefix)
                .SelectMany(x => document.Evaluate(x, manager).ToArray())
                .ForEach(RemoveElementOrAttribute);

            return this;
        }

        private XmlNamespaceManager GetNamespaceManager(XNode node)
        {
            if (_manager != null) return _manager;
            var navigator = node.CreateNavigator();

            _manager = new XmlNamespaceManager(navigator.NameTable ?? new NameTable());
            _manager.AddNamespace(Prefix, navigator.NamespaceURI);

            return _manager;
        }

        private XDocument CreateTemplate(Type type)
        {
            var xmlRoot = type.GetCustomAttribute<XmlRootAttribute>();
            var xmlType = type.GetCustomAttribute<XmlTypeAttribute>();

            var objectType = ObjectTypes.GetObjectType(type);
            var version = ObjectTypes.GetVersion(type);
            var attribute = "version";

            if (OptionsIn.DataVersion.Version200.Equals(version))
            {
                objectType = objectType.ToPascalCase();
                attribute = "schemaVersion";
            }
            else if (typeof(IEnergisticsCollection).IsAssignableFrom(type))
            {
                objectType = ObjectTypes.SingleToPlural(objectType);
            }

            XNamespace ns = xmlType?.Namespace ?? xmlRoot.Namespace;
            var document = new XDocument(new XElement(ns + objectType));

            CreateTemplate(type, ns, document.Root);

            // Set version attribute for top level data objects
            if (document.Root != null && document.Root.Attributes(attribute).Any())
                document.Root.SetAttributeValue(attribute, version);

            return document;
        }

        private void CreateTemplate(Type objectType, XNamespace ns, XElement parent)
        {
            if (objectType == null || _excluded.Contains(objectType))
            {
                return;
            }

            foreach (var property in objectType.GetProperties())
            {
                var xmlAttribute = property.GetCustomAttribute<XmlAttributeAttribute>();
                var xmlElement = property.GetCustomAttribute<XmlElementAttribute>();
                var xmlArray = property.GetCustomAttribute<XmlArrayAttribute>();

                if ((xmlAttribute == null && xmlElement == null && xmlArray == null) ||
                    _excluded.Contains(property.PropertyType) ||
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

                // Arrays
                if (xmlArray != null)
                {
                    var xmlArrayItem = property.GetCustomAttribute<XmlArrayItemAttribute>();
                    var array = new XElement(ns + xmlArray.ElementName,
                                new XElement(ns + xmlArrayItem.ElementName));

                    parent.Add(array);
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

        private string IncludeNamespacePrefix(string expression)
        {
            var xpath = new StringBuilder();
            var matches = _regex.Matches(expression);

            foreach (Match match in matches)
            {
                var ns = match.Groups["namespace"].Value;
                var element = match.Groups["element"].Value;

                if (string.IsNullOrWhiteSpace(ns) && !element.StartsWith("@"))
                {
                    xpath.AppendFormat("{0}{1}:{2}", match.Groups["selector"].Value, Prefix, element);
                }
                else
                {
                    xpath.Append(match.Value);
                }
            }

            return xpath.ToString();
        }

        private static void RemoveElementOrAttribute(object elementOrAttribute)
        {
            (elementOrAttribute as XElement)?.Remove();
            (elementOrAttribute as XAttribute)?.Remove();
        }
    }
}
