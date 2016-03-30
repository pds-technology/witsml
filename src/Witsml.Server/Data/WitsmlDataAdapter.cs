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
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for WITSML data objects.
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
        /// <value>The logger.</value>
        protected ILog Logger { get; private set; }

        /// <summary>
        /// Gets or sets the composition container.
        /// </summary>
        /// <value>The composition container.</value>
        [Import]
        public IContainer Container { get; set; }

        /// <summary>
        /// Queries the data object(s) specified by the parser.
        /// </summary>
        /// <param name="parser">The parser that specifies the query parameters.</param>
        /// <returns>
        /// A collection of data objects retrieved from the data store.
        /// </returns>
        public virtual WitsmlResult<IEnergisticsCollection> Query(WitsmlQueryParser parser)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds a data object to the data store.
        /// </summary>
        /// <param name="entity">The data object to be added.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public virtual WitsmlResult Add(T entity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <param name="parser">The update parser.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public virtual WitsmlResult Update(WitsmlQueryParser parser)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes or partially updates a data object in the data store.
        /// </summary>
        /// <param name="parser">The parser that specifies the object to delete.</param>
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
        /// <param name="uri">The data object URI.</param>
        /// <returns>true if the entity exists; otherwise, false</returns>
        public virtual bool Exists(EtpUri uri)
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
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public virtual List<T> GetAll(EtpUri? parentUri = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>The data object instance.</returns>
        object IEtpDataAdapter.Get(EtpUri uri)
        {
            return Get(uri);
        }

        /// <summary>
        /// Gets a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>The data object instance.</returns>
        public virtual T Get(EtpUri uri)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>A WITSML result.</returns>
        public virtual WitsmlResult Put(DataObject dataObject)
        {
            var context = new RequestContext(Functions.PutObject, ObjectTypes.GetObjectType<T>(),
                Encoding.UTF8.GetString(dataObject.Data), null, null);

            var parser = new WitsmlQueryParser(context);

            return Put(parser);
        }

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="parser">The input parser.</param>
        /// <returns>A WITSML result.</returns>
        public virtual WitsmlResult Put(WitsmlQueryParser parser)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>A WITSML result.</returns>
        public virtual WitsmlResult Delete(EtpUri uri)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parses the specified XML string.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <returns>An instance of <see cref="T"/>.</returns>
        public virtual T Parse(WitsmlQueryParser parser)
        {
            throw new NotImplementedException();
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

        /// <summary>
        /// Validates the entity based on the specified function.
        /// </summary>
        /// <param name="function">The WITSML API function.</param>
        /// <param name="entity">The entity to validate.</param>
        protected void Validate(Functions function, T entity)
        {
            var validator = Container.Resolve<IDataObjectValidator<T>>();
            validator.Validate(function, entity);
        }
    }
}
