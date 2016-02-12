using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Energistics.DataAccess;
using PDS.Witsml.Properties;

namespace PDS.Witsml
{
    public static class ObjectTypes
    {
        private static readonly string DefaultDataSchemaVersion = Settings.Default.DefaultDataSchemaVersion;

        public const string CapClient = "capClient";
        public const string CapServer = "capServer";

        public const string Well = "well";
        public const string Wellbore = "wellbore";
        public const string Log = "log";
        public const string Rig = "rig";
        public const string Trajectory = "trajectory";
        public const string ChangeLog = "changeLog";

        public static string GetObjectType<T>() where T : IEnergisticsCollection
        {
            return GetObjectType(typeof(T));
        }

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

        public static string GetVersion(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                return (string)doc.Root.Attribute("version");
            }
            catch (Exception)
            {
                return DefaultDataSchemaVersion;
            }
        }
    }
}
