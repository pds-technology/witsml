using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Channels
{
    [TestClass]
    public class ChannelDataChunkAdapterUpdateTests
    {
        private DevKit141Aspect DevKit;
        private Well Well;
        private Wellbore Wellbore;
        private Log Log;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect(TestContext);

            //DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
            //    .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
            //    .ToArray();

            Well = new Well
            {
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Well 01"),
                TimeZone = DevKit.TimeZone
            };

            Wellbore = new Wellbore()
            {
                UidWell = Well.Uid,
                NameWell = Well.Name,
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Wellbore 01")
            };

            Log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = Wellbore.Uid,
                NameWellbore = Wellbore.Name,
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Log 01")
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            WitsmlSettings.DepthRangeSize = DevKitAspect.DefaultDepthChunkRange;
            WitsmlSettings.TimeRangeSize = DevKitAspect.DefaultTimeChunkRange;
            WitsmlSettings.LogMaxDataPointsGet = DevKitAspect.DefaultLogMaxDataPointsGet;
            WitsmlSettings.LogMaxDataPointsUpdate = DevKitAspect.DefaultLogMaxDataPointsAdd;
            WitsmlSettings.LogMaxDataPointsAdd = DevKitAspect.DefaultLogMaxDataPointsUpdate;
            WitsmlSettings.LogMaxDataPointsDelete = DevKitAspect.DefaultLogMaxDataPointsDelete;
            WitsmlSettings.LogMaxDataNodesGet = DevKitAspect.DefaultLogMaxDataNodesGet;
            WitsmlSettings.LogMaxDataNodesAdd = DevKitAspect.DefaultLogMaxDataNodesAdd;
            WitsmlSettings.LogMaxDataNodesUpdate = DevKitAspect.DefaultLogMaxDataNodesUpdate;
            WitsmlSettings.LogMaxDataNodesDelete = DevKitAspect.DefaultLogMaxDataNodesDelete;
            WitsmlSettings.MaxDataLength = DevKitAspect.DefaultMaxDataLength;
        }


        [TestMethod]
        public void ChannelDataChunkAdapter_UpdateInStore_ChunkMerge_With_File_Storage()
        {
            var channelDataChunkAdapter = DevKit.Container.Resolve<ChannelDataChunkAdapter>();

            // Ensure that our chunk size is large enough to contain all of the test data in one chunk
            WitsmlSettings.DepthRangeSize = 1000;
            WitsmlSettings.MaxDataLength = 5000000;

            // Add the parent well and wellbore
            AddParents();

            // Add the log header
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.AddAndAssert<LogList, Log>(Log);

            // Perform two updates that should merge the data into a single chunk
            Log.LogData[0].Data = new List<string>
                {
                    "100,1,1",
                    "200,2,2",
                    "300,3,3",
                    "400,4,4",
                    "500,5,5"
                };
            DevKit.UpdateAndAssert(Log);

            Log.LogData[0].Data = new List<string>
            {
                "600,6,6",
                "700,7,7",
                "800,8,8",
                "900,9,9"
            };
            DevKit.UpdateAndAssert(Log);

            // Test there there is only one chunk
            var filter = channelDataChunkAdapter.BuildDataFilter(Log.GetUri(), null, new Range<double?>(null, null), true);
            var chunks = channelDataChunkAdapter.GetData(filter, true).ToList();
            Assert.AreEqual(1, chunks.Count, "More than one data chunk was found");

            // Add data that will push us into two chunks
            Log.LogData[0].Data = new List<string>
            {
                "1000,10,10",
                "1100,11,11"
            };
            DevKit.UpdateAndAssert(Log);

            // Test that we now have two chunks
            chunks = channelDataChunkAdapter.GetData(filter, true).ToList();
            Assert.AreEqual(2, chunks.Count, "More than two data chunks were found");
        }

        protected virtual void AddParents()
        {
            DevKit.AddAndAssert<WellList, Well>(Well);
            DevKit.AddAndAssert<WellboreList, Wellbore>(Wellbore);
        }
    }
}
