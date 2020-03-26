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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Etp.Common.Datatypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data.Channels;
using PDS.WITSMLstudio.Store.Models;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    [TestClass]
    public class Log141MongoDataAdapterDeleteTests
    {
        private DevKit141Aspect _devKit;
        private Well _well;
        private Wellbore _wellbore;
        private Log _log;
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

            _well = new Well {Uid = _devKit.Uid(), Name = _devKit.Name("Well 01"), TimeZone = _devKit.TimeZone};

            _wellbore = new Wellbore()
            {
                Uid = _devKit.Uid(),
                UidWell = _well.Uid,
                NameWell = _well.Name,
                Name = _devKit.Name("Wellbore 01")
            };

            _log = _devKit.CreateLog(_devKit.Uid(), _devKit.Name("Log 01"), _well.Uid, _well.Name, _wellbore.Uid, _wellbore.Name);
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
        public void Log141Adapter_DeleteFromStore_Can_Delete_Log_With_Data()
        {
            const int numOfRows = 10;

            // Add log
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), numOfRows);

            AddLog(_log);

            // Query log
            var result = GetLog(_log);
            var logDatas = result.LogData;
            Assert.IsNotNull(logDatas);
            var logData = logDatas.FirstOrDefault();
            Assert.IsNotNull(logData);
            Assert.AreEqual(numOfRows, logData.Data.Count);

            // Delete log
            DeleteLog(_log, string.Empty);

            // Assert log is deleted
            var query = _devKit.CreateLog(_log.Uid, null, _log.UidWell, null, _log.UidWellbore, null);
            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);

            var uri = _log.GetUri();

            // Assert Channel Data Chunk is deleted
            var chunks = GetDataChunks(uri);
            Assert.IsTrue(chunks.Count == 0);
        }

        [TestMethod]
        public void Log141Adapter_DeleteFromStore_Can_Delete_Log_With_Data_File()
        {
            // Add log
            AddParents();

            // Adjust Points and Nodes for large file
            WitsmlSettings.LogMaxDataPointsAdd = 5000000;
            WitsmlSettings.LogMaxDataNodesAdd = 15000;
            WitsmlSettings.LogMaxDataPointsGet = 5000000;
            WitsmlSettings.LogMaxDataNodesGet = 15000;

            var xmlfile = Path.Combine(_testDataDir, string.Format(_exceedFileFormat, "log"));
            var xmlin = File.ReadAllText(xmlfile);

            var logList = EnergisticsConverter.XmlToObject<LogList>(xmlin);
            Assert.IsNotNull(logList);

            var log = logList.Log.FirstOrDefault();
            Assert.IsNotNull(log);

            log.Uid = _devKit.Uid();
            log.UidWell = _well.Uid;
            log.UidWellbore = _wellbore.Uid;
            log.NameWell = _well.Name;
            log.NameWellbore = _wellbore.Name;

            var logDataAdded = log.LogData.FirstOrDefault();
            Assert.IsNotNull(logDataAdded);

            AddLog(log);

            // Query log
            var result = GetLog(log);
            var logDatas = result.LogData;
            Assert.IsNotNull(logDatas);
            var logData = logDatas.FirstOrDefault();
            Assert.IsNotNull(logData);
            Assert.AreEqual(logDataAdded.Data.Count, logData.Data.Count);

            var uri = log.GetUri();

            // Query Data Chunk
            var chunks = GetDataChunks(uri);
            Assert.IsTrue(chunks.Count > 0);

            // Query Mongo File
            var fileChunks = chunks.Where(c => string.IsNullOrEmpty(c.Data)).ToList();
            Assert.IsTrue(fileChunks.Count > 0);

            var database = _provider.GetDatabase();

            foreach (var fc in fileChunks)
            {
                Assert.IsNull(fc.Data);
                var mongoFile = GetMongoFile(database, fc.Uid);
                Assert.IsNotNull(mongoFile);
            }

            var fileUids = fileChunks.Select(fc => fc.Uid).ToList();

            // Delete log
            DeleteLog(log, string.Empty);

            // Assert log is deleted
            var query = _devKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);

            // Assert Channel Data Chunk is deleted          
            chunks = GetDataChunks(uri);
            Assert.IsTrue(chunks.Count == 0);

            // Assert Mongo file is deleted
            foreach (var uid in fileUids)
            {
                var mongoFile = GetMongoFile(database, uid);
                Assert.IsNull(mongoFile);
            }
        }

        private void AddParents()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        private void AddLog(Log log)
        {
            var response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        private Log GetLog(Log log)
        {
            var query = _devKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

            return result;
        }

        private void DeleteLog(Log log, string delete)
        {
            var queryIn = string.Format(DevKit141Aspect.BasicDeleteLogXmlTemplate, log.Uid, log.UidWell, log.UidWellbore, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Log, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        private List<ChannelDataChunk> GetDataChunks(EtpUri uri)
        {
            var filter = MongoDbUtility.BuildFilter<ChannelDataChunk>("Uri", uri.ToString());
            var database = _provider.GetDatabase();
            var collection = database.GetCollection<ChannelDataChunk>("channelDataChunk");
            return collection.Find(filter).ToList();
        }

        private GridFSFileInfo GetMongoFile(IMongoDatabase database, string uid)
        {
            var bucket = new GridFSBucket(database, new GridFSBucketOptions
            {
                BucketName = ChannelDataChunkAdapter.BucketName,
                ChunkSizeBytes = WitsmlSettings.ChunkSizeBytes
            });

            var mongoFileFilter = Builders<GridFSFileInfo>.Filter.Eq(fi => fi.Metadata[ChannelDataChunkAdapter.FileName], uid);
            return bucket.Find(mongoFileFilter).FirstOrDefault();
        }
    }
}
