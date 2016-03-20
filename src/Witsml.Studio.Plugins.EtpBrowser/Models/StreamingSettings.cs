using System.Runtime.Serialization;
using Caliburn.Micro;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.Models
{
    /// <summary>
    /// Encapsulates the ETP Browser settings for the Channel Streaming protocol.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    [DataContract]
    public class StreamingSettings : PropertyChangedBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingSettings"/> class.
        /// </summary>
        public StreamingSettings()
        {
            Uris = new BindableCollection<string>();
        }

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

        /// <summary>
        /// Gets or sets the uri collection.
        /// </summary>
        /// <value>The collection of uris.</value>
        [DataMember]
        public BindableCollection<string> Uris { get; private set; }
    }
}
