using System.ComponentModel.Composition;

namespace PDS.Witsml.Studio.Connections
{
    [Export("Etp", typeof(IConnectionTest))]
    public class EtpConnectionTest : IConnectionTest
    {
        public bool CanConnect(Connection connection)
        {
            // TODO: Make an Etp connection and test by fetching version information.
            return true;
        }
    }
}
