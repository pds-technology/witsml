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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.Validation;
using Energistics.Etp.Common.Datatypes;
using log4net;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Transactions;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for WITSML data objects.
    /// </summary>
    /// <typeparam name="T">Type of the object.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.IWitsmlDataAdapter{T}" />
    public abstract class WitsmlDataAdapter<T> : IWitsmlDataAdapter<T>, IWitsmlDataAdapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlDataAdapter{T}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        protected WitsmlDataAdapter(IContainer container)
        {
            Logger = LogManager.GetLogger(GetType());
            Container = container;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILog Logger { get; }

        /// <summary>
        /// Gets the composition container.
        /// </summary>
        /// <value>The composition container.</value>
        protected IContainer Container { get; }

        /// <summary>
        /// Gets the data object type.
        /// </summary>
        public Type DataObjectType => typeof(T);

        /// <summary>
        /// Gets or sets the transaction factory.
        /// </summary>
        [Import]
        public ExportFactory<IWitsmlTransaction> TransactionFactory { get; set; }
        
        /// <summary>
        /// Gets the server sort order.
        /// </summary>
        public virtual string ServerSortOrder => ObjectTypes.NameProperty;

        /// <summary>
        /// Gets a reference to a new <see cref="IWitsmlTransaction"/> instance.
        /// </summary>
        /// <returns>A new <see cref="IWitsmlTransaction" /> instance.</returns>
        public virtual IWitsmlTransaction GetTransaction()
        {
            var export = TransactionFactory.CreateExport();
            return new TransactionWrapper(export);
        }

        /// <summary>
        /// Gets a value indicating whether validation is enabled for this data adapter.
        /// </summary>
        /// <param name="function">The WITSML API method.</param>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object.</param>
        /// <returns><c>true</c> if validation is enabled for this data adapter; otherwise, <c>false</c>.</returns>
        public virtual bool IsValidationEnabled(Functions function, WitsmlQueryParser parser, T dataObject) => true;

        /// <summary>
        /// Retrieves data objects from the data store using the specified parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <param name="context">The response context.</param>
        /// <returns>
        /// A collection of data objects retrieved from the data store.
        /// </returns>
        public virtual List<T> Query(WitsmlQueryParser parser, ResponseContext context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds a data object to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be added.</param>
        public virtual void Add(WitsmlQueryParser parser, T dataObject)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be updated.</param>
        public virtual void Update(WitsmlQueryParser parser, T dataObject)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Replaces a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be replaced.</param>
        public virtual void Replace(WitsmlQueryParser parser, T dataObject)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes or partially updates the specified object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        public virtual void Delete(WitsmlQueryParser parser)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        public virtual void Delete(EtpUri uri)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines whether the entity exists in the data store.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>true if the entity exists; otherwise, false</returns>
        public virtual bool Exists(EtpUri uri)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the count of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>The count of related data objects.</returns>
        public virtual int Count(EtpUri? parentUri = null)
        {
            return GetAllQuery(parentUri).Count();
        }

        /// <summary>
        /// Determines if the specified URI has child data objects.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>If there are any related data objects.</returns>
        public virtual bool Any(EtpUri? parentUri = null)
        {
            return GetAllQuery(parentUri).Any();
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public virtual List<T> GetAll(EtpUri? parentUri = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        IList IWitsmlDataAdapter.GetAll(EtpUri? parentUri)
        {
            return GetAll(parentUri);
        }

        /// <summary>
        /// Gets a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="fields">The requested fields.</param>
        /// <returns>The data object instance.</returns>
        object IWitsmlDataAdapter.Get(EtpUri uri, params string[] fields)
        {
            return Get(uri);
        }

        /// <summary>
        /// Gets a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="fields">The requested fields.</param>
        /// <returns>The data object instance.</returns>
        public virtual T Get(EtpUri uri, params string[] fields)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets an <see cref="IQueryable{T}"/> instance to by used by the GetAll method.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>An executable query.</returns>
        protected virtual IQueryable<T> GetAllQuery(EtpUri? parentUri)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validates the growing object data request.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <param name="dataObjects">The data object headers.</param>
        protected virtual void ValidateGrowingObjectDataRequest(WitsmlQueryParser parser, List<T> dataObjects)
        {
            Logger.DebugFormat("Validating growing object data request. Count: {0}", dataObjects.Count);

            if (dataObjects.Count > parser.QueryCount)
            {
                throw new WitsmlException(ErrorCodes.MissingSubsetOfGrowingDataObject);
            }
        }

        /// <summary>
        /// Gets a list of the property names to project during a query.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of property names.</returns>
        protected virtual List<string> GetProjectionPropertyNames(WitsmlQueryParser parser)
        {
            return null;
        }

        /// <summary>
        /// Gets a list of the element names to ignore during a query.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of element names.</returns>
        protected virtual List<string> GetIgnoredElementNamesForQuery(WitsmlQueryParser parser)
        {
            return null;
        }

        /// <summary>
        /// Gets a list of the element names to ignore during an update.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of element names.</returns>
        protected virtual List<string> GetIgnoredElementNamesForUpdate(WitsmlQueryParser parser)
        {
            return null;
        }

        /// <summary>
        /// Creates the query template.
        /// </summary>
        /// <returns>A query template.</returns>
        protected virtual WitsmlQueryTemplate<T> CreateQueryTemplate()
        {
            return new WitsmlQueryTemplate<T>();
        }

        /// <summary>
        /// Gets the URI for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>The data object URI.</returns>
        EtpUri IWitsmlDataAdapter.GetUri(object dataObject)
        {
            if (!(dataObject is T)) return default(EtpUri);
            return GetUri((T)dataObject);
        }

        /// <summary>
        /// Gets the URI for the specified data object.
        /// </summary>
        /// <param name="instance">The data object.</param>
        /// <returns>The URI representing the data object.</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        protected virtual EtpUri GetUri(T instance)
        {
            var wellboreObject = instance as IWellboreObject;
            if (wellboreObject != null) return wellboreObject.GetUri();

            var wellObject = instance as IWellObject;
            if (wellObject != null) return wellObject.GetUri();

            var dataObject = instance as IDataObject;
            if (dataObject != null) return dataObject.GetUri();

            var abstractObject = instance as Energistics.DataAccess.WITSML200.AbstractObject;
            if (abstractObject != null) return abstractObject.GetUri();

            var prodmlObject = instance as Energistics.DataAccess.PRODML200.AbstractObject;
            if (prodmlObject != null) return prodmlObject.GetUri();

            var resqmlObject = instance as Energistics.DataAccess.RESQML210.AbstractObject;
            if (resqmlObject != null) return resqmlObject.GetUri();

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Validates the updated entity.
        /// </summary>
        /// <param name="function">The WITSML API function.</param>
        /// <param name="uri">The URI.</param>
        protected virtual void ValidateUpdatedEntity(Functions function, EtpUri uri)
        {
            IList<ValidationResult> results;

            var entity = Get(uri);
            DataObjectValidator.TryValidate(entity, out results);
            WitsmlValidator.ValidateResults(function, results);
        }

        /// <summary>
        /// Deletes all child objects related to the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        protected virtual void DeleteAll(EtpUri uri)
        {
            var adapters = new List<IWitsmlDataAdapter>();

            if (uri.IsRelatedTo(EtpUris.Witsml200) || uri.IsRelatedTo(EtpUris.Eml210))
            {
                // Cascade delete not defined for WITSML 2.0 / ETP
                return;
            }
            if (ObjectTypes.Well.EqualsIgnoreCase(uri.ObjectType))
            {
                adapters.Add(Container.Resolve<IWitsmlDataAdapter>(new ObjectName(ObjectTypes.Wellbore, uri.Family, uri.Version)));
            }
            else if (ObjectTypes.Wellbore.EqualsIgnoreCase(uri.ObjectType))
            {
                var exclude = new[] { ObjectTypes.Well, ObjectTypes.Wellbore, ObjectTypes.ChangeLog };

                var type = OptionsIn.DataVersion.Version141.Equals(uri.Version)
                    ? typeof(IWitsml141Configuration)
                    : typeof(IWitsml131Configuration);

                Container
                    .ResolveAll(type)
                    .Cast<IWitsmlDataAdapter>()
                    .Where(x => !exclude.ContainsIgnoreCase(ObjectTypes.GetObjectType(x.DataObjectType)))
                    .ForEach(adapters.Add);
            }

            foreach (var adapter in adapters)
            {
                var dataObjects = adapter.GetAll(uri);

                foreach (var dataObject in dataObjects)
                {
                    adapter.Delete(adapter.GetUri(dataObject));
                }
            }
        }
    }
}
