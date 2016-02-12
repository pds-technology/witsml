using Caliburn.Micro;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Models;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.ViewModels
{
    public class HierarchyViewModel : Screen
    {
        public HierarchyViewModel()
        {
            DisplayName = "Tree View";
        }

        public EtpSettings Model
        {
            get { return ((MainViewModel)Parent).Model; }
        }
    }
}
