using System;
using System.Threading.Tasks;
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
            CanTestConnection = true;
        }

        /// <summary>
        /// Gets the connection type
        /// </summary>
        public ConnectionTypes ConnectionType { get; private set; }

        /// <summary>
        /// Gets and sets the connection details for a connection
        /// </summary>
        public Connection Connection { get; set; }

        private bool _canTestConnection;
        /// <summary>
        /// Gets or sets a value indicating whether this instance can execute a connection test.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can test connection; otherwise, <c>false</c>.
        /// </value>
        public bool CanTestConnection
        {
            get { return _canTestConnection; }
            set
            {
                if (_canTestConnection != value)
                {
                    _canTestConnection = value;
                    NotifyOfPropertyChange(() => CanTestConnection);
                }
            }
        }

        /// <summary>
        /// Executes a connection test and reports the result to the user.
        /// </summary>
        public void TestConnection()
        {
            // Resolve a connection test specific to the current ConnectionType
            var connectionTest = App.Current.Container().Resolve<IConnectionTest>(ConnectionType.ToString());

            if (connectionTest != null)
            {
                UiServices.SetBusyState();
                CanTestConnection = false;

                Task.Run(async() =>
                {
                    var result = await connectionTest.CanConnect(Connection);
                    await App.Current.Dispatcher.BeginInvoke(new Action<bool>(ShowTestResult), result);
                });
            }
        }

        private void ShowTestResult(bool result)
        {
            CanTestConnection = true;

            if (result)
            {
                MessageBox.Show(Application.Current.MainWindow, "Connection successful", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(Application.Current.MainWindow, "Connection failed", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
