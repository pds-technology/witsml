using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using PDS.Witsml.Studio.Models;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.Models
{
    //
    // Summary:
    //     This class represents the returnElements OptionIn for GetFromStore
    public enum ReturnElementType
    {
        //
        // Summary:
        //     Only explicitly specified data-object-selection and data item selection items.
        requested = 0,
        //
        // Summary:
        //     May be used for all data-object types.
        all = 1,
        //
        // Summary:
        //     May be used for all data-object types.
        id_only = 2,
        //
        // Summary:
        //     Used only for growing data-objects.
        header_only = 3,
        //
        // Summary:
        //     Used only for growing data-objects.
        data_only = 4,
        //
        // Summary:
        //     A specialization of “data-only” for used only for the trajectory data-object.
        station_location_only = 5,
        //
        // Summary:
        //     Used for changeLog data-object only.
        latest_change_only = 6
    }

    public class Browser : PropertyChangedBase
    {
        public Browser()
        {
            Connection = new Connection();

            ReturnElementTypes = new BindableCollection<ReturnElementType>();
            ReturnElementTypes.AddRange(Enum.GetValues(typeof(ReturnElementType)).OfType<ReturnElementType>());
            ReturnElementType = ReturnElementType.requested;
            MaxDataRows = 1000;

            WitsmlVersions = new BindableCollection<string>();

            // TODO: Remove after testing
            XmlQuery =
        "<? xml version = \"1.0\" encoding = \"utf-8\" standalone = \"yes\" ?>\n" +
        "< wells version = \"1.4.1.1\" xmlns = \"http://www.witsml.org/schemas/1series\" >\n" +
        "    < well uid = \"uid1\" >\n" +
        "        <name>Test Well 1</name>\n" +
        "    </ well >\n" +
        "</ wells > \n";

            // TODO: Remove after testing
            XmlQuery2 =
                "<? xml version = \"1.0\" encoding = \"utf-8\" standalone = \"yes\" ?>\n" +
                "< wells version = \"1.4.1.1\" xmlns = \"http://www.witsml.org/schemas/1series\" >\n" +
                "    < well uid = \"uid1\" >\n" +
                "        <name>Test Well 1</name>\n" +
                "    </ well >\n" +
                "</ wells > \n";
        }

        private IWindowManager _windowManager;
        public IWindowManager WindowManager
        {
            get
            {
                if (_windowManager == null)
                {
                    _windowManager = Application
                        .Current
                        .Container()
                        .Resolve<IWindowManager>();
                }
                return _windowManager;
            }
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

        public BindableCollection<ReturnElementType> ReturnElementTypes { get; }

        private ReturnElementType _returnElementType;
        public ReturnElementType ReturnElementType
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

        public BindableCollection<string> WitsmlVersions { get; }

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

        // TODO: Remove after testing
        private string _xmlQuery2;
        public string XmlQuery2
        {
            get { return _xmlQuery2; }
            set
            {
                if (!string.Equals(_xmlQuery2, value))
                {
                    _xmlQuery2 = value;
                    NotifyOfPropertyChange(() => XmlQuery2);
                }
            }
        }
    }
}
