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
            (Parent as RequestViewModel).SubmitQuery(RequestTypes.Add);
        }

        public void UpdateInStore()
        {
            (Parent as RequestViewModel).SubmitQuery(RequestTypes.Update);
        }

        public void DeleteFromStore()
        {
            (Parent as RequestViewModel).SubmitQuery(RequestTypes.Delete);
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
