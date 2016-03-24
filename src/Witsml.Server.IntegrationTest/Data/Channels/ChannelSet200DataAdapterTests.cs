using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;
using PDS.Witsml.Data.Logs;

namespace PDS.Witsml.Server.Data.Channels
{
    [TestClass]
    public class ChannelSet200DataAdapterTests
    {
        private DevKit200Aspect DevKit;
        private Log200Generator LogGenerator;
        private IContainer Container;
        private IDatabaseProvider Provider;
        private IEtpDataAdapter<ChannelSet> ChannelSetAdapter;
        private ChannelDataChunkAdapter ChunkAdapter;
        private ChannelSet ChannelSet;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit200Aspect();
            LogGenerator = new Log200Generator();
            Container = ContainerFactory.Create();
            Provider = new DatabaseProvider(new MongoDbClassMapper());

            ChunkAdapter = new ChannelDataChunkAdapter(Provider) { Container = Container };
            ChannelSetAdapter = new ChannelSet200DataAdapter(Provider, ChunkAdapter) { Container = Container };

            var log = new Log();
            var mdChannelIndex = LogGenerator.CreateMeasuredDepthIndex(IndexDirection.increasing);
            DevKit.InitHeader(log, LoggingMethod.MWD, mdChannelIndex);

            ChannelSet = log.ChannelSet.First();
        }

        [TestMethod]
        public void ChannelSet_can_be_added_with_depth_data()
        {
            var result = ChannelSetAdapter.Put(ChannelSet);
            Assert.AreEqual(ErrorCodes.Success, result.Code);
        }
    }
}
