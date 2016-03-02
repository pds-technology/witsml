using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Result
{
    public class ResultViewModel : Screen
    {
        public new MainViewModel Parent
        {
            get { return (MainViewModel)base.Parent; }
        }

        public Models.WitsmlSettings Model
        {
            get { return Parent.Model; }
        }
    }
}
