using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Result
{
    public class RawViewModel : Screen
    {
        public RawViewModel()
        {
            DisplayName = "Raw";
        }

        public Models.Browser Model
        {
            get { return ((ResultViewModel)Parent).Model; }
        }
    }
}
