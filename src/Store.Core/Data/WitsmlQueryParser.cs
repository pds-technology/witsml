//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
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
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess;
using Energistics.Etp.Common.Datatypes;
using Witsml200 = Energistics.DataAccess.WITSML200;
using Prodml200 = Energistics.DataAccess.PRODML200;
using Resqml210 = Energistics.DataAccess.RESQML210;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Provides helper methods that can be used to parse WITSML Query.
    /// </summary>
    public class WitsmlQueryParser
    {
        private readonly string _options;
        private readonly XNamespace _namespace;
        private readonly XElement _element;
        private readonly IEnumerable<XElement> _elements;

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlQueryParser" /> class.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="objectType">The object type.</param>
        /// <param name="options">The options.</param>
        public WitsmlQueryParser(XElement element, string objectType, string options)
        {
            Root = element;
            ObjectType = objectType;
            Options = OptionsIn.Parse(options);

            _options = options;
            _namespace = element.GetDefaultNamespace();

            _element = element;
            _elements = element.Attributes("version").Any()
                ? element.Elements(_namespace + objectType)
                : new[] { element };

            QueryCount = _elements.Count();
        }

        /// <summary>
        /// Gets the object type.
        /// </summary>
        /// <value>
        /// The object type.
        /// </value>
        public string ObjectType { get; private set; }

        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public Dictionary<string, string> Options { get; private set; }

        /// <summary>
        /// Gets the root element.
        /// </summary>
        /// <value>
        /// The root element.
        /// </value>
        public XElement Root { get; }

        /// <summary>
        /// Gets the query count.
        /// </summary>
        /// <value>
        /// The query count.
        /// </value>
        public int QueryCount { get; }

        /// <summary>
        /// Get the ReturnElements.
        /// </summary>
        /// <returns>The ReturnElements.</returns>
        public string ReturnElements()
        {
            return OptionsIn.GetValue(Options, OptionsIn.ReturnElements.Requested);
        }

        /// <summary>
        /// Get the interval range inclusion.
        /// </summary>
        /// <returns>The IntervalRangeInclusion.</returns>
        public string IntervalRangeInclusion()
        {
            return OptionsIn.GetValue(Options, OptionsIn.IntervalRangeInclusion.MinimumPoint);
        }

        /// <summary>
        /// Requests the object selection capability.
        /// </summary>
        /// <returns>The capability value.</returns>
        public string RequestObjectSelectionCapability()
        {
            return OptionsIn.GetValue(Options, OptionsIn.RequestObjectSelectionCapability.None);
        }

        /// <summary>
        /// Requests the private group only.
        /// </summary>
        /// <returns></returns>
        public bool RequestPrivateGroupOnly()
        {
            string value = OptionsIn.GetValue(Options, OptionsIn.RequestPrivateGroupOnly.False);
            bool result;

            if (!bool.TryParse(value, out result))
                result = false;

            return result;
        }

        /// <summary>
        /// Requests the CascadedDelete OptionIn.
        /// </summary>
        /// <returns>The CascadedDelete value</returns>
        public bool CascadedDelete()
        {
            string value = OptionsIn.GetValue(Options, OptionsIn.CascadedDelete.False);
            bool result;

            if (!bool.TryParse(value, out result))
                result = false;

            return result;
        }

        /// <summary>
        /// The maximum number of nodes that can be returned to the client.
        /// </summary>
        /// <returns>The number of maximum nodes to returned if it exists in the Options In, null otherwise.</returns>
        public int? MaxReturnNodes()
        {
            var dataRowCount = GetDataRowCount();
            if (dataRowCount != null)
                return dataRowCount;

            if (!Options.ContainsKey(OptionsIn.MaxReturnNodes.Keyword))
                return null;

            int nodeCount;
            return int.TryParse(Options[OptionsIn.MaxReturnNodes.Keyword], out nodeCount)
                ? nodeCount
                : (int?)null;
        }

        /// <summary>
        /// Requests the latest values.
        /// </summary>
        /// <returns>The number of latest values requested in it exists in the Options In, null otherwise.</returns>
        public int? RequestLatestValues()
        {
            if (!Options.ContainsKey(OptionsIn.RequestLatestValues.Keyword))
                return null;

            int requestLatestValues;
            return int.TryParse(Options[OptionsIn.RequestLatestValues.Keyword], out requestLatestValues)
                ? requestLatestValues
                : (int?)null;
        }

        /// <summary>
        /// Gets the URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>The URI.</returns>
        public EtpUri GetUri<T>()
        {
            return GetUri(typeof(T));
        }

        /// <summary>
        /// Gets the URI.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The URI.</returns>
        public EtpUri GetUri(Type type)
        {
            var objectType = ObjectTypes.GetObjectType(type);
            var baseUri = EtpUris.GetUriFamily(type);

            if (typeof(Witsml200.AbstractObject).IsAssignableFrom(type) ||
                typeof(Prodml200.AbstractObject).IsAssignableFrom(type) ||
                typeof(Resqml210.AbstractObject).IsAssignableFrom(type))
                return baseUri.Append(objectType, Attribute("uuid"));

            if (typeof(IWellObject).IsAssignableFrom(type))
                baseUri = baseUri.Append(ObjectTypes.Well, Attribute("uidWell"), true);

            if (typeof(IWellboreObject).IsAssignableFrom(type))
                baseUri = baseUri.Append(ObjectTypes.Wellbore, Attribute("uidWellbore"), true);

            return baseUri.Append(objectType, Attribute("uid"), true);
        }

        /// <summary>
        /// Get the elements of the root.
        /// </summary>
        /// <returns>The elements of the root.</returns>
        public IEnumerable<XElement> Elements()
        {
            return _elements;
        }

        /// <summary>
        /// Returns the element.
        /// </summary>
        /// <returns>The element.</returns>
        public XElement Element()
        {
            return Elements().FirstOrDefault();
        }

        /// <summary>
        /// Get the attributes of the element.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>The attributes of the element.</returns>
        public String Attribute(string name)
        {
            if (HasAttribute(name))
            {
                return (String)Element().Attribute(name);
            }
            return null;
        }

        /// <summary>
        /// Determines whether the specified name has attribute.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <returns>True if has the attribute.</returns>
        public bool HasAttribute(string name)
        {
            var element = Element();
            return element != null && element.Attribute(name) != null;
        }

        /// <summary>
        /// Determines whether the query contains a documentInfo element.
        /// </summary>
        /// <returns>True if the query contains a documentInfo element.</returns>
        public bool HasDocumentInfo()
        {
            return _element?.Elements(_namespace + ObjectTypes.DocumentInfo).Any() ?? false;
        }

        /// <summary>
        /// Gets the documents information element.
        /// </summary>
        /// <returns></returns>
        public XElement DocumentInfo()
        {
            return _element?.Element(_namespace + ObjectTypes.DocumentInfo);
        }

        /// <summary>
        /// Determines whether the document contains the element.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>True if contains.</returns>
        public bool Contains(string name)
        {
            return Element().Elements(_namespace + name).Any();
        }

        /// <summary>
        /// Get the element.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The element</returns>
        public XElement Property(string name)
        {
            return Element().Elements(_namespace + name).FirstOrDefault();
        }

        /// <summary>
        /// Get the elements by name.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>The elements.</returns>
        public IEnumerable<XElement> Properties(string name)
        {
            return Properties(Element(), name);
        }

        /// <summary>
        /// Get the elements by name.
        /// </summary>
        /// <param name="element">The parent element.</param>
        /// <param name="name">The name.</param>
        /// <returns>The elements.</returns>
        public IEnumerable<XElement> Properties(XElement element, string name)
        {
            return element.Elements(_namespace + name);
        }

        /// <summary>
        /// Get the elements by name.
        /// </summary>
        /// <param name="elements">The list of parent elements.</param>
        /// <param name="name">The name of the element.</param>
        /// <returns>The elements</returns>
        public IEnumerable<XElement> Properties(IEnumerable<XElement> elements, string name)
        {
            return elements.Elements(_namespace + name);
        }

        /// <summary>
        /// Determines whether the root element has any child elements.
        /// </summary>
        /// <returns><c>true</c> if the root element has any child elements; otherwise, <c>false</c>.</returns>
        public bool HasElements()
        {
            return HasElements(Element());
        }

        /// <summary>
        /// Determines whether the specified element has any child elements.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns><c>true</c> if the element has any child elements; otherwise, <c>false</c>.</returns>
        public bool HasElements(XElement element)
        {
            return element.Elements().Any();
        }

        /// <summary>
        /// Determines whether the specified name has elements.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>True if has elements.</returns>
        public bool HasElements(string name)
        {
            return HasElements(Element(), name);
        }

        /// <summary>
        /// Determines whether the specified element has elements.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="name">The name.</param>
        /// <returns>True if has the element.</returns>
        public bool HasElements(XElement element, string name)
        {
            return element != null &&
                element.Elements(_namespace + name).Any();
        }

        /// <summary>
        /// Get the element value.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The element value.</returns>
        public string PropertyValue(string name)
        {
            if (!HasElements(name))
            {
                return null;
            }
            return PropertyValue(Element(), name);
        }

        /// <summary>
        /// Get the element value.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="name">The name.</param>
        /// <returns>The value of the element.</returns>
        public string PropertyValue(XElement element, string name)
        {
            if (!HasElements(element, name))
            {
                return null;
            }
            return element
                .Elements(_namespace + name)
                .Select(e => e.Value)
                .FirstOrDefault();
        }

        /// <summary>
        /// Determines whether the element has a value.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the element has a value; otherwise, <c>false</c>.</returns>
        public bool HasPropertyValue(string name)
        {
            return HasPropertyValue(Element(), name);
        }

        /// <summary>
        /// Determines whether the element has a value.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the element has a value; otherwise, <c>false</c>.</returns>
        public bool HasPropertyValue(XElement element, string name)
        {
            return !string.IsNullOrWhiteSpace(PropertyValue(element, name));
        }

        /// <summary>
        /// Gets the attribute value.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="attribute">The attribute.</param>
        /// <returns>The attribute value.</returns>
        public string PropertyAttribute(string name, string attribute)
        {
            if (!HasElements(name))
            {
                return null;
            }
            return Element()
                .Elements(_namespace + name)
                .Select(e => (string)e.Attribute(attribute))
                .FirstOrDefault();
        }

        /// <summary>
        /// Determines whether the query contains structural range criteria.
        /// </summary>
        /// <returns><c>true</c> if the query contains structural range elements; otherwise, <c>false</c>.</returns>
        public bool IsStructuralRangeQuery()
        {
            return HasPropertyValue("startIndex") || HasPropertyValue("endIndex") ||
                   HasPropertyValue("startDateTimeIndex") || HasPropertyValue("endDateTimeIndex") ||
                   HasPropertyValue("mdMn") || HasPropertyValue("mdMx") ||
                   HasPropertyValue("startMd") || HasPropertyValue("endMd");
        }

        /// <summary>
        /// Clones the current instance with new options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The Witsml query parser.</returns>
        public WitsmlQueryParser Clone(string options)
        {
            return new WitsmlQueryParser(Root, ObjectType, options);
        }

        /// <summary>
        /// Creates a Witsml query parser for the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>The Witsml query parser.</returns>
        public WitsmlQueryParser Fork(XElement element, string objectType)
        {
            return new WitsmlQueryParser(element, objectType, _options);
        }

        /// <summary>
        /// Creates a list of Witsml query parser.
        /// </summary>
        /// <param name="elements">The elements.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>The list of Witsml query parser.</returns>
        public IEnumerable<WitsmlQueryParser> Fork(IEnumerable<XElement> elements, string objectType)
        {
            foreach (var element in elements)
                yield return Fork(element, objectType);
        }

        /// <summary>
        /// Create list of Witsml query parsers for the element
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>The list of Witsml query parsers.</returns>
        public IEnumerable<WitsmlQueryParser> ForkProperties(string name, string objectType)
        {
            return Fork(Properties(name), objectType);
        }

        /// <summary>
        /// Create a list of Witsml query parsers.
        /// </summary>
        /// <returns>The list of Witsml query parsers.</returns>
        public IEnumerable<WitsmlQueryParser> ForkElements()
        {
            return Fork(Elements(), ObjectType);
        }

        /// <summary>
        /// Removes the sub elements from the first element.
        /// </summary>
        public void RemoveSubElements()
        {
            Element()?.RemoveNodes();
        }

        private int? GetDataRowCount()
        {
            // Only valid for 131, otherwise we want to ignore.
            var dataRowCount = WitsmlOperationContext.Current.DataSchemaVersion.Equals(OptionsIn.DataVersion.Version131.Value)
                ? PropertyValue("dataRowCount") ?? PropertyValue("DataRowCount")
                : null;

            // return null if dataRowCount not provided
            if (string.IsNullOrWhiteSpace(dataRowCount))
                return null;

            // Return count if parse was successful, otherwise null
            int count;
            return int.TryParse(dataRowCount, out count)
                ? count
                : (int?)null;
        }
    }
}
