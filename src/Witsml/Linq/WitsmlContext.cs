//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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
using System.Collections.Generic;
using System.Security;
using Energistics.DataAccess;

namespace PDS.Witsml.Linq
{
    public abstract class WitsmlContext : IWitsmlContext, IDisposable
    {
        protected WitsmlContext(string url, double timeoutInMinutes, WMLSVersion version) : this()
        {
            Connect(url, timeoutInMinutes, version);
        }

        protected WitsmlContext(string url, string username, string password, double timeoutInMinutes, WMLSVersion version) : this()
        {
            Connect(url, username, password, timeoutInMinutes, version);
        }

        protected WitsmlContext(string url, string username, SecureString password, double timeoutInMinutes, WMLSVersion version) : this()
        {
            Connect(url, username, password, timeoutInMinutes, version);
        }

        private WitsmlContext()
        {
            LogQuery = (f, q, o) => { };
            LogResponse = (f, q, o, r) => { };
        }

        public WITSMLWebServiceConnection Connection { get; private set; }

        public abstract string DataSchemaVersion { get; }

        public Action<Functions, IEnergisticsCollection, IDictionary<string, string>> LogQuery { get; set; }

        public Action<Functions, IEnergisticsCollection, IDictionary<string, string>, IEnergisticsCollection> LogResponse { get; set; }

        public List<T> One<T>()
        {
            return new List<T>()
            {
                Activator.CreateInstance<T>()
            };
        }

        public abstract IEnumerable<IDataObject> GetAllWells();

        public abstract IEnumerable<IWellObject> GetWellbores(string uri); 

        protected IWitsmlQuery<T> CreateQuery<T, TList>() where TList : IEnergisticsCollection
        {
            return new WitsmlQuery<T, TList>(this);
        }

        private void Connect(string url, string username, string password, double timeoutInMinutes, WMLSVersion version)
        {
            Connect(url, timeoutInMinutes, version);

            if (string.IsNullOrWhiteSpace(username)) return;

            Connection.UseDefaultNetworkCredentials = false;
            Connection.Username = username;
            Connection.SetPassword(password);
        }

        private void Connect(string url, string username, SecureString password, double timeoutInMinutes, WMLSVersion version)
        {
            Connect(url, timeoutInMinutes, version);

            if (string.IsNullOrWhiteSpace(username)) return;

            Connection.UseDefaultNetworkCredentials = false;
            Connection.Username = username;
            Connection.SetSecurePassword(password);
        }

        private void Connect(string url, double timeoutInMinutes, WMLSVersion version)
        {
            Connection = new WITSMLWebServiceConnection(url, version)
            {
                UseDefaultNetworkCredentials = true,
                Timeout = (int)(60000 * timeoutInMinutes)
            };
        }

        #region IDisposable Support

        private bool _disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // NOTE: dispose managed state (managed objects).
                }

                // NOTE: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // NOTE: set large fields to null.
                Connection = null;

                _disposedValue = true;
            }
        }

        // NOTE: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WitsmlContext() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // NOTE: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
