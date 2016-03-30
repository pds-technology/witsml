using Energistics.DataAccess;
using Energistics.Datatypes;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for WITSML data objects
    /// </summary>
    /// <typeparam name="T">The typed WITSML object</typeparam>
    public interface IWitsmlDataAdapter<T>
    {
        /// <summary>
        /// Queries the data object(s) specified by the parser.
        /// </summary>
        /// <param name="parser">The parser that specifies the query parameters.</param>
        /// <returns>
        /// A collection of data objects retrieved from the data store.
        /// </returns>
        WitsmlResult<IEnergisticsCollection> Query(WitsmlQueryParser parser);

        /// <summary>
        /// Adds a data object to the data store.
        /// </summary>
        /// <param name="entity">The data object to be added.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        WitsmlResult Add(T entity);

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <param name="parser">The update parser.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        WitsmlResult Update(WitsmlQueryParser parser);

        /// <summary>
        /// Deletes or partially updates the specified object in the data store.
        /// </summary>
        /// <param name="parser">The parser that specifiee the object to delete.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        WitsmlResult Delete(WitsmlQueryParser parser);

        /// <summary>
        /// Determines whether the entity exists in the data store.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>true if the entity exists; otherwise, false</returns>
        bool Exists(EtpUri uri);

        /// <summary>
        /// Parses the specified XML string.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <returns>An instance of <see cref="T"/>.</returns>
        T Parse(WitsmlQueryParser parser);
    }
}
