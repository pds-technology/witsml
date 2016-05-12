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
using System.IO;
using System.Linq;
using System.Windows;
using Caliburn.Micro;
using Energistics.DataAccess;
using PDS.Witsml.Studio.Core.Connections;
using PDS.Witsml.Studio.Core.Runtime;
using PDS.Witsml.Studio.Core.ViewModels;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    /// <summary>
    /// Manages the behavior for the Settings view UI elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class SettingsViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(SettingsViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        public SettingsViewModel(IRuntimeService runtime)
        {
            _log.Debug("Creating view model instance");
            Runtime = runtime;
            DisplayName = "Settings";
            WitsmlVersions = new BindableCollection<string>();
            ConnectionPicker = new ConnectionPickerViewModel(runtime, ConnectionTypes.Witsml)
            {
                OnConnectionChanged = OnConnectionChanged
            };
        }

        /// <summary>
        /// Gets the Parent <see cref="T:Caliburn.Micro.IConductor" /> for this view model
        /// </summary>
        public new RequestViewModel Parent
        {
            get { return (RequestViewModel)base.Parent; }
        }

        /// <summary>
        /// Gets the proxy for the WITSML web service.
        /// </summary>
        /// <value>
        /// The WITSML seb service proxy.
        /// </value>
        public WITSMLWebServiceConnection Proxy
        {
            get { return Parent.Proxy; }
        }

        /// <summary>
        /// Gets or sets the data model.
        /// </summary>
        /// <value>
        /// The WitsmlSettings data model.
        /// </value>
        public Models.WitsmlSettings Model
        {
            get { return Parent.Model; }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the connection picker view model.
        /// </summary>
        /// <value>The connection picker view model.</value>
        public ConnectionPickerViewModel ConnectionPicker { get; }

        /// <summary>
        /// Gets the witsml versions retrieved from the server.
        /// </summary>
        /// <value>
        /// The server's supported witsml versions.
        /// </value>
        public BindableCollection<string> WitsmlVersions { get; }

        /// <summary>
        /// Gets the capabilities from the server.
        /// </summary>
        public void GetCapabilities()
        {
            Parent.Parent.GetCapabilities();
        }

        /// <summary>
        /// Selects the output path.
        /// </summary>
        public void SelectOutputPath()
        {
            var info = new DirectoryInfo(Model.OutputPath);
            var owner = new Win32WindowHandle(Application.Current.MainWindow);
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Output Path",
                SelectedPath = info.FullName,
                ShowNewFolderButton = true,
            };

            if (dialog.ShowDialog(owner) == System.Windows.Forms.DialogResult.OK)
            {
                Model.OutputPath = dialog.SelectedPath;
            }
        }

        /// <summary>
        /// Gets the supported versions crom the server.
        /// </summary>
        /// <param name="proxy">The proxy.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        internal string GetVersions(WITSMLWebServiceConnection proxy, Connection connection)
        {
            proxy.Url = connection.Uri;

            if (!string.IsNullOrWhiteSpace(Model.Connection.Username))
            {
                proxy.Username = connection.Username;
                proxy.SetSecurePassword(connection.SecurePassword);
            }

            var supportedVersions = proxy.GetVersion();
            _log.DebugFormat("Supported versions '{0}' found on WITSML server with uri '{1}'", supportedVersions, connection.Uri);

            return supportedVersions;
        }

        /// <summary>
        /// Called when initializing the SettingsViewModel.
        /// </summary>
        protected override void OnInitialize()
        {
            _log.Debug("Initializing screen");
            base.OnInitialize();
            Model.ReturnElementType = OptionsIn.ReturnElements.All;
        }

        private void OnConnectionChanged(Connection connection)
        {
            Model.Connection = connection;

            _log.DebugFormat("Selected connection changed: Name: {0}; Uri: {1}; Username: {2}",
                Model.Connection.Name, Model.Connection.Uri, Model.Connection.Username);

            // Make connection and get version
            Runtime.ShowBusy();
            Runtime.InvokeAsync(() =>
            {
                Runtime.ShowBusy(false);
                GetVersions();
            });
        }

        /// <summary>
        /// Gets the supported versions from the server and initializes the UI element for version selection.
        /// </summary>
        private void GetVersions()
        {
            _log.Debug("Selecting supported versions from WITSML server.");
            try
            {
                WitsmlVersions.Clear();
                var versions = GetVersions(Proxy, Model.Connection);

                if (!string.IsNullOrEmpty(versions))
                {
                    WitsmlVersions.AddRange(versions.Split(','));
                    Model.WitsmlVersion = WitsmlVersions.Last();
                }
                else
                {
                    var msg = "The Witsml server does not support any versions.";
                    _log.Warn(msg);
                    Runtime.ShowError(msg);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format("{0}{1}{1}{2}", "Error connecting to server.", Environment.NewLine, "Invalid URL");
                
                // Log the error
                _log.Error(errorMessage, ex);

                // Show the user the error in a dialog.
                Runtime.ShowError(errorMessage, ex);
            }
        }
    }
}
