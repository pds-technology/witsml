using System;
using Caliburn.Micro;
using Energistics.DataAccess;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    public class SettingsViewModel : Screen
    {
        public SettingsViewModel()
        {
            DisplayName = "Settings";
        }

        public Models.Browser Model
        {
            get { return ((RequestViewModel)Parent).Model; }
        }

        public WITSMLWebServiceConnection Proxy { get; private set; }

        public void ShowConnectionDialog()
        {
            var viewModel = new ConnectionViewModel();

            if (Model.WindowManager.ShowDialog(viewModel).GetValueOrDefault())
            {
                Model.Connection = viewModel.Connection;

                // TODO: Make connection and get version
                GetVersions();

                // TODO: GetCap
                // TODO: GetWells
            }
        }

        private void GetVersions()
        {
            try
            {
                Model.WitsmlVersions.Clear();
                Proxy = new WITSMLWebServiceConnection(Model.Connection.Uri, WMLSVersion.WITSML141);
                var versions = Proxy.GetVersion();
                if (!string.IsNullOrEmpty(versions))
                {
                    Model.WitsmlVersions.AddRange(versions.Split(','));
                }
                else
                {
                    App.Current.ShowError("The Witsml server does not support any versions.");
                }
                Model.NotifyOfPropertyChange(() => Model.HasWitsmlVersions);
            }
            catch(Exception ex)
            {
                App.Current.ShowError("The connection URL entered may not be valid. Re-enter a new connection.", ex);
            }
        }
    }
}
