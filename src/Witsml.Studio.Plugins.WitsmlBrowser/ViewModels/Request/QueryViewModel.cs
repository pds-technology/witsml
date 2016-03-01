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

        public void GetFromStore()
        {
            (Parent as RequestViewModel).SubmitQuery(RequestTypes.Get);
        }

        public void AddToStore()
        {
            MessageBox.Show("Add coming soon");
        }

        public void UpdateInStore()
        {
            MessageBox.Show("Update coming soon");
        }

        public void DeleteFromStore()
        {
            MessageBox.Show("Delete coming soon");
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
