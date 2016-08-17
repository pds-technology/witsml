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
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Energistics.DataAccess;
using log4net;
using PDS.Framework;
using PDS.Witsml.Data;

namespace PDS.Witsml
{
    /// <summary>
    /// Provides static helper methods that can be used to parse WITSML XML strings.
    /// </summary>
    public class WitsmlParser : DataObjectNavigator<WitsmlParserContext>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlParser));

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlParser"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        private WitsmlParser(WitsmlParserContext context) : base(context)
        {
        }

        /// <summary>
        /// Parses the specified XML document using LINQ to XML.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <param name="debug">if set to <c>true</c> includes debug log output.</param>
        /// <returns>An <see cref="XDocument" /> instance.</returns>
        /// <exception cref="WitsmlException"></exception>
        public static XDocument Parse(string xml, bool debug = true)
        {
            if (debug)
            {
                _log.Debug("Parsing XML string.");
            }

            try
            {
                // remove invalid character along with leading/trailing white space
                xml = xml?.Trim().Replace("\x00", string.Empty) ?? string.Empty;

                return XDocument.Parse(xml);
            }
            catch (XmlException ex)
            {
                throw new WitsmlException(ErrorCodes.InputTemplateNonConforming, ex);
            }
        }

        /// <summary>
        /// Parses the specified XML document using the Standards DevKit.
        /// </summary>
        /// <typeparam name="T">The data object type.</typeparam>
        /// <param name="element">The XML element.</param>
        /// <param name="removeNaN">if set to <c>true</c> remove NaN elements.</param>
        /// <returns>The data object instance.</returns>
        /// <exception cref="WitsmlException"></exception>
        public static T Parse<T>(XElement element, bool removeNaN = true)
        {
            _log.DebugFormat("Deserializing XML element for type: {0}", typeof(T).FullName);

            try
            {
                // Create a copy of the element to prevent loss of NaN elements
                var xml = removeNaN
                    ? RemoveNaNElements<T>(new XElement(element))
                    : element.ToString();

                return EnergisticsConverter.XmlToObject<T>(xml);
            }
            catch (WitsmlException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new WitsmlException(ErrorCodes.InputTemplateNonConforming, ex);
            }
        }

        /// <summary>
        /// Parses the specified XML document.
        /// </summary>
        /// <param name="type">The data object type.</param>
        /// <param name="element">The XML element.</param>
        /// <returns>The data object instance.</returns>
        /// <exception cref="WitsmlException"></exception>
        public static object Parse(Type type, XElement element)
        {
            var method = typeof(WitsmlParser).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(x => x.Name == "Parse" && x.GetGenericArguments().Any());

            try
            {
                return method?.MakeGenericMethod(type)
                    .Invoke(null, new object[] { element });
            }
            catch (Exception ex)
            {
                var witsmlException = ex.GetBaseException<WitsmlException>();
                if (witsmlException == null) throw;
                throw witsmlException;
            }
        }

        /// <summary>
        /// Serialize WITSML query results to XML and remove empty elements and xsi:nil attributes.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The serialized XML string.</returns>
        public static string ToXml(object obj)
        {
            _log.Debug("Serializing object to XML.");

            if (obj == null) return string.Empty;

            var xml = EnergisticsConverter.ObjectToXml(obj);
            var xmlDoc = Parse(xml);
            var root = xmlDoc.Root;

            if (root == null) return string.Empty;

            foreach (var element in root.Elements())
            {
                RemoveEmptyElements(element);
            }

            return root.ToString();
        }

        /// <summary>
        /// Removes the empty descendant nodes from the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        public static void RemoveEmptyElements(XElement element)
        {
            _log.Debug("Removing empty elements.");

            Func<XElement, bool> predicate = e => e.Attributes(Xsi("nil")).Any() || 
                (string.IsNullOrEmpty(e.Value) && !e.HasAttributes && !e.HasElements);

            while (element.Descendants().Any(predicate))
            {
                element.Descendants().Where(predicate).Remove();
            }
        }

        /// <summary>
        /// Removes elements that are numeric type and have NaN value.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns>The xml with NaN removed.</returns>
        public static string RemoveNaNElements<T>(XElement element)
        {
            _log.Debug("Removing NaN elements.");

            var context = new WitsmlParserContext<T>(element);
            var parser = new WitsmlParser(context);

            context.IgnoreUnknownElements = true;
            context.RemoveNaNElements = true;
            parser.Navigate(context.Element);

            return context.Element.ToString();
        }

        /// <summary>
        /// Handles the NaN value during parse navigation by removing NaN values.
        /// </summary>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected override void HandleNaNValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            if (Context.RemoveNaNElements)
                Remove(xmlObject);
        }
    }
}
