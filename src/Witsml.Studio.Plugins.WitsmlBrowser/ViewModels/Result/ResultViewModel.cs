using System.Windows;
using Caliburn.Micro;
using PDS.Witsml.Studio.Runtime;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Result
{
    /// <summary>
    /// Manages the behavior for the query result UI elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class ResultViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ResultViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        public ResultViewModel(IRuntimeService runtime)
        {
            _log.Debug("Creating view model instance");
            Runtime = runtime;
        }

        /// <summary>
        /// Gets the Parent <see cref="T:Caliburn.Micro.IConductor" /> for this view model.
        /// </summary>
        public new MainViewModel Parent
        {
            get { return (MainViewModel)base.Parent; }
        }

        /// <summary>
        /// Gets the data model for this view model.
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

        private bool _messagesWrapped;

        /// <summary>
        /// Gets or sets a value indicating whether the Messages document text is wrapped.
        /// </summary>
        /// <value>
        ///   <c>true</c> if text is wrapped; otherwise, <c>false</c>.
        /// </value>
        public bool MessagesWrapped
        {
            get { return _messagesWrapped; }
            set
            {
                if (_messagesWrapped != value)
                {
                    _messagesWrapped = value;
                    NotifyOfPropertyChange(() => MessagesWrapped);
                    NotifyOfPropertyChange(() => MessagesWrappedText);
                }
            }
        }

        private bool _resultsWrapped;

        /// <summary>
        /// Gets or sets a value indicating whether the Results document text is wrapped.
        /// </summary>
        /// <value>
        ///   <c>true</c> if text is wrapped; otherwise, <c>false</c>.
        /// </value>
        public bool ResultsWrapped
        {
            get { return _resultsWrapped; }
            set
            {
                if (_resultsWrapped != value)
                {
                    _resultsWrapped = value;
                    NotifyOfPropertyChange(() => ResultsWrapped);
                    NotifyOfPropertyChange(() => ResultsWrappedText);
                }
            }
        }

        /// <summary>
        /// Gets the messages wrapped context menu text.
        /// </summary>
        /// <value>
        /// The messages wrapped menu text.
        /// </value>
        public string MessagesWrappedText
        {
            get { return Parent.GetWrappedText(MessagesWrapped); }
        }

        /// <summary>
        /// Gets the results wrapped context menu text.
        /// </summary>
        /// <value>
        /// The results wrapped menu text.
        /// </value>
        public string ResultsWrappedText
        {
            get { return Parent.GetWrappedText(ResultsWrapped); }
        }

        /// <summary>
        /// Copies the results to the clipboard.
        /// </summary>
        public void CopyResults()
        {
            Runtime.Invoke(() => Clipboard.SetText(Parent.QueryResults.Text));
        }

        /// <summary>
        /// Clears the results.
        /// </summary>
        public void ClearResults()
        {
            Runtime.Invoke(() => Parent.QueryResults.Text = string.Empty);
        }

        /// <summary>
        /// Copies the Results to the clipboard.
        /// </summary>
        public void CopyMessages()
        {
            Runtime.Invoke(() => Clipboard.SetText(Parent.Messages.Text));
        }

        /// <summary>
        /// Clears the messages.
        /// </summary>
        public void ClearMessages()
        {
            Runtime.Invoke(() => Parent.Messages.Text = string.Empty);
        }

        /// <summary>
        /// Toggles the Messages document text wrapping flag.
        /// </summary>
        public void WrapMessages()
        {
            MessagesWrapped = !MessagesWrapped;
        }

        /// <summary>
        /// Toggles the Results document text wrapping flag.
        /// </summary>
        public void WrapResults()
        {
            ResultsWrapped = !ResultsWrapped;
        }

        /// <summary>
        /// Called when activating the Results screen.
        /// </summary>
        protected override void OnActivate()
        {
            base.OnActivate();

            MessagesWrapped = false;
        }
    }
}
