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

        /// <summary>
        /// Gets or sets the uri collection.
        /// </summary>
        /// <value>The collection of uris.</value>
        [DataMember]
        public BindableCollection<string> Uris { get; private set; }

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

        private int _maxDataItems;
        /// <summary>
        /// Gets or sets the maximum data items.
        /// </summary>
        /// <value>The maximum data items.</value>
        [DataMember]
        public int MaxDataItems
        {
            get { return _maxDataItems; }
            set
            {
                if (_maxDataItems != value)
                {
                    _maxDataItems = value;
                    NotifyOfPropertyChange(() => MaxDataItems);
                }
            }
        }

        private int _maxMessageRate;
        /// <summary>
        /// Gets or sets the maximum message rate, in milliseconds.
        /// </summary>
        /// <value>The maximum message rate.</value>
        [DataMember]
        public int MaxMessageRate
        {
            get { return _maxMessageRate; }
            set
            {
                if (_maxMessageRate != value)
                {
                    _maxMessageRate = value;
                    NotifyOfPropertyChange(() => MaxMessageRate);
                }
            }
        }

        private int _startIndex;
        /// <summary>
        /// Gets or sets the start index.
        /// </summary>
        /// <value>The start index.</value>
        [DataMember]
        public int StartIndex
        {
            get { return _startIndex; }
            set
            {
                if (_startIndex != value)
                {
                    _startIndex = value;
                    NotifyOfPropertyChange(() => StartIndex);
                }
            }
        }

        private int _endIndex;
        /// <summary>
        /// Gets or sets the end index.
        /// </summary>
        /// <value>The end index.</value>
        [DataMember]
        public int EndIndex
        {
            get { return _endIndex; }
            set
            {
                if (_endIndex != value)
                {
                    _endIndex = value;
                    NotifyOfPropertyChange(() => EndIndex);
                }
            }
        }
    }
}
