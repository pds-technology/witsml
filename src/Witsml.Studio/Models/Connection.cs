using System.Runtime.Serialization;
using Caliburn.Micro;

namespace PDS.Witsml.Studio.Models
{
    /// <summary>
    /// A data object of connection properties
    /// </summary>
    [DataContract]
    public class Connection : PropertyChangedBase
    {
        private string _name;
        /// <summary>
        /// The name of the connection
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
        /// The uri to acces the connection
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
        /// The username to authenticate the connection
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
        /// The password to authenticate the connection
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
    }
}
