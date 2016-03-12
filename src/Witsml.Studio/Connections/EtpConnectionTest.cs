using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Energistics;

namespace PDS.Witsml.Studio.Connections
{
    /// <summary>
    /// Provides a connection test for an Ept Connection instance.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Studio.Connections.IConnectionTest" />
    [Export("Etp", typeof(IConnectionTest))]
    public class EtpConnectionTest : IConnectionTest
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(EtpConnectionTest));

        /// <summary>
        /// Determines whether this Connection instance can connect to the specified connection Uri.
        /// </summary>
        /// <param name="connection">The connection instanace being tested.</param>
        /// <returns>The boolean result from the asynchronous operation.</returns>
        public async Task<bool> CanConnect(Connection connection)
        {
            try
            {
                var headers = EtpClient.Authorization(connection.Username, connection.Password);

                using (var client = new EtpClient(connection.Uri, "ETP Browser", headers))
                {
                    var count = 0;
                    client.Open();

                    while (string.IsNullOrWhiteSpace(client.SessionId) && count < 10)
                    {
                        await Task.Delay(1000);
                        count++;
                    }

                    var result = !string.IsNullOrWhiteSpace(client.SessionId);
                    _log.DebugFormat("Etp connection test {0}", result ? "passed" : "failed");

                    return result;
                }
            }
            catch
            {
                _log.Debug("Etp connection test failed");
                return false;
            }
        }
    }
}
