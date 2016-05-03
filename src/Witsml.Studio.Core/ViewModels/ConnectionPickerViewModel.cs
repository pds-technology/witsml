//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Caliburn.Micro;
using Newtonsoft.Json;
using PDS.Framework;
using PDS.Witsml.Studio.Core.Connections;
using PDS.Witsml.Studio.Core.Properties;
using PDS.Witsml.Studio.Core.Runtime;

namespace PDS.Witsml.Studio.Core.ViewModels
{
    /// <summary>
    /// Manages the behavior of the connection drop down list control.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class ConnectionPickerViewModel : Screen
    {
        private static readonly Connection SelectConnectionItem = new Connection { Name = "Select Connection..." };
        private static readonly Connection AddNewConnectionItem = new Connection { Name = "(Add New Connection...)" };
        private static readonly string PersistedDataFolderName = Settings.Default.PersistedDataFolderName;
        private static readonly string ConnectionListBaseFileName = Settings.Default.ConnectionListBaseFileName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPickerViewModel" /> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        /// <param name="connectionType">The connection type.</param>
        public ConnectionPickerViewModel(IRuntimeService runtime, ConnectionTypes connectionType)
        {
            Runtime = runtime;
            ConnectionType = connectionType;
            Connections = new BindableCollection<Connection>();
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the type of the connection.
        /// </summary>
        /// <value>The type of the connection.</value>
        public ConnectionTypes ConnectionType { get; }

        /// <summary>
        /// Gets or sets the delegate that will be invoked when the selected connection changes.
        /// </summary>
        /// <value>The delegate that will be invoked.</value>
        public Action<Connection> OnConnectionChanged { get; set; }

        /// <summary>
        /// Gets the collection of connections.
        /// </summary>
        /// <value>The collection of connections.</value>
        public BindableCollection<Connection> Connections { get; } 

        private Connection _connection;

        /// <summary>
        /// Gets or sets the selectected connection.
        /// </summary>
        /// <value>The selected connection.</value>
        public Connection Connection
        {
            get { return _connection; }
            set
            {
                if (!ReferenceEquals(_connection, value))
                {
                    var previous = _connection;
                    _connection = value;
                    NotifyOfPropertyChange(() => Connection);
                    Runtime.Invoke(() => OnSelectedConnectionChanged(previous));
                }
            }
        }

        /// <summary>
        /// Shows the connection dialog to add or update connection settings.
        /// </summary>
        public void ShowConnectionDialog(Connection connection = null)
        {
            var viewModel = new ConnectionViewModel(Runtime, ConnectionType)
            {
                DataItem = connection ?? new Connection(),
            };

            if (Runtime.ShowDialog(viewModel))
            {
                // Ensure connection has a Name specified
                if (string.IsNullOrWhiteSpace(viewModel.DataItem.Name))
                    viewModel.DataItem.Name = viewModel.DataItem.Uri;

                // Initialize collection of new connection items
                var connections = (connection == null)
                    ? new[] { viewModel.DataItem }
                    : new Connection[0];

                // Reset Connections list
                connection = connection ?? viewModel.DataItem;
                InsertConnections(connections, connection);
            }
        }

        public void EditConnection(Connection connection, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ShowConnectionDialog(connection);
        }

        protected override void OnViewLoaded(object view)
        {
            if (Connections.Any()) return;

            var connections = LoadConnectionsFromFile();
            InsertConnections(connections, SelectConnectionItem);
        }

        private void OnSelectedConnectionChanged(Connection previous)
        {
            if (Connection == SelectConnectionItem) return;

            if (Connection == AddNewConnectionItem)
            {
                _connection = previous;
                NotifyOfPropertyChange(() => Connection);
                ShowConnectionDialog();
                return;
            }

            // Invoke delegate that will handle the connection change
            OnConnectionChanged?.Invoke(Connection);
        }

        private void InsertConnections(IEnumerable<Connection> connections, Connection selected)
        {
            var list = Connections
                .Skip(1)
                .Take(Connections.Count - 2)
                .Concat(connections)
                .OrderBy(x => x.Name)
                .ToList();

            if (Connections.Any())
            {
                SaveConnectionsToFile(list);
            }

            Connections.Clear();
            Connections.Add(SelectConnectionItem);
            Connections.AddRange(list);
            Connections.Add(AddNewConnectionItem);
            Connection = selected;
        }

        private IEnumerable<Connection> LoadConnectionsFromFile()
        {
            var fileName = GetConnectionFileName();

            if (File.Exists(fileName))
            {
                //_log.DebugFormat("Reading persisted Connection from '{0}'", filename);
                var json = File.ReadAllText(fileName);
                var connections = JsonConvert.DeserializeObject<List<Connection>>(json);

                connections.ForEach(x =>
                {
                    x.Password = x.Password.Decrypt();
                    x.SecurePassword = x.Password.ToSecureString();
                });

                return connections;
            }

            return new Connection[0];
        } 

        private void SaveConnectionsToFile(List<Connection> connections)
        {
            EnsureDataFolder();
            var fileName = GetConnectionFileName();
            //_log.DebugFormat("Persisting Connection to '{0}'", filename);
            connections.ForEach(x => x.Password = x.Password.Encrypt());
            File.WriteAllText(fileName, JsonConvert.SerializeObject(connections));
            connections.ForEach(x => x.Password = x.Password.Decrypt());
        }

        /// <summary>
        /// Gets the connection list file name.
        /// </summary>
        /// <returns>The path and file name for the connection list file with format "[data-folder]/[connection-type]ConnectionList.json".</returns>
        internal string GetConnectionFileName()
        {
            return string.Format("{0}/{1}/{2}{3}",
                Environment.CurrentDirectory,
                PersistedDataFolderName,
                ConnectionType,
                ConnectionListBaseFileName);
        }

        /// <summary>
        /// Checks for the existance of the data folder and creates it if necessary.
        /// </summary>
        private void EnsureDataFolder()
        {
            var directory = new DirectoryInfo(PersistedDataFolderName);
            Directory.CreateDirectory(directory.FullName);
        }
    }
}
