using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace PDS.Witsml.Server.Data.Wells
{
    [TestClass]
    public class Well200DataAdapterTests
    {
        private DevKit200Aspect DevKit;
        private IDatabaseProvider _provider;
        private IEtpDataAdapter<Well> _adapter;

        private Well Well1;
        private Well Well2;

        [TestInitialize]
        public void TestSetUp()
        {
            _provider = new DatabaseProvider(new MongoDbClassMapper());
            _adapter = new Well200DataAdapter(_provider);

            DevKit = new DevKit200Aspect();

            Well1 = new Well()
            {
                Uuid = DevKit.Uid(),
                Citation = new Citation { Title = DevKit.Name("Well 01") },
            };

            Well2 = new Well()
            {
                Citation = new Citation { Title = DevKit.Name("Well 02") },
            };
        }

        [TestMethod]
        public void CanAddAndGetSingleWellWithUuid()
        {
            _adapter.Put(Well1);

            var well1 = _adapter.Get(Well1.Uuid);

            Assert.AreEqual(Well1.Citation.Title, well1.Citation.Title);
        }

        [TestMethod]
        public void CanAddAndGetSingleWellWithoutUuid()
        {
            _adapter.Put(Well2);

            var well2 = _provider.GetDatabase().GetCollection<Well>(ObjectNames.Well200).AsQueryable()
                .Where(x => x.Citation.Title == Well2.Citation.Title)
                .FirstOrDefault();

            Assert.AreEqual(Well2.Citation.Title, well2.Citation.Title);
        }
    }
}
