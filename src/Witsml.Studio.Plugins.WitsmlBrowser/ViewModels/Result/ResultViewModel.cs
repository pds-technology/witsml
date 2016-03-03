using System.Windows;
using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Result
{
    public class ResultViewModel : Screen
    {
        public new MainViewModel Parent
        {
            get { return (MainViewModel)base.Parent; }
        }

        public Models.WitsmlSettings Model
        {
            get { return Parent.Model; }
        }

        private bool _messagesWrapped;
        public bool MessagesWrapped
        {
            get { return _messagesWrapped; }
            set
            {
                if (_messagesWrapped != value)
                {
                    _messagesWrapped = value;
                    NotifyOfPropertyChange(() => MessagesWrapped);
                    NotifyOfPropertyChange(() => MessagesWrappedText);
                }
            }
        }

        private bool _resultsWrapped;
        public bool ResultsWrapped
        {
            get { return _resultsWrapped; }
            set
            {
                if (_resultsWrapped != value)
                {
                    _resultsWrapped = value;
                    NotifyOfPropertyChange(() => ResultsWrapped);
                    NotifyOfPropertyChange(() => ResultsWrappedText);
                }
            }
        }

        public string MessagesWrappedText
        {
            get { return Parent.GetWrappedText(MessagesWrapped); }
        }

        public string ResultsWrappedText
        {
            get { return Parent.GetWrappedText(ResultsWrapped); }
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            MessagesWrapped = false;
        }

        /// <summary>
        /// Copies the results to the clipboard.
        /// </summary>
        public void CopyResults()
        {
            App.Current.Invoke(() => Clipboard.SetText(Parent.QueryResults.Text));
        }

        /// <summary>
        /// Clears the results.
        /// </summary>
        public void ClearResults()
        {
            App.Current.Invoke(() => Parent.QueryResults.Text = string.Empty);
        }

        /// <summary>
        /// Copies the Results to the clipboard.
        /// </summary>
        public void CopyMessages()
        {
            App.Current.Invoke(() => Clipboard.SetText(Parent.Messages.Text));
        }

        /// <summary>
        /// Clears the messages.
        /// </summary>
        public void ClearMessages()
        {
            App.Current.Invoke(() => Parent.Messages.Text = string.Empty);
        }

        public void WrapMessages()
        {
            MessagesWrapped = !MessagesWrapped;
        }

        public void WrapResults()
        {
            ResultsWrapped = !ResultsWrapped;
        }
    }
}
