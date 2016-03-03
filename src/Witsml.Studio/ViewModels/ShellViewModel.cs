using System;
using System.ComponentModel.Composition;
using System.Linq;
using Caliburn.Micro;
using PDS.Witsml.Studio.Runtime;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// Manages the main application user interface
    /// </summary>
    public class ShellViewModel : Conductor<IScreen>.Collection.OneActive, IShellViewModel
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ShellViewModel));

        /// <summary>
        /// Initializes an instance of the ShellViewModel
        /// </summary>
        [ImportingConstructor]
        public ShellViewModel(IRuntimeService runtime)
        {
            _log.Debug("Loading Shell");

            Runtime = runtime;
            DisplayName = "WITSML Studio";
            StatusBarText = "Ready.";
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service instance.</value>
        public IRuntimeService Runtime { get; private set; }

        private string _breadcrumbText;

        /// <summary>
        /// Gets or sets the breadcrumb path for the application shell
        /// </summary>
        public string BreadcrumbText
        {
            get { return _breadcrumbText; }
            set
            {
                if (!string.Equals(_breadcrumbText, value))
                {
                    _breadcrumbText = value;
                    NotifyOfPropertyChange(() => BreadcrumbText);
                }
            }
        }

        private string _statusBarText;

        /// <summary>
        /// Gets or sets the status bar text for the application shell
        /// </summary>
        public string StatusBarText
        {
            get { return _statusBarText; }
            set
            {
                if (!string.Equals(_statusBarText, value))
                {
                    _statusBarText = value;
                    NotifyOfPropertyChange(() => StatusBarText);
                }
            }
        }

        /// <summary>
        /// Exits the application.
        /// </summary>
        public void Exit()
        {
            App.Current.Shutdown();
        }

        /// <summary>
        /// Shows the About dialog for the application.
        /// </summary>
        public void About()
        {
            Runtime.ShowInfo("WITSML Studio v0.1");
        }

        /// <summary>
        /// Loads the plug-ins.
        /// </summary>
        internal void LoadPlugins()
        {
            Items.AddRange(Runtime.Container
                .ResolveAll<IPluginViewModel>()
                .OrderBy(x => x.DisplayOrder));

            if (_log.IsDebugEnabled)
            {
                _log.DebugFormat("{0}{1}{2}", "Plugins Loaded:", Environment.NewLine, string.Join(Environment.NewLine, Items.Select(x => x.DisplayName)));
            }

            ActivateItem(Items.FirstOrDefault());
        }

        /// <summary>
        /// Called the first time the page's LayoutUpdated event fires after it is navigated to.
        /// </summary>
        /// <param name="view">The view attached to the current view model.</param>
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            LoadPlugins();
        }

        /// <summary>
        /// Called by a subclass when an activation needs processing.
        /// </summary>
        /// <param name="item">The item on which activation was attempted.</param>
        /// <param name="success">if set to <c>true</c> activation was successful.</param>
        protected override void OnActivationProcessed(IScreen item, bool success)
        {
            base.OnActivationProcessed(item, success);

            if (item != null && success)
            {
                BreadcrumbText = item.DisplayName;
            }
        }
    }
}
