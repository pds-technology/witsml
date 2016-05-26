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
    /// <summary>
    /// Manages the context for WITSML connections and data.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Linq.IWitsmlContext" />
    /// <seealso cref="System.IDisposable" />
    public abstract class WitsmlContext : IWitsmlContext, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlContext"/> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="timeoutInMinutes">The timeout in minutes.</param>
        /// <param name="version">The version.</param>
        protected WitsmlContext(string url, double timeoutInMinutes, WMLSVersion version) : this()
        {
            Connect(url, timeoutInMinutes, version);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlContext"/> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="timeoutInMinutes">The timeout in minutes.</param>
        /// <param name="version">The version.</param>
        protected WitsmlContext(string url, string username, string password, double timeoutInMinutes, WMLSVersion version) : this()
        {
            Connect(url, username, password, timeoutInMinutes, version);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlContext"/> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="timeoutInMinutes">The timeout in minutes.</param>
        /// <param name="version">The version.</param>
        protected WitsmlContext(string url, string username, SecureString password, double timeoutInMinutes, WMLSVersion version) : this()
        {
            Connect(url, username, password, timeoutInMinutes, version);
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="WitsmlContext"/> class from being created.
        /// </summary>
        private WitsmlContext()
        {
            LogQuery = (f, q, o) => { };
            LogResponse = (f, q, o, r, c, s) => { };
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        public WITSMLWebServiceConnection Connection { get; private set; }

        /// <summary>
        /// Gets the data schema version.
        /// </summary>
        /// <value>
        /// The data schema version.
        /// </value>
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

        /// <summary>
        /// Creates one instance of the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> One<T>()
        {
            return new List<T>()
            {
                Activator.CreateInstance<T>()
            };
        }

        /// <summary>
        /// Gets all wells.
        /// </summary>
        /// <returns>The wells.</returns>
        public abstract IEnumerable<IDataObject> GetAllWells();

        /// <summary>
        /// Gets the wellbores.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>The wellbores.</returns>
        public abstract IEnumerable<IWellObject> GetWellbores(EtpUri parentUri);

        /// <summary>
        /// Gets the wellbore objects.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>The wellbore objects of specified type.</returns>
        public IEnumerable<IWellboreObject> GetWellboreObjects(string objectType, EtpUri parentUri)
        {
            return GetObjects<IWellboreObject>(objectType, parentUri, OptionsIn.ReturnElements.IdOnly);
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

        /// <summary>
        /// Gets the objects of the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Creates the WITSML query.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns></returns>
        protected IWitsmlQuery CreateWitsmlQuery(string objectType)
        {
            var listType = ObjectTypes.GetObjectGroupType(objectType, DataSchemaVersion);
            var dataType = ObjectTypes.GetObjectType(objectType, DataSchemaVersion);

            return GetType()
                .GetMethod("CreateQuery", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(dataType, listType)
                .Invoke(this, new object[0]) as IWitsmlQuery;
        }

        /// <summary>
        /// Creates the WITSML query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TList">The type of the list.</typeparam>
        /// <returns></returns>
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

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
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
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
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
