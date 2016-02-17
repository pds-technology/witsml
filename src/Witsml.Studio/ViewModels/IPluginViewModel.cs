using System.ComponentModel.Composition;
using Caliburn.Micro;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// Provides access to the main user interface for a plug-in
    /// </summary>
    [InheritedExport]
    public interface IPluginViewModel : IScreen
    {
        /// <summary>
        /// Gets the display order of the plug-in when loaded by the main application shell
        /// </summary>
        int DisplayOrder { get; }
    }
}
