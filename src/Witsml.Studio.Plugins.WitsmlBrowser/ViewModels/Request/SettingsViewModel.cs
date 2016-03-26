using System;
using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using Energistics.DataAccess;
using PDS.Witsml.Studio.Connections;
using PDS.Witsml.Studio.Runtime;
using PDS.Witsml.Studio.ViewModels;

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
        public IRuntimeService Runtime { get; private set; }

        /// <summary>
        /// Gets the witsml versions retrieved from the server.
        /// </summary>
        /// <value>
        /// The server's supported witsml versions.
        /// </value>
        public BindableCollection<string> WitsmlVersions { get; }

        /// <summary>
        /// Gets the options in return elements.
        /// </summary>
        /// <value>
        /// The options in return elements.
        /// </value>
        public IEnumerable<OptionsIn.ReturnElements> ReturnElements
        {
            get { return OptionsIn.ReturnElements.GetValues(); }
        }

        /// <summary>
        /// Shows the connection dialog to add or update connection settings.
        /// </summary>
        public void ShowConnectionDialog()
        {
            var viewModel = new ConnectionViewModel(Runtime, ConnectionTypes.Witsml)
            {
                DataItem = Model.Connection,
            };

            _log.Debug("Opening connection dialog");

            if (Runtime.ShowDialog(viewModel))
            {
                Model.Connection = viewModel.DataItem;

                _log.DebugFormat("Connection details updated from dialog:{3}Name: {0}{3}Uri: {1}{3}Username: {2}{3}{3}", 
                    Model.Connection.Name, Model.Connection.Uri, Model.Connection.Username, Environment.NewLine);

                // Make connection and get version
                GetVersions();
            }
        }

        /// <summary>
        /// Gets the capabilities from the server.
        /// </summary>
        public void GetCapabilities()
        {
            Parent.Parent.GetCapabilities();
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
