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

using Caliburn.Micro;
using PDS.Witsml.Studio.Core.Connections;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.Models
{
    public class WitsmlSettings : PropertyChangedBase
    {
        public WitsmlSettings()
        {
            Connection = new Connection();
            StoreFunction = Functions.GetFromStore;
            MaxDataRows = 1000;
        }

        private Connection _connection;
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

        private OptionsIn.ReturnElements _returnElementType;
        public OptionsIn.ReturnElements ReturnElementType
        {
            get { return _returnElementType; }
            set
            {
                if (_returnElementType != value)
                {
                    _returnElementType = value;
                    NotifyOfPropertyChange(() => ReturnElementType);
                }
            }
        }

        private bool _isRequestObjectSelectionCapability;
        public bool IsRequestObjectSelectionCapability
        {
            get { return _isRequestObjectSelectionCapability; }
            set
            {
                if (_isRequestObjectSelectionCapability != value)
                {
                    _isRequestObjectSelectionCapability = value;
                    OnRequestObjectSelectionCapabilityChanged();
                    NotifyOfPropertyChange(() => IsRequestObjectSelectionCapability);
                }
            }
        }

        private bool _isRequestPrivateGroupOnly;
        public bool IsRequestPrivateGroupOnly
        {
            get { return _isRequestPrivateGroupOnly; }
            set
            {
                if (_isRequestPrivateGroupOnly != value)
                {
                    _isRequestPrivateGroupOnly = value;
                    NotifyOfPropertyChange(() => IsRequestPrivateGroupOnly);
                }
            }
        }

        private string _witsmlVersion;
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

        private int _maxDataRows;
        public int MaxDataRows
        {
            get { return _maxDataRows; }
            set
            {
                if (_maxDataRows != value)
                {
                    _maxDataRows = value;
                    NotifyOfPropertyChange(() => MaxDataRows);
                }
            }
        }

        private Functions _storeFunction;
        public Functions StoreFunction
        {
            get { return _storeFunction; }
            set
            {
                if (_storeFunction != value)
                {
                    _storeFunction = value;
                    NotifyOfPropertyChange(() => StoreFunction);
                }
            }
        }

        private void OnRequestObjectSelectionCapabilityChanged()
        {
            ReturnElementType = IsRequestObjectSelectionCapability ? null : OptionsIn.ReturnElements.All;

            if (IsRequestObjectSelectionCapability && IsRequestPrivateGroupOnly)
            {
                IsRequestPrivateGroupOnly = false;
            }
        }
    }
}
