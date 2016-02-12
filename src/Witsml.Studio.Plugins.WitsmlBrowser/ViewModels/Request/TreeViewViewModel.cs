using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    public class TreeViewViewModel : Screen
    {
        public TreeViewViewModel()
        {
            DisplayName = "Tree View";
        }

        public Models.Browser Model
        {
            get { return ((RequestViewModel)Parent).Model; }
        }
    }
}
