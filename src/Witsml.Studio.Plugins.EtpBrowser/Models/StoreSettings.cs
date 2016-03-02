using System.Runtime.Serialization;
using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.Models
{
    [DataContract]
    public class StoreSettings : PropertyChangedBase
    {
        private string _uri;
        /// <summary>
        /// Gets or sets the uri.
        /// </summary>
        /// <value>The uri.</value>
        [DataMember]
        public string Uri
        {
            get { return _uri; }
            set
            {
                if (!ReferenceEquals(_uri, value))
                {
                    _uri = value;
                    NotifyOfPropertyChange(() => Uri);
                }
            }
        }

        private string _uuid;
        /// <summary>
        /// Gets or sets the uuid.
        /// </summary>
        /// <value>The uuid.</value>
        [DataMember]
        public string Uuid
        {
            get { return _uuid; }
            set
            {
                if (!ReferenceEquals(_uuid, value))
                {
                    _uuid = value;
                    NotifyOfPropertyChange(() => Uuid);
                }
            }
        }

        private string _name;
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [DataMember]
        public string Name
        {
            get { return _name; }
            set
            {
                if (!ReferenceEquals(_name, value))
                {
                    _name = value;
                    NotifyOfPropertyChange(() => Name);
                }
            }
        }

        private string _contentType;
        /// <summary>
        /// Gets or sets the content type.
        /// </summary>
        /// <value>The content type.</value>
        [DataMember]
        public string ContentType
        {
            get { return _contentType; }
            set
            {
                if (!ReferenceEquals(_contentType, value))
                {
                    _contentType = value;
                    NotifyOfPropertyChange(() => ContentType);
                }
            }
        }
    }
}
