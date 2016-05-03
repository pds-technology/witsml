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
using PDS.Framework;

namespace PDS.Witsml
{
    /// <summary>
    /// Provides static helper methods that can be used to parse WITSML XML strings.
    /// </summary>
    public static class WitsmlParser
    {
        public static readonly XNamespace Xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

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
                xml = RemoveNaNElements<T>(xml);
                return EnergisticsConverter.XmlToObject<T>(xml);
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
        /// <param name="xml">The XML string.</param>
        /// <returns>The data object instance.</returns>
        /// <exception cref="WitsmlException"></exception>
        public static object Parse(Type type, string xml)
        {
            var method = typeof(WitsmlParser).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(x => x.Name == "Parse" && x.GetGenericArguments().Any());

            try
            {
                return method?.MakeGenericMethod(type)
                    .Invoke(null, new object[] { xml });
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
            if (obj == null) return string.Empty;
            var xml = EnergisticsConverter.ObjectToXml(obj);
            var xmlDoc = Parse(xml);
            var root = xmlDoc.Root;

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
            Func<XElement, bool> predicate = e => e.Attributes(Xsi.GetName("nil")).Any() || 
                (string.IsNullOrEmpty(e.Value) && !e.HasAttributes && !e.HasElements);

            while (element.Descendants().Any(predicate))
            {
                element.Descendants().Where(predicate).Remove();
            }
        }

        /// <summary>
        /// Determines whether the specified element is a numeric type.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>True if it is a numeric type.</returns>
        public static bool IsNumericField<T>(XElement element)
        {
            var assembly = Assembly.GetAssembly(typeof(T));
            foreach (Type type in assembly.GetTypes())
            {
                if (type.Name.EqualsIgnoreCase(element.Parent.Name.LocalName))
                {
                    PropertyInfo propertyInfo = type.GetProperty(element.Name.LocalName.ToPascalCase());
                    Type propertyType = (propertyInfo != null) ? propertyInfo.PropertyType : null;

                    if (propertyType.IsNumeric())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Removes elements that are numeric type and have NaN value.
        /// </summary>
        /// <param name="xml">The XML.</param>
        /// <returns>The xml with NaN removed.</returns>
        public static string RemoveNaNElements<T>(string xml)
        {  
            Func<XElement, bool> predicate = e => e.Value.Equals("NaN") && IsNumericField<T>(e);

            var xmlDoc = Parse(xml);
            var root = xmlDoc.Root;

            foreach (var element in root.Elements())
            {
                if (element.Descendants().Any(predicate))
                {
                    element.Descendants().Where(predicate).Remove();
                }
            }

            return xmlDoc.ToString();           
        }
    }
}
