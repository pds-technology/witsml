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
        /// Gets a data object by the specified URI.
        /// </summary>
        /// <param uri>The data object URI.</param>
        /// <returns>The data object instance.</returns>
        T Get(EtpUri uri);

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="parser">The input parser.</param>
        /// <returns>A WITSML result.</returns>
        WitsmlResult Put(WitsmlQueryParser parser);

        /// <summary>
        /// Deletes a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>A WITSML result.</returns>
        WitsmlResult Delete(EtpUri uri);

        /// <summary>
        /// Parses the specified XML string.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <returns>An instance of <see cref="T"/>.</returns>
        T Parse(WitsmlQueryParser parser);
    }
}
