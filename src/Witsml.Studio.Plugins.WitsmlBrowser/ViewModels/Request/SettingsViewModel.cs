using System;
using System.Collections.Generic;
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

        public IWindowManager WindowManager
        {
            get { return ((RequestViewModel)Parent).WindowManager; }
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
            var viewModel = new ConnectionViewModel();

            // TODO: Move to App Extension so we don't have to resolve WindowManager each time.  
            //... Return boolean instead of nullable to avoid GetValueOrDefault()
            if (WindowManager.ShowDialog(viewModel).GetValueOrDefault())
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
                WitsmlVersions.Clear();
                Proxy.Url = Model.Connection.Uri;
                var versions = Proxy.GetVersion();
                if (!string.IsNullOrEmpty(versions))
                {
                    WitsmlVersions.AddRange(versions.Split(','));
                }
                else
                {
                    App.Current.ShowError("The Witsml server does not support any versions.");
                }
            }
            catch (Exception ex)
            {
                App.Current.ShowError("The connection URL entered may not be valid. Re-enter a new connection.", ex);
            }
        }
    }
}
