using System.Linq;
using Caliburn.Micro;
using Energistics.DataAccess;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    public class RequestViewModel : Conductor<IScreen>.Collection.OneActive
    {
        // TODO: Figure out why log4net is not logging for a Conductor class.
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(RequestViewModel));

        public Models.Browser Model
        {
            get { return ((MainViewModel)Parent).Model; }
        }

        public WITSMLWebServiceConnection Proxy
        {
            get { return ((MainViewModel)Parent).Proxy; }
        }

        protected override void OnInitialize()
        {
            _log.Debug("Loading Request View Models");

            base.OnInitialize();

            ActivateItem(new SettingsViewModel());
            Items.Add(new TreeViewViewModel());
            Items.Add(new TemplatesViewModel());
            Items.Add(new QueryViewModel());
        }
    }
}
