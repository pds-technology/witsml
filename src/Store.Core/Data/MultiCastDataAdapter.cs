//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
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
using System.ComponentModel.Composition;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;

using Witsml141 = Energistics.DataAccess.WITSML141;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Multi-cast data adapter that encapsulates CRUD functionality for WITSML objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.WitsmlDataAdapter{T}" />
    public class MultiCastDataAdapter<T> : WitsmlDataAdapter<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiCastDataAdapter{T}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="dataAdapter">The data adapter.</param>
        [ImportingConstructor]
        public MultiCastDataAdapter(IContainer container, IWitsmlDataAdapter<T> dataAdapter) : base(container)
        {
            var objectType = ObjectTypes.GetObjectType<T>();
            var version = ObjectTypes.GetVersion(typeof(T));
            ObjectName = new ObjectName(objectType, version);
            DataAdapter = dataAdapter;
        }

        /// <summary>
        /// Gets the name and version of the data object.
        /// </summary>
        private ObjectName ObjectName { get; }

        /// <summary>
        /// Gets the data adapter.
        /// </summary>
        private IWitsmlDataAdapter<T> DataAdapter { get; }

        /// <summary>
        /// Gets or sets the data adapters.
        /// </summary>
        [ImportMany]
        public List<IWitsmlDataAdapter<T>> DataAdapters { get; set; }

        /// <summary>
        /// Gets a value indicating whether validation is enabled for this data adapter.
        /// </summary>
        /// <param name="function">The WITSML API method.</param>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object.</param>
        /// <returns><c>true</c> if validation is enabled for this data adapter; otherwise, <c>false</c>.</returns>
        public override bool IsValidationEnabled(Functions function, WitsmlQueryParser parser, T dataObject) => false;

        /// <summary>
        /// Gets a data object by the specified UUID.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="fields">The requested fields.</param>
        /// <returns>The data object instance.</returns>
        public override T Get(EtpUri uri, params string[] fields)
        {
            return GetEntity(uri, fields);
        }

        /// <summary>
        /// Retrieves data objects from the data store using the specified parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <param name="context">The response context.</param>
        /// <returns>
        /// A collection of data objects retrieved from the data store.
        /// </returns>
        public override List<T> Query(WitsmlQueryParser parser, ResponseContext context)
        {
            if (WitsmlOperationContext.Current != null)
            {
                WitsmlOperationContext.Current.Response = context;
            }

            return QueryEntities(parser);
        }

        /// <summary>
        /// Adds a data object to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be added.</param>
        public override void Add(WitsmlQueryParser parser, T dataObject)
        {
            InsertEntity(dataObject);
        }

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be updated.</param>
        public override void Update(WitsmlQueryParser parser, T dataObject)
        {
            UpdateEntity(dataObject);
            //ValidateUpdatedEntity(Functions.UpdateInStore, GetUri(dataObject));
        }

        /// <summary>
        /// Replaces a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be replaced.</param>
        public override void Replace(WitsmlQueryParser parser, T dataObject)
        {
            ReplaceEntity(dataObject);
            //ValidateUpdatedEntity(Functions.PutObject, GetUri(dataObject));
        }

        /// <summary>
        /// Deletes or partially updates the specified object by uid.
        /// </summary>
        /// <param name="parser">The query parser that specifies the object.</param>
        public override void Delete(WitsmlQueryParser parser)
        {
            var uri = parser.GetUri<T>();

            if (parser.HasElements())
            {
                // TODO: PartialDeleteEntity(parser, uri);
                //ValidateUpdatedEntity(Functions.DeleteFromStore, uri);
                throw new NotImplementedException();
            }
            else
            {
                Delete(uri);
            }
        }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        public override void Delete(EtpUri uri)
        {
            DeleteEntity(uri);
        }

        /// <summary>
        /// Determines whether the entity exists in the data store.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>true if the entity exists; otherwise, false</returns>
        public override bool Exists(EtpUri uri)
        {
            return GetEntity(uri) != null;
        }

        /// <summary>
        /// Gets the count of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>The count of related data objects.</returns>
        public override int Count(EtpUri? parentUri = null)
        {
            return GetAll(parentUri).Count;
        }

        /// <summary>
        /// Determines if the specified URI has child data objects.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>If there are any related data objects.</returns>
        public override bool Any(EtpUri? parentUri = null)
        {
            return Count(parentUri) > 0;
        }

        /// <summary>
        /// Gets a collection of data objects based on the specified query template parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <returns>A collection of data objects retrieved from the data store.</returns>
        protected virtual List<T> GetAll(WitsmlQueryParser parser)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a data object based on the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>A data object retrieved from the data store.</returns>
        protected virtual T GetObject(EtpUri uri)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets an object from the data store by uid
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="fields">The requested fields.</param>
        /// <returns>The entity represented by the indentifier.</returns>
        protected virtual T GetEntity(EtpUri uri, params string[] fields)
        {
            return GetEntity<T>(uri, ObjectName.Name, fields);
        }

        /// <summary>
        /// Gets an object from the data store by uid
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="uri">The data object URI.</param>
        /// <param name="objectMappingKey">The object mapping key.</param>
        /// <param name="fields">The requested fields.</param>
        /// <returns>The entity represented by the indentifier.</returns>
        protected virtual TObject GetEntity<TObject>(EtpUri uri, string objectMappingKey, params string[] fields)
        {
            try
            {
                Logger.DebugFormat("Querying {0} data object; uid: {1}", objectMappingKey, uri.ObjectId);
                var entity = (object) GetObject(uri);

                if (entity is TObject)
                    return (TObject) entity;

                return default(TObject);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Error querying {0} data object:{1}{2}", objectMappingKey, Environment.NewLine, ex);
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Queries the data store using the specified <see cref="WitsmlQueryParser"/>.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <returns>The query results collection.</returns>
        protected virtual List<T> QueryEntities(WitsmlQueryParser parser)
        {
            return QueryEntities(parser, ObjectName.Name);
        }

        /// <summary>
        /// Queries the data store using the specified <see cref="WitsmlQueryParser" />.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <param name="objectMappingKey">The object mapping key.</param>
        /// <returns>The query results collection.</returns>
        /// <exception cref="WitsmlException"></exception>
        protected virtual List<T> QueryEntities(WitsmlQueryParser parser, string objectMappingKey)
        {
            if (OptionsIn.RequestObjectSelectionCapability.True.Equals(parser.RequestObjectSelectionCapability()))
            {
                Logger.DebugFormat("Requesting {0} query template.", objectMappingKey);
                var template = CreateQueryTemplate();
                return template.AsList();
            }

            var returnElements = parser.ReturnElements();
            Logger.DebugFormat("Querying with return elements '{0}'", returnElements);

            try
            {
                Logger.DebugFormat("Querying {0} data object.", objectMappingKey);
                return GetAll(parser);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Error querying {0} data object: {1}", objectMappingKey, ex);

                if (ex is WitsmlException) throw;

                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Inserts a data object into the data store.
        /// </summary>
        /// <param name="entity">The object to be inserted.</param>
        protected virtual void InsertEntity(T entity)
        {
            InsertEntity(entity, GetUri(entity), ObjectName.Name);
        }

        /// <summary>
        /// Inserts a data object into the data store.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="entity">The object to be inserted.</param>
        /// <param name="uri">The data object URI.</param>
        /// <param name="objectMappingKey">The name of the database collection.</param>
        protected virtual void InsertEntity<TObject>(TObject entity, EtpUri uri, string objectMappingKey)
        {
            try
            {
                Logger.DebugFormat("Inserting {0} data object.", objectMappingKey);
                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Error inserting {0} data object:{1}{2}", objectMappingKey, Environment.NewLine, ex);

                if (ex is WitsmlException) throw;
                throw new WitsmlException(ErrorCodes.ErrorAddingToDataStore, ex);
            }
        }

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <param name="entity">The object to be updated.</param>
        private void UpdateEntity(T entity)
        {
            UpdateEntity(entity, GetUri(entity), ObjectName.Name);
        }

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="entity">The object to be updated.</param>
        /// <param name="uri">The data object URI.</param>
        /// <param name="objectMappingKey">The object mapping key.</param>
        private void UpdateEntity<TObject>(TObject entity, EtpUri uri, string objectMappingKey)
        {
            try
            {
                Logger.DebugFormat("Updating {0} data object.", objectMappingKey);
                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Error updating {0} data object:{1}{2}", objectMappingKey, Environment.NewLine, ex);

                if (ex is WitsmlException) throw;
                throw new WitsmlException(ErrorCodes.ErrorUpdatingInDataStore, ex);
            }
        }

        /// <summary>
        /// Replaces a data object in the data store.
        /// </summary>
        /// <param name="entity">The object to be replaced.</param>
        private void ReplaceEntity(T entity)
        {
            ReplaceEntity(entity, GetUri(entity), ObjectName.Name);
        }

        /// <summary>
        /// Replaces a data object in the data store.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="entity">The object to be replaced.</param>
        /// <param name="uri">The data object URI.</param>
        /// <param name="objectMappingKey">The object mapping key.</param>
        private void ReplaceEntity<TObject>(TObject entity, EtpUri uri, string objectMappingKey)
        {
            try
            {
                Logger.DebugFormat("Replacing {0} data object.", objectMappingKey);
                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Error replacing {0} data object:{1}{2}", objectMappingKey, Environment.NewLine, ex);

                if (ex is WitsmlException) throw;
                throw new WitsmlException(ErrorCodes.ErrorReplacingInDataStore, ex);
            }
        }

        /// <summary>
        /// Deletes a data object from the data store.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        protected virtual void DeleteEntity(EtpUri uri)
        {
            DeleteEntity<T>(uri, ObjectName.Name);
        }

        /// <summary>
        /// Deletes a data object from the data store.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="uri">The data object URI.</param>
        /// <param name="objectMappingKey">The object mapping key.</param>
        protected virtual void DeleteEntity<TObject>(EtpUri uri, string objectMappingKey)
        {
            try
            {
                Logger.DebugFormat("Deleting {0} data object.", objectMappingKey);
                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Error deleting {0} data object:{1}{2}", objectMappingKey, Environment.NewLine, ex);

                if (ex is WitsmlException) throw;
                throw new WitsmlException(ErrorCodes.ErrorDeletingFromDataStore, ex);
            }
        }
    }
}