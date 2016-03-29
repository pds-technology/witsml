using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PDS.Witsml.Server.Data
{
    [TestClass]
    public class DatabaseProviderTests
    {
        private IDatabaseProvider Provider;
        private DevKit141Aspect DevKit;

        private Well Well1;
        private Well Well2;

        [TestInitialize]
        public void TestSetUp()
        {
            Provider = new DatabaseProvider(new MongoDbClassMapper());
            DevKit = new DevKit141Aspect();

            Well1 = new Well() { Name = DevKit.Name("Mongo Well 01"), TimeZone = DevKit.TimeZone, Uid = DevKit.Uid() };
            Well2 = new Well() { Name = DevKit.Name("Mongo Well 02"), TimeZone = DevKit.TimeZone };
        }

        [TestMethod]
        public void Can_add_and_query_well()
        {
            var database = Provider.GetDatabase();
            var collection = database.GetCollection<Well>(ObjectNames.Well141);

            collection.InsertMany(new[] { Well1, Well2 });

            var exclude = Builders<Well>.Projection.Exclude("_id");
            var filter = Builders<Well>.Filter.Regex("Uid", new BsonRegularExpression("/^" + Well1.Uid + "$/i"));
            var result = collection.Find(filter).Project<Well>(exclude).ToList();

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(Well1.Name, result[0].Name);
        }
    }
}
