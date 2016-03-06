using System.ComponentModel.Composition;
using PDS.Witsml.Studio.Runtime;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// Provides access to the main application user interface
    /// </summary>
    [InheritedExport]
    public interface IShellViewModel
    {
        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service instance.</value>
        IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets or sets the status bar text for the application shell
        /// </summary>
        string StatusBarText { get; set; }

        /// <summary>
        /// Gets or sets the breadcrumb path for the application shell
        /// </summary>
        string BreadcrumbText { get; set; }

        /// <summary>
        /// Sets the breadcrumb text.
        /// </summary>
        /// <param name="values">The values.</param>
        void SetBreadcrumb(params object[] values);
    }
}
