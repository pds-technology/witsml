using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionalities on WITSML objects.
    /// </summary>
    /// <typeparam name="T">Type of the object.</typeparam>
    /// <seealso cref="Data.IWitsmlDataAdapter{T}" />
    public abstract class WitsmlDataAdapter<T> : IWitsmlDataAdapter<T>
    {
        /// <summary>
        /// Queries the object(s) specified by the parser.
        /// </summary>
        /// <param name="parser">The parser that specifies the query parameters.</param>
        /// <returns>
        /// Queried objects.
        /// </returns>
        public abstract WitsmlResult<List<T>> Query(WitsmlQueryParser parser);

        /// <summary>
        /// Adds an object to the data store.
        /// </summary>
        /// <param name="entity">The object.</param>
        /// <returns>
        /// A WITSML result that includes return code and/or message.
        /// </returns>
        public abstract WitsmlResult Add(T entity);

        /// <summary>
        /// Updates the specified object.
        /// </summary>
        /// <param name="entity">The object.</param>
        /// <returns>
        /// A WITSML result that includes return code and/or message.
        /// </returns>
        public abstract WitsmlResult Update(T entity);

        /// <summary>
        /// Deletes or partially updates the specified object by uid.
        /// </summary>
        /// <param name="parser">The parser that specifies the object.</param>
        /// <returns>
        /// A WITSML result that includes return code and/or message.
        /// </returns>
        public abstract WitsmlResult Delete(WitsmlQueryParser parser);
        // TODO: Move to common project to be shared 

        public static IQueryable<T> FilterQuery(WitsmlQueryParser parser, IQueryable<T> query, List<string> names)
        {
            // For entity property name and its value
            var nameValues = new Dictionary<string, string>();

            // For each name pair ("<xml name>,<entity propety name>") 
            //... create a dictionary of property names and corresponding values.
            names.ForEach(n =>
            {
                // Split out the xml name and entity property names for ease of use.
                var nameAndProperty = n.Split(',');
                nameValues.Add(nameAndProperty[1], parser.PropertyValue(nameAndProperty[0]));
            });

            query = QueryByNames(query, nameValues);

            return query;
        }

        // TODO: Move to common project to be shared 
        public static IQueryable<T> QueryByNames(IQueryable<T> query, Dictionary<string, string> nameValues)
        {
            if (nameValues.Values.ToList().TrueForAll(nameValue => !string.IsNullOrEmpty(nameValue)))
            {
                nameValues.Keys.ToList().ForEach(nameKey =>
                {
                    query = query.Where(string.Format("{0} = \"{1}\"", nameKey, nameValues[nameKey]));
                });
            }

            return query;
        }
    }
}
