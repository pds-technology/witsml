using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using log4net;
using Witsml131 = Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Server.Data.CapServers
{
    /// <summary>
    /// Provides common WTISML server capabilities for data schema version 1.3.1.1.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.CapServers.CapServerProvider{Energistics.DataAccess.WITSML131.CapServers}" />
    [Export(typeof(ICapServerProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class CapServer131Provider : CapServerProvider<Witsml131.CapServers>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CapServer131Provider));

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

            // TODO: move these to Settings
            capServer.Name = "PDS Witsml Server";
            capServer.Vendor = "PDS";
            capServer.Version = "1.0";

            return new Witsml131.CapServers()
            {
                CapServer = capServer,
                Version = capServer.ApiVers
            };
        }
    }
}
