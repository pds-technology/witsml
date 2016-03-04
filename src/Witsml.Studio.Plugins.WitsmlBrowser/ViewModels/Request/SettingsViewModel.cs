using System;
using System.Collections.Generic;
using Caliburn.Micro;
using Energistics.DataAccess;
using PDS.Witsml.Studio.Connections;
using PDS.Witsml.Studio.Runtime;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    public class SettingsViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(SettingsViewModel));

        public SettingsViewModel(IRuntimeService runtime)
        {
            _log.Debug("Creating view model instance");
            Runtime = runtime;
            DisplayName = "Settings";
            WitsmlVersions = new BindableCollection<string>();
        }

        public new RequestViewModel Parent
        {
            get { return (RequestViewModel)base.Parent; }
        }

        public WITSMLWebServiceConnection Proxy
        {
            get { return Parent.Proxy; }
        }

        public Models.WitsmlSettings Model
        {
            get { return Parent.Model; }
        }

        public IRuntimeService Runtime { get; private set; }

        public BindableCollection<string> WitsmlVersions { get; }

        public IEnumerable<OptionsIn.ReturnElements> ReturnElements
        {
            get
            {
                return OptionsIn.ReturnElements.GetValues();
            }
        }

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

        public void GetCapabilities()
        {
            Parent.Parent.GetCapabilities();
        }

        internal string GetVersions(WITSMLWebServiceConnection proxy, string uri)
        {
            proxy.Url = uri;
            var supportedVersions = proxy.GetVersion();
            _log.DebugFormat("Supported versions '{0}' found on WITSML server with uri '{1}'", supportedVersions, uri);

            return supportedVersions;
        }

        protected override void OnInitialize()
        {
            _log.Debug("Initializing screen");
            base.OnInitialize();
            Model.ReturnElementType = OptionsIn.ReturnElements.All;
        }

        private void GetVersions()
        {
            _log.Debug("Selecting supported versions from WITSML server.");
            try
            {
                WitsmlVersions.Clear();
                var versions = GetVersions(Proxy, Model.Connection.Uri);
                if (!string.IsNullOrEmpty(versions))
                {
                    WitsmlVersions.AddRange(versions.Split(','));
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
                
                _log.Error(errorMessage, ex);
                Runtime.ShowError(errorMessage);
            }
        }
    }
}
