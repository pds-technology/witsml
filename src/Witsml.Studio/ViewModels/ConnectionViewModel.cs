using Caliburn.Micro;
using PDS.Witsml.Studio.Models;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// The view model for the connection view used to enter ETP or WITSML connection details.
    /// </summary>
    public class ConnectionViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ConnectionViewModel));

        /// <summary>
        /// Creates an instance of the ConnectionViewModel and initializes it.
        /// </summary>
        public ConnectionViewModel()
        {
            _log.Debug("Creating View Model");

            DisplayName = "Connection";
            Connection = new Connection()
            {
                //Uri = "http://localhost:5000",
                //Name = "Name",
                //Username = "username",
                //Password = "password"
            };
        }

        /// <summary>
        /// The connection details for a connection
        /// </summary>
        public Connection Connection { get; set; }
    }
}
