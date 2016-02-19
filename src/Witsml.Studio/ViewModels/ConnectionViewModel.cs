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

            Connection = new Connection()
            {
                ConnectionType = ConnectionTypes.Witsml.Value // default
            };
        }

        private Connection _connection;
        /// <summary>
        /// Gets and sets the connection details for a connection
        /// </summary>
        public Connection Connection
        {
            get { return _connection; }
            set
            {
                if (!ReferenceEquals(_connection, value))
                {
                    _connection = value;
                    DisplayName = string.Format("{0} Connection", _connection.ConnectionType);
                    NotifyOfPropertyChange(() => Connection);
                }
            }
        }
    }
}
