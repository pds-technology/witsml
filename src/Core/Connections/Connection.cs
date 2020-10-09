//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.ComponentModel;
using System.Net;
using System.Runtime.Serialization;
using System.Security;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;

namespace PDS.WITSMLstudio.Connections
{
    /// <summary>
    /// Connection details for a connection
    /// </summary>
    [DataContract]
    public class Connection : INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes the <see cref="Connection"/> class.
        /// </summary>
        static Connection()
        {
            AutoMapper.Mapper.Initialize(x => x.CreateMap<Connection, Connection>());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        public Connection()
        {
            SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            IsAuthenticationBasic = true;
            PreAuthenticate = true;
            ProxyPort = 80;
            RedirectPort = 9005;
            SubProtocol = EtpSettings.EtpSubProtocolName;
            EtpEncoding = "binary";
        }

        private AuthenticationTypes _authenticationType;

        /// <summary>
        /// Gets or sets the authentication type.
        /// </summary>
        /// <value>The authentication type.</value>
        [DataMember]
        public AuthenticationTypes AuthenticationType
        {
            get { return _authenticationType; }
            set
            {
                if (_authenticationType != value)
                {
                    _authenticationType = value;
                    NotifyOfPropertyChange(nameof(AuthenticationType));
                }
            }
        }

        private SecurityProtocolType _securityProtocol;

        /// <summary>
        /// Gets or sets the security protocol.
        /// </summary>
        /// <value>The security protocol.</value>
        [DataMember]
        public SecurityProtocolType SecurityProtocol
        {
            get { return _securityProtocol; }
            set
            {
                if (_securityProtocol != value)
                {
                    _securityProtocol = value;
                    NotifyOfPropertyChange(nameof(SecurityProtocol));
                }
            }
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
                    NotifyOfPropertyChange(nameof(Name));
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
                    NotifyOfPropertyChange(nameof(Uri));
                }
            }
        }

        private string _clientId;

        /// <summary>
        /// Gets or sets the client ID
        /// </summary>
        [DataMember]
        public string ClientId
        {
            get { return _clientId; }
            set
            {
                if (!string.Equals(_clientId, value))
                {
                    _clientId = value;
                    NotifyOfPropertyChange(nameof(ClientId));
                }
            }
        }

        private int _redirectPort;

        /// <summary>
        /// Gets or sets the redirect port.
        /// </summary>
        /// <value>The redirect port.</value>
        [DataMember]
        public int RedirectPort
        {
            get { return _redirectPort; }
            set
            {
                if (_redirectPort != value)
                {
                    _redirectPort = value;
                    NotifyOfPropertyChange(nameof(RedirectPort));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the proxy host name is not a URI.
        /// </summary>
        public bool IsProxyHostName => !ProxyHost?.Contains("://") ?? false;

        private string _proxyHost;

        /// <summary>
        /// Gets or sets the proxy host name.
        /// </summary>
        [DataMember]
        public string ProxyHost
        {
            get { return _proxyHost; }
            set
            {
                if (!string.Equals(_proxyHost, value))
                {
                    _proxyHost = value;
                    NotifyOfPropertyChange(nameof(ProxyHost));
                    NotifyOfPropertyChange(nameof(IsProxyHostName));
                }
            }
        }

        private int _proxyPort;

        /// <summary>
        /// Gets or sets the proxy port.
        /// </summary>
        /// <value>The proxy port.</value>
        [DataMember]
        public int ProxyPort
        {
            get { return _proxyPort; }
            set
            {
                if (_proxyPort != value)
                {
                    _proxyPort = value;
                    NotifyOfPropertyChange(nameof(ProxyPort));
                }
            }
        }

        private string _proxyUsername;

        /// <summary>
        /// Gets or sets the username to authenticate with the proxy server.
        /// </summary>
        [DataMember]
        public string ProxyUsername
        {
            get { return _proxyUsername; }
            set
            {
                if (!string.Equals(_proxyUsername, value))
                {
                    _proxyUsername = value;
                    NotifyOfPropertyChange(nameof(ProxyUsername));
                }
            }
        }

        /// <summary>
        /// Gets or sets the proxy password.
        /// </summary>
        [DataMember]
        public string ProxyPassword { get; set; }

        /// <summary>
        /// Gets or sets the SecureString password to authenticate with the proxy server.
        /// </summary>
        public SecureString SecureProxyPassword { get; set; }

        private bool _proxyUseDefaultCredentials;

        /// <summary>
        /// Gets or sets a value indicating whether to use default credentials for the web proxy.
        /// </summary>
        [DataMember]
        public bool ProxyUseDefaultCredentials
        {
            get { return _proxyUseDefaultCredentials; }
            set
            {
                if (_proxyUseDefaultCredentials != value)
                {
                    _proxyUseDefaultCredentials = value;
                    NotifyOfPropertyChange(nameof(ProxyUseDefaultCredentials));
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
                    NotifyOfPropertyChange(nameof(Username));
                }
            }
        }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        [DataMember]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the SecureString password to authenticate the connection.
        /// </summary>
        public SecureString SecurePassword { get; set; }

        private string _jsonWebToken;

        /// <summary>
        /// Gets or sets the JSON Web Token to authenticate the connection.
        /// </summary>
        [DataMember]
        public string JsonWebToken
        {
            get { return _jsonWebToken; }
            set
            {
                if (!string.Equals(_jsonWebToken, value))
                {
                    _jsonWebToken = value;
                    NotifyOfPropertyChange(nameof(JsonWebToken));
                }
            }
        }

        private WebSocketType _webSocketType;

        /// <summary>
        /// Gets or sets the type of the web socket.
        /// </summary>
        [DataMember]
        public WebSocketType WebSocketType
        {
            get { return _webSocketType; }
            set
            {
                if (_webSocketType == value) return;
                _webSocketType = value;
                NotifyOfPropertyChange(nameof(WebSocketType));
            }
        }

        private string _subProtocol;

        /// <summary>
        /// Gets or sets the sub protocol.
        /// </summary>
        [DataMember]
        public string SubProtocol
        {
            get { return _subProtocol; }
            set
            {
                if (!string.Equals(_subProtocol, value))
                {
                    _subProtocol = value;
                    NotifyOfPropertyChange(nameof(SubProtocol));
                }
            }
        }

        private string _etpEncoding;

        /// <summary>
        /// Gets or sets the ETP encoding.
        /// </summary>
        [DataMember]
        public string EtpEncoding
        {
            get { return _etpEncoding; }
            set
            {
                if (!string.Equals(_etpEncoding, value))
                {
                    _etpEncoding = value;
                    NotifyOfPropertyChange(nameof(EtpEncoding));
                }
            }
        }

        private string _etpCompression;

        /// <summary>
        /// Gets or sets the ETP compression.
        /// </summary>
        [DataMember]
        public string EtpCompression
        {
            get { return _etpCompression; }
            set
            {
                if (!string.Equals(_etpCompression, value))
                {
                    _etpCompression = value;
                    NotifyOfPropertyChange(nameof(EtpCompression));
                }
            }
        }

        private bool _acceptInvalidCertificates;

        /// <summary>
        /// Gets or sets a value indicating whether to accept invalid certificates.
        /// </summary>
        [DataMember]
        public bool AcceptInvalidCertificates
        {
            get { return _acceptInvalidCertificates; }
            set
            {
                if (_acceptInvalidCertificates != value)
                {
                    _acceptInvalidCertificates = value;
                    NotifyOfPropertyChange(nameof(AcceptInvalidCertificates));
                }
            }
        }

        private bool _preAuthenticate;

        /// <summary>
        /// Gets or sets a value indicating whether to pre-authenticate.
        /// </summary>
        [DataMember]
        public bool PreAuthenticate
        {
            get { return _preAuthenticate; }
            set
            {
                if (_preAuthenticate != value)
                {
                    _preAuthenticate = value;
                    NotifyOfPropertyChange(nameof(PreAuthenticate));
                }
            }
        }

        private bool _isAuthenticationBasic;

        /// <summary>
        /// Gets or sets a value indicating whether to connect using Basic authentication.
        /// </summary>
        [DataMember]
        public bool IsAuthenticationBasic
        {
            get { return _isAuthenticationBasic; }
            set
            {
                if (_isAuthenticationBasic != value)
                {
                    _isAuthenticationBasic = value;
                    IsAuthenticationBearer = !value;
                    NotifyOfPropertyChange(nameof(IsAuthenticationBasic));
                }
            }
        }
        
        private bool _isAuthenticationBearer;

        /// <summary>
        /// Gets or sets a value indicating whether to connect using Bearer authentication.
        /// </summary>
        [DataMember]
        public bool IsAuthenticationBearer
        {
            get { return _isAuthenticationBearer; }
            set
            {
                if (_isAuthenticationBearer != value)
                {
                    _isAuthenticationBearer = value;
                    IsAuthenticationBasic = !value;
                    NotifyOfPropertyChange(nameof(IsAuthenticationBearer));
                }
            }
        }

        private CompressionMethods _soapRequestCompressionMethod;

        /// <summary>
        /// Gets or sets the compression method for SOAP client requests.
        /// </summary>
        [DataMember]
        public CompressionMethods SoapRequestCompressionMethod
        {
            get { return _soapRequestCompressionMethod; }
            set
            {
                if (_soapRequestCompressionMethod != value)
                {
                    _soapRequestCompressionMethod = value;
                    NotifyOfPropertyChange(nameof(SoapRequestCompressionMethod));
                }
            }
        }

        private bool _soapAcceptCompressedResponses;

        /// <summary>
        /// Gets or sets the compression method for SOAP client requests.
        /// </summary>
        [DataMember]
        public bool SoapAcceptCompressedResponses
        {
            get { return _soapAcceptCompressedResponses; }
            set
            {
                if (_soapAcceptCompressedResponses != value)
                {
                    _soapAcceptCompressedResponses = value;
                    NotifyOfPropertyChange(nameof(SoapAcceptCompressedResponses));
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"Uri: {Uri}; Username: {Username}; AuthenticationType: {AuthenticationType}; SecurityProtocol: {SecurityProtocol};" +
                   $" ProxyHost: {ProxyHost}; ProxyPort: {ProxyPort}; ProxyUsername: {ProxyUsername}; ProxyUseDefaultCredentials: {ProxyUseDefaultCredentials};" +
                   $" WebSocketType: {WebSocketType};" +
                   (!string.IsNullOrWhiteSpace(SubProtocol) ? $" SubProtocol: {SubProtocol};" : string.Empty) +
                   (!string.IsNullOrWhiteSpace(EtpEncoding) ? $" EtpEncoding: {EtpEncoding};" : string.Empty) +
                   (!string.IsNullOrWhiteSpace(EtpCompression) ? $" EtpCompression: {EtpCompression};" : string.Empty) +
                   $" SoapRequestCompressionMethod: {SoapRequestCompressionMethod};" +
                   $" SoapAcceptCompressedResponses: {SoapAcceptCompressedResponses};";
        }

        #region INotifyPropertyChanged Members
        /// <summary>
        /// Occurs when a property value changes. 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggers PropertyChanged Event
        /// </summary>
        /// <param name="info">Name of property changed</param>
        protected void NotifyOfPropertyChange(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
        #endregion INotifyPropertyChanged Members
    }
}
