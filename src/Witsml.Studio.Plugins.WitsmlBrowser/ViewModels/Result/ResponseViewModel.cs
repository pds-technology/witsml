using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Result
{
    public class ResponseViewModel : Screen
    {
        public ResponseViewModel()
        {
            DisplayName = "Results";
        }

        public Models.Browser Model
        {
            get { return ((ResultViewModel)Parent).Model; }
        }
    }
}
