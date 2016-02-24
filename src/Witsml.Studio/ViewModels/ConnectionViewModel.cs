using System;
using System.IO;
using System.Windows;
using Caliburn.Micro;
using Newtonsoft.Json;
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
        }

        private Connection _editItem;

        /// <summary>
        /// Gets the editing connection details that are bound to the view
        /// </summary>
        /// <value>
        /// The connection edited from the view
        /// </value>
        public Connection EditItem
        {
            get { return _editItem; }
            set
            {
                if (!ReferenceEquals(_editItem, value))
                {
                    _editItem = value;
                    NotifyOfPropertyChange(() => EditItem);
                }
            }
        }

        /// <summary>
        /// Gets the connection type
        /// </summary>
        public ConnectionTypes ConnectionType { get; private set; }

        /// <summary>
        /// Gets and sets the connection details for a connection
        /// </summary>
        public Connection DataItem { get; set; }

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
                if (connectionTest.CanConnect(EditItem))
                {
                    MessageBox.Show("Connection successful", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Connection failed", "Connection Status", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Accepts the edited connection
        /// </summary>
        public void AcceptEdit()
        {
            DataItem.Assign(EditItem);
            SaveConnectionFile(DataItem);
            TryClose(true);
        }

        public void CancelEdit()
        {
            TryClose(false);
        }

        /// <summary>
        /// Opens the connection file of persisted Connection instance for the current ConnectionType.
        /// </summary>
        /// <returns>The Connection instance from the file or null if the file does not exist.</returns>
        internal Connection OpenConnectionFile()
        {
            var filename = GetConnectionFilename();

            if (File.Exists(filename))
            {
                var json = File.ReadAllText(filename);
                return JsonConvert.DeserializeObject<Connection>(json);
            }

            return null;
        }

        /// <summary>
        /// Saves a Connection instance to a JSON file for the current connection type.
        /// </summary>
        internal void SaveConnectionFile(Connection connection)
        {
            string filename = GetConnectionFilename();
            File.WriteAllText(filename, JsonConvert.SerializeObject(connection));
        }

        /// <summary>
        /// Gets the connection filename.
        /// </summary>
        /// <returns>The path and filename for the connection file with format "[data-folder]/[connection-type]ConnectionData.json".</returns>
        internal string GetConnectionFilename()
        {
            return string.Format("{0}/{1}ConnectionData.json", Environment.CurrentDirectory, ConnectionType.ToString());
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            if (DataItem != null && !string.IsNullOrWhiteSpace(DataItem.Uri))
            {
                _editItem = DataItem.Clone();
            }
            else
            {
                _editItem = OpenConnectionFile() ?? new Connection();
            }
        }
    }
}
