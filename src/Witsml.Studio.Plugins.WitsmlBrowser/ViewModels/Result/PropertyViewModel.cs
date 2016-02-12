using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Result
{
    public class PropertyViewModel : Screen
    {
        public PropertyViewModel()
        {
            DisplayName = "Properties";
        }

        public Models.Browser Model
        {
            get { return ((ResultViewModel)Parent).Model; }
        }
    }
}
