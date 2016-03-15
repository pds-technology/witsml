using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Energistics.DataAccess;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using log4net;
using PDS.Framework;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionalities on WITSML objects.
    /// </summary>
    /// <typeparam name="T">Type of the object.</typeparam>
    /// <seealso cref="PDS.Witsml.Server.Data.IWitsmlDataAdapter{T}" />
    /// <seealso cref="PDS.Witsml.Server.Data.IEtpDataAdapter{T}" />
    public abstract class WitsmlDataAdapter<T> : IWitsmlDataAdapter<T>, IEtpDataAdapter<T>, IEtpDataAdapter
    {
        protected WitsmlDataAdapter()
        {
            Logger = LogManager.GetLogger(GetType());
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        protected ILog Logger { get; private set; }

        /// <summary>
        /// Gets or sets the composition container.
        /// </summary>
        /// <value>The composition container.</value>
        [Import]
        public IContainer Container { get; set; }

        /// <summary>
        /// Queries the object(s) specified by the parser.
        /// </summary>
        /// <param name="parser">The parser that specifies the query parameters.</param>
        /// <returns>
        /// Queried objects.
        /// </returns>
        public virtual WitsmlResult<IEnergisticsCollection> Query(WitsmlQueryParser parser)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds an object to the data store.
        /// </summary>
        /// <param name="entity">The object.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public virtual WitsmlResult Add(T entity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the specified object.
        /// </summary>
        /// <param name="entity">The object.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public virtual WitsmlResult Update(T entity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes or partially updates the specified object by uid.
        /// </summary>
        /// <param name="parser">The parser that specifies the object.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public virtual WitsmlResult Delete(WitsmlQueryParser parser)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines whether the entity exists in the data store.
        /// </summary>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <returns>true if the entity exists; otherwise, false</returns>
        public virtual bool Exists(DataObjectId dataObjectId)
        {
            throw new NotImplementedException();
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
        /// Gets a data object by the specified UUID.
        /// </summary>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <returns>The data object instance.</returns>
        public virtual T Get(DataObjectId dataObjectId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>A WITSML result.</returns>
        public virtual WitsmlResult Put(T entity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes a data object by the specified UUID.
        /// </summary>
        /// <param name="uuid">The UUID.</param>
        /// <returns>A WITSML result.</returns>
        public virtual WitsmlResult Delete(string uuid)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        IList IEtpDataAdapter.GetAll(EtpUri? parentUri)
        {
            return GetAll(parentUri);
        }

        /// <summary>
        /// Gets a data object by the specified UUID.
        /// </summary>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <returns>The data object instance.</returns>
        object IEtpDataAdapter.Get(DataObjectId dataObjectId)
        {
            return Get(dataObjectId);
        }

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>A WITSML result.</returns>
        WitsmlResult IEtpDataAdapter.Put(DataObject dataObject)
        {
            var xml = Encoding.UTF8.GetString(dataObject.Data);
            var entity = Parse(xml);
            return Put(entity);
        }

        /// <summary>
        /// Deletes a data object by the specified UUID.
        /// </summary>
        /// <param name="uuid">The UUID.</param>
        /// <returns>A WITSML result.</returns>
        WitsmlResult IEtpDataAdapter.Delete(string uuid)
        {
            return Delete(uuid);
        }

        /// <summary>
        /// Parses the specified XML string.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <returns>An instance of <see cref="T"/>.</returns>
        protected virtual T Parse(string xml)
        {
            return WitsmlParser.Parse<T>(xml);
        }

        /// <summary>
        /// Creates the query template.
        /// </summary>
        /// <returns>A query template.</returns>
        protected virtual WitsmlQueryTemplate<T> CreateQueryTemplate()
        {
            return new WitsmlQueryTemplate<T>();
        }
    }
}
