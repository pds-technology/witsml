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
using System.Xml.Linq;
using System.Xml.Serialization;
using Energistics.DataAccess;
using AbstractObject = Energistics.DataAccess.WITSML200.ComponentSchemas.AbstractObject;
using PDS.Framework;

namespace PDS.Witsml
{
    /// <summary>
    /// Defines properties and methods for specifying or determining a WITSML data object's type.
    /// </summary>
    public static class ObjectTypes
    {
        public const string Id = "Id";
        public const string Uid = "Uid";
        public const string Uuid = "Uuid";
        public const string NameProperty = "Name";

        public const string Unknown = "unknown";
        public const string CapClient = "capClient";
        public const string CapServer = "capServer";

        public const string Well = "well";
        public const string Wellbore = "wellbore";
        public const string Log = "log";
        public const string LogCurveInfo = "logCurveInfo";
        public const string Rig = "rig";
        public const string Trajectory = "trajectory";
        public const string MudLog = "mudLog";
        public const string ChangeLog = "changeLog";
        public const string ChannelSet = "channelSet";
        public const string Channel = "channel";
        public const string ChannelIndex = "channelIndex";
        public const string ChannelDataValues = "channelDataValues";
        public const string ChannelDataChunk = "channelDataChunk";
        public const string MongoDbTransaction = "mongoTransaction";

        private static readonly string[] GrowingObjects = new [] { Log, MudLog, Trajectory };

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <param name="pluralObject">The plural object.</param>
        /// <returns>The WITSML data object type, as a string.</returns>
        public static string GetObjectType(IEnergisticsCollection pluralObject)
        {
            return GetObjectType(pluralObject.GetType());
        }

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>The WITSML data object type, as a string.</returns>
        public static string GetObjectType(IDataObject dataObject)
        {
            return GetObjectType(dataObject.GetType());
        }

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>The WITSML data object type, as a string.</returns>
        public static string GetObjectType(AbstractObject dataObject)
        {
            return GetObjectType(dataObject.GetType());
        }

