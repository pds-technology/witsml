using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Energistics.DataAccess;

namespace PDS.Witsml
{
    /// <summary>
    /// Defines properties and methods for specifying or determining a WITSML data object's type.
    /// </summary>
    public static class ObjectTypes
    {
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
        public const string ChannelDataValues = "channelDataValues";

        private static readonly string[] GrowingObjects = new [] { Log, MudLog, Trajectory };

        /// <summary>
        /// Gets the type of the data object.
        /// </summary>
        /// <typeparam name="T">The type of object.</typeparam>
        /// <returns>The WITSML data object type, as a string.</returns>
        public static string GetObjectType<T>() where T : IEnergisticsCollection
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
            if (!typeof(IEnergisticsCollection).IsAssignableFrom(type))
            {
                throw new ArgumentException("Invalid WITSML object type, does not implement IEnergisticsCollection", "type");
            }

            return type.GetCustomAttributes(typeof(XmlRootAttribute), false)
                .OfType<XmlRootAttribute>()
                .Select(x => x.ElementName.Substring(0, x.ElementName.Length - 1))
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
        /// <param name="xml">The XML document.</param>
        /// <returns>The WITSML data object type, as a string.</returns>
        public static string GetObjectType(XDocument document)
        {
            try
            {
                return document.Root.Elements()
                    .Select(x => x.Name.LocalName)
                    .FirstOrDefault();
            }
            catch
            {
                return Unknown;
            }
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
        /// <param name="xml">The XML document.</param>
        /// <returns>The WITSML data object group type, as a string.</returns>
        public static string GetObjectGroupType(XDocument document)
        {
            try
            {
                return document.Root.Name.LocalName;
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
        /// <param name="xml">The XML document.</param>
        /// <returns>The data schema version.</returns>
        public static string GetVersion(XDocument document)
        {
            try
            {
                return (string)document.Root.Attribute("version");
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
        /// <returns></returns>
        public static string SingleToPlural(string singleString)
        {
            return singleString + "s";
        }

        private static string PluralToSingle(string pluralString)
        {
            return pluralString.EndsWith("s")
                ? pluralString.Substring(0, pluralString.Length - 1)
                : pluralString;
        }
    }
}
