using System.ComponentModel.Composition;

namespace PDS.Witsml.Studio.Connections
{
    [Export("Witsml", typeof(IConnectionTest))]
    public class WitsmlConnectionTest : IConnectionTest
    {
        public bool CanConnect(Connection connection)
        {
            // TODO: Make a Witsml connection and test by fetching version information.
            return true;
        }
    }
}
