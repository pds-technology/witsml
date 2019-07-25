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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using System.Security;
using Energistics.DataAccess;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Linq
{
    /// <summary>
    /// Manages the context for WITSML connections and data.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Linq.IWitsmlContext" />
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
            SetDataSchemaVersion(version);
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
            SetDataSchemaVersion(version);
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
            SetDataSchemaVersion(version);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlContext"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="version">The version.</param>
        protected WitsmlContext(WITSMLWebServiceConnection connection, WMLSVersion version) : this()
        {
            Connection = connection;
            SetDataSchemaVersion(version);
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="WitsmlContext"/> class from being created.
        /// </summary>
        private WitsmlContext()
        {
            LogQuery = (f, t, q, o) => { };
            LogResponse = (f, t, q, o, r, c, s) => { };
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        public WITSMLWebServiceConnection Connection { get; private set; }

        /// <summary>
        /// Gets the data schema version.
        /// </summary>
        public string DataSchemaVersion { get; private set; }

        /// <summary>
        /// Gets or sets the log query action.
        /// </summary>
        public Action<Functions, string, string, string> LogQuery { get; set; }

        /// <summary>
        /// Gets or sets the log response action.
        /// </summary>
        public Action<Functions, string, string, string, string, short, string> LogResponse { get; set; }

        /// <summary>
        /// Creates one instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The specified type</typeparam>
        /// <returns>A <see cref="List{T}"/> containing one instance of the specified tyep.</returns>
        public List<T> One<T>()
        {
            return new List<T>()
            {
                Activator.CreateInstance<T>()
            };
        }

        /// <summary>
        /// Gets the supported get from store objects.
        /// </summary>
        /// <returns>The array of supported get from store objects.</returns>
        public abstract string[] GetSupportedGetFromStoreObjects();

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
        /// Gets the name and IDs of active wellbores.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <param name="logXmlResponse">If set to <c>true</c> then log the XML response.</param>
        /// <returns>The name and IDs of the wellbores.</returns>
        public virtual IEnumerable<IWellObject> GetActiveWellbores(EtpUri parentUri, bool logXmlResponse = true)
        {
            return GetObjects<IWellboreObject>(ObjectTypes.Wellbore, parentUri, OptionsIn.ReturnElements.IdOnly).Where(o => o.GetWellboreStatus().GetValueOrDefault());
        }

        /// <summary>
        /// Gets the wellbore objects.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <param name="logXmlResponse">If set to <c>true</c> then log the XML response.</param>
        /// <returns>The wellbore objects of specified type.</returns>
        public virtual IEnumerable<IWellboreObject> GetWellboreObjects(string objectType, EtpUri parentUri, bool logXmlResponse = true)
        {
            return GetObjects<IWellboreObject>(objectType, parentUri, OptionsIn.ReturnElements.IdOnly);
        }

        /// <summary>
        /// Gets the names and IDs of wellbore objects.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>The names and IDs of wellbore objects of specified type.</returns>
        public virtual IEnumerable<IWellboreObject> GetWellboreObjectIds(string objectType, EtpUri parentUri)
        {
            return GetObjects<IWellboreObject>(objectType, parentUri, OptionsIn.ReturnElements.IdOnly);
        }

        /// <summary>
        /// Gets the growing object header only.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The header for the specified growing objects.</returns>
        public virtual IWellboreObject GetGrowingObjectHeaderOnly(string objectType, EtpUri uri)
        {
            return GetObjects<IWellboreObject>(objectType, uri, OptionsIn.ReturnElements.HeaderOnly).FirstOrDefault();
        }

        /// <summary>
        /// Gets the name and IDs of growing objects with active status.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <param name="logXmlResponse">If set to <c>true</c> then log the XML response.</param>
        /// <returns>The name and IDs of the wellbore objects of specified type.</returns>
        public virtual IEnumerable<IWellboreObject> GetGrowingObjects(string objectType, EtpUri parentUri, bool logXmlResponse = true)
        {
            var objects = GetObjects<IWellboreObject>(objectType, parentUri, OptionsIn.ReturnElements.HeaderOnly);

            return objects.Where(o => o.GetObjectGrowingStatus() ?? false);
        }

        /// <summary>
        /// Gets the growing objects id-only with object growing status.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <param name="indexType">Type of the index.</param>
        /// <returns> The wellbore objects of specified type with header. </returns>
        public virtual IEnumerable<IWellboreObject> GetGrowingObjectsWithStatus(string objectType, EtpUri parentUri, string indexType = null)
        {
            return GetObjects<IWellboreObject>(objectType, parentUri, OptionsIn.ReturnElements.HeaderOnly);
        }
        
        /// <summary>
        /// Gets the object identifier only.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The object identifier.</returns>
        public virtual IDataObject GetObjectIdOnly(string objectType, EtpUri uri)
        {
            return GetObjects<IDataObject>(objectType, uri, OptionsIn.ReturnElements.IdOnly).FirstOrDefault();
        }

        /// <summary>
        /// Gets the object details.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The object detail.</returns>
        public virtual IDataObject GetObjectDetails(string objectType, EtpUri uri)
        {
            return GetObjects<IDataObject>(objectType, uri, OptionsIn.ReturnElements.All).FirstOrDefault();
        }

        /// <summary>
        /// Gets the object details.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <returns>The object detail.</returns>
        public virtual IDataObject GetObjectDetails(string objectType, EtpUri uri, params OptionsIn[] optionsIn)
        {
            return GetObjects<IDataObject>(objectType, uri, optionsIn).FirstOrDefault();
        }

        /// <summary>
        /// Gets the objects of the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <returns></returns>
        protected virtual IEnumerable<T> GetObjects<T>(string objectType, EtpUri uri, params OptionsIn[] optionsIn) where T : IDataObject
        {
            var filters = new List<string>();
            var values = new List<object>();
            var count = 0;

            // Create dictionary with case-insensitive keys
            var objectIds = uri.GetObjectIds()
                .ToDictionary(x => x.ObjectType, x => x.ObjectId, StringComparer.InvariantCultureIgnoreCase);

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

            var query = CreateWitsmlQuery(objectType, uri.Family);
            query = FormatWitsmlQuery(query, optionsIn);

            var result = query
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
        /// Formats the WITSML query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="optionsIn">The options in.</param>
        protected virtual IWitsmlQuery FormatWitsmlQuery(IWitsmlQuery query, params OptionsIn[] optionsIn)
        {
            optionsIn.ForEach(x => query.With(x));
            return query;
        }

        /// <summary>
        /// Creates the WITSML query.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="family">The object family.</param>
        /// <returns>An <see cref="IWitsmlQuery"/></returns>
        protected IWitsmlQuery CreateWitsmlQuery(string objectType, string family)
        {
            var listType = ObjectTypes.GetObjectGroupType(objectType, family, DataSchemaVersion);
            var dataType = ObjectTypes.GetObjectType(objectType, family, DataSchemaVersion);

            return GetType()
                .GetMethod("CreateQuery", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(dataType, listType)
                .Invoke(this, new object[0]) as IWitsmlQuery;
        }

        /// <summary>
        /// Creates the WITSML query.
        /// </summary>
        /// <typeparam name="T">The specified type</typeparam>
        /// <typeparam name="TList">The type of the list.</typeparam>
        /// <returns>An <see cref="IWitsmlQuery{T}"/> of the specified type.</returns>
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

        /// <summary>
        /// Sets the data schema version from WITSML version.
        /// </summary>
        /// <param name="version">The WITSML version.</param>
        private void SetDataSchemaVersion(WMLSVersion version)
        {
            var dataVersion = version == WMLSVersion.WITSML131
                ? OptionsIn.DataVersion.Version131.Value
                : OptionsIn.DataVersion.Version141.Value;

            DataSchemaVersion = dataVersion;
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
