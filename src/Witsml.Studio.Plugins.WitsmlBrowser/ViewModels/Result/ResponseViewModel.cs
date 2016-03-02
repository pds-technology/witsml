using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Result
{
    public class ResponseViewModel : Screen
    {
        public ResponseViewModel()
        {
            DisplayName = "Results";
        }

        public new ResultViewModel Parent
        {
            get { return (ResultViewModel)base.Parent; }
        }

        public Models.WitsmlSettings Model
        {
            get { return Parent.Model; }
        }
    }
}
