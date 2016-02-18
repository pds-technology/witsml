using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Witsml131 = Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Server.Data.CapServers
{
    [Export(typeof(ICapServerProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class CapServer131Provider : CapServerProvider<Witsml131.CapServers>
    {
        public override string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version131.Value; }
        }

        [ImportMany]
        public IEnumerable<IWitsml131Configuration> Providers { get; set; }

        protected override Witsml131.CapServers CreateCapServer()
        {
            if (!Providers.Any())
            {
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
                CapServer = capServer
            };
        }
    }
}
