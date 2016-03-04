using System;
using System.ComponentModel.Composition;
using Caliburn.Micro;
using Energistics.DataAccess;
using ICSharpCode.AvalonEdit.Document;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.Properties;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Result;
using PDS.Witsml.Studio.Runtime;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels
{
    public class MainViewModel : Conductor<IScreen>.Collection.AllActive, IPluginViewModel
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MainViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        [ImportingConstructor]
        public MainViewModel(IRuntimeService runtime)
        {
            _log.Debug("Creating view model instance");

            Runtime = runtime;

            // Create the model for our witsml settings
            Model = new Models.WitsmlSettings();

            // Create documents used by Avalon Editors used on query/result tabs.
            XmlQuery = new TextDocument();
            QueryResults = new TextDocument();
            Messages = new TextDocument();

            // Create a default client proxy object.
            Proxy = CreateProxy();

            // Create view models displayed within this view model.
            RequestControl = new RequestViewModel(Runtime);
            ResultControl = new ResultViewModel(Runtime);

            DisplayName = Settings.Default.PluginDisplayName;

            // Handle notifications for our witsml settings model changes
            Model.PropertyChanged += Model_PropertyChanged;
        }

        public WITSMLWebServiceConnection Proxy { get; private set; }

        /// <summary>
        /// Gets the display order of the plug-in when loaded by the main application shell
        /// </summary>
        public int DisplayOrder
        {
            get { return Settings.Default.PluginDisplayOrder; }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; private set; }

        private Models.WitsmlSettings _model;
        public Models.WitsmlSettings Model
        {
            get { return _model; }
            set
            {
                if (!ReferenceEquals(_model, value))
                {
                    _model = value;
                    NotifyOfPropertyChange(() => Model);
                }
            }
        }

        public RequestViewModel RequestControl { get; set; }

        public ResultViewModel ResultControl { get; set; }

        private TextDocument _xmlQuery;
        public TextDocument XmlQuery
        {
            get { return _xmlQuery; }
            set
            {
                if (!string.Equals(_xmlQuery, value))
                {
                    _xmlQuery = value;
                    NotifyOfPropertyChange(() => XmlQuery);
                }
            }
        }

        private TextDocument _queryResults;
        public TextDocument QueryResults
        {
            get { return _queryResults; }
            set
            {
                if (!string.Equals(_queryResults, value))
                {
                    _queryResults = value;
                    NotifyOfPropertyChange(() => QueryResults);
                }
            }
        }

        private TextDocument _messages;
        public TextDocument Messages
        {
            get { return _messages; }
            set
            {
                if (!string.Equals(_messages, value))
                {
                    _messages = value;
                    NotifyOfPropertyChange(() => Messages);
                }
            }
        }

        public void SubmitQuery(Functions functionType)
        {
            string xmlOut = string.Empty;
            string suppMsgOut = string.Empty;
            string optionsIn = null;

            try
            {
                _log.DebugFormat("Query submitted for function '{0}'", functionType);

                QueryResults.Text = string.Empty;

                SubmitQuery(functionType, XmlQuery.Text, ref xmlOut, ref suppMsgOut, ref optionsIn);

                _log.DebugFormat("Query returned with{3}{3}xmlOut: {0}{3}{3}suppMsgOut: {1}{3}{3}optionsIn: {2}{3}{3}",
                    GetLogStringText(xmlOut),
                    GetLogStringText(suppMsgOut),
                    GetLogStringText(optionsIn),
                    Environment.NewLine);

                OutputResults(xmlOut, suppMsgOut);
                OutputMessages(functionType, XmlQuery.Text, xmlOut, suppMsgOut, optionsIn);
            }
            catch (Exception ex)
            {
                var message = string.Format("Error submitting query for function '{0}'{3}{3}Error Message: {1}{3}{3}Stack Trace:{3}{2}{3}",
                    functionType, ex.Message, ex.StackTrace, Environment.NewLine);

                // Log the error message
                _log.Error(message);

                // Output the error to the user
                OutputResults(null, message);
                OutputMessages(functionType, XmlQuery.Text, null, message, optionsIn);
            }
        }

        public void GetCapabilities()
        {
            SubmitQuery(Functions.GetCap);
        }

        internal void SubmitQuery(Functions functionType, string xmlIn, ref string xmlOut, ref string suppMsgOut, ref string optionsIn)
        {
            using (var client = Proxy.CreateClientProxy())
            {
                var wmls = client as IWitsmlClient;

                var objectType = ObjectTypes.GetObjectTypeFromGroup(xmlIn);

                switch (functionType)
                {
                    case Functions.GetCap:
                        // Set options in for the selected WitsmlVersion.
                        optionsIn = new OptionsIn.DataVersion(Model.WitsmlVersion);
                        wmls.WMLS_GetCap(optionsIn, out xmlOut, out suppMsgOut);
                        break;
                    case Functions.AddToStore:
                        wmls.WMLS_AddToStore(objectType, xmlIn, null, null, out suppMsgOut);
                        break;
                    case Functions.UpdateInStore:
                        //Runtime.ShowInfo("Coming soon.");
                        break;
                    case Functions.DeleteFromStore:
                        //Runtime.ShowInfo("Coming soon.");
                        break;
                    default:
                        optionsIn = GetGetFromStoreOptionsIn();
                        wmls.WMLS_GetFromStore(objectType, xmlIn, optionsIn, null, out xmlOut, out suppMsgOut);
                        break;
                }
            }
        }

        internal void LoadScreens()
        {
            _log.Debug("Loading MainViewModel screens");
            Items.Add(RequestControl);
            Items.Add(ResultControl);
        }

        internal string GetWrappedText(bool isWrapped)
        {
            return isWrapped ? "No Wrap" : "Wrap";
            }

        /// <summary>
        /// Creates a WITSMLWebServiceConnection for the current connection uri and witsml version.
        /// </summary>
        /// <returns></returns>
        internal WITSMLWebServiceConnection CreateProxy()
        {
            _log.DebugFormat("A new Proxy is being created with {2}{2}uri: {0}{2}{2}WitsmlVersion: {1}{2}{2}", Model.Connection.Uri, Model.WitsmlVersion, Environment.NewLine);
            return new WITSMLWebServiceConnection(Model.Connection.Uri, GetWitsmlVersionEnum(Model.WitsmlVersion));
        }

        /// <summary>
        /// Gets the witsml version enum.
        /// </summary>
        /// <returns>
        /// The WMLSVersion enum value based on the current value of Model.WitsmlVersion.
        /// If Model.WitsmlVersion has not been established the the default is WMLSVersion.WITSML141.
        /// </returns>
        internal WMLSVersion GetWitsmlVersionEnum(string witsmlVersion)
        {
            return witsmlVersion != null && witsmlVersion.Equals(OptionsIn.DataVersion.Version131.Value)
                ? WMLSVersion.WITSML131
                : WMLSVersion.WITSML141;
        }

        protected override void OnInitialize()
        {
            _log.Debug("Initializing screen");
            base.OnInitialize();
            LoadScreens();
        }

        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("WitsmlVersion"))
            {
                _log.Debug("WitsmlVersion property changed");

                Runtime.Shell.BreadcrumbText = !string.IsNullOrEmpty(Model.WitsmlVersion)
                    ? string.Format("{0}/{1}", Settings.Default.PluginDisplayName, Model.WitsmlVersion)
                    : Settings.Default.PluginDisplayName;

                // Reset the Proxy when the version changes
                Proxy = CreateProxy();

                // Get the server capabilities for the newly selected version.
                GetCapabilities();

                // TODO: GetWells for the TreeView
            }
        }

        private void OutputResults(string xmlOut, string suppMsgOut)
        {
            QueryResults.Text = string.IsNullOrEmpty(suppMsgOut) ? xmlOut : suppMsgOut;
        }

        private void OutputMessages(Functions functionType, string queryText, string xmlOut, string suppMsgOut, string optionsIn)
        {
            var none = "<!-- None -->";
            var now = DateTime.Now.ToString("G");

            Messages.Insert(
                Messages.TextLength,
                string.Format(
                    "<!-- {5}: {4} -->{3}<!-- OptionsIn: {6} -->{3}{0}{3}{3}<!-- Message: {4} -->{3}<!-- {1} -->{3}{3}<!-- Output: {4} -->{3}{2}{3}{3}",
                    queryText == null ? string.Empty : queryText,
                    string.IsNullOrEmpty(suppMsgOut) ? "None" : suppMsgOut,
                    string.IsNullOrEmpty(xmlOut) ? none : xmlOut,
                    Environment.NewLine,
                    now,
                    functionType.ToDescription(),
                    string.IsNullOrEmpty(optionsIn) ? "None" : optionsIn));
        }

        private string GetGetFromStoreOptionsIn()
        {
            return
                string.Concat(
                    Model.ReturnElementType,
                    Model.IsRequestObjectSelectionCapability ? ";" + OptionsIn.RequestObjectSelectionCapability.True : string.Empty,
                    Model.IsRequestPrivateGroupOnly ? ";" + OptionsIn.RequestPrivateGroupOnly.True : string.Empty
                    );
        }

        private string GetLogStringText(string logString)
        {
            return string.IsNullOrEmpty(logString) ? "<None>" : logString;
        }
    }
}
