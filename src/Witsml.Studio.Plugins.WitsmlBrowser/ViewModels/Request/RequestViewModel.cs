using Caliburn.Micro;
using Energistics.DataAccess;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    public class RequestViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(RequestViewModel));

        public new MainViewModel Parent
        {
            get { return (MainViewModel)base.Parent; }
        }

        public Models.WitsmlSettings Model
        {
            get { return Parent.Model; }
        }

        public WITSMLWebServiceConnection Proxy
        {
            get { return Parent.Proxy; }
        }

        public void SubmitQuery(Functions functionType)
        {
            Parent.QueryResults.Text = string.Empty;

            using (var client = Proxy.CreateClientProxy())
            {
                var wmls = client as IWitsmlClient;

                string xmlOut;
                string suppMsgOut;

                var objectType = ObjectTypes.GetObjectTypeFromGroup(Parent.XmlQuery.Text);

                switch (functionType)
                {
                    case Functions.AddToStore:
                        wmls.WMLS_AddToStore(objectType, Parent.XmlQuery.Text, null, null, out suppMsgOut);
                        Parent.QueryResults.Text = suppMsgOut;
                        break;
                    case Functions.UpdateInStore:
                        App.Current.ShowInfo("Coming soon.");
                        break;
                    case Functions.DeleteFromStore:
                        App.Current.ShowInfo("Coming soon.");
                        break;
                    default:
                        wmls.WMLS_GetFromStore(objectType, Parent.XmlQuery.Text, null, null, out xmlOut, out suppMsgOut);
                        Parent.QueryResults.Text = string.IsNullOrEmpty(suppMsgOut) ? xmlOut : suppMsgOut;
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
