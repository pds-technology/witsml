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
    /// Defines properties and methods that can be used to interact with the current runtime environment (e.g. Desktop, Console, Test, etc.).
    /// </summary>
    public interface IRuntimeService
    {
        /// <summary>
        /// Gets a reference to the composition container used for dependency injection.
        /// </summary>
        IContainer Container { get; }

        /// <summary>
        /// Gets a reference the root application shell.
        /// </summary>
        /// <value>The application shell.</value>
        IShellViewModel Shell { get; }

        /// <summary>
        /// Gets a reference to a Caliburn WindowManager.
        /// </summary>
        /// <value>The window manager.</value>
        IWindowManager WindowManager { get; }

        /// <summary>
        /// Shows the error message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        void ShowError(string message, Exception error = null);

        /// <summary>
        /// Shows the confirmation message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="buttons">The buttons.</param>
        /// <returns><c>true</c> if the user clicks OK/Yes; otherwise, <c>false</c>.</returns>
        bool ShowConfirm(string message, MessageBoxButton buttons = MessageBoxButton.OKCancel);

        /// <summary>
        /// Shows the information message.
        /// </summary>
        /// <param name="message">The message.</param>
        void ShowInfo(string message);

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <returns>The view model dialog's result.</returns>
        bool ShowDialog(object viewModel);

        /// <summary>
        /// Invokes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        void Invoke(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal);

        /// <summary>
        /// Invokes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        Task InvokeAsync(System.Action action, DispatcherPriority priority = DispatcherPriority.Normal);
    }
}
