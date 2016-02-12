using System;
using System.Windows;
using PDS.Framework;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio
{
    public static class AppExtensions
    {
        public static IContainer Container(this Application app)
        {
            return ((Bootstrapper)app.Resources["bootstrapper"]).Container;
        }

        public static IShellViewModel Shell(this Application app)
        {
            return app.MainWindow.DataContext as IShellViewModel;
        }

        public static void ShowError(this Application app, string message, Exception error = null)
        {
#if DEBUG
            if (error != null)
            {
                message = string.Format("{0}{2}{2}{1}", message, error.Message, Environment.NewLine);
            }
#endif
            MessageBox.Show(Application.Current.MainWindow, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static bool ShowConfirm(this Application app, string message, MessageBoxButton buttons = MessageBoxButton.OKCancel)
        {
            var result = MessageBox.Show(Application.Current.MainWindow, message, "Confirm", buttons, MessageBoxImage.Question);
            return (result == MessageBoxResult.OK || result == MessageBoxResult.Yes);
        }
    }
}
