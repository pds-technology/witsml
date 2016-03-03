using System.Windows;
using Caliburn.Micro;
using PDS.Witsml.Studio.Runtime;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    public class QueryViewModel : Screen
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        public QueryViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = "Query";
        }

        public new RequestViewModel Parent
        {
            get { return (RequestViewModel)base.Parent; }
        }

        public MainViewModel MainViewModel
        {
            get { return (MainViewModel)Parent.Parent; }
        }

        public Models.WitsmlSettings Model
        {
            get { return Parent.Model; }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; private set; }

        private bool _queryWrapped;
        public bool QueryWrapped
        {
            get { return _queryWrapped; }
            set
            {
                if (_queryWrapped != value)
                {
                    _queryWrapped = value;
                    NotifyOfPropertyChange(() => QueryWrapped);
                    NotifyOfPropertyChange(() => QueryWrappedText);
                }
            }
        }

        public string QueryWrappedText
        {
            get { return MainViewModel.GetWrappedText(QueryWrapped); }
        }

        public void GetFromStore()
        {
            MainViewModel.SubmitQuery(Functions.GetFromStore);
        }

        public void AddToStore()
        {
            MainViewModel.SubmitQuery(Functions.AddToStore);
        }

        public void UpdateInStore()
        {
            MainViewModel.SubmitQuery(Functions.UpdateInStore);
        }

        public void DeleteFromStore()
        {
            MainViewModel.SubmitQuery(Functions.DeleteFromStore);
        }

        public void SaveQuery(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Save coming soon");
        }

        public void OpenQuery(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Open coming soon");
        }

        /// <summary>
        /// Copies the query to the clipboard.
        /// </summary>
        public void CopyQuery()
        {
            Runtime.Invoke(() => Clipboard.SetText(Parent.Parent.XmlQuery.Text));
        }

        /// <summary>
        /// Clears the query.
        /// </summary>
        public void ClearQuery()
        {
            Runtime.Invoke(() => Parent.Parent.XmlQuery.Text = string.Empty);
        }

        public void WrapQuery()
        {
            QueryWrapped = !QueryWrapped;
        }
    }
}
