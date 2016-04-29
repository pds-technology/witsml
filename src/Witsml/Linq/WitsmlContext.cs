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
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using System.Security;
using Energistics.DataAccess;
using Energistics.Datatypes;

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
            LogResponse = (f, q, o, r, c, s) => { };
        }

        public WITSMLWebServiceConnection Connection { get; private set; }

        public abstract string DataSchemaVersion { get; }

        public Action<Functions, IEnergisticsCollection, string> LogQuery { get; set; }

        public Action<Functions, IEnergisticsCollection, string, IEnergisticsCollection, short, string> LogResponse { get; set; }

        public List<T> One<T>()
        {
            return new List<T>()
            {
                Activator.CreateInstance<T>()
            };
        }

        public abstract IEnumerable<IDataObject> GetAllWells();

        public abstract IEnumerable<IWellObject> GetWellbores(string uri);

        public virtual IEnumerable<IWellboreObject> GetWellboreObjects(string objectType, string uri)
        {
            var etpUri = new EtpUri(uri);

            var objectIds = etpUri.GetObjectIds()
                .ToDictionary(x => x.Key, x => x.Value);

            var uidWell = objectIds[ObjectTypes.Well];
            var uidWellbore = etpUri.ObjectId;

            var result = CreateWitsmlQuery(objectType)
                .With(OptionsIn.ReturnElements.IdOnly)
                .Where("UidWell = @0 && UidWellbore = @1", uidWell, uidWellbore)
                .GetEnumerator();

            var dataObjects = new List<IWellboreObject>();

            while (result.MoveNext())
            {
                dataObjects.Add((IWellboreObject)result.Current);
            }

            return dataObjects.OrderBy(x => x.Name);
        }

        public virtual IWellboreObject GetGrowingObjectHeaderOnly(string objectType, string uri)
        {
            var etpUri = new EtpUri(uri);

            var objectIds = etpUri.GetObjectIds()
                .ToDictionary(x => x.Key, x => x.Value);

            var uidWell = objectIds[ObjectTypes.Well];
            var uidWellbore = objectIds[ObjectTypes.Wellbore];
            var uid = etpUri.ObjectId;

            var result = CreateWitsmlQuery(objectType)
                .With(OptionsIn.ReturnElements.HeaderOnly)
                .Where("UidWell = @0 && UidWellbore = @1 && Uid = @2", uidWell, uidWellbore, uid)
                .GetEnumerator();

            if (result.MoveNext())
                return (IWellboreObject) result.Current;

            return null;
        }

        protected IWitsmlQuery CreateWitsmlQuery(string objectType)
        {
            var listType = ObjectTypes.GetObjectGroupType(objectType, DataSchemaVersion);
            var dataType = ObjectTypes.GetObjectType(objectType, DataSchemaVersion);

            return GetType()
                .GetMethod("CreateQuery", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(dataType, listType)
                .Invoke(this, new object[0]) as IWitsmlQuery;
        }

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
