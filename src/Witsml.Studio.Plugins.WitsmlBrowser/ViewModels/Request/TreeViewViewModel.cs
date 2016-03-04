using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    public class TreeViewViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(TreeViewViewModel));

        public TreeViewViewModel()
        {
            _log.Debug("Creating view model instance");
            DisplayName = "Tree View";
        }

        public new RequestViewModel Parent
        {
            get { return (RequestViewModel)base.Parent; }
        }

        public Models.WitsmlSettings Model
        {
            get { return Parent.Model; }
        }
    }
}
