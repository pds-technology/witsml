using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Xml.Linq;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using log4net;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Configuration
{
    /// <summary>
    /// Provides common WITSML server capabilities for data schema version 1.3.1.1.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Configuration.CapServerProvider{Energistics.DataAccess.WITSML131.CapServers}" />
    [Export(typeof(ICapServerProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class CapServer131Provider : CapServerProvider<Witsml131.CapServers>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CapServer131Provider));

        private const string Namespace131 = "http://www.witsml.org/schemas/131";

        /// <summary>
        /// Gets the data schema version.
        /// </summary>
        /// <value>The data schema version.</value>
        public override string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version131.Value; }
        }

        /// <summary>
        /// Gets or sets the collection of <see cref="IWitsml131Configuration"/> providers.
        /// </summary>
        /// <value>The collection of providers.</value>
        [ImportMany]
        public IEnumerable<IWitsml131Configuration> Providers { get; set; }

        /// <summary>
        /// Validates the namespace for a specific WITSML data schema version.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <exception cref="WitsmlException"></exception>
        protected override void ValidateNamespace(XDocument document)
        {
            if (!Namespace131.Equals(GetNamespace(document)))
            {
                throw new WitsmlException(ErrorCodes.MissingDefaultWitsmlNamespace);
            }
        }

        /// <summary>
        /// Creates the capServers instance for a specific data schema version.
        /// </summary>
        /// <returns>The capServers instance.</returns>
        protected override Witsml131.CapServers CreateCapServer()
        {
            if (!Providers.Any())
            {
                _log.WarnFormat("No WITSML configuration providers loaded for data schema version {0}", DataSchemaVersion);
                return null;
            }

            var capServer = new Witsml131.CapServer();

            foreach (var config in Providers)
            {
                config.GetCapabilities(capServer);
            }

            capServer.ApiVers = "1.3.1";
            capServer.SchemaVersion = DataSchemaVersion;

            capServer.Name = Settings.Default.DefaultServerName;
            capServer.Version = Settings.Default.DefaultServerVersion;
            capServer.Description = Settings.Default.DefaultServerDescription;
            capServer.Vendor = Settings.Default.DefaultVendorName;
            capServer.Contact = new Contact()
            {
                Name = Settings.Default.DefaultContactName,
                Email = Settings.Default.DefaultContactEmail,
                Phone = Settings.Default.DefaultContactPhone
            };

            return new Witsml131.CapServers()
            {
                CapServer = capServer,
                Version = capServer.ApiVers
            };
        }
    }
}
