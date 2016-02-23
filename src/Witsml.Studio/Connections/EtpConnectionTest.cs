using System.ComponentModel.Composition;
using Energistics;

namespace PDS.Witsml.Studio.Connections
{
    [Export("Etp", typeof(IConnectionTest))]
    public class EtpConnectionTest : IConnectionTest
    {
        public bool CanConnect(Connection connection)
        {
            try
            {
                var client = new EtpClient(connection.Uri, "ETP Browser");
                client.Open();
                return client.IsOpen;
            }
            catch
            {
                return false;
            }
        }
    }
}
