using Caliburn.Micro;
using PDS.Witsml.Studio.Models;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.Models
{
    // TODO: Rename class to what the data is related to, i.e., WitsmlSettings
    public class Browser : PropertyChangedBase
    {
        public Browser()
        {
            Connection = new Connection() { ConnectionType = ConnectionTypes.Witsml.Value };
            MaxDataRows = 1000;

            // TODO: Remove after testing
            XmlQuery =
        "<? xml version = \"1.0\" encoding = \"utf-8\" standalone = \"yes\" ?>\n" +
        "< wells version = \"1.4.1.1\" xmlns = \"http://www.witsml.org/schemas/1series\" >\n" +
        "    < well uid = \"uid1\" >\n" +
        "        <name>Test Well 1</name>\n" +
        "    </ well >\n" +
        "</ wells > \n";
        }

        private Connection _connection;
        public Connection Connection
        {
            get { return _connection; }
            set
            {
                if (!ReferenceEquals(_connection, value))
                {
                    _connection = value;
                    NotifyOfPropertyChange(() => Connection);
                }
            }
        }

        private string _returnElementType;
        public string ReturnElementType
        {
            get { return _returnElementType; }
            set
            {
                if (_returnElementType != value)
                {
                    _returnElementType = value;
                    NotifyOfPropertyChange(() => ReturnElementType);
                }
            }
        }

        private string _witsmlVersion;
        public string WitsmlVersion
        {
            get { return _witsmlVersion; }
            set
            {
                if (_witsmlVersion != value)
                {
                    _witsmlVersion = value;
                    NotifyOfPropertyChange(() => WitsmlVersion);
                }
            }
        }

        private int _maxDataRows;
        public int MaxDataRows
        {
            get { return _maxDataRows; }
            set
            {
                if (_maxDataRows != value)
                {
                    _maxDataRows = value;
                    NotifyOfPropertyChange(() => MaxDataRows);
                }
            }
        }

        // TODO: Remove after testing
        private string _xmlQuery;
        public string XmlQuery
        {
            get { return _xmlQuery; }
            set
            {
                if (!string.Equals(_xmlQuery, value))
                {
                    _xmlQuery = value;
                    NotifyOfPropertyChange(() => XmlQuery);
                }
            }
        }
    }
}
