using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.Datatypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using PDS.Framework;
using PDS.Witsml.Server.Data.Wells;

namespace PDS.Witsml.Server.Data.Wellbores
{
    [TestClass]
    public class Wellbore200DataAdapterTests
    {
        private DevKit200Aspect DevKit;
        private IContainer Container;
        private IDatabaseProvider Provider;
        private IEtpDataAdapter<Well> WellAdapter;
        private IEtpDataAdapter<Wellbore> WellboreAdapter;

        private Well Well1;
        private Well Well2;
        private Wellbore Wellbore1;
        private Wellbore Wellbore2;
        private DataObjectReference WellReference;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit200Aspect();
            Container = ContainerFactory.Create();
            Provider = new DatabaseProvider(new MongoDbClassMapper());

            WellAdapter = new Well200DataAdapter(Provider) { Container = Container };
            WellboreAdapter = new Wellbore200DataAdapter(Provider) { Container = Container };

            Well1 = new Well() { Citation = new Citation { Title = DevKit.Name("Well 01") }, TimeZone = DevKit.TimeZone, Uuid = DevKit.Uid() };
            Well2 = new Well() { Citation = new Citation { Title = DevKit.Name("Well 02") }, TimeZone = DevKit.TimeZone, Uuid = DevKit.Uid() };

            Well1.GeographicLocationWGS84 = new GeodeticWellLocation();
            Well2.GeographicLocationWGS84 = new GeodeticWellLocation();

            WellReference = new DataObjectReference
            {
                ContentType = ContentTypes.Witsml200 + "type=" + ObjectTypes.Well,
                Title = Well1.Citation.Title,
                Uuid = Well1.Uuid
            };

            Wellbore1 = new Wellbore() { Citation = new Citation { Title = DevKit.Name("Wellbore 01") }, ReferenceWell = WellReference, Uuid = DevKit.Uid() };
            Wellbore2 = new Wellbore() { Citation = new Citation { Title = DevKit.Name("Wellbore 02") }, ReferenceWell = WellReference };
        }

        [TestMethod]
        public void CanAddAndGetSingleWellboreWithUuid()
        {
            WellAdapter.Put(Well1);
            WellboreAdapter.Put(Wellbore1);

            var wellbore1 = WellboreAdapter.Get(Wellbore1.Uuid);

            Assert.AreEqual(Wellbore1.Citation.Title, wellbore1.Citation.Title);
        }

        [TestMethod]
        public void CanAddAndGetSingleWellboreWithoutUuid()
        {
            WellAdapter.Put(Well1);
            WellboreAdapter.Put(Wellbore2);

            var wellbore2 = Provider.GetDatabase().GetCollection<Wellbore>(ObjectNames.Wellbore200).AsQueryable()
                .Where(x => x.Citation.Title == Wellbore2.Citation.Title)
                .FirstOrDefault();

            Assert.AreEqual(Wellbore2.Citation.Title, wellbore2.Citation.Title);
        }
    }
}
