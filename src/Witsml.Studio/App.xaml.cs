using System.IO;
using System.Windows;
using log4net.Config;

namespace PDS.Witsml.Studio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            XmlConfigurator.ConfigureAndWatch(new FileInfo("log4net.config"));
        }
    }
}
