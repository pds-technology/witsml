using System.Runtime.Serialization;
using System.Security;
using Caliburn.Micro;

namespace PDS.Witsml.Studio.Connections
{
    /// <summary>
    /// Connection details for a connection
    /// </summary>
    [DataContract]
    public class Connection : PropertyChangedBase
    {
        /// <summary>
        /// Initializes the <see cref="Connection"/> class.
        /// </summary>
        static Connection()
        {
            AutoMapper.Mapper.Initialize(x => x.CreateMap<Connection, Connection>());
        }

        private string _name;
        /// <summary>
        /// Gets or sets the name of the connection
        /// </summary>
        [DataMember]
        public string Name
        {
            get { return _name; }
            set
            {
                if (!string.Equals(_name, value))
                {
                    _name = value;
                    NotifyOfPropertyChange(() => Name);
                }
            }
        }

        private string _uri;
        /// <summary>
        /// Gets or sets the uri to access the connection
        /// </summary>
        [DataMember]
        public string Uri
        {
            get { return _uri; }
            set
            {
                if (!string.Equals(_uri, value))
                {
                    _uri = value;
                    NotifyOfPropertyChange(() => Uri);
                }
            }
        }

        private string _username;
        /// <summary>
        /// Gets or sets the username to authenticate the connection
        /// </summary>
        [DataMember]
        public string Username
        {
            get { return _username; }
            set
            {
                if (!string.Equals(_username, value))
                {
                    _username = value;
                    NotifyOfPropertyChange(() => Username);
                }
            }
        }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the SecureString password to authenticate the connection.
        /// </summary>
        public SecureString SecurePassword { get; set; }
    }
}
