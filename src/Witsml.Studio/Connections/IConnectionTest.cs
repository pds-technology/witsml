using System.Threading.Tasks;

namespace PDS.Witsml.Studio.Connections
{
    /// <summary>
    /// Interface for a connection test against a Connection instance.
    /// </summary>
    public interface IConnectionTest
    {
        /// <summary>
        /// Determines whether this Connection instance can connect the specified connection Uri.
        /// </summary>
        /// <param name="connection">The connection instanace being tested.</param>
        /// <returns>The boolean result from the asynchronous operation.</returns>
        Task<bool> CanConnect(Connection connection);
    }
}
