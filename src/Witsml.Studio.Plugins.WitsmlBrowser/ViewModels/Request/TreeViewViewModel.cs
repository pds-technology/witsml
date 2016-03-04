using Caliburn.Micro;
using PDS.Witsml.Studio.Runtime;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    /// <summary>
    /// Manages the behavior for the TreeView view UI elements.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class TreeViewViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(TreeViewViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeViewViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        public TreeViewViewModel(IRuntimeService runtime)
        {
            _log.Debug("Creating view model instance");
            Runtime = runtime;
            DisplayName = "Tree View";
        }

        /// <summary>
        /// Gets the Parent <see cref="T:Caliburn.Micro.IConductor" /> for this view model.
        /// </summary>
        public new RequestViewModel Parent
        {
            get { return (RequestViewModel)base.Parent; }
        }

        /// <summary>
        /// Gets or sets the data model.
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
    }
}
