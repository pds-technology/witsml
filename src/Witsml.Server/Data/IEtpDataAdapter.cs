using System.Collections.Generic;

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
        List<T> GetAll(string parentUri = null);

        /// <summary>
        /// Gets a data object by the specified UUID.
        /// </summary>
        /// <param name="uuid">The UUID.</param>
        /// <returns>The data object instance.</returns>
        T Get(string uuid);

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>A WITSML result.</returns>
        WitsmlResult Put(T entity);

        /// <summary>
        /// Deletes a data object by the specified UUID.
        /// </summary>
        /// <param name="uuid">The UUID.</param>
        /// <returns>A WITSML result.</returns>
        WitsmlResult Delete(string uuid);
    }
}
