//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Caliburn.Micro;
using Energistics.DataAccess;
using ICSharpCode.AvalonEdit.Document;
using PDS.Framework;
using PDS.Witsml.Studio.Core.Connections;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.Models;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.Properties;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Result;
using PDS.Witsml.Studio.Core.Runtime;
using PDS.Witsml.Studio.Core.ViewModels;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the main user interface for the Witsml Browser plug-in.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Conductor{IScreen}.Collection.AllActive" />
    /// <seealso cref="PDS.Witsml.Studio.Core.ViewModels.IPluginViewModel" />
    public sealed class MainViewModel : Conductor<IScreen>.Collection.AllActive, IPluginViewModel, IConnectionAware
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MainViewModel));
        public const string QueryTemplateText = "Query Templates";

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        [ImportingConstructor]
        public MainViewModel(IRuntimeService runtime)
        {
            _log.Debug("Creating view model instance");

            Runtime = runtime;
            DisplayName = Settings.Default.PluginDisplayName;
            DataObjects = new BindableCollection<string>() { QueryTemplateText };

            // Create the model for our witsml settings
            Model = new WitsmlSettings();

            // Create documents used by Avalon Editors used on query/result tabs.
            XmlQuery = new TextDocument();
            QueryResults = new TextDocument();
            Messages = new TextDocument();

            // Create a default client proxy object.
            Proxy = CreateProxy();

            // Create view models displayed within this view model.
            RequestControl = new RequestViewModel(Runtime);
            ResultControl = new ResultViewModel(Runtime, QueryResults, Messages);

            // Handle notifications for our witsml settings model changes
            Model.PropertyChanged += Model_PropertyChanged;
        }

        /// <summary>
        /// Gets the proxy for the WITSML web service.
        /// </summary>
        /// <value>
        /// The WITSML seb service proxy.
        /// </value>
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

        /// <summary>
        /// Gets the collection of supported data objects.
        /// </summary>
        /// <value>The collection of data objects.</value>
        public BindableCollection<string> DataObjects { get; private set; }

        private string _dataObject;

        /// <summary>
        /// Gets or sets the selected data object.
        /// </summary>
        /// <value>The selected data object.</value>
        public string DataObject
        {
            get { return _dataObject; }
            set
            {
                if (!string.Equals(_dataObject, value))
                {
                    _dataObject = value;
                    NotifyOfPropertyChange(() => DataObject);
                    OnDataObjectSelected();
                }
            }
        }

        private WitsmlSettings _model;

        /// <summary>
        /// Gets or sets the data model.
        /// </summary>
        /// <value>
        /// The WitsmlSettings data model.
        /// </value>
        public WitsmlSettings Model
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

        /// <summary>
        /// Gets or sets the reference to the request view model.
        /// </summary>
        /// <value>
        /// The request view model.
        /// </value>
        public RequestViewModel RequestControl { get; set; }

        /// <summary>
        /// Gets or sets the reference to the result view model.
        /// </summary>
        /// <value>
        /// The result view model.
        /// </value>
        public ResultViewModel ResultControl { get; set; }

        private TextDocument _xmlQuery;

        /// <summary>
        /// Gets or sets the XML query document.
        /// </summary>
        /// <value>
        /// The XML query document.
        /// </value>
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

        /// <summary>
        /// Gets or sets the query results document.
        /// </summary>
        /// <value>
        /// The query results document.
        /// </value>
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

        /// <summary>
        /// Gets or sets the messages document.
        /// </summary>
        /// <value>
        /// The messages document.
        /// </value>
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

        /// <summary>
        /// Called when the selected WITSML version has changed.
        /// </summary>
        /// <param name="version">The WITSML version.</param>
        public void OnWitsmlVersionChanged(string version)
        {
            // Reset the Proxy when the version changes
            Proxy = CreateProxy();

            // Get the server capabilities for the newly selected version.
            if (!string.IsNullOrEmpty(version))
            {
                GetCapabilities();
            }

            RequestControl.OnWitsmlVersionChanged(version);
        }

        /// <summary>
        /// Submits an asynchronous query to the WITSML server for a given function type.
        /// The results of a query are displayed in the Results and Messages tabs.
        /// </summary>
        /// <param name="functionType">Type of the function.</param>
        public void SubmitQuery(Functions functionType)
        {
            string xmlIn = XmlQuery.Text;

            _log.DebugFormat("Query submitted for function '{0}'", functionType);

            // Clear any previous query results
            QueryResults.Text = string.Empty;

            Task.Run(async () =>
            {
                // Call internal SubmitQuery method with references to all inputs and outputs.
                var result = await SubmitQuery(functionType, xmlIn);
                await Runtime.InvokeAsync(() => ShowSubmitResult(functionType, result));
            });
        }

        /// <summary>
        /// Submits a query to get the server capabilities.
        /// </summary>
        public void GetCapabilities()
        {
            SubmitQuery(Functions.GetCap);
        }

        /// <summary>
        /// Submits the query to the WITSML server for the given function type and input XML.
        /// </summary>
        /// <param name="functionType">Type of the function to execute.</param>
        /// <param name="xmlIn">The XML in.</param>
        /// <returns>
        /// A tuple of four result values in the following order: xmlOut, suppMsgOut, optionsIn and returnCode.
        /// </returns>
        internal async Task<WitsmlResult> SubmitQuery(Functions functionType, string xmlIn)
        {
            string xmlOut = null;
            string optionsIn = null;
            short returnCode = 0;

            // Compute the object type of the incoming xml.
            var objectType = ObjectTypes.GetObjectTypeFromGroup(xmlIn);

            try
            {
                using (var client = Proxy.CreateClientProxy())
                {
                    var wmls = (IWitsmlClient)client;
                    string suppMsgOut;

                    // Execute the WITSML server function for the given functionType
                    switch (functionType)
                    {
                        case Functions.GetCap:
                            // Set options in for the selected WitsmlVersion.
                            optionsIn = new OptionsIn.DataVersion(Model.WitsmlVersion);
                            returnCode = wmls.WMLS_GetCap(optionsIn, out xmlOut, out suppMsgOut);
                            ProcessCapServer(xmlOut);
                            break;
                        case Functions.AddToStore:
                            returnCode = wmls.WMLS_AddToStore(objectType, xmlIn, null, null, out suppMsgOut);
                            break;
                        case Functions.UpdateInStore:
                            returnCode = wmls.WMLS_UpdateInStore(objectType, xmlIn, null, null, out suppMsgOut);
                            break;
                        case Functions.DeleteFromStore:
                            returnCode = wmls.WMLS_DeleteFromStore(objectType, xmlIn, null, null, out suppMsgOut);
                            break;
                        default:
                            optionsIn = GetGetFromStoreOptionsIn();
                            returnCode = wmls.WMLS_GetFromStore(objectType, xmlIn, optionsIn, null, out xmlOut, out suppMsgOut);
                            break;
                    }

                    return await Task.FromResult(new WitsmlResult(objectType, xmlIn, optionsIn, null, xmlOut, suppMsgOut, returnCode));
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("Error calling WITSML Store API method '{0}'{3}{3}Error Message: {1}{3}{3}Stack Trace:{3}{2}{3}",
                    functionType, ex.Message, ex.StackTrace, Environment.NewLine);

                // Log the error message
                _log.Error(message);

                // Return the error to the caller so message and call stack can be displayed to the user
                return await Task.FromResult(new WitsmlResult(objectType, xmlIn, optionsIn, null, xmlOut, message, returnCode));
            }
        }

        /// <summary>
        /// Loads the screens hosted by the MainViewModel.
        /// </summary>
        internal void LoadScreens()
        {
            _log.Debug("Loading MainViewModel screens");
            Items.Add(RequestControl);
            Items.Add(ResultControl);
        }

        /// <summary>
        /// Creates a WITSMLWebServiceConnection for the current connection uri and witsml version.
        /// </summary>
        /// <returns></returns>
        internal WITSMLWebServiceConnection CreateProxy()
        {
            _log.DebugFormat("A new Proxy is being created with URI: {0}; WitsmlVersion: {1}", Model.Connection.Uri, Model.WitsmlVersion);
            return Model.Connection.CreateProxy(GetWitsmlVersionEnum(Model.WitsmlVersion));
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

        /// <summary>
        /// Logs and displays the results of a WITSML submitted query.
        /// </summary>
        /// <param name="functionType">Type of the function.</param>
        /// <param name="result">The WITSML Store API method result.</param>
        internal void ShowSubmitResult(Functions functionType, WitsmlResult result)
        {
            var xmlOut = functionType == Functions.GetFromStore
                ? ProcessQueryResult(result.XmlOut, result.OptionsIn)
                : result.XmlOut;

            _log.DebugFormat("Query returned with{3}{3}xmlOut: {0}{3}{3}suppMsgOut: {1}{3}{3}optionsIn: {2}{3}{3}",
                GetLogStringText(xmlOut),
                GetLogStringText(result.MessageOut),
                GetLogStringText(result.OptionsIn),
                Environment.NewLine);

            // Output query results to the Results tab
            OutputResults(xmlOut, result.MessageOut, result.ReturnCode);

            // Don't display query contents when GetCap is executed.
            var xmlIn = functionType == Functions.GetCap ? string.Empty : XmlQuery.Text;

            // Append these results to the Messages tab
            OutputMessages(functionType, xmlIn, xmlOut, result.MessageOut, result.OptionsIn, result.ReturnCode);

            // Show data object on the Properties tab
            if (functionType == Functions.GetFromStore && result.ReturnCode > 0)
                ShowObjectProperties(result);
        }

        /// <summary>
        /// Called when initializing the MainViewModel.
        /// </summary>
        protected override void OnInitialize()
        {
            _log.Debug("Initializing screen");
            base.OnInitialize();
            LoadScreens();
        }

        /// <summary>
        /// Shows the object properties.
        /// </summary>
        /// <param name="result">The WITSML query result.</param>
        private void ShowObjectProperties(WitsmlResult result)
        {
            try
            {
                ResultControl.ObjectProperties.SetCurrentObject(result.ObjectType, result.XmlOut, Model.WitsmlVersion);
            }
            catch (WitsmlException ex)
            {
                _log.ErrorFormat("Error parsing query response: {0}{2}{2}{1}", result.XmlOut, ex, Environment.NewLine);
                var message = string.Format("{0}{2}{2}{1}", ex.Message, ex.GetBaseException().Message, Environment.NewLine);

                OutputResults(string.Empty, message, (short)ex.ErrorCode);
                OutputMessages(Functions.GetFromStore, string.Empty, string.Empty, message, string.Empty, (short) ex.ErrorCode);
            }
        }

        /// <summary>
        /// Processes the GetFromStore query result.
        /// </summary>
        /// <param name="xmlOut">The XML out.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <returns>An XML string.</returns>
        private string ProcessQueryResult(string xmlOut, string optionsIn)
        {
            if (string.IsNullOrWhiteSpace(xmlOut)) return xmlOut;
            if (string.IsNullOrWhiteSpace(Model.OutputPath)) return xmlOut;

            var options = OptionsIn.Parse(optionsIn);
            var returnElements = OptionsIn.GetValue(options, OptionsIn.ReturnElements.Requested);
            var outputPath = new DirectoryInfo(Path.Combine(Model.OutputPath, returnElements)).FullName;
            var document = WitsmlParser.Parse(xmlOut);

            if (Model.IsSaveQueryResponse || xmlOut.Length > Model.TruncateSize)
                outputPath = SaveQueryResult(outputPath, document, Model.IsSplitResults);

            if (xmlOut.Length > Model.TruncateSize)
            {
                xmlOut = $"<!-- WARNING: Response larger than { Model.TruncateSize } characters -->" + Environment.NewLine +
                         $"<!-- Results automatically saved to { outputPath } -->";
            }
            else if (!xmlOut.Contains(Environment.NewLine))
            {
                xmlOut = document.ToString();
            }

            return xmlOut;
        }

        /// <summary>
        /// Saves the query result to the file system.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="document">The XML document.</param>
        /// <param name="splitResults">if set to <c>true</c> results will be split into multiple files.</param>
        /// <returns>The full output path.</returns>
        private string SaveQueryResult(string outputPath, XDocument document, bool splitResults)
        {
            if (document?.Root == null) return outputPath;

            if (!splitResults)
            {
                Directory.CreateDirectory(outputPath);
                outputPath = Path.Combine(outputPath, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".xml");
                document.Save(outputPath);
                return outputPath;
            }

            var ns = document.Root.GetDefaultNamespace();
            var objectPath = outputPath;

            document.Root.Elements().ForEach(x =>
            {
                var ids = new[]
                {
                    x.Element(ns + "nameWell")?.Value,
                    x.Element(ns + "nameWellbore")?.Value,
                    x.Element(ns + "name")?.Value,
                };

                var fileName = string.Join("_", ids.Where(id => !string.IsNullOrWhiteSpace(id))) + ".xml";
                objectPath = Path.Combine(outputPath, x.Name.LocalName);
                Directory.CreateDirectory(objectPath);

                var clone = new XElement(document.Root);
                clone.RemoveNodes();
                clone.Add(new XElement(x));
                clone.Save(Path.Combine(objectPath, fileName));
            });

            return objectPath;
        }

        /// <summary>
        /// Processes the capServer response recieved.
        /// </summary>
        /// <param name="capServers">The cap servers.</param>
        private void ProcessCapServer(string capServers)
        {
            if (string.IsNullOrWhiteSpace(capServers))
                return;

            DataObjects.Clear();
            DataObjects.Add(QueryTemplateText);
            DataObject = QueryTemplateText;

            var xml = XDocument.Parse(capServers);
            var dataObjects = new List<string>();

            xml.Descendants()
                .Where(x => x.Name.LocalName == "dataObject")
                .ForEach(x =>
                {
                    if (!dataObjects.Contains(x.Value))
                        dataObjects.Add(x.Value);
                });

            dataObjects.Sort();
            DataObjects.AddRange(dataObjects);
        }

        /// <summary>
        /// Called when a data object is selected.
        /// </summary>
        private void OnDataObjectSelected()
        {
            if (DataObject == null || DataObject == QueryTemplateText)
                return;

            var type = ObjectTypes.GetObjectGroupType(DataObject, Model.WitsmlVersion);
            var query = Proxy.BuildEmtpyQuery(type, Model.WitsmlVersion);

            Runtime.Invoke(() =>
            {
                XmlQuery.Text = WitsmlParser.ToXml(query);
                DataObject = QueryTemplateText;
            });
        }

        /// <summary>
        /// Handles the PropertyChanged event of the Model control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Handle changes for the WitsmlVersion property
            if (e.PropertyName.Equals("WitsmlVersion"))
            {
                _log.Debug("WitsmlVersion property changed");
                OnWitsmlVersionChanged(Model.WitsmlVersion);
            }
        }

        /// <summary>
        /// Outputs the results of a query to the Results tab.
        /// </summary>
        /// <param name="xmlOut">The XML out.</param>
        /// <param name="suppMsgOut">The supplemental message out.</param>
        /// <param name="returnCode">The return code.</param>
        private void OutputResults(string xmlOut, string suppMsgOut, short returnCode)
        {
            QueryResults.Text = string.IsNullOrEmpty(xmlOut)
                ? (returnCode < 0
                    ? string.Format("{0}{1}{1}Error Code: {2}", suppMsgOut, Environment.NewLine, returnCode)
                    : suppMsgOut)
                : xmlOut;
        }

        /// <summary>
        /// Appends results of a query to the Messages tab.
        /// </summary>
        /// <param name="functionType">Type of the function.</param>
        /// <param name="queryText">The query text.</param>
        /// <param name="xmlOut">The XML output text.</param>
        /// <param name="suppMsgOut">The supplemental message out.</param>
        /// <param name="optionsIn">The OptionsIn settings to the server.</param>
        /// <param name="returnCode">The return code.</param>
        private void OutputMessages(Functions functionType, string queryText, string xmlOut, string suppMsgOut, string optionsIn, short returnCode)
        {
            var none = "<!-- None -->";
            var now = DateTime.Now.ToString("G");

            Messages.Insert(
                Messages.TextLength,
                string.Format(
                    "<!-- {5}: {4} -->{3}<!-- OptionsIn: {6} -->{3}{0}{3}{3}<!-- Message: {4} -->{3}<!-- {1} -->{3}{3}<!-- Output: {4} -->{3}<!-- Return Code: {7} -->{3}{2}{3}{3}",
                    queryText == null ? string.Empty : queryText,
                    string.IsNullOrEmpty(suppMsgOut) ? "None" : suppMsgOut,
                    string.IsNullOrEmpty(xmlOut) ? none : xmlOut,
                    Environment.NewLine,
                    now,
                    functionType.GetDescription(),
                    string.IsNullOrEmpty(optionsIn) ? "None" : optionsIn,
                    returnCode));
        }

        /// <summary>
        /// Gets the GetFromStore OptionsIn.
        /// </summary>
        /// <returns></returns>
        private string GetGetFromStoreOptionsIn()
        {
            var optionsIn = new List<string>();

            optionsIn.Add(Model.ReturnElementType ?? string.Empty);
            optionsIn.Add(Model.IsRequestObjectSelectionCapability ? OptionsIn.RequestObjectSelectionCapability.True : string.Empty);
            optionsIn.Add(Model.IsRequestPrivateGroupOnly ? OptionsIn.RequestPrivateGroupOnly.True : string.Empty);

            if (Model.MaxDataRows.HasValue && Model.MaxDataRows.Value > 0)
                optionsIn.Add(new OptionsIn.MaxReturnNodes(Model.MaxDataRows.Value));

            if (Model.RequestLatestValues.HasValue && Model.RequestLatestValues.Value > 0)
                optionsIn.Add(new OptionsIn.RequestLatestValues(Model.RequestLatestValues.Value));

            return string.Join(";", optionsIn.Where(o => !string.IsNullOrEmpty(o)));
        }

        /// <summary>
        /// Gets the log string text.
        /// </summary>
        /// <param name="logString">The log string.</param>
        /// <returns>Returns the logString text if it is not null, otherwise "&lt;None&gt;" is returned as the string.</returns>
        private string GetLogStringText(string logString)
        {
            return string.IsNullOrEmpty(logString) ? "<None>" : logString;
        }
    }
}
