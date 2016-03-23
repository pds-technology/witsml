using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using PDS.Framework;
using PDS.Witsml.Server.Data.Channels;
using PDS.Witsml.Server.Data.Wellbores;
using PDS.Witsml.Server.Data.Wells;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    public class Log200DataAdapterTests
    {
        private DevKit200Aspect DevKit;
        private IContainer Container;
        private IDatabaseProvider Provider;
        private IEtpDataAdapter<Well> WellAdapter;
        private IEtpDataAdapter<Wellbore> WellboreAdapter;
        private IEtpDataAdapter<Log> LogAdapter;

        private Well Well1;
        private Wellbore Wellbore1;
        private Log Log1;
        private Log Log2;
        private Log LogDecreasing;
        private DataObjectReference WellReference;
        private DataObjectReference WellboreReference;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit200Aspect();
            Container = ContainerFactory.Create();
            Provider = new DatabaseProvider(new MongoDbClassMapper());

            WellAdapter = new Well200DataAdapter(Provider) { Container = Container };
            WellboreAdapter = new Wellbore200DataAdapter(Provider) { Container = Container };
            LogAdapter = new Log200DataAdapter(Provider, new ChannelDataAdapter(Provider)) { Container = Container };
            Well1 = new Well() { Citation = DevKit.Citation("Well 01"), TimeZone = DevKit.TimeZone, Uuid = DevKit.Uid() };

            Well1.GeographicLocationWGS84 = DevKit.Location();

            WellReference = new DataObjectReference
            {
                ContentType = EtpContentTypes.Witsml200.For(ObjectTypes.Well),
                Title = Well1.Citation.Title,
                Uuid = Well1.Uuid
            };

            Wellbore1 = new Wellbore() { Citation = DevKit.Citation("Wellbore 01"), ReferenceWell = WellReference, Uuid = DevKit.Uid() };

            WellboreReference = new DataObjectReference
            {
                ContentType = EtpContentTypes.Witsml200.For(ObjectTypes.Wellbore),
                Title = Wellbore1.Citation.Title,
                Uuid = Wellbore1.Uuid
            };

            Log1 = new Log() { Citation = DevKit.Citation("Log 01"), Wellbore = WellboreReference, Uuid = DevKit.Uid() };
            Log2 = new Log() { Citation = DevKit.Citation("Log 02"), Wellbore = WellboreReference };
            LogDecreasing = new Log() { Citation = DevKit.Citation("Log Decreasing"), Wellbore = WellboreReference, Uuid = DevKit.Uid() };

            DevKit.InitHeader(Log1, LoggingMethod.MWD, ChannelIndexType.measureddepth);
            DevKit.InitHeader(Log2, LoggingMethod.Surface, ChannelIndexType.datetime);
            DevKit.InitHeader(LogDecreasing, LoggingMethod.Surface, ChannelIndexType.measureddepth, IndexDirection.decreasing);
        }

        [TestMethod]
        public void Log_can_be_added_with_uuid()
        {
            WellAdapter.Put(Well1);
            WellboreAdapter.Put(Wellbore1);
            LogAdapter.Put(Log1);

            var log1 = LogAdapter.Get(Log1.GetObjectId());

            Assert.AreEqual(Log1.Citation.Title, log1.Citation.Title);
        }

        [TestMethod]
        public void Log_can_be_added_without_uuid()
        {
            WellAdapter.Put(Well1);
            WellboreAdapter.Put(Wellbore1);
            LogAdapter.Put(Log2);

            var log2 = Provider.GetDatabase().GetCollection<Log>(ObjectNames.Log200).AsQueryable()
                .Where(x => x.Citation.Title == Log2.Citation.Title)
                .FirstOrDefault();

            Assert.AreEqual(Log2.Citation.Title, log2.Citation.Title);
        }

        [TestMethod]
        public void Log_can_be_added_with_secondary_index()
        {
            var secondaryIndex = DevKit.ChannelIndex(ChannelIndexType.datetime);
            var channelSet = Log1.ChannelSet.First();
            channelSet.Index.Add(secondaryIndex);
            
            DevKit.CreateMockChannelSetData(channelSet, channelSet.Index);

            WellAdapter.Put(Well1);
            WellboreAdapter.Put(Wellbore1);
            LogAdapter.Put(Log1);


            var log1 = LogAdapter.Get(Log1.GetObjectId());

            Assert.AreEqual(Log1.Citation.Title, log1.Citation.Title);
        }

        [TestMethod]
        public void Log_can_be_added_with_increasing_log_data()
        {
            var numDataValue = 150;
            var secondaryIndex = DevKit.ChannelIndex(ChannelIndexType.datetime);
            var channelSet = Log1.ChannelSet.First();
            channelSet.Index.Add(secondaryIndex);

            // Save the Well and Wellbore
            WellAdapter.Put(Well1);
            WellboreAdapter.Put(Wellbore1);

            // Generate 150 rows of data
            DevKit.GenerateChannelData(Log1.ChannelSet, numDataValue);
            LogAdapter.Put(Log1);

            var cda = new ChannelDataAdapter(Provider);

            // Retrieve the data
            var uidLog = Log1.Uuid;
            var mnemonics = Log1.ChannelSet.First().Channel.Select(c => c.Mnemonic).ToList();
            var logData = cda.GetData(uidLog, mnemonics, null, true);

            var rowCount = logData.Sum(ld => DevKit.DeserializeChannelSetData(ld.Data).Count);

            var start = logData.First().Indices.First().Start;
            var end = logData.Last().Indices.First().End;

            // Test that the rows of data before and after are the same.
            Assert.AreEqual(numDataValue, rowCount);

            // Test the log is still increasing
            Assert.IsTrue(end > start);
        }

        [TestMethod]
        public void Log_can_be_added_with_decreasing_log_data()
        {
            var numDataValue = 150;
            var secondaryIndex = DevKit.ChannelIndex(ChannelIndexType.datetime);
            var channelSet = LogDecreasing.ChannelSet.First();
            channelSet.Index.Add(secondaryIndex);

            // Save the Well and Wellbore
            WellAdapter.Put(Well1);
            WellboreAdapter.Put(Wellbore1);

            // Generate 150 rows of data
            DevKit.GenerateChannelData(LogDecreasing.ChannelSet, numDataValue);
            LogAdapter.Put(LogDecreasing);

            var cda = new ChannelDataAdapter(Provider);

            // Retrieve the data
            var uidLog = LogDecreasing.Uuid;
            var mnemonics = LogDecreasing.ChannelSet.First().Channel.Select(c => c.Mnemonic).ToList();
            var logData = cda.GetData(uidLog, mnemonics, null, false);

            var rowCount = logData.Sum(ld => DevKit.DeserializeChannelSetData(ld.Data).Count);

            var start = logData.First().Indices.First().Start;
            var end = logData.Last().Indices.First().End;

            // Test that the rows of data before and after are the same.
            Assert.AreEqual(numDataValue, rowCount);

            // Test the log is still decreasing
            Assert.IsTrue(end < start);
        }
    }
}
