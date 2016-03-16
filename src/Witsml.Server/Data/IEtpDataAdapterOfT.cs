using System.Collections.Generic;
using Energistics.Datatypes;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Defines the methods needed to support ETP.
    /// </summary>
    /// <typeparam name="T">The typed WITSML object</typeparam>
    public interface IEtpDataAdapter<T>
    {
        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        List<T> GetAll(EtpUri? parentUri = null);

        /// <summary>
        /// Gets a data object by the specified identifier.
        /// </summary>
        /// <param dataObjectId>The data object identifier.</param>
        /// <returns>The data object instance.</returns>
        T Get(DataObjectId dataObjectId);

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>A WITSML result.</returns>
        WitsmlResult Put(T entity);

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <returns>A WITSML result.</returns>
        WitsmlResult Delete(DataObjectId dataObjectId);

        /// <summary>
        /// Parses the specified XML string.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <returns>An instance of <see cref="T"/>.</returns>
        T Parse(WitsmlQueryParser parser);
    }
}
