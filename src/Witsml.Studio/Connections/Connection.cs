using System.Runtime.Serialization;
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

        private string _password;
        /// <summary>
        /// Gets or sets the password to authenticate the connection
        /// </summary>
        [DataMember]
        public string Password
        {
            get { return _password; }
            set
            {
                if (!string.Equals(_password, value))
                {
                    _password = value;
                    NotifyOfPropertyChange(() => Password);
                }
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance 
        /// by testing that all of the public properties are equal (Name, Uri, Username, Password).
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var connection = obj as Connection;

            if (connection != null)
            {
                return (
                    Name.Equals(connection.Name) &&
                    Uri.Equals(connection.Uri) &&
                    Username.Equals(connection.Username) &&
                    Password.Equals(connection.Password));
            }
            return false;
        }
    }
}
