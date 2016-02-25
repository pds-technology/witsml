using MongoDB.Driver;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Provides access to a MongoDb data store.
    /// </summary>
    public interface IDatabaseProvider
    {
        /// <summary>
        /// Gets the MongoDb client interface.
        /// </summary>
        /// <value>The client interface.</value>
        IMongoClient Client { get; }

        /// <summary>
        /// Gets the MongoDb database interface.
        /// </summary>
        /// <returns>The database interface.</returns>
        IMongoDatabase GetDatabase();
    }
}
