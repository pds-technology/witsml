using System.ComponentModel.Composition;
using Caliburn.Micro;

namespace PDS.Witsml.Studio.ViewModels
{
    [InheritedExport]
    public interface IPluginViewModel : IScreen
    {
        int DisplayOrder { get; }
    }
}
