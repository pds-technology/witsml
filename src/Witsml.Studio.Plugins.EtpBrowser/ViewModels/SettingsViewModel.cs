using System.Windows;
using Caliburn.Micro;
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
            var windowManager = Application.Current.Container().Resolve<IWindowManager>();

            var viewModel = new ConnectionViewModel()
            {
                Connection = Model.Connection
            };

            if (windowManager.ShowDialog(viewModel).GetValueOrDefault())
            {
                Model.Connection = viewModel.Connection;

                ((MainViewModel)Parent).OnConnectionChanged();
            }
        }
    }
}
