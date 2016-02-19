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
            // TODO: Remove (Task 4465)
            var windowManager = Application.Current.Container().Resolve<IWindowManager>();

            var viewModel = new ConnectionViewModel(ConnectionTypes.Etp)
            {
                Connection = Model.Connection
            };

            // TODO: Replace with App.ShowDialog (Task 4465)
            if (windowManager.ShowDialog(viewModel).GetValueOrDefault())
            {
                Model.Connection = viewModel.Connection;

                ((MainViewModel)Parent).OnConnectionChanged();
            }
        }
    }
}
