using System;
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

                string xmlOut = string.Empty;
                string suppMsgOut = string.Empty;

                var objectType = ObjectTypes.GetObjectTypeFromGroup(Parent.XmlQuery.Text);

                switch (functionType)
                {
                    case Functions.AddToStore:
                        wmls.WMLS_AddToStore(objectType, Parent.XmlQuery.Text, null, null, out suppMsgOut);
                        break;
                    case Functions.UpdateInStore:
                        App.Current.ShowInfo("Coming soon.");
                        break;
                    case Functions.DeleteFromStore:
                        App.Current.ShowInfo("Coming soon.");
                        break;
                    default:
                        wmls.WMLS_GetFromStore(objectType, Parent.XmlQuery.Text, Model.ReturnElementType.ToString(), null, out xmlOut, out suppMsgOut);
                        break;
                }
                OutputResults(xmlOut, suppMsgOut);
                OutputMessages(functionType, Parent.XmlQuery.Text, xmlOut, suppMsgOut);
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

        private void OutputResults(string xmlOut, string suppMsgOut)
        {
            Parent.QueryResults.Text = string.IsNullOrEmpty(suppMsgOut) ? xmlOut : suppMsgOut;
        }

        private void OutputMessages(Functions functionType, string queryText, string xmlOut, string suppMsgOut)
        {
            var none = "<!-- None -->";
            var now = DateTime.Now.ToString("G");

            Parent.Messages.Insert(
                Parent.Messages.TextLength, 
                string.Format(
                    "<!-- {5}: {4} -->{3}{0}{3}{3}<!-- Message: {4} -->{3}<!-- {1} -->{3}{3}<!-- Output: {4} -->{3}{2}{3}{3}",
                    queryText == null ? string.Empty : queryText,
                    string.IsNullOrEmpty(suppMsgOut) ? "None" : suppMsgOut,
                    string.IsNullOrEmpty(xmlOut) ? none : xmlOut,
                    Environment.NewLine,
                    now,
                    functionType.ToDescription()));
        }
    }
}
