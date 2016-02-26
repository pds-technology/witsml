using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using AutoMapper;
using Caliburn.Micro;
using Newtonsoft.Json;
using PDS.Witsml.Studio.Connections;
using PDS.Witsml.Studio.Properties;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// Manages the data entry for connection details.
    /// </summary>
    public class ConnectionViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ConnectionViewModel));
        private static readonly string PersistedDataFolderName = Settings.Default.PersistedDataFolderName;
        private static readonly string ConnectionBaseFileName = Settings.Default.ConnectionBaseFileName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionViewModel"/> class.
        /// </summary>
        /// <param name="connectionType">Type of the connection.</param>
        public ConnectionViewModel(ConnectionTypes connectionType)
        {
            _log.Debug("Creating View Model");

            ConnectionType = connectionType;
            DisplayName = string.Format("{0} Connection", ConnectionType.ToString().ToUpper());
            CanTestConnection = true;
        }

        /// <summary>
        /// Gets the connection type
        /// </summary>
        public ConnectionTypes ConnectionType { get; private set; }

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
        /// Gets or sets the connection details for a connection
        /// </summary>
        public Connection DataItem { get; set; }

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

        private bool _isTestSuccess;

        /// <summary>
        /// Gets or sets a value indicating whether a connection test was successful.
        /// </summary>
        /// <value>
        /// <c>true</c> if a connection test was executed and successful; otherwise, <c>false</c>.
        /// </value>
        public bool IsTestSuccess
        {
            get { return _isTestSuccess; }
            set
            {
                if (_isTestSuccess != value)
                {
                    _isTestSuccess = value;
                    NotifyOfPropertyChange(() => IsTestSuccess);
                }
            }
        }

        private bool _isTestFailure;

        /// <summary>
        /// Gets or sets a value indicating whether a connection test was a failure.
        /// </summary>
        /// <value>
        /// <c>true</c> if a connection test was executed and failed; otherwise, <c>false</c>.
        /// </value>
        public bool IsTestFailure
        {
            get { return _isTestFailure; }
            set
            {
                if (_isTestFailure != value)
                {
                    _isTestFailure = value;
                    NotifyOfPropertyChange(() => IsTestFailure);
                }
            }
        }

        /// <summary>
        /// Executes a connection test and reports the result to the user.
        /// </summary>
        public void TestConnection(System.Action callback = null)
        {
            IsTestSuccess = false;
            IsTestFailure = false;

            _log.DebugFormat("Testing a {0} connection", ConnectionType);

            // Resolve a connection test specific to the current ConnectionType
            var connectionTest = App.Current.Container().Resolve<IConnectionTest>(ConnectionType.ToString());

            if (connectionTest != null)
            {
                UiServices.SetBusyState();
                CanTestConnection = false;

                Task.Run(async() =>
                {
                    var result = await connectionTest.CanConnect(EditItem);
                    await App.Current.Dispatcher.BeginInvoke(new Action<bool>(ShowTestResult), result);
                    if (callback != null)
                    {
                        callback();
                    }
                });
            }
        }

        /// <summary>
        /// Called when [password changed].
        /// </summary>
        /// <param name="control">The control.</param>
        public void OnPasswordChanged(PasswordBox control)
        {
            EditItem.SecurePassword = control.SecurePassword;
        }

        /// <summary>
        /// Called when [URL changed] to trim the start of the Uri string.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void OnUrlChanged(string uri)
        {
            if (EditItem.Uri != uri.TrimStart())
            {
                EditItem.Uri = uri.TrimStart();
            }
        }

        /// <summary>
        /// Accepts the edited connection by assigning all changes 
        /// from the EditItem to the DataItem and persisting the changes.
        /// </summary>
        public void Accept()
        {
            TestConnection(() => 
            {
                if (IsTestSuccess)
                {
                    _log.Debug("Connection changes accepted");
                    Mapper.Map(EditItem, DataItem);
                    SaveConnectionFile(DataItem);
                    TryClose(true);
                }
            });
        }

        /// <summary>
        /// Cancels the edited connection.  
        /// Changes are not persisted or passed back to the caller.
        /// </summary>
        public void Cancel()
        {
            _log.Debug("Connection changes canceled");
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
                _log.DebugFormat("Reading persisted Connection from '{0}'", filename);
                var json = File.ReadAllText(filename);
                return JsonConvert.DeserializeObject<Connection>(json);
            }

            return null;
        }

        /// <summary>
        /// Saves a Connection instance to a JSON file for the current connection type.
        /// </summary>
        /// <param name="connection">The connection instance being saved.</param>
        internal void SaveConnectionFile(Connection connection)
        {
            EnsureDataFolder();
            string filename = GetConnectionFilename();
            _log.DebugFormat("Persisting Connection to '{0}'", filename);
            File.WriteAllText(filename, JsonConvert.SerializeObject(connection));
        }

        /// <summary>
        /// Gets the connection filename.
        /// </summary>
        /// <returns>The path and filename for the connection file with format "[data-folder]/[connection-type]ConnectionData.json".</returns>
        internal string GetConnectionFilename()
        {
            return string.Format("{0}/{1}/{2}{3}", 
                Environment.CurrentDirectory, 
                PersistedDataFolderName, 
                ConnectionType, 
                ConnectionBaseFileName);
        }

        /// <summary>
        /// Initializes the EditItem property.
        ///     1) Clones the incoming DataItem, if provided, to the EditItem to use as a working copy.
        ///     2) If a DataItem was not provided the EditItem is set using the persisted connection
        ///     data for the current connection type.
        ///     3) If there is no persisted connection data then the EditItem is set to a blank connection.
        /// </summary>
        internal void InitializeEditItem()
        {
            if (DataItem != null && !string.IsNullOrWhiteSpace(DataItem.Uri))
            {
                EditItem = Mapper.Map(DataItem, new Connection());
            }
            else
            {
                EditItem = OpenConnectionFile() ?? new Connection();
            }
        }

        /// <summary>
        /// When the screen is activated the EditItem is initialized.
        /// </summary>
        protected override void OnActivate()
        {
            base.OnActivate();

            InitializeEditItem();
        }

        /// <summary>
        /// Checks for the existance of the data folder and creates it if necessary.
        /// </summary>
        private void EnsureDataFolder()
        {
            var dataFolder = string.Format("{0}/{1}", Environment.CurrentDirectory, PersistedDataFolderName);
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }
        }

        /// <summary>
        /// Shows the test result for the connection test.
        /// </summary>
        /// <param name="result">if set to <c>true</c> [result].</param>
        private void ShowTestResult(bool result)
        {
            _log.Debug(result ? "Connection successful" : "Connection failed");

            if (result)
            {
                IsTestSuccess = true;
            }
            else
            {
                IsTestFailure = true;
            }

            CanTestConnection = true;
        }
    }
}
