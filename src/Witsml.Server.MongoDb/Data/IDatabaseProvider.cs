using MongoDB.Driver;

namespace PDS.Witsml.Server.Data
{
    public interface IDatabaseProvider
    {
        IMongoClient Client { get; }

        IMongoDatabase GetDatabase();
    }
}
