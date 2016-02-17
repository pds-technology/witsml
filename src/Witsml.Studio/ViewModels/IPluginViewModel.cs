using System.ComponentModel.Composition;
using Caliburn.Micro;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// Interface to an application shell plugin
    /// </summary>
    [InheritedExport]
    public interface IPluginViewModel : IScreen
    {
        /// <summary>
        /// The ascending display order of a plugin tab
        /// </summary>
        int DisplayOrder { get; }
    }
}
