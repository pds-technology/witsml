using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Energistics.DataAccess;

namespace PDS.Witsml.Studio.Connections
{
    /// <summary>
    /// Provides a connection test for a Witsml Connection instance.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Studio.Connections.IConnectionTest" />
    [Export("Witsml", typeof(IConnectionTest))]
    public class WitsmlConnectionTest : IConnectionTest
    {
        /// <summary>
        /// Determines whether this Connection instance can connect to the specified connection Uri.
        /// </summary>
        /// <param name="connection">The connection instanace being tested.</param>
        /// <returns>The boolean result from the asynchronous operation.</returns>
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
