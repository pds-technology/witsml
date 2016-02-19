using System.Runtime.Serialization;
using Caliburn.Micro;
using PDS.Witsml.Studio.Models;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.Models
{
    [DataContract]
    public class EtpSettings : PropertyChangedBase
    {
        public EtpSettings()
        {
            Connection = new Connection() { ConnectionType = ConnectionTypes.Etp.Value };
        }

        private Connection _connection;
        [DataMember]
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

        private string _version;
        [DataMember]
        public string Version
        {
            get { return _version; }
            set
            {
                if (!string.Equals(_version, value))
                {
                    _version = value;
                    NotifyOfPropertyChange(() => Version);
                }
            }
        }
    }
}
