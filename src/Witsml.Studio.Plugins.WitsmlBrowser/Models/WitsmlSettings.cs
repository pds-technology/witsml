using System;
using Caliburn.Micro;
using ICSharpCode.AvalonEdit.Document;
using PDS.Witsml.Studio.Connections;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.Models
{
    public class WitsmlSettings : PropertyChangedBase
    {
        public WitsmlSettings()
        {
            Connection = new Connection();
            MaxDataRows = 1000;
            XmlQuery = new TextDocument();
            QueryResults = new TextDocument();

            // TODO: Remove after testing
            XmlQuery.Text = 
                "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>" + Environment.NewLine +
                "<wells version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\" />";
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

        private TextDocument _xmlQuery;
        public TextDocument XmlQuery
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

        private TextDocument _queryResults;
        public TextDocument QueryResults
        {
            get { return _queryResults; }
            set
            {
                if (!string.Equals(_queryResults, value))
                {
                    _queryResults = value;
                    NotifyOfPropertyChange(() => QueryResults);
                }
            }
        }
    }
}
