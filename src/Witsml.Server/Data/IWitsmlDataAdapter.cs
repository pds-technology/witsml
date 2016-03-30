using System.Collections.Generic;
using Energistics.DataAccess;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionalities on WITSML objects
    /// </summary>
    /// <typeparam name="T">The typed WITSML object</typeparam>
    public interface IWitsmlDataAdapter<T>
    {
        /// <summary>
        /// Queries the object(s) specified by the parser.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <returns>Queried objects.</returns>
        WitsmlResult<IEnergisticsCollection> Query(WitsmlQueryParser parser);

        /// <summary>
        /// Adds an object to the data store.
        /// </summary>
        /// <param name="entity">The object.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        WitsmlResult Add(T entity);

        /// <summary>
        /// Updates the specified object.
        /// </summary>
        /// <param name="parser">The update parser.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        WitsmlResult Update(WitsmlQueryParser parser);

        /// <summary>
        /// Deletes or partially updates the specified object by uid.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        WitsmlResult Delete(WitsmlQueryParser parser);

        /// <summary>
        /// Determines whether the entity exists in the data store.
        /// </summary>
        /// <param name="dataObjectId">The data object identifier.</param>
        /// <returns>true if the entity exists; otherwise, false</returns>
        bool Exists(DataObjectId dataObjectId);

        /// <summary>
        /// Parses the specified XML string.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <returns>An instance of <see cref="T"/>.</returns>
        T Parse(WitsmlQueryParser parser);
    }
}
