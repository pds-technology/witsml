using Caliburn.Micro;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141.WMLS;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    public enum RequestTypes { Get, Add, Update, Delete };

    public class RequestViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(RequestViewModel));

        public Models.WitsmlSettings Model
        {
            get { return ((MainViewModel)Parent).Model; }
        }

        public WITSMLWebServiceConnection Proxy
        {
            get { return ((MainViewModel)Parent).Proxy; }
        }

        public void SubmitQuery(RequestTypes requestType)
        {
            Model.QueryResults.Text = string.Empty;

            // TODO: Create the correct version of the WMLS instance
            //... We may want to change the DevKit to expose a "CreateClient()" method
            //... e.g., Proxy.CreateClient();
            using (var wmls = new WMLS() { Url = Model.Connection.Uri })
            {                
                string xmlOut;
                string suppMsgOut;

                var objectType = ObjectTypes.GetObjectTypeFromGroup(Model.XmlQuery.Text);

                switch (requestType)
                {
                    case RequestTypes.Add:
                        break;
                    case RequestTypes.Update:
                        break;
                    case RequestTypes.Delete:
                        break;
                    default:
                        wmls.WMLS_GetFromStore(objectType, Model.XmlQuery.Text, null, null, out xmlOut, out suppMsgOut);
                        Model.QueryResults.Text = string.IsNullOrEmpty(suppMsgOut) ? xmlOut : suppMsgOut;
                        break;
                }
            }

            // TODO: Add exception handling.  We don't want the app to crash because of a bad query.
        }

        protected override void OnInitialize()
        {
            _log.Debug("Loading Request View Models");

            base.OnInitialize();

            ActivateItem(new SettingsViewModel());
            //Items.Add(new TreeViewViewModel());
            //Items.Add(new TemplatesViewModel());
            Items.Add(new QueryViewModel());
        }
    }
}
