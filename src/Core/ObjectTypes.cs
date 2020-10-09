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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using Energistics.DataAccess;
using Energistics.Etp.Common.Datatypes;
using Witsml200 = Energistics.DataAccess.WITSML200;
using Prodml200 = Energistics.DataAccess.PRODML200;
using Resqml210 = Energistics.DataAccess.RESQML210;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Defines properties and methods for specifying or determining a WITSML data object's type.
    /// </summary>
    public static partial class ObjectTypes
    {
        /// <summary>
        /// The default sort order for v2.0 and above.
        /// </summary>
        public const string DefaultSortOrder = "Citation/Title, Uuid";

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
        /// The ObjectType identifier for a Uri.
        /// </summary>
        public const string Uri = "Uri";

        /// <summary>
        /// The ObjectType identifier for a Name.
        /// </summary>
        public const string NameProperty = "Name";

        /// <summary>
        /// The ObjectType identifier for Unknown.
        /// </summary>
        public const string Unknown = "unknown";

        /// <summary>
        /// The ObjectType identifier for a DocumentInfo.
        /// </summary>
        public const string DocumentInfo = "documentInfo";

        /// <summary>
        /// The ObjectType identifier for a CustomData.
        /// </summary>
        public const string CustomData = "customData";

        /// <summary>
        /// The ObjectType identifier for a FileCreationInformation.
        /// </summary>
        public const string FileCreationInformation = "fileCreationInformation";

        /// <summary>
        /// The ObjectType identifier for a CapClient.
        /// </summary>
        public const string CapClient = "capClient";

        /// <summary>
        /// The ObjectType identifier for a CapServer.
        /// </summary>
        public const string CapServer = "capServer";

        /// <summary>
        /// The ObjectType identifier for a LogCurveInfo.
        /// </summary>
        public const string LogCurveInfo = "logCurveInfo";

        /// <summary>
        /// The ObjectType identifier for a TrajectoryStation.
        /// </summary>
        public const string TrajectoryStation = "trajectoryStation";

        /// <summary>
        /// The ObjectType identifier for a GeologyInterval.
        /// </summary>
        public const string GeologyInterval = "geologyInterval";

        /// <summary>
        /// The ObjectType identifier for a ChangeLog.
        /// </summary>
        public const string ChangeLog = "changeLog";

        /// <summary>
        /// The ObjectType identifier for a ChannelIndex.
        /// </summary>
        public const string ChannelIndex = "channelIndex";

        /// <summary>
        /// The ObjectType identifier for a CuttingsGeology.
        /// </summary>
        public const string CuttingsGeology = "cuttingsGeology";

        /// <summary>
        /// The ObjectType identifier for a CuttingsGeologyInterval.
        /// </summary>
        public const string CuttingsGeologyInterval = "cuttingsGeologyInterval";

        /// <summary>
        /// The ObjectType identifier for a InterpretedGeology.
        /// </summary>
        public const string InterpretedGeology = "interpretedGeology";

        /// <summary>
        /// The ObjectType identifier for a InterpretedGeologyInterval.
        /// </summary>
        public const string InterpretedGeologyInterval = "interpretedGeologyInterval";

        /// <summary>
        /// The ObjectType identifier for a ShowEvaluation.
        /// </summary>
        public const string ShowEvaluation = "showEvaluation";

        /// <summary>
        /// The ObjectType identifier for a ShowEvaluationInterval.
        /// </summary>
        public const string ShowEvaluationInterval = "showEvaluationInterval";

        private static readonly string[] _growingObjects = { Log, MudLog, Trajectory, CuttingsGeology, InterpretedGeology, ShowEvaluation };

        private static readonly string[] _growingPartTypes = { LogCurveInfo, GeologyInterval, TrajectoryStation, CuttingsGeologyInterval, InterpretedGeologyInterval, ShowEvaluationInterval };

        private static readonly string[] _decoratorObjects = { Activity, DataAssuranceRecord };

        private static Dictionary<Type, string> _elementNameOverrides = new Dictionary<Type, string>
        {
            { typeof(Witsml200.CuttingsGeology), "CuttingsIntervalSet"},
            { typeof(Witsml200.CuttingsGeologyInterval), "CuttingsInterval"},
            { typeof(Witsml200.InterpretedGeology), "InterpretedGeologyIntervalSet"},
            { typeof(Witsml200.InterpretedGeologyInterval), "GeologicIntervalInterpreted"},
            { typeof(Witsml200.ComponentSchemas.InterpretedIntervalLithology), "InterpretedLithology"},
            { typeof(Witsml200.ShowEvaluation), "ShowIntervalSet"},
            { typeof(Witsml200.ShowEvaluationInterval), "EvaluatedIntervalShow"}
        };

        /// <summary>
        /// The object type map.
        /// </summary>
        public static readonly IDictionary<string, string> ObjectTypeMap;

        /// <summary>
        /// The child object reference map.
        /// </summary>
        public static readonly IDictionary<EtpContentType, string[]> ChildObjectReferences;

        /// <summary>
        /// Initializes the <see cref="ObjectTypes"/> class.
        /// </summary>
        static ObjectTypes()
        {
            ObjectTypeMap = typeof(ObjectTypes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(x => x.FieldType == typeof(string))
                .Select(x => x.GetValue(null))
                .Where(x => x != null)
                .Cast<string>()
                .ToDictionary(x => x, StringComparer.InvariantCultureIgnoreCase);

            ChildObjectReferences = new Dictionary<EtpContentType, string[]>
            {
                { EtpContentTypes.Witsml200.For(BhaRun), new [] { "Tubular" }},
                { EtpContentTypes.Witsml200.For(CementJob), new [] { "HoleConfig" }},
                { EtpContentTypes.Witsml200.For(MudLogReport), new [] { "WellboreGeometry" }},
                //{ EtpContentTypes.Witsml200.For(MudLogReportInterval), new [] { "CuttingsGeologyInterval" }},
                //{ EtpContentTypes.Witsml200.For(MudLogReportInterval), new [] { "InterpretedGeologyInterval" }},
                //{ EtpContentTypes.Witsml200.For(MudLogReportInterval), new [] { "ShowEvaluationInterval" }},
                { EtpContentTypes.Witsml200.For(OpsReport), new [] { "WellboreGeometry" }},
                { EtpContentTypes.Witsml200.For(TrajectoryStation), new [] { "IscwsaToolErrorModel" }},
                { EtpContentTypes.Witsml200.For(WellboreMarker), new [] { "Trajectory" }}
            };
        }

        /// <summary>
        /// Gets whether the specified type represents and Energistics data object type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static bool IsEnergisticsObjectType(Type type)
        {
            return type.Assembly == typeof(IWitsmlDataObject).Assembly
                   || type.Assembly == typeof(IProdmlDataObject).Assembly
                   || type.Assembly == typeof(IResqmlDataObject).Assembly;
        }

        /// <summary>
        /// Determines whether the specified property name is for a child data object reference.
        /// </summary>
        /// <param name="contentType">The content type.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns><c>true</c> if the property name is for a child data object reference; otherwise, <c>false</c>.</returns>
        public static bool IsChildObjectReference(EtpContentType contentType, string propertyName)
        {
            string[] values;
            return ChildObjectReferences.TryGetValue(contentType, out values) && (values?.ContainsIgnoreCase(propertyName) ?? false);
        }

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
        /// Gets the type of the object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>The WITSML data object type, as a string.</returns>
        public static string GetObjectType(Prodml200.AbstractObject dataObject)
        {
            return GetObjectType(dataObject.GetType());
        }

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>The WITSML data object type, as a string.</returns>
        public static string GetObjectType(Resqml210.AbstractObject dataObject)
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
                !typeof(Witsml200.AbstractObject).IsAssignableFrom(type) &&
                !typeof(Prodml200.AbstractObject).IsAssignableFrom(type) &&
                !typeof(Resqml210.AbstractObject).IsAssignableFrom(type))
            {
                throw new ArgumentException(@"Invalid WITSML object type, does not implement IEnergisticsCollection, IDataObject or AbstractObject", nameof(type));
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
                    .Where(x => !DocumentInfo.EqualsIgnoreCase(x.Name.LocalName))
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
        /// <param name="family">THe data object family name.</param>
        /// <param name="version">The WITSML version.</param>
        /// <returns>The .NET type for the data object.</returns>
        public static Type GetObjectType(string objectType, string family, WMLSVersion version)
        {
            return GetObjectType(objectType, family, version == WMLSVersion.WITSML131
                ? OptionsIn.DataVersion.Version131.Value
                : OptionsIn.DataVersion.Version141.Value);
        }

        /// <summary>
        /// Gets the .NET type for the specified object type and WITSML version.
        /// </summary>
        /// <param name="objectType">The data object type.</param>
        /// <param name="family">THe data object family name.</param>
        /// <param name="version">The WITSML version.</param>
        /// <returns>The .NET type for the data object.</returns>
        /// <remarks>Handles versions of the format X.Y[.X.[.W]] and dataVersion=X.Y[.Z[.W]].</remarks>
        public static Type GetObjectType(string objectType, string family, string version)
        {
            if (string.IsNullOrEmpty(version) || version.Length < 3)
                return null;

            Assembly assembly;
            string familyUpper = family.ToUpperInvariant();

            if (familyUpper == ObjectFamilies.Witsml)
                assembly = typeof(IWitsmlDataObject).Assembly;
            else if (familyUpper == ObjectFamilies.Prodml)
                assembly = typeof(IProdmlDataObject).Assembly;
            else if (familyUpper == ObjectFamilies.Resqml)
                assembly = typeof(IResqmlDataObject).Assembly;
            else if (familyUpper == ObjectFamilies.Eml)
            {
                // TODO: Update this once common is implemented independently.
                familyUpper = "WITSML";
                version = "2.0";
                assembly = typeof(IWitsmlDataObject).Assembly;
            }
            else
                return null;

            var index = version.LastIndexOf('=');
            index++; // index will be -1 if no '=' is found so no special case needed.

            char build = version.Length > index + 4 ? version[index + 4] : '0';
            var ns = $"Energistics.DataAccess.{familyUpper}{version[index]}{version[index + 2]}{build}.";

            if (WbGeometry.EqualsIgnoreCase(objectType) && !OptionsIn.DataVersion.Version200.Equals(version))
                objectType = $"StandAlone{WellboreGeometry.ToPascalCase()}";

            return assembly.GetType(ns + objectType.ToPascalCase());
        }

        /// <summary>
        /// Gets the property name for the recurring element within the container.
        /// </summary>
        /// <param name="objectType">The data object type.</param>
        /// <param name="family">THe data object family name.</param>
        /// <param name="version">The version.</param>
        /// <returns>The recurring element property name.</returns>
        public static string GetObjectTypeListProperty(string objectType, string family, string version)
        {
            return GetObjectTypeListPropertyInfo(objectType, family, version)?.Name;
        }

        /// <summary>
        /// Gets the propertyinfo for the recurring element within the container.
        /// </summary>
        /// <param name="objectType">The data object type.</param>
        /// <param name="family">THe data object family name.</param>
        /// <param name="version">The version.</param>
        /// <returns>The recurring element propertyinfo.</returns>
        public static PropertyInfo GetObjectTypeListPropertyInfo(string objectType, string family, string version)
        {
            var objectGroupType = GetObjectGroupType(objectType, family, version);

            return objectGroupType?
                .GetProperties()
                .Select(x => new { Property = x, Attribute = XmlAttributeCache<XmlElementAttribute>.GetCustomAttribute(x) })
                .Where(x => objectType.EqualsIgnoreCase(x.Attribute?.ElementName))
                .Select(x => x.Property)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the .NET type of the collection for the specified data object type and WITSML version.
        /// </summary>
        /// <param name="objectType">The data object type.</param>
        /// <param name="family">THe data object family name.</param>
        /// <param name="version">The WITSML version.</param>
        /// <returns>The .NET type for the data object collection.</returns>
        public static Type GetObjectGroupType(string objectType, string family, WMLSVersion version)
        {
            if (WbGeometry.EqualsIgnoreCase(objectType))
                objectType = WellboreGeometry;

            return GetObjectType(objectType + "List", family, version);
        }

        /// <summary>
        /// Gets the .NET type of the collection for the specified data object type and WITSML version.
        /// </summary>
        /// <param name="objectType">The data object type.</param>
        /// <param name="family">THe data object family name.</param>
        /// <param name="version">The WITSML version.</param>
        /// <returns>The .NET type for the data object collection.</returns>
        public static Type GetObjectGroupType(string objectType, string family, string version)
        {
            if (WbGeometry.EqualsIgnoreCase(objectType))
                objectType = WellboreGeometry;

            var suffix = OptionsIn.DataVersion.Version200.Equals(version)
                ? string.Empty
                : "List";

            return GetObjectType(objectType + suffix, family, version);
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
        /// Gets the data schema version for the specified data object type.
        /// </summary>
        /// <param name="type">The data object type.</param>
        /// <returns>The data schema version.</returns>
        public static string GetVersion(Type type)
        {
            if (string.IsNullOrWhiteSpace(type.Namespace)) return null;
            var ns = type.Namespace;

            return ns.StartsWith("Energistics.DataAccess.WITSML131")
                ? OptionsIn.DataVersion.Version131.Value
                : ns.StartsWith("Energistics.DataAccess.WITSML200")
                ? OptionsIn.DataVersion.Version200.Value
                : OptionsIn.DataVersion.Version141.Value;
        }

        /// <summary>
        /// Gets the data object family for the specified data object type.
        /// </summary>
        /// <param name="type">The data object type.</param>
        /// <returns>The data family.</returns>
        public static string GetFamily(Type type)
        {
            return GetFamily(type.Namespace);
        }

        /// <summary>
        /// Gets the data object family for the specified data object type.
        /// </summary>
        /// <param name="namespace">The data object type's namespace.</param>
        /// <returns>The data family.</returns>
        public static string GetFamily(string @namespace)
        {
            if (string.IsNullOrWhiteSpace(@namespace)) return null;

            // TODO: Update this once common is implemented independently.

            return @namespace.Contains(ObjectFamilies.Witsml)
                ? ObjectFamilies.Witsml
                : @namespace.Contains(ObjectFamilies.Prodml)
                ? ObjectFamilies.Prodml
                : @namespace.Contains(ObjectFamilies.Resqml)
                ? ObjectFamilies.Resqml
                : null;
        }

        /// <summary>
        /// Gets the data object family of the object.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns>The data object family.</returns>
        public static string GetFamily(XElement element)
        {
            var ns = element?.GetDefaultNamespace()?.NamespaceName;
            if (string.IsNullOrEmpty(ns))
                return null;

            if (ns.ContainsIgnoreCase(ObjectFamilies.Witsml))
                return ObjectFamilies.Witsml;
            if (ns.ContainsIgnoreCase(ObjectFamilies.Prodml))
                return ObjectFamilies.Prodml;
            if (ns.ContainsIgnoreCase(ObjectFamilies.Resqml))
                return ObjectFamilies.Resqml;

            // TODO: Update this once common is implemented independently.

            if (ns.ContainsIgnoreCase("common"))
                return ObjectFamilies.Witsml;

            return null;
        }

        /// <summary>
        /// Gets the data object family of the object.
        /// </summary>
        /// <param name="pluralObject">The plural object.</param>
        /// <returns>The data object family.</returns>
        public static string GetFamily(IEnergisticsCollection pluralObject)
        {
            return GetFamily(pluralObject.GetType());
        }

        /// <summary>
        /// Determines whether the object type is a decorator object.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns></returns>
        public static bool IsDecoratorObject(string objectType)
        {
            return _decoratorObjects.ContainsIgnoreCase(objectType);
        }

        /// <summary>
        /// Determines whether the object type is a growing data object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns></returns>
        public static bool IsGrowingDataObject(string objectType)
        {
            return _growingObjects.ContainsIgnoreCase(objectType);
        }

        /// <summary>
        /// Gets the object type for the growing part of a growing object.
        /// </summary>
        /// <param name="objectType">The growing object type.</param>
        /// <returns>The growing part type.</returns>
        public static string GetGrowingObjectType(string objectType)
        {
            if (!IsGrowingDataObject(objectType)) return null;

            var index = Array.IndexOf(_growingObjects, objectType);
            return _growingPartTypes.Skip(index).FirstOrDefault();
        }

        /// <summary>
        /// Convert a singular string to plural.
        /// </summary>
        /// <param name="singleString">The single string.</param>
        /// <param name="isWitsmlPlural">if set to <c>true</c> use WITSML plural rules.</param>
        /// <returns>The plural string.</returns>
        public static string SingleToPlural(string singleString, bool isWitsmlPlural = true)
        {
            if (isWitsmlPlural)
                return singleString + "s";

            if (singleString.EndsWith("y"))
                return singleString.Substring(0, singleString.Length - 1) + "ies";

            if (singleString.EndsWith("x"))
                return singleString + "es";

            return singleString + "s";
        }

        /// <summary>
        /// Converts a plural string to singlular.
        /// </summary>
        /// <param name="pluralString">The plural string.</param>
        /// <returns>The singular string.</returns>
        public static string PluralToSingle(string pluralString)
        {
            return pluralString.EndsWith("ies")
                ? pluralString.Substring(0, pluralString.Length - 3) + "y"
                : pluralString.EndsWith("xes")
                ? pluralString.Substring(0, pluralString.Length - 2)
                : pluralString.EndsWith("s")
                ? pluralString.Substring(0, pluralString.Length - 1)
                : pluralString;
        }

        /// <summary>
        /// Gets the element name override.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>Name override</returns>
        public static string GetElementNameOverride(Type objectType)
        {
            string elementName;
            _elementNameOverrides.TryGetValue(objectType, out elementName);
            return elementName;
        }
    }
}
