using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Result
{
    public class RawViewModel : Screen
    {
        public RawViewModel()
        {
            DisplayName = "Raw";
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
