namespace PDS.Witsml.Studio.Connections
{
    public interface IConnectionTest
    {
        bool CanConnect(Connection connection);
    }
}
