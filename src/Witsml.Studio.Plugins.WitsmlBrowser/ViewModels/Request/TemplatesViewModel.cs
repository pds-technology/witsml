using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    public class TemplatesViewModel : Screen
    {
        public TemplatesViewModel()
        {
            DisplayName = "Templates";
        }

        public Models.Browser Model
        {
            get { return ((RequestViewModel)Parent).Model; }
        }
    }
}
