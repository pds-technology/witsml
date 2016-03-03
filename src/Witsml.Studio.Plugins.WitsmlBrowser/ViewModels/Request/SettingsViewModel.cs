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


            if (Runtime.ShowDialog(viewModel))
            {
                Model.Connection = viewModel.DataItem;

                // Make connection and get version
                GetVersions();

                // TODO: GetCap
                // TODO: GetWells for the TreeView
            }
        }

        public void GetCapabilities()
        {
            Parent.Parent.GetCapabilities();
        }

        internal string GetVersions(WITSMLWebServiceConnection proxy, string uri)
        {
            proxy.Url = uri;
            return proxy.GetVersion();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            Model.ReturnElementType = OptionsIn.ReturnElements.All;
        }

        private void GetVersions()
        {
            try
            {
                WitsmlVersions.Clear();
                var versions = GetVersions(Proxy, Model.Connection.Uri);
                if (!string.IsNullOrEmpty(versions))
                {
                    WitsmlVersions.AddRange(versions.Split(','));
                    _log.DebugFormat("WitsmlVersions fetched {0}", versions);
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
