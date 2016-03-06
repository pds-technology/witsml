using System;
using System.Windows;
using Caliburn.Micro;
using ICSharpCode.AvalonEdit.Document;
using PDS.Witsml.Studio.Runtime;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    /// <summary>
    /// Manages the behavior for the query view UI elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class QueryViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(QueryViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        public QueryViewModel(IRuntimeService runtime, TextDocument xmlQuery)
        {
            _log.Debug("Creating view model instance");
            Runtime = runtime;
            DisplayName = "Query";

            XmlQuery = new TextEditorViewModel(runtime, "XML")
            {
                Document = xmlQuery
            };
        }

        /// <summary>
        /// Gets the Parent <see cref="T:Caliburn.Micro.IConductor" /> for this view model
        /// </summary>
        public new RequestViewModel Parent
        {
            get { return (RequestViewModel)base.Parent; }
        }

        /// <summary>
        /// Gets the main view model.
        /// </summary>
        /// <value>
        /// The main view model.
        /// </value>
        public MainViewModel MainViewModel
        {
            get { return (MainViewModel)Parent.Parent; }
        }

        /// <summary>
        /// Gets the data model.
        /// </summary>
        /// <value>
        /// The WitsmlSettings data model.
        /// </value>
        public Models.WitsmlSettings Model
        {
            get { return Parent.Model; }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; private set; }

        private TextEditorViewModel _xmlQuery;

        /// <summary>
        /// Gets or sets the XML query editor.
        /// </summary>
        /// <value>The XML query editor.</value>
        public TextEditorViewModel XmlQuery
        {
            get { return _xmlQuery; }
            set
            {
                if (!ReferenceEquals(_xmlQuery, value))
                {
                    _xmlQuery = value;
                    NotifyOfPropertyChange(() => XmlQuery);
                }
            }
        }

        /// <summary>
        /// Submits a query to the WITSML server for the given function type.
        /// </summary>
        /// <param name="functionText">The function type text.</param>
        public void SubmitQuery(string functionText)
        {
            _log.DebugFormat("Submitting a query for '{0}'", functionText);

            MainViewModel.SubmitQuery((Functions)Enum.Parse(typeof(Functions), functionText));
        }

        /// <summary>
        /// Saves the current query text to file.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void SaveQuery(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Save coming soon");
        }

        /// <summary>
        /// Sets the current query text from file.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void OpenQuery(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Open coming soon");
        }
    }
}
