using System;
using System.Windows;
using PDS.Framework;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio
{
    /// <summary>
    /// Static extension class to reference application level properties and functionality.
    /// </summary>
    public static class AppExtensions
    {
        /// <summary>
        /// An extension method to reference the application IoC container
        /// </summary>
        /// <param name="app">The reference to the application</param>
        /// <returns>The application IoC container</returns>
        public static IContainer Container(this Application app)
        {
            return ((Bootstrapper)app.Resources["bootstrapper"]).Container;
        }

        /// <summary>
        /// An extension method to reference the application shell
        /// </summary>
        /// <param name="app">The reference to the application</param>
        /// <returns>The application shell</returns>
        public static IShellViewModel Shell(this Application app)
        {
            return app.MainWindow.DataContext as IShellViewModel;
        }

        /// <summary>
        /// An extension method to display an error message in a Windows MessageBox
        /// </summary>
        /// <param name="app">The reference to the application</param>
        /// <param name="message">The error message to be displayed</param>
        /// <param name="error">An optional Exception parameter</param>
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

        /// <summary>
        /// An extension method to display a confirmation message in a Windows MessageBox
        /// </summary>
        /// <param name="app">The reference to the application</param>
        /// <param name="message">The confirmation message to be displayed</param>
        /// <param name="buttons">The Windows MessageBoxButton type for the dialog buttons</param>
        /// <returns>Returns true of the MessageBoxResult is OK or Yes, false otherwise</returns>
        public static bool ShowConfirm(this Application app, string message, MessageBoxButton buttons = MessageBoxButton.OKCancel)
        {
            var result = MessageBox.Show(Application.Current.MainWindow, message, "Confirm", buttons, MessageBoxImage.Question);
            return (result == MessageBoxResult.OK || result == MessageBoxResult.Yes);
        }
    }
}
