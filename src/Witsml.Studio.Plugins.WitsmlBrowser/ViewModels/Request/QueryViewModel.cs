using System.Windows;
using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    public class QueryViewModel : Screen
    {
        public QueryViewModel()
        {
            DisplayName = "Query";
        }

        public Models.WitsmlSettings Model
        {
            get { return ((RequestViewModel)Parent).Model; }
        }

        public void ExecuteQuery(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Execute coming soon");
        }

        public void SaveQuery(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Save coming soon");
        }

        public void OpenQuery(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Open coming soon");
        }
    }
}
