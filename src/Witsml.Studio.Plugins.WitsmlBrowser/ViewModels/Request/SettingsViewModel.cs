using System;
using System.Collections.Generic;
using Caliburn.Micro;
using Energistics.DataAccess;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    public class SettingsViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(SettingsViewModel));

        public SettingsViewModel()
        {
            DisplayName = "Settings";
            WitsmlVersions = new BindableCollection<string>();
        }

        public RequestViewModel ParentViewModel
        {
            get { return (RequestViewModel)Parent; }
        }

        public WITSMLWebServiceConnection Proxy
        {
            get { return ((RequestViewModel)Parent).Proxy; }
        }

        public Models.Browser Model
        {
            get { return ParentViewModel.Model; }
        }

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
            var viewModel = new ConnectionViewModel()
            {
                Connection = Model.Connection
            };


            if (App.Current.ShowDialog(viewModel))
            {
                Model.Connection = viewModel.Connection;

                // TODO: Make connection and get version
                GetVersions();

                // TODO: GetCap
                // TODO: GetWells for the TreeView
            }
        }

        private void GetVersions()
        {
            try
            {
                WitsmlVersions.Clear();
                Proxy.Url = Model.Connection.Uri;
                var versions = Proxy.GetVersion();
                if (!string.IsNullOrEmpty(versions))
                {
                    WitsmlVersions.AddRange(versions.Split(','));
                    _log.DebugFormat("WitsmlVersions fetched {0}", versions);
                }
                else
                {
                    var msg = "The Witsml server does not support any versions.";
                    _log.Warn(msg);
                    App.Current.ShowError(msg);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = "The connection URL entered may not be valid. Re-enter a new connection.";

                _log.Error(errorMessage, ex);
                App.Current.ShowError(errorMessage, ex);
            }
        }
    }
}
