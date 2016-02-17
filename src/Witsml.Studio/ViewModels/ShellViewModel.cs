using System;
using System.Linq;
using System.Windows;
using Caliburn.Micro;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// The view model for the application shell.
    /// </summary>
    public class ShellViewModel : Conductor<IScreen>.Collection.OneActive, IShellViewModel
    {
        // TODO: Figure out why log4net is not logging for a Conductor class.
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ShellViewModel));

        private string _breadcrumbText;
        private string _statusBarText;

        /// <summary>
        /// Initialize an instance of the ShellViewModel
        /// </summary>
        public ShellViewModel()
        {
            _log.Debug("Loading Shell");

            DisplayName = "WITSML Studio";
            StatusBarText = "Ready.";
        }

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

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);

            Items.AddRange(Application.Current.Container()
                .ResolveAll<IPluginViewModel>()
                .OrderBy(x => x.DisplayOrder));

            if (_log.IsDebugEnabled)
            {
                _log.DebugFormat("{0}{1}{2}", "Plugins Loaded:", Environment.NewLine, string.Join(Environment.NewLine, Items.Select(x => x.DisplayName)));
            }

            ActivateItem(Items.FirstOrDefault());
        }

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
