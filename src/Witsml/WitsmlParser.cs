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
using System.Xml;
using System.Xml.Linq;
using Energistics.DataAccess;

namespace PDS.Witsml
{
    /// <summary>
    /// Provides static helper methods that can be used to parse WITSML XML strings.
    /// </summary>
    public static class WitsmlParser
    {
        private static readonly XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

        /// <summary>
        /// Parses the specified XML document using LINQ to XML.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <returns>An <see cref="XDocument"/> instance.</returns>
        /// <exception cref="WitsmlException"></exception>
        public static XDocument Parse(string xml)
        {
            try
            {
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
        /// <param name="xml">The XML string.</param>
        /// <returns>The data object instance.</returns>
        /// <exception cref="WitsmlException"></exception>
        public static T Parse<T>(string xml)
        {
            try
            {
                return EnergisticsConverter.XmlToObject<T>(xml);
            }
            catch (Exception ex)
            {
                throw new WitsmlException(ErrorCodes.InputTemplateNonConforming, ex);
            }
        }

        /// <summary>
        /// Serialize WITSML query results to XML and remove empty elements and xsi:nil attributes.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The serialized XML string.</returns>
        public static string ToXml(object obj)
        {
            var xml = EnergisticsConverter.ObjectToXml(obj);
            var xmlDoc = Parse(xml);
            var root = xmlDoc.Root;
            foreach (var element in root.Elements().ToList())
            {
                ProcessElement(element);
            }

            return root.ToString();
        }

        public static void ProcessElement(XElement element)
        {
            element.Descendants().Attributes().Where(a => a.Name == xsi.GetName("nil")).Remove();
            while (element.Descendants().Any(e => string.IsNullOrEmpty(e.Value) && !e.HasAttributes && !e.HasElements))
            {
                element.Descendants().Where(e => string.IsNullOrEmpty(e.Value) && !e.HasAttributes && !e.HasElements).Remove();
            }
        }
    }
}
