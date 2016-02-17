using Caliburn.Micro;
using PDS.Witsml.Studio.Models;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// Manages the data entry for connection details.
    /// </summary>
    public class ConnectionViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ConnectionViewModel));

        /// <summary>
        /// Initializes an instance of the ConnectionViewModel.
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
        /// Gets and sets the connection details for a connection
        /// </summary>
        public Connection Connection { get; set; }
    }
}
