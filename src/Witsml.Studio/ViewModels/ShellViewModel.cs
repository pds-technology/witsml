using System.Linq;
using System.Windows;
using Caliburn.Micro;

namespace PDS.Witsml.Studio.ViewModels
{
    public class ShellViewModel : Conductor<IScreen>.Collection.OneActive, IShellViewModel
    {
        // TODO: Figure out why log4net is not logging for a Conductor class.
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ShellViewModel));

        private string _breadcrumbText;
        private string _statusBarText;

        public ShellViewModel()
        {
            _log.Debug("Loading Shell");

            DisplayName = "WITSML Studio";
            StatusBarText = "Ready.";
        }

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
            _log.Debug("Loading Plugins");

            base.OnViewReady(view);

            Items.AddRange(Application.Current.Container()
                .ResolveAll<IPluginViewModel>()
                .OrderBy(x => x.DisplayOrder));

            Items.ToList().ForEach(x => _log.Debug(x.DisplayName));

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
