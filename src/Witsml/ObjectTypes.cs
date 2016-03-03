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

        public const string Unknown = "unknown";
        public const string CapClient = "capClient";
        public const string CapServer = "capServer";

        public const string Well = "well";
        public const string Wellbore = "wellbore";
        public const string Log = "log";
        public const string Rig = "rig";
        public const string Trajectory = "trajectory";
        public const string ChangeLog = "changeLog";

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
            try
            {
                var doc = XDocument.Parse(xml);
                return doc.Root.Elements()
                    .Select(x => x.Name.LocalName)
                    .FirstOrDefault();
            }
            catch (Exception)
            {
                return Unknown;
            }
        }

        /// <summary>
        /// Gets the object type from group (plural) name.
        /// </summary>
        /// <param name="xml">The XML.</param>
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
        /// <param name="xml">The XML.</param>
        /// <returns>The WITSML data object group type, as a string.</returns>
        public static string GetObjectGroupType(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                return doc.Root.Name.LocalName;
            }
            catch (Exception)
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
            try
            {
                var doc = XDocument.Parse(xml);
                return (string)doc.Root.Attribute("version");
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string PluralToSingle(string pluralString)
        {
            return pluralString.EndsWith("s")
                ? pluralString.Substring(0, pluralString.Length - 1)
                : pluralString;
        }
    }
}
