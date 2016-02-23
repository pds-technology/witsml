using System.Windows;
using Caliburn.Micro;
using PDS.Witsml.Studio.Connections;

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
        public ConnectionViewModel(ConnectionTypes connectionType)
        {
            _log.Debug("Creating View Model");

            ConnectionType = connectionType;
            DisplayName = string.Format("{0} Connection", ConnectionType.ToString().ToUpper());
            Connection = new Connection();
        }

        /// <summary>
        /// Gets the connection type
        /// </summary>
        public ConnectionTypes ConnectionType { get; private set; }

        /// <summary>
        /// Gets and sets the connection details for a connection
        /// </summary>
        public Connection Connection { get; set; }


        /// <summary>
        /// Executes a connection test and reports the result to the user.
        /// </summary>
        public void TestConnection()
        {
            // Resolve a connection test specific to the current ConnectionType
            var connectionTest = App.Current.Container().Resolve<IConnectionTest>(ConnectionType.ToString());

            if (connectionTest != null)
            {
                if (connectionTest.CanConnect(Connection))
                {
                    MessageBox.Show("Connection successful", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Connection failed", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
