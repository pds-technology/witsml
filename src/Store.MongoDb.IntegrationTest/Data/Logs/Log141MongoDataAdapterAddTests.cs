//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.IO;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data.Channels;
using PDS.WITSMLstudio.Store.Models;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    [TestClass]
    public class Log141MongoDataAdapterAddTests
    {
        private DevKit141Aspect _devKit;
        private Well _well;
        private Wellbore _wellbore;
        private string _testDataDir;
        private string _exceedFileFormat = "Test-exceed-max-doc-size-{0}-0001.xml";

        private IDatabaseProvider _provider;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);
            _provider = _devKit.Container.Resolve<IDatabaseProvider>();

            _testDataDir = new DirectoryInfo(@".\TestData").FullName;

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _well = new Well { Name = _devKit.Name("Well 01"), TimeZone = _devKit.TimeZone };

            _wellbore = new Wellbore()
            {
                NameWell = _well.Name,
                Name = _devKit.Name("Wellbore 01")
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            WitsmlSettings.LogMaxDataPointsGet = DevKitAspect.DefaultLogMaxDataPointsGet;
            WitsmlSettings.LogMaxDataPointsUpdate = DevKitAspect.DefaultLogMaxDataPointsAdd;
            WitsmlSettings.LogMaxDataPointsAdd = DevKitAspect.DefaultLogMaxDataPointsUpdate;
            WitsmlSettings.LogMaxDataPointsDelete = DevKitAspect.DefaultLogMaxDataPointsDelete;
            WitsmlSettings.LogMaxDataNodesGet = DevKitAspect.DefaultLogMaxDataNodesGet;
            WitsmlSettings.LogMaxDataNodesAdd = DevKitAspect.DefaultLogMaxDataNodesAdd;
            WitsmlSettings.LogMaxDataNodesUpdate = DevKitAspect.DefaultLogMaxDataNodesUpdate;
            WitsmlSettings.LogMaxDataNodesDelete = DevKitAspect.DefaultLogMaxDataNodesDelete;
            _devKit = null;
        }

        [TestMethod]
        public void Log141Adapter_AddToStore_Can_Add_Data_Chunk_Exceeds_MongoDb_Document_Size()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            // Adjust Points and Nodes for large file
            WitsmlSettings.LogMaxDataPointsAdd = 5000000;
            WitsmlSettings.LogMaxDataNodesAdd = 15000;
            WitsmlSettings.LogMaxDataPointsGet = 5000000;
            WitsmlSettings.LogMaxDataNodesGet = 15000;

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var xmlfile = Path.Combine(_testDataDir, string.Format(_exceedFileFormat, "log"));
            var xmlin = File.ReadAllText(xmlfile);

            var logList = EnergisticsConverter.XmlToObject<LogList>(xmlin);
            Assert.IsNotNull(logList);

            var log = logList.Log.FirstOrDefault();
            Assert.IsNotNull(log);

            log.Uid = null;
            log.UidWell = _wellbore.UidWell;
            log.UidWellbore = response.SuppMsgOut;
            log.NameWell = _well.Name;
            log.NameWellbore = _wellbore.Name;

            var logDataAdded = log.LogData.FirstOrDefault();
            Assert.IsNotNull(logDataAdded);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            log.Uid = uidLog;
            var uri = log.GetUri();

            // Query Channel Data Chunk
            var filter = MongoDbUtility.BuildFilter<ChannelDataChunk>("Uri", uri.ToString());
            var database = _provider.GetDatabase();
            var collection = database.GetCollection<ChannelDataChunk>("channelDataChunk");
            var chunks = collection.Find(filter).ToList();
            Assert.IsTrue(chunks.Count > 0);

            // Query Mongo File
            var fileChunks = chunks.Where(c => string.IsNullOrEmpty(c.Data)).ToList();
            Assert.IsTrue(fileChunks.Count > 0);

            var bucket = new GridFSBucket(database, new GridFSBucketOptions
            {
                BucketName = ChannelDataChunkAdapter.BucketName,
                ChunkSizeBytes = WitsmlSettings.ChunkSizeBytes
            });

            foreach (var fc in fileChunks)
            {
                Assert.IsNull(fc.Data);
                var mongoFileFilter = Builders<GridFSFileInfo>.Filter.Eq(fi => fi.Metadata[ChannelDataChunkAdapter.FileName], fc.Uid);
                var mongoFile = bucket.Find(mongoFileFilter).FirstOrDefault();
                Assert.IsNotNull(mongoFile);
            }

            // Query Log
            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.First();
            Assert.IsNotNull(result);

            var logDataReturned = result.LogData.FirstOrDefault();
            Assert.IsNotNull(logDataReturned);

            Assert.AreEqual(logDataAdded.Data.Count, logDataReturned.Data.Count);
        }
    }
}
