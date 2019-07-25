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
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Energistics.DataAccess;
using log4net;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Provides static helper methods that can be used to parse WITSML XML strings.
    /// </summary>
    public class WitsmlParser : DataObjectNavigator<WitsmlParserContext>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlParser));
        private static readonly MethodInfo _upgradeMethod;
        private static readonly MethodInfo _parseMethod;

        /// <summary>
        /// Initializes the <see cref="WitsmlParser"/> class.
        /// </summary>
        static WitsmlParser()
        {
            _upgradeMethod = typeof(EnergisticsConverter).GetMethod("UpgradeVersion", BindingFlags.Public | BindingFlags.Static);

            _parseMethod = typeof(WitsmlParser).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(x => x.Name == "Parse" && x.GetGenericArguments().Any());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlParser"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        private WitsmlParser(WitsmlParserContext context) : base(null, context)
        {
        }

        /// <summary>
        /// Transforms the supplied data object to the specified version.
        /// </summary>
        /// <param name="collection">The data object collection.</param>
        /// <param name="dataVersion">The data schema version.</param>
        /// <returns>The transformed data object, if successful; otherwise, the original data object.</returns>
        public static IEnergisticsCollection Transform(IEnergisticsCollection collection, string dataVersion)
        {
            var objectType = ObjectTypes.GetObjectType(collection);
            var family = ObjectTypes.GetFamily(collection);
            var listType = ObjectTypes.GetObjectGroupType(objectType, family, dataVersion);
            var converter = _upgradeMethod.MakeGenericMethod(collection.GetType(), listType);

            try
            {
                collection = (IEnergisticsCollection)converter.Invoke(null, new object[] { collection });
                collection.SetVersion(dataVersion);
            }
            catch (Exception ex)
            {
                _log.Warn($"Unable to convert to data schema version: {dataVersion}", ex);
            }

            return collection;
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
        /// <param name="removeNaN">if set to <c>true</c> remove NaN elements.</param>
        /// <returns>The data object instance.</returns>
        /// <exception cref="WitsmlException"></exception>
        public static object Parse(Type type, XElement element, bool removeNaN = true)
        {
            try
            {
                return _parseMethod?.MakeGenericMethod(type)
                    .Invoke(null, new object[] { element, removeNaN });
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
        /// <param name="nilOnly">if set to <c>true</c> only elements with nil="true" are removed.</param>
        /// <param name="removeTypePrefix">if set to <c>true</c> any type prefix will be removed.</param>
        /// <returns>The serialized XML string.</returns>
        public static string ToXml(object obj, bool nilOnly = false, bool removeTypePrefix = false)
        {
            _log.Debug("Serializing object to XML.");

            if (obj == null) return string.Empty;

            var xml = EnergisticsConverter.ObjectToXml(obj);
            var xmlDoc = Parse(xml);
            var root = xmlDoc.Root;

            if (root == null) return string.Empty;

            var elementName = ObjectTypes.GetElementNameOverride(obj.GetType());
            root = root.UpdateRootElementName(obj.GetType(), removeTypePrefix, elementNameOverride: elementName);

            if (ObjectTypes.GetVersion(root).StartsWith("1."))
            {
                foreach (var element in root.Elements())
                {
                    RemoveEmptyElements(element, nilOnly);
                }
            }
            else
            {
                RemoveEmptyElements(root, nilOnly);
            }

            return root.ToString(SaveOptions.OmitDuplicateNamespaces);
        }

        /// <summary>
        /// Removes the empty attributes from from the specified element and optionally the descendant nodes.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="includeDescendants">if set to <c>true</c> only elements with nil="true" are removed.</param>
        public static void RemoveEmptyAttributes(XElement element, bool includeDescendants = false)
        {
            _log.Debug("Removing empty attributes.");

            Action<XElement> predicate = e => e.Attributes().Where(a => string.IsNullOrEmpty(a.Value)).Remove();

            predicate(element);

            if (includeDescendants)
            {
                element.Descendants().ForEach(predicate);
            }
        }

        /// <summary>
        /// Removes the empty descendant nodes from the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="nilOnly">if set to <c>true</c> only elements with nil="true" are removed.</param>
        public static void RemoveEmptyElements(XElement element, bool nilOnly = false)
        {
            _log.Debug("Removing empty elements.");

            Func<XElement, bool> predicate = e => e.Attributes(Xsi("nil")).Any() || 
                (string.IsNullOrEmpty(e.Value) && !e.HasAttributes && !e.HasElements && !nilOnly);

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
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected override void HandleNaNValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            if (Context.RemoveNaNElements)
                Remove(xmlObject);
        }
    }
}
