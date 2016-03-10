using System;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using PDS.Framework;

namespace PDS.Witsml.Server.Data.Wells
{
    [TestClass]
    public class Well200DataAdapterTests
    {
        private DevKit200Aspect DevKit;
        private IContainer Container;
        private IDatabaseProvider Provider;
        private IEtpDataAdapter<Well> WellAdapter;

        private Well Well1;
        private Well Well2;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit200Aspect();
            Container = ContainerFactory.Create();
            Provider = new DatabaseProvider(new MongoDbClassMapper());

            WellAdapter = new Well200DataAdapter(Provider) { Container = Container };

            Well1 = new Well()
            {
                Citation = new Citation { Title = DevKit.Name("Well 01") },
                GeographicLocationWGS84 = DevKit.Location(),
                TimeZone = DevKit.TimeZone,
                Uuid = DevKit.Uid(),
            };

            Well2 = new Well()
            {
                Citation = new Citation { Title = DevKit.Name("Well 02") },
                GeographicLocationWGS84 = DevKit.Location(),
                TimeZone = DevKit.TimeZone,
            };
        }

        [TestMethod]
        public void CanSerializeWellToXml()
        {
            var xml = EnergisticsConverter.ObjectToXml(Well1);
            Console.WriteLine(xml);
            Assert.IsNotNull(xml);
        }

        [TestMethod]
        public void CanAddAndGetSingleWellWithUuid()
        {
            WellAdapter.Put(Well1);

            var well1 = WellAdapter.Get(Well1.Uuid);

            Assert.AreEqual(Well1.Citation.Title, well1.Citation.Title);
        }

        [TestMethod]
        public void CanAddAndGetSingleWellWithoutUuid()
        {
            WellAdapter.Put(Well2);

            var well2 = Provider.GetDatabase().GetCollection<Well>(ObjectNames.Well200).AsQueryable()
                .Where(x => x.Citation.Title == Well2.Citation.Title)
                .FirstOrDefault();

            Assert.AreEqual(Well2.Citation.Title, well2.Citation.Title);
        }
    }
}
