using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Energistics.DataAccess;

namespace PDS.Witsml.Studio.Connections
{
    [Export("Witsml", typeof(IConnectionTest))]
    public class WitsmlConnectionTest : IConnectionTest
    {
        public async Task<bool> CanConnect(Connection connection)
        {
            await Task.Yield();

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
