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
    /// Provides an implementation of <see cref="IRuntimeService"/> that can be used from within unit/integation tests.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Studio.Runtime.IRuntimeService" />
    public class TestRuntimeService : IRuntimeService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestRuntimeService"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public TestRuntimeService(IContainer container)
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
        public IShellViewModel Shell { get; set; }

        /// <summary>
        /// Gets a reference to a Caliburn WindowManager.
        /// </summary>
        /// <value>The window manager.</value>
        public IWindowManager WindowManager { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the value to be returned by the ShowConfirm and ShowDialog methods.
        /// </summary>
        /// <value><c>true</c> if ShowConfirm and ShowDialog should return <c>true</c>; otherwise, <c>false</c>.</value>
        public bool DialogResult { get; set; }

        /// <summary>
        /// Invokes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        public void Invoke(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            action();
        }

        /// <summary>
        /// Invokes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        /// <returns>An awaitable <see cref="Task" />.</returns>
        public async Task InvokeAsync(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            await Task.Delay(250);
            action();
        }

        /// <summary>
        /// Shows the confirmation message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="buttons">The buttons.</param>
        /// <returns><c>true</c> if the user clicks OK/Yes; otherwise, <c>false</c>.</returns>
        public bool ShowConfirm(string message, MessageBoxButton buttons = MessageBoxButton.OKCancel)
        {
            Console.WriteLine("Confirm: {0}", message);
            return DialogResult;
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <returns>The view model dialog's result.</returns>
        public bool ShowDialog(object viewModel)
        {
            Console.WriteLine("ShowDialog: {0}", viewModel);
            return DialogResult;
        }

        /// <summary>
        /// Shows the error message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        public void ShowError(string message, Exception error = null)
        {
            Console.WriteLine("ShowError: {0}{1}{2}", message, Environment.NewLine, error);
        }

        /// <summary>
        /// Shows the information message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void ShowInfo(string message)
        {
            Console.WriteLine("ShowInfo: {0}", message);
        }
    }
}
