//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Runtime.Serialization;
using Caliburn.Micro;
using PDS.Witsml.Studio.Connections;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Properties;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.Models
{
    /// <summary>
    /// Defines all of the properties needed to comunicate via ETP.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    [DataContract]
    public class EtpSettings : PropertyChangedBase
    {
        private static readonly int DefaultMaxDataItems = Settings.Default.ChannelStreamingDefaultMaxDataItems;
        private static readonly int DefaultMaxMessageRate = Settings.Default.ChannelStreamingDefaultMaxMessageRate;

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpSettings"/> class.
        /// </summary>
        public EtpSettings()
        {
            Connection = new Connection();
            Streaming = new StreamingSettings()
            {
                MaxDataItems = DefaultMaxDataItems,
                MaxMessageRate = DefaultMaxMessageRate,
                StreamingType = "LatestValue",
                StartTime = DateTime.Now,
                StartIndex = 0,
                IndexCount = 10
            };
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

        private StreamingSettings _streaming;
        /// <summary>
        /// Gets or sets the Channel Streaming settings.
        /// </summary>
        /// <value>The Channel Streaming settings.</value>
        [DataMember]
        public StreamingSettings Streaming
        {
            get { return _streaming; }
            set
            {
                if (!ReferenceEquals(_streaming, value))
                {
                    _streaming = value;
                    NotifyOfPropertyChange(() => Streaming);
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

        private string _applicationName;
        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        /// <value>The name of the application.</value>
        [DataMember]
        public string ApplicationName
        {
            get { return _applicationName; }
            set
            {
                if (!string.Equals(_applicationName, value))
                {
                    _applicationName = value;
                    NotifyOfPropertyChange(() => ApplicationName);
                }
            }
        }

        private string _applicationVersion;
        /// <summary>
        /// Gets or sets the version of the application.
        /// </summary>
        /// <value>The version of the application.</value>
        [DataMember]
        public string ApplicationVersion
        {
            get { return _applicationVersion; }
            set
            {
                if (!string.Equals(_applicationVersion, value))
                {
                    _applicationVersion = value;
                    NotifyOfPropertyChange(() => ApplicationVersion);
                }
            }
        }
    }
}