        /// <summary>
        /// Gets the type of the data object.
        /// </summary>
        /// <typeparam name="T">The type of object.</typeparam>
        /// <returns>The WITSML data object type, as a string.</returns>
        public static string GetObjectType<T>()
        {
            return GetObjectType(typeof(T));
        }

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <param name="type">The type of object.</param>
        /// <returns>The WITSML data object type, as a string.</returns>
        /// <exception cref="System.ArgumentException">Invalid WITSML object type, does not implement IEnergisticsCollection</exception>
        public static string GetObjectType(Type type)
        {
            if (!typeof(IEnergisticsCollection).IsAssignableFrom(type) && 
                !typeof(IDataObject).IsAssignableFrom(type) &&
                !typeof(AbstractObject).IsAssignableFrom(type))
            {
                throw new ArgumentException("Invalid WITSML object type, does not implement IEnergisticsCollection, IDataObject or AbstractObject", nameof(type));
            }

            if (typeof(IDataObject).IsAssignableFrom(type))
            {
                return type.GetCustomAttributes(typeof(XmlTypeAttribute), true)
                    .OfType<XmlTypeAttribute>()
                    .Select(x => x.TypeName.Substring(4).ToCamelCase())
                    .FirstOrDefault();
            }

            return type.GetCustomAttributes(typeof(XmlRootAttribute), true)
                .OfType<XmlRootAttribute>()
                .Select(x =>
                {
                    return typeof(IEnergisticsCollection).IsAssignableFrom(type)
                        ? PluralToSingle(x.ElementName)
                        : x.ElementName.ToCamelCase();
                })
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <returns>The WITSML data object type, as a string.</returns>
        public static string GetObjectType(string xml)
        {
            var document = WitsmlParser.Parse(xml);
            return GetObjectType(document);
        }

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <param name="document">The XML document.</param>
        /// <returns>The WITSML data object type, as a string.</returns>
        public static string GetObjectType(XDocument document)
        {
            try
            {
                return document.Root?.Elements()
                    .Select(x => x.Name.LocalName)
                    .FirstOrDefault() ?? Unknown;
            }
            catch
            {
                return Unknown;
            }
        }

        /// <summary>
        /// Gets the .NET type for the specified object type and WITSML version.
        /// </summary>
        /// <param name="objectType">The data object type.</param>
        /// <param name="version">The WITSML version.</param>
        /// <returns>The .NET type for the data object.</returns>
        public static Type GetObjectType(string objectType, WMLSVersion version)
        {
            return GetObjectType(objectType, version == WMLSVersion.WITSML131
                ? OptionsIn.DataVersion.Version131.Value
                : OptionsIn.DataVersion.Version141.Value);
        }

        /// <summary>
        /// Gets the .NET type for the specified object type and WITSML version.
        /// </summary>
        /// <param name="objectType">The data object type.</param>
        /// <param name="version">The WITSML version.</param>
        /// <returns>The .NET type for the data object.</returns>
        public static Type GetObjectType(string objectType, string version)
        {
            var ns = OptionsIn.DataVersion.Version131.Equals(version)
                ? "Energistics.DataAccess.WITSML131."
                : OptionsIn.DataVersion.Version200.Equals(version)
                ? "Energistics.DataAccess.WITSML200."
                : "Energistics.DataAccess.WITSML141.";

            return typeof(IDataObject).Assembly.GetType(ns + objectType.ToPascalCase());
        }

        /// <summary>
        /// Gets the .NET type of the collection for the specified data object type and WITSML version.
        /// </summary>
        /// <param name="objectType">The data object type.</param>
        /// <param name="version">The WITSML version.</param>
        /// <returns>The .NET type for the data object collection.</returns>
        public static Type GetObjectGroupType(string objectType, WMLSVersion version)
        {
            return GetObjectType(objectType + "List", version);
        }

        /// <summary>
        /// Gets the .NET type of the collection for the specified data object type and WITSML version.
        /// </summary>
        /// <param name="objectType">The data object type.</param>
        /// <param name="version">The WITSML version.</param>
        /// <returns>The .NET type for the data object collection.</returns>
        public static Type GetObjectGroupType(string objectType, string version)
        {
            return GetObjectType(objectType + "List", version);
        }

        /// <summary>
        /// Gets the object type from group (plural) name.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <returns>The WITSML Object Type In</returns>
        public static string GetObjectTypeFromGroup(string xml)
        {
            try
            {
                return string.IsNullOrEmpty(xml) 
                    ? Unknown 
                    : PluralToSingle(GetObjectGroupType(xml));
            }
            catch
            {
                return Unknown;
            }
        }

        /// <summary>
        /// Gets the type of the object group.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <returns>The WITSML data object group type, as a string.</returns>
        public static string GetObjectGroupType(string xml)
        {
            var document = WitsmlParser.Parse(xml);
            return GetObjectGroupType(document);
        }

        /// <summary>
        /// Gets the type of the object group.
        /// </summary>
        /// <param name="document">The XML document.</param>
        /// <returns>The WITSML data object group type, as a string.</returns>
        public static string GetObjectGroupType(XDocument document)
        {
            try
            {
                return document.Root?.Name.LocalName ?? Unknown;
            }
            catch
            {
                return Unknown;
            }
        }

        /// <summary>
        /// Gets the data schema version.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <returns>The data schema version.</returns>
        public static string GetVersion(string xml)
        {
            var document = WitsmlParser.Parse(xml);
            return GetVersion(document);
        }

        /// <summary>
        /// Gets the data schema version.
        /// </summary>
        /// <param name="document">The XML document.</param>
        /// <returns>The data schema version.</returns>
        public static string GetVersion(XDocument document)
        {
            try
            {
                return (string)document.Root?.Attribute("version") ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Determines whether the object type is a growing data object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns></returns>
        public static bool IsGrowingDataObject(string objectType)
        {
            return GrowingObjects.Contains(objectType);
        }

        /// <summary>
        /// Convert a singular string to plural.
        /// </summary>
        /// <param name="singleString">The single string.</param>
        /// <returns>The singular string.</returns>
        internal static string SingleToPlural(string singleString)
        {
            return singleString + "s";
        }

        /// <summary>
        /// Converts a plural string to singlular.
        /// </summary>
        /// <param name="pluralString">The plural string.</param>
        /// <returns>The plural string.</returns>
        internal static string PluralToSingle(string pluralString)
        {
            return pluralString.EndsWith("s")
                ? pluralString.Substring(0, pluralString.Length - 1)
                : pluralString;
        }
    }
}
