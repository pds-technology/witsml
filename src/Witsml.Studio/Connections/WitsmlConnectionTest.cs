using System.ComponentModel.Composition;
using Energistics.DataAccess;

namespace PDS.Witsml.Studio.Connections
{
    [Export("Witsml", typeof(IConnectionTest))]
    public class WitsmlConnectionTest : IConnectionTest
    {
        public bool CanConnect(Connection connection)
        {
            try
            {
                var proxy = new WITSMLWebServiceConnection(connection.Uri, WMLSVersion.WITSML141);
                var versions = proxy.GetVersion();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
