using System.ComponentModel.Composition;

namespace PDS.Witsml.Studio.ViewModels
{
    [InheritedExport]
    public interface IShellViewModel
    {
        string StatusBarText { get; set; }
        string BreadcrumbText { get; set; }
    }
}
