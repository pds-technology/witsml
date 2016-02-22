using System.Collections.Generic;

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
        /// <param name="parser">The parser that specifies the query parameters.</param>
        /// <returns>
        /// Queried objects.
        /// </returns>
        WitsmlResult<List<T>> Query(WitsmlQueryParser parser);

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
        /// <param name="entity">The object.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        WitsmlResult Update(T entity);

        /// <summary>
        /// Deletes or partially updates the specified object by uid.
        /// </summary>
        /// <param name="parser">The parser that specifies the object.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        WitsmlResult Delete(WitsmlQueryParser parser);

        /// <summary>
        /// Determines whether the entity existed.
        /// </summary>
        /// <param name="uid">The uid.</param>
        /// <param name="dbCollectionName">Name of the database collection.</param>
        /// <returns></returns>
        bool IsEntityExisted(string uid, string dbCollectionName);
    }
}
