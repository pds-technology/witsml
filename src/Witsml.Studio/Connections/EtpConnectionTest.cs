using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Energistics;

namespace PDS.Witsml.Studio.Connections
{
    [Export("Etp", typeof(IConnectionTest))]
    public class EtpConnectionTest : IConnectionTest
    {
        public async Task<bool> CanConnect(Connection connection)
        {
            try
            {
                using (var client = new EtpClient(connection.Uri, "ETP Browser"))
                {
                    var count = 0;
                    client.Open();

                    while (string.IsNullOrWhiteSpace(client.SessionId) && count < 10)
                    {
                        await Task.Delay(1000);
                        count++;
                    }

                    return !string.IsNullOrWhiteSpace(client.SessionId);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
