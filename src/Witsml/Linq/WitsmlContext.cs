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
using PDS.Framework;

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

        /// <summary>
        /// Gets or sets the log query action.
        /// </summary>
        /// <value>
        /// The log query action.
        /// </value>
        public Action<Functions, string, string> LogQuery { get; set; }

        /// <summary>
        /// Gets or sets the log response action.
        /// </summary>
        /// <value>
        /// The log response action.
        /// </value>
        public Action<Functions, string, string, string, short, string> LogResponse { get; set; }

        public List<T> One<T>()
        {
            return new List<T>()
            {
                Activator.CreateInstance<T>()
            };
        }

        public abstract IEnumerable<IDataObject> GetAllWells();

        public abstract IEnumerable<IWellObject> GetWellbores(EtpUri uri);

        /// <summary>
        /// Gets the wellbore objects.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>The wellbore objects of specified type.</returns>
        public IEnumerable<IWellboreObject> GetWellboreObjects(string objectType, EtpUri uri)
        {
            return GetObjects<IWellboreObject>(objectType, uri, OptionsIn.ReturnElements.IdOnly);
        }

        /// <summary>
        /// Gets the growing object header only.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The header for the specified growing objects.</returns>
        public IWellboreObject GetGrowingObjectHeaderOnly(string objectType, EtpUri uri)
        {
            return GetObjects<IWellboreObject>(objectType, uri, OptionsIn.ReturnElements.HeaderOnly).FirstOrDefault();
        }

        /// <summary>
        /// Gets the object identifier only.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The object identifier.</returns>
        public IDataObject GetObjectIdOnly(string objectType, EtpUri uri)
        {
            return GetObjects<IDataObject>(objectType, uri, OptionsIn.ReturnElements.IdOnly).FirstOrDefault();
        }

        /// <summary>
        /// Gets the object details.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The object detail.</returns>
        public IDataObject GetObjectDetails(string objectType, EtpUri uri)
        {
            return GetObjects<IDataObject>(objectType, uri, OptionsIn.ReturnElements.All).FirstOrDefault();
        }

        protected IEnumerable<T> GetObjects<T>(string objectType, EtpUri uri, OptionsIn optionsIn) where T : IDataObject
        {
            var filters = new List<string>();
            var values = new List<object>();
            var count = 0;

            var objectIds = uri.GetObjectIds()
                .ToDictionary(x => x.ObjectType, x => x.ObjectId);

            if (!string.IsNullOrWhiteSpace(uri.ObjectId))
            {
                filters.Add("Uid = @" + (count++));
                values.Add(uri.ObjectId);
            }
            if (objectIds.ContainsKey(ObjectTypes.Well) && !ObjectTypes.Well.EqualsIgnoreCase(objectType))
            {
                filters.Add("UidWell = @" + (count++));
                values.Add(objectIds[ObjectTypes.Well]);
            }
            if (objectIds.ContainsKey(ObjectTypes.Wellbore) && !ObjectTypes.Wellbore.EqualsIgnoreCase(objectType))
            {
                filters.Add("UidWellbore = @" + count);
                values.Add(objectIds[ObjectTypes.Wellbore]);
            }

            var result = CreateWitsmlQuery(objectType)
                .With(optionsIn)
                .Where(string.Join(" && ", filters), values.ToArray())
                .GetEnumerator();

            var dataObjects = new List<T>();

            while (result.MoveNext())
            {
                dataObjects.Add((T)result.Current);
            }

            return dataObjects.OrderBy(x => x.Name);
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
