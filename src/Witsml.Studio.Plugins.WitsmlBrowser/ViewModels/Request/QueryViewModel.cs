using System;
using System.Windows;
using Caliburn.Micro;
using PDS.Witsml.Studio.Runtime;

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
        public QueryViewModel(IRuntimeService runtime)
        {
            _log.Debug("Creating view model instance");
            Runtime = runtime;
            DisplayName = "Query";
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

        private bool _queryWrapped;

        /// <summary>
        /// Gets or sets a value indicating whether query document text is wrapped.
        /// </summary>
        /// <value>
        ///   <c>true</c> if query document text is wrapped; otherwise, <c>false</c>.
        /// </value>
        public bool QueryWrapped
        {
            get { return _queryWrapped; }
            set
            {
                if (_queryWrapped != value)
                {
                    _queryWrapped = value;
                    NotifyOfPropertyChange(() => QueryWrapped);
                    NotifyOfPropertyChange(() => QueryWrappedText);
                }
            }
        }

        /// <summary>
        /// Gets the query wrapped context menu text.
        /// </summary>
        /// <value>
        /// The query wrapped menu text.
        /// </value>
        public string QueryWrappedText
        {
            get { return MainViewModel.GetWrappedText(QueryWrapped); }
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

        /// <summary>
        /// Copies the query to the clipboard.
        /// </summary>
        public void CopyQuery()
        {
            Runtime.Invoke(() => Clipboard.SetText(Parent.Parent.XmlQuery.Text));
        }

        /// <summary>
        /// Clears the query.
        /// </summary>
        public void ClearQuery()
        {
            Runtime.Invoke(() => Parent.Parent.XmlQuery.Text = string.Empty);
        }

        /// <summary>
        /// Toggles the XML Query document text wrapping flag.
        /// </summary>
        public void WrapQuery()
        {
            QueryWrapped = !QueryWrapped;
        }
    }
}
