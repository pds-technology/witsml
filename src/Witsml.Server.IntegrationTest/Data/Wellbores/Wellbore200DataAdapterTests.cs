using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace PDS.Witsml.Server.Data.Wellbores
{
    [TestClass]
    public class Wellbore200DataAdapterTests
    {
        private DevKit200Aspect DevKit;
        private IDatabaseProvider _provider;
        private IEtpDataAdapter<Wellbore> _adapter;

        private Wellbore Wellbore1;
        private Wellbore Wellbore2;

        [TestInitialize]
        public void TestSetUp()
        {
            _provider = new DatabaseProvider(new MongoDbClassMapper());
            _adapter = new Wellbore200DataAdapter(_provider);

            DevKit = new DevKit200Aspect();

            Wellbore1 = new Wellbore()
            {
                Uuid = DevKit.Uid(),
                Citation = new Citation { Title = DevKit.Name("Wellbore 01") },
            };

            Wellbore2 = new Wellbore()
            {
                Citation = new Citation { Title = DevKit.Name("Wellbore 02") },
            };
        }

        [TestMethod]
        public void CanAddAndGetSingleWellboreWithUuid()
        {
            _adapter.Put(Wellbore1);

            var wellbore1 = _adapter.Get(Wellbore1.Uuid);

            Assert.AreEqual(Wellbore1.Citation.Title, wellbore1.Citation.Title);
        }

        [TestMethod]
        public void CanAddAndGetSingleWellboreWithoutUuid()
        {
            _adapter.Put(Wellbore2);

            var wellbore2 = _provider.GetDatabase().GetCollection<Wellbore>(ObjectNames.Wellbore200).AsQueryable()
                .Where(x => x.Citation.Title == Wellbore2.Citation.Title)
                .FirstOrDefault();

            Assert.AreEqual(Wellbore2.Citation.Title, wellbore2.Citation.Title);
        }
    }
}
