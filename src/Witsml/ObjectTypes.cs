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
using Witsml200 = Energistics.DataAccess.WITSML200;
using PDS.Framework;

namespace PDS.Witsml
{
    /// <summary>
    /// Defines properties and methods for specifying or determining a WITSML data object's type.
    /// </summary>
    public static class ObjectTypes
    {
        /// <summary>
        /// The ObjectType identifier for an Id.
        /// </summary>
        public const string Id = "Id";

        /// <summary>
        /// The ObjectType identifier for a Uid.
        /// </summary>
        public const string Uid = "Uid";

        /// <summary>
        /// The ObjectType identifier for a Uuid.
        /// </summary>
        public const string Uuid = "Uuid";

        /// <summary>
        /// The ObjectType identifier for a Name.
        /// </summary>
        public const string NameProperty = "Name";

        /// <summary>
        /// The ObjectType identifier for Unknown.
        /// </summary>
        public const string Unknown = "unknown";

        /// <summary>
        /// The ObjectType identifier for a CapClient.
        /// </summary>
        public const string CapClient = "capClient";

        /// <summary>
        /// The ObjectType identifier for a CapServer.
        /// </summary>
        public const string CapServer = "capServer";

        /// <summary>
        /// The ObjectType identifier for a Well.
        /// </summary>
        public const string Well = "well";

        /// <summary>
        /// The ObjectType identifier for a Wellbore.
        /// </summary>
        public const string Wellbore = "wellbore";

        /// <summary>
        /// The ObjectType identifier for a Log.
        /// </summary>
        public const string Log = "log";

        /// <summary>
        /// The ObjectType identifier for a LogCurveInfo.
        /// </summary>
        public const string LogCurveInfo = "logCurveInfo";

        /// <summary>
        /// The ObjectType identifier for a Rig.
        /// </summary>
        public const string Rig = "rig";

        /// <summary>
        /// The ObjectType identifier for a Message.
        /// </summary>
        public const string Message = "message";

        /// <summary>
        /// The ObjectType identifier for a Trajectory.
        /// </summary>
        public const string Trajectory = "trajectory";

        /// <summary>
        /// The ObjectType identifier for a MudLog.
        /// </summary>
        public const string MudLog = "mudLog";

        /// <summary>
        /// The ObjectType identifier for a ChangeLog.
        /// </summary>
        public const string ChangeLog = "changeLog";

        /// <summary>
        /// The ObjectType identifier for a ChannelSet.
        /// </summary>
        public const string ChannelSet = "channelSet";

        /// <summary>
        /// The ObjectType identifier for a Channel.
        /// </summary>
        public const string Channel = "channel";

        /// <summary>
        /// The ObjectType identifier for a ChannelIndex.
        /// </summary>
        public const string ChannelIndex = "channelIndex";      

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
        public static string GetObjectType(Witsml200.AbstractObject dataObject)
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
                !typeof(Witsml200.AbstractObject).IsAssignableFrom(type))
            {
                throw new ArgumentException("Invalid WITSML object type, does not implement IEnergisticsCollection, IDataObject or AbstractObject", nameof(type));
            }

            if (typeof(IDataObject).IsAssignableFrom(type))
            {
                var xsdType = GetSchemaType(type);
                return xsdType.Substring(xsdType.IndexOf('_') + 1);
            }

            return type.GetCustomAttributes(typeof(XmlRootAttribute), true)
                .OfType<XmlRootAttribute>()
                .Select(x =>
                {
                    var elementName = string.IsNullOrWhiteSpace(x.ElementName)
                        ? type.Name
                        : x.ElementName;

                    return typeof(IEnergisticsCollection).IsAssignableFrom(type)
                        ? PluralToSingle(elementName)
                        : elementName.ToCamelCase();
                })
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns>The WITSML data object type, as a string.</returns>
        public static string GetObjectType(XElement element)
        {
            try
            {
                return element.Elements()
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
        /// <param name="element">The XML element.</param>
        /// <returns>The WITSML Object Type In</returns>
        public static string GetObjectTypeFromGroup(XElement element)
        {
            try
            {
                return element == null 
                    ? Unknown 
                    : PluralToSingle(GetObjectGroupType(element));
            }
            catch
            {
                return Unknown;
            }
        }

        /// <summary>
        /// Gets the type of the object group.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns>The WITSML data object group type, as a string.</returns>
        public static string GetObjectGroupType(XElement element)
        {
            try
            {
                return element.Name.LocalName ?? Unknown;
            }
            catch
            {
                return Unknown;
            }
        }

        /// <summary>
        /// Gets the XSD type for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>The XSD type.</returns>
        public static string GetSchemaType(object dataObject)
        {
            return GetSchemaType(dataObject?.GetType());
        }

        /// <summary>
        /// Gets the XSD type for the specified data object type.
        /// </summary>
        /// <param name="type">The data object type.</param>
        /// <returns>The XSD type.</returns>
        public static string GetSchemaType(Type type)
        {
            return type?.GetCustomAttributes(typeof(XmlTypeAttribute), true)
                .OfType<XmlTypeAttribute>()
                .Select(x => string.IsNullOrWhiteSpace(x.TypeName) ? type.Name : x.TypeName)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the data schema version.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns>The data schema version.</returns>
        public static string GetVersion(XElement element)
        {
            try
            {
                return (string)element.Attribute("version") ?? string.Empty;
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
