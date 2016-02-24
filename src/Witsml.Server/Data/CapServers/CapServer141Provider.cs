using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using log4net;
using Witsml141 = Energistics.DataAccess.WITSML141;
using System.Reflection;

namespace PDS.Witsml.Server.Data.CapServers
{
    /// <summary>
    /// Provides common WTISML server capabilities for data schema version 1.4.1.1.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.CapServers.CapServerProvider{Energistics.DataAccess.WITSML141.CapServers}" />
    [Export(typeof(ICapServerProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class CapServer141Provider : CapServerProvider<Witsml141.CapServers>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CapServer141Provider));
        private Witsml141.CapServer _capServer;
        private PropertyInfo[] PropertyInfo;

        /// <summary>
        /// Gets the data schema version.
        /// </summary>
        /// <value>The data schema version.</value>
        public override string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version141.Value; }
        }

        /// <summary>
        /// Gets or sets the collection of <see cref="IWitsml141Configuration"/> providers.
        /// </summary>
        /// <value>The collection of providers.</value>
        [ImportMany]
        public IEnumerable<IWitsml141Configuration> Providers { get; set; }

        /// <summary>
        /// Validates the add to store configuration parameters
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="WitsmlException">
        /// </exception>
        public override void ValidateAddToStoreConfiguration(Dictionary<string, string> options)
        {
            ValidateConfiguration(options);

            PropertyInfo[] propertyInfo = GetPropertyInfo();         
            foreach (KeyValuePair<string, string> entry in options)
            {
                string name = entry.Key;
                switch (name)
                {
                    case "compressionMethod":
                        {
                            var property = PropertyInfo.Where(x => x.Name.Equals("CompressionMethod")).FirstOrDefault();
                            string v = property.GetValue(_capServer) as string;
                            if (string.IsNullOrWhiteSpace(v) && !string.IsNullOrWhiteSpace(entry.Value))
                            {
                                throw new WitsmlException(ErrorCodes.InvalidKeywordValue, ErrorCodes.InvalidKeywordValue.GetDescription());
                            }
                        }
                        break;
                    default:
                        throw new WitsmlException(ErrorCodes.KeywordNotSupportedByFunction, name);
                }
            }
        }

        /// <summary>
        /// Validates the server support the capabilities.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="WitsmlException"></exception>
        private void ValidateConfiguration(Dictionary<string, string> options)
        {
            PropertyInfo[] propertyInfo = GetPropertyInfo();
            foreach (KeyValuePair<string, string> entry in options)
            {
                var property = propertyInfo.Where(x => x.Name.Equals(entry.Key)).FirstOrDefault();
                if (property == null)
                    throw new WitsmlException(ErrorCodes.KeywordNotSupportedByServer, ErrorCodes.KeywordNotSupportedByServer.GetDescription());
            }
        }

        private PropertyInfo[] GetPropertyInfo()
        {
            if (PropertyInfo == null)
            {
                if (_capServer == null)
                    CreateCapServer();
                PropertyInfo = _capServer.GetType().GetProperties();
            }
            return PropertyInfo;
        }

        /// <summary>
        /// Creates the capServers instance for a specific data schema version.
        /// </summary>
        /// <returns>The capServers instance.</returns>
        protected override Witsml141.CapServers CreateCapServer()
        {
            if (!Providers.Any())
            {
                _log.WarnFormat("No WITSML configuration providers loaded for data schema version {0}", DataSchemaVersion);
                return null;
            }

            var capServer = new Witsml141.CapServer();

            foreach (var config in Providers)
            {
                config.GetCapabilities(capServer);
            }

            capServer.ApiVers = "1.4.1";
            capServer.SchemaVersion = DataSchemaVersion;
            capServer.SupportUomConversion = false; // TODO: update after UoM conversion implemented

            // TODO: move these to Settings
            capServer.Name = "PDS Witsml Server";
            capServer.Vendor = "PDS";
            capServer.Version = "1.0";

            _capServer = capServer;

            return new Witsml141.CapServers()
            {
                CapServer = capServer
            };
        }
    }
}
