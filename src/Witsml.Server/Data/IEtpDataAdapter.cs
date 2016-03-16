using System.Collections;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Defines the methods needed to support ETP.
    /// </summary>
    public interface IEtpDataAdapter
    {
        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        IList GetAll(EtpUri? parentUri = null);

        /// <summary>
        /// Gets a data object by the specified identifier.
        /// </summary>
        /// <param dataObjectId>The data object identifier.</param>
        /// <returns>The data object instance.</returns>
        object Get(DataObjectId dataObjectId);

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>A WITSML result.</returns>
        WitsmlResult Put(DataObject dataObject);

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <returns>A WITSML result.</returns>
        WitsmlResult Delete(DataObjectId dataObjectId);
    }
}
