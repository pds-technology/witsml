using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Result
{
    public class ResponseViewModel : Screen
    {
        public ResponseViewModel()
        {
            DisplayName = "Results";
        }

        public Models.WitsmlSettings Model
        {
            get { return ((ResultViewModel)Parent).Model; }
        }
    }
}
