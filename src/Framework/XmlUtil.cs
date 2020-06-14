//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2018.3
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace PDS.WITSMLstudio.Framework
{
    /// <summary>
    /// Provides custom extension methods for .NET XML types.
    /// </summary>
    public static class XmlUtil
    {
        /// <summary>The xsi namespace.</summary>
        public static readonly XNamespace Xsi;
        private static readonly Regex _regex;
        private const string DefaultPrefix = "pds";

        /// <summary>
        /// Initializes the <see cref="XmlUtil"/> class.
        /// </summary>
        static XmlUtil()
        {
            Xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
            _regex = new Regex(@"(?<selector>[//]+)?(?<namespace>[A-z0-9_]+:)?(?<element>[\@\*A-z0-9_\[\]]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        /// <summary>
        /// Gets the namespace manager for the specified XML node.
        /// </summary>
        /// <param name="node">The XML node.</param>
        /// <param name="prefix">The prefix.</param>
        /// <returns>An <see cref="XmlNamespaceManager" /> instance.</returns>
        public static XmlNamespaceManager GetNamespaceManager(this XNode node, string prefix = DefaultPrefix)
        {
            var navigator = node.CreateNavigator();

            var manager = new XmlNamespaceManager(navigator.NameTable ?? new NameTable());
            manager.AddNamespace(prefix, navigator.NamespaceURI);

            return manager;
        }

        /// <summary>
        /// Includes the namespace prefix.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="prefix">The prefix.</param>
        /// <returns>The updated expression.</returns>
        public static string IncludeNamespacePrefix(string expression, string prefix = DefaultPrefix)
        {
            var xpath = new StringBuilder();
            var matches = _regex.Matches(expression);

            foreach (Match match in matches)
            {
                var ns = match.Groups["namespace"].Value;
                var element = match.Groups["element"].Value;

                if (string.IsNullOrWhiteSpace(ns) && !element.StartsWith("@"))
                {
                    xpath.AppendFormat("{0}{1}:{2}", match.Groups["selector"].Value, DefaultPrefix, element);
                }
                else
                {
                    xpath.Append(match.Value);
                }
            }

            return xpath.ToString();
        }

        /// <summary>
        /// Get the node in the document using the specified XPath expression.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="xpath">The xpath.</param>
        /// <returns>A <see cref="XElement"/> instance.</returns>
        public static XElement GetElement(this XDocument document, string xpath)
        {
            var manager = GetNamespaceManager(document.Root);
            xpath = IncludeNamespacePrefix(xpath);

            return document.XPathSelectElement(xpath, manager);
        }

        /// <summary>
        /// Creates a clone of the node in the document using the specified XPath expression.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="xpath">The xpath.</param>
        /// <returns>A new <see cref="XDocument"/> instance.</returns>
        public static XDocument Clone(this XDocument document, string xpath)
        {
            var manager = GetNamespaceManager(document.Root);
            xpath = IncludeNamespacePrefix(xpath);

            var node = document.XPathSelectElement(xpath, manager);
            return node == null ? null : new XDocument(node);
        }

        /// <summary>
        /// Updates the name of the root element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="type">The type.</param>
        /// <param name="removeTypePrefix">if set to <c>true</c> any type prefix will be removed.</param>
        /// <param name="elementNameOverride"></param>
        /// <returns>A new <see cref="XElement"/> instance.</returns>
        public static XElement UpdateRootElementName(this XElement element, Type type, bool removeTypePrefix = false, string elementNameOverride = null)
        {
            var xmlRoot = XmlAttributeCache<XmlRootAttribute>.GetCustomAttribute(type);
            var xmlType = XmlAttributeCache<XmlTypeAttribute>.GetCustomAttribute(type);
            var elementName = type.Name;

            if (!string.IsNullOrWhiteSpace(xmlRoot?.ElementName))
                elementName = xmlRoot.ElementName;

            else if (!string.IsNullOrWhiteSpace(xmlType?.TypeName))
                elementName = xmlType.TypeName;

            if (removeTypePrefix)
            {
                if (elementName.StartsWith("obj_"))
                    elementName = elementName.Substring(4);
                else if (elementName.StartsWith("cs_"))
                    elementName = elementName.Substring(3);
            }

            if (!string.IsNullOrWhiteSpace(elementNameOverride))
            {
                elementName = elementNameOverride;
            }

            if (element.Name.LocalName.Equals(elementName))
                return element;

            var xElementName = !string.IsNullOrWhiteSpace(xmlRoot?.Namespace)
                ? XNamespace.Get(xmlRoot.Namespace).GetName(elementName)
                : !string.IsNullOrWhiteSpace(xmlType?.Namespace)
                    ? XNamespace.Get(xmlType.Namespace).GetName(elementName)
                    : elementName;

            // Update element name to match XSD type name
            var clone = new XElement(element)
            {
                Name = xElementName
            };

            // Remove xsi:type attribute used for abstract types
            clone.Attribute(Xsi.GetName("type"))?.Remove();

            return clone;
        }

        /// <summary>
        /// Converts an <see cref="XElement"/> to an <see cref="XmlElement"/>.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>An <see cref="XmlElement"/> instance.</returns>
        public static XmlElement ToXmlElement(this XElement element)
        {
            using (var reader = element.CreateReader())
            {
                var doc = new XmlDocument();
                doc.Load(reader);
                return doc.DocumentElement;
            }
        }

        /// <summary>
        /// Evaluates the specified XPath expression.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="resolver">The resolver.</param>
        /// <returns>A collection of elements or attributes.</returns>
        public static IEnumerable<object> Evaluate(this XDocument document, string expression, IXmlNamespaceResolver resolver)
        {
            return ((IEnumerable)document.XPathEvaluate(expression, resolver)).Cast<object>();
        }
    }
}
