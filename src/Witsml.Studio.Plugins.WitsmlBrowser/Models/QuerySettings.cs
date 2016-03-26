using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.Models
{
    public class QuerySettings : PropertyChangedBase
    {
        public QuerySettings()
        {
            StoreFunction = Functions.GetFromStore;
        }

        private Functions _storeFunction;
        public Functions StoreFunction
        {
            get { return _storeFunction; }
            set
            {
                if (_storeFunction != value)
                {
                    _storeFunction = value;
                    NotifyOfPropertyChange(() => StoreFunction);
                }
            }
        }

    }
}
