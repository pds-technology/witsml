using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Energistics.DataAccess;
using log4net;

namespace PDS.Witsml.Server.Data.CapServers
{
    /// <summary>
    /// Provides common WTISML server capabilities for any data schema version.
    /// </summary>
    /// <typeparam name="T">The capServers type.</typeparam>
    /// <seealso cref="PDS.Witsml.Server.Data.ICapServerProvider" />
    public abstract class CapServerProvider<T> : ICapServerProvider
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CapServer131Provider));

        private T _capServer;
        private XDocument _capServerDoc;
        private string _capServerXml;

        /// <summary>
        /// Gets the data schema version.
        /// </summary>
        /// <value>The data schema version.</value>
        public abstract string DataSchemaVersion { get; }

        /// <summary>
        /// Returns the server capabilities object as XML.
        /// </summary>
        /// <returns>A capServers object as an XML string.</returns>
        public string ToXml()
        {
            if (!string.IsNullOrWhiteSpace(_capServerXml))
            {
                return _capServerXml;
            }

            var capServer = GetCapServer();

            if (capServer != null)
            {
                _capServerXml = EnergisticsConverter.ObjectToXml(capServer);
            }

            return _capServerXml;
        }

        /// <summary>
        /// Determines whether the specified function is supported for the object type.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// true if the WITSML Store supports the function for the specified object type, otherwise, false
        /// </returns>
        public bool IsSupported(Functions function, string objectType)
        {
            var capServerDoc = GetCapServerDocument();
            var ns = XNamespace.Get(capServerDoc.Root.CreateNavigator().GetNamespace(string.Empty));

            var supported = capServerDoc.Descendants(ns + "dataObject")
                .Where(x => x.Value == objectType && x.Parent.Attribute("name").Value == "WMLS_" + function)
                .Any();

            _log.DebugFormat("Function: {0}; Data Object: {1}; IsSupported: {2}", function, objectType, supported);

            return supported;
        }

        /// <summary>
        /// Performs validation for the specified function and supplied parameters.
        /// </summary>
        /// <param name="function">The WITSML Store API function.</param>
        /// <param name="witsmlType">The type of the data object.</param>
        /// <param name="xml">The XML string for the data object.</param>
        /// <param name="options">The options.</param>
        /// <param name="capabilities">The client's capabilities object (capClient).</param>
        public virtual void Validate(Functions function, string witsmlType, string xml, string options, string capabilities)
        {
            ValidateRootElement(witsmlType, xml);
            //ValidateChildElement(witsmlType, xml);
        }

        private void ValidateRootElement(string witsmlType, string xml)
        {
            var objectGroupType = ObjectTypes.GetObjectGroupType(xml);
            bool a = string.Equals(objectGroupType.Substring(0, objectGroupType.Length - 1), witsmlType);
            bool b = objectGroupType[objectGroupType.Length - 1].Equals('s');
            if (!(string.Equals(objectGroupType.Substring(0, objectGroupType.Length - 1), witsmlType) && objectGroupType[objectGroupType.Length - 1].Equals('s')))
                throw new WitsmlException(ErrorCodes.MissingPluralRootElement);
        }

        /// <summary>
        /// Creates the capServers instance for a specific data schema version.
        /// </summary>
        /// <returns>The capServers instance.</returns>
        protected abstract T CreateCapServer();

        /// <summary>
        /// Gets the cached capServers instance or creates a new one.
        /// </summary>
        /// <returns>The capServers instance.</returns>
        protected T GetCapServer()
        {
            if (_capServer != null)
            {
                return _capServer;
            }

            _capServer = CreateCapServer();

            return _capServer;
        }

        /// <summary>
        /// Gets the cached capServers object as an <see cref="XDocument"/>.
        /// </summary>
        /// <returns>The <see cref="XDocument"/> instance.</returns>
        private XDocument GetCapServerDocument()
        {
            if (_capServerDoc != null)
            {
                return _capServerDoc;
            }

            _capServerDoc = XDocument.Parse(ToXml());

            return _capServerDoc;
        }
    }
}
