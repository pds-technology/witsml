using System.Threading.Tasks;

namespace PDS.Witsml.Studio.Connections
{
    public interface IConnectionTest
    {
        Task<bool> CanConnect(Connection connection);
    }
}
