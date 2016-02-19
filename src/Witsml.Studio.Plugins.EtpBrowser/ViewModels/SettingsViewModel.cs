using System.Windows;
using Caliburn.Micro;
using PDS.Witsml.Studio.Models;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Models;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.ViewModels
{
    public class SettingsViewModel : Screen
    {
        public SettingsViewModel()
        {
            DisplayName = "Settings";
        }

        public EtpSettings Model
        {
            get { return ((MainViewModel)Parent).Model; }
        }

        public void ShowConnectionDialog()
        {
            var viewModel = new ConnectionViewModel(ConnectionTypes.Etp)
            {
                Connection = Model.Connection
            };

            if (App.Current.ShowDialog(viewModel))
            {
                Model.Connection = viewModel.Connection;

                ((MainViewModel)Parent).OnConnectionChanged();
            }
        }
    }
}
