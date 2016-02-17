using System.ComponentModel.Composition;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// Provides access to the main application user interface
    /// </summary>
    [InheritedExport]
    public interface IShellViewModel
    {
        /// <summary>
        /// Gets or sets the status bar text for the application shell
        /// </summary>
        string StatusBarText { get; set; }

        /// <summary>
        /// Gets or sets the breadcrumb path for the application shell
        /// </summary>
        string BreadcrumbText { get; set; }
    }
}
