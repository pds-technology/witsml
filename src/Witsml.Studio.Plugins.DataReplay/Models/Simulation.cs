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

using System.Runtime.Serialization;
using Caliburn.Micro;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Datatypes.ChannelData;
using PDS.Witsml.Studio.Core.Connections;

namespace PDS.Witsml.Studio.Plugins.DataReplay.Models
{
    [DataContract]
    public class Simulation : PropertyChangedBase
    {
        public Simulation()
        {
            Channels = new BindableCollection<ChannelMetadataRecord>();
            WitsmlConnection = new Connection();
            EtpConnection = new Connection();
            LogIndexType = LogIndexType.measureddepth;
            IsSimpleStreamer = true;
            PortNumber = 9000;
        }

        private string _name;
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

        private Connection _witsmlConnection;
        [DataMember]
        public Connection WitsmlConnection
        {
            get { return _witsmlConnection; }
            set
            {
                if (!ReferenceEquals(_witsmlConnection, value))
                {
                    _witsmlConnection = value;
                    NotifyOfPropertyChange(() => WitsmlConnection);
                }
            }
        }

        private Connection _etpConnection;
        [DataMember]
        public Connection EtpConnection
        {
            get { return _etpConnection; }
            set
            {
                if (!ReferenceEquals(_etpConnection, value))
                {
                    _etpConnection = value;
                    NotifyOfPropertyChange(() => EtpConnection);
                }
            }
        }

        private int _portNumber;
        [DataMember]
        public int PortNumber
        {
            get { return _portNumber; }
            set
            {
                if (_portNumber != value)
                {
                    _portNumber = value;
                    NotifyOfPropertyChange(() => PortNumber);
                }
            }
        }

        private string _wellName;
        [DataMember]
        public string WellName
        {
            get { return _wellName; }
            set
            {
                if (!string.Equals(_wellName, value))
                {
                    _wellName = value;
                    NotifyOfPropertyChange(() => WellName);
                }
            }
        }

        private string _wellUid;
        [DataMember]
        public string WellUid
        {
            get { return _wellUid; }
            set
            {
                if (!string.Equals(_wellUid, value))
                {
                    _wellUid = value;
                    NotifyOfPropertyChange(() => WellUid);
                }
            }
        }

        private string _wellboreName;
        [DataMember]
        public string WellboreName
        {
            get { return _wellboreName; }
            set
            {
                if (!string.Equals(_wellboreName, value))
                {
                    _wellboreName = value;
                    NotifyOfPropertyChange(() => WellboreName);
                }
            }
        }

        private string _wellboreUid;
        [DataMember]
        public string WellboreUid
        {
            get { return _wellboreUid; }
            set
            {
                if (!string.Equals(_wellboreUid, value))
                {
                    _wellboreUid = value;
                    NotifyOfPropertyChange(() => WellboreUid);
                }
            }
        }

        private string _logName;
        [DataMember]
        public string LogName
        {
            get { return _logName; }
            set
            {
                if (!string.Equals(_logName, value))
                {
                    _logName = value;
                    NotifyOfPropertyChange(() => LogName);
                }
            }
        }

        private string _logUid;
        [DataMember]
        public string LogUid
        {
            get { return _logUid; }
            set
            {
                if (!string.Equals(_logUid, value))
                {
                    _logUid = value;
                    NotifyOfPropertyChange(() => LogUid);
                }
            }
        }

        private LogIndexType _logIndexType;
        [DataMember]
        public LogIndexType LogIndexType
        {
            get { return _logIndexType; }
            set
            {
                if (_logIndexType != value)
                {
                    _logIndexType = value;
                    NotifyOfPropertyChange(() => LogIndexType);
                }
            }
        }

        private BindableCollection<ChannelMetadataRecord> _channels;
        [DataMember]
        public BindableCollection<ChannelMetadataRecord> Channels
        {
            get { return _channels; }
            set
            {
                if (!ReferenceEquals(_channels, value))
                {
                    _channels = value;
                    NotifyOfPropertyChange(() => Channels);
                }
            }
        }

        private string _witsmlVersion;
        [DataMember]
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

        private bool _isSimpleStreamer;
        [DataMember]
        public bool IsSimpleStreamer
        {
            get { return _isSimpleStreamer; }
            set
            {
                if (_isSimpleStreamer != value)
                {
                    _isSimpleStreamer = value;
                    NotifyOfPropertyChange(() => IsSimpleStreamer);
                }
            }
        }
    }
}
