using MongoDB.Bson;
using MongoDB.Driver;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Defined helper methods that can be used with MongoDB APIs.
    /// </summary>
    public static class MongoDbExtensions
    {
        /// <summary>
        /// Creates a regular expression filter to perform a case-insensitive search.
        /// </summary>
        /// <typeparam name="T">The data object type.</typeparam>
        /// <param name="filter">The filter definition builder.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <returns>The regular expression filter definition.</returns>
        public static FilterDefinition<T> EqIgnoreCase<T>(this FilterDefinitionBuilder<T> filter, string propertyPath, string propertyValue)
        {
            return filter.Regex(propertyPath, new BsonRegularExpression("/^" + propertyValue + "$/i"));
        }
    }
}
