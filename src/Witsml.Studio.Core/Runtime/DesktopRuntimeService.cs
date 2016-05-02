//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Caliburn.Micro;
using PDS.Framework;
using PDS.Witsml.Studio.Core.ViewModels;

namespace PDS.Witsml.Studio.Core.Runtime
{
    /// <summary>
    /// Provides an implementation of <see cref="IRuntimeService"/> that can be used from within desktop applications.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Studio.Core.Runtime.IRuntimeService" />
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
            get { return Application.Current.MainWindow?.DataContext as IShellViewModel; }
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
        /// Shows the busy indicator cursor.
        /// </summary>
        /// <param name="isBusy">if set to <c>true</c>, shows the busy indicator.</param>
        public void ShowBusy(bool isBusy = true)
        {
            Invoke(() => Mouse.OverrideCursor = isBusy ? Cursors.Wait : null);
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
            var settings = new Dictionary<string, object>()
            {
                { "WindowStartupLocation", WindowStartupLocation.CenterOwner }
            };

            return WindowManager.ShowDialog(viewModel, null, settings).GetValueOrDefault();
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
