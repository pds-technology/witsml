using System.Threading;
using System.Threading.Tasks;
using Energistics.DataAccess;
using PDS.Witsml.Studio.Connections;

namespace PDS.Witsml.Studio.Plugins.DataReplay.ViewModels.Proxies
{
    public abstract class WitsmlProxyViewModel
    {
        public WitsmlProxyViewModel(Connection connection, WMLSVersion version)
        {
            Connection = CreateConnection(connection, version);
            Version = version;
        }

        public WITSMLWebServiceConnection Connection { get; private set; }

        public WMLSVersion Version { get; private set; }

        public abstract Task Start(Models.Simulation model, CancellationToken token, int interval = 5000);

        /// <summary>
        /// Creates a WITSMLWebServiceConnection for the current connection uri and witsml version.
        /// </summary>
        /// <returns></returns>
        private WITSMLWebServiceConnection CreateConnection(Connection connection, WMLSVersion version)
        {
            //_log.DebugFormat("A new Proxy is being created with URI: {0}; WitsmlVersion: {1};", connection.Uri, version);
            var proxy = new WITSMLWebServiceConnection(connection.Uri, version);

            if (!string.IsNullOrWhiteSpace(connection.Username))
            {
                proxy.Username = connection.Username;
                proxy.SetSecurePassword(connection.SecurePassword);
            }

            return proxy;
        }
    }
}
