using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using PDS.Framework;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Runtime
{
    /// <summary>
    /// Provides an implementation of <see cref="IRuntimeService"/> that can be used from within desktop applications.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Studio.Runtime.IRuntimeService" />
    public class DesktopRuntimeService : IRuntimeService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DesktopRuntimeService"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public DesktopRuntimeService(IContainer container)
        {
            Container = container;
        }

        /// <summary>
        /// Gets a reference to the composition container used for dependency injection.
        /// </summary>
        public IContainer Container { get; private set; }

        /// <summary>
        /// Gets a reference the root application shell.
        /// </summary>
        /// <value>The application shell.</value>
        public IShellViewModel Shell
        {
            get { return Application.Current.MainWindow.DataContext as IShellViewModel; }
        }

        /// <summary>
        /// Gets a reference to a Caliburn WindowManager.
        /// </summary>
        /// <value>The window manager.</value>
        public IWindowManager WindowManager
        {
            get { return Container.Resolve<IWindowManager>(); }
        }

        /// <summary>
        /// Invokes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        public void Invoke(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            Application.Current.Dispatcher.BeginInvoke(action, priority);
        }

        /// <summary>
        /// Invokes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        /// <returns>An awaitable <see cref="Task" />.</returns>
        public async Task InvokeAsync(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            await Application.Current.Dispatcher.BeginInvoke(action, priority);
        }

        /// <summary>
        /// Shows the confirmation message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="buttons">The buttons.</param>
        /// <returns><c>true</c> if the user clicks OK/Yes; otherwise, <c>false</c>.</returns>
        public bool ShowConfirm(string message, MessageBoxButton buttons = MessageBoxButton.OKCancel)
        {
            var result = MessageBox.Show(Application.Current.MainWindow, message, "Confirm", buttons, MessageBoxImage.Question);
            return (result == MessageBoxResult.OK || result == MessageBoxResult.Yes);
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <returns>The view model dialog's result.</returns>
        public bool ShowDialog(object viewModel)
        {
            return WindowManager.ShowDialog(viewModel).GetValueOrDefault();
        }

        /// <summary>
        /// Shows the error message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        public void ShowError(string message, Exception error = null)
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
        /// Shows the information message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void ShowInfo(string message)
        {
            MessageBox.Show(Application.Current.MainWindow, message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
