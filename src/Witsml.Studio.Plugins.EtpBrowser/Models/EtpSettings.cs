using System.Runtime.Serialization;
using Caliburn.Micro;
using PDS.Witsml.Studio.Connections;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.Models
{
    /// <summary>
    /// Defines all of the properties needed to comunicate via ETP.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    [DataContract]
    public class EtpSettings : PropertyChangedBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EtpSettings"/> class.
        /// </summary>
        public EtpSettings()
        {
            Connection = new Connection();
            Store = new StoreSettings();
        }

        private Connection _connection;
        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>The connection.</value>
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

        private StoreSettings _store;
        /// <summary>
        /// Gets or sets the Store settings.
        /// </summary>
        /// <value>The Store settings.</value>
        [DataMember]
        public StoreSettings Store
        {
            get { return _store; }
            set
            {
                if (!ReferenceEquals(_store, value))
                {
                    _store = value;
                    NotifyOfPropertyChange(() => Store);
                }
            }
        }
    }
}
