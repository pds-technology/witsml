//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    public class Log141DataAdapterAddTests
    {        
        private DevKit141Aspect _devKit;
        private Well _well;
        private Wellbore _wellbore;
        private Log _log;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _well = new Well { Uid = _devKit.Uid(), Name = _devKit.Name("Well 01"), TimeZone = _devKit.TimeZone };

            _wellbore = new Wellbore()
            {
                Uid = _devKit.Uid(),
                UidWell = _well.Uid,
                NameWell = _well.Name,
                Name = _devKit.Name("Wellbore 01")
            };

            _log = new Log()
            {
                Uid = _devKit.Uid(),
                UidWell = _well.Uid,
                UidWellbore = _wellbore.Uid,
                NameWell = _well.Name,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            WitsmlSettings.DepthRangeSize = DevKitAspect.DefaultDepthChunkRange;
            WitsmlSettings.TimeRangeSize = DevKitAspect.DefaultTimeChunkRange;
            WitsmlSettings.MaxDataPoints = DevKitAspect.DefaultMaxDataPoints;
            WitsmlSettings.MaxDataNodes = DevKitAspect.DefaultMaxDataNodes;
        }

        [TestMethod]
        public void Log_can_be_added_without_depth_data()
        {
            _well.Uid = "804415d0-b5e7-4389-a3c6-cdb790f5485f";
            _well.Name = "Test Well 1.4.1.1";

            // check if well already exists
            var wlResults = _devKit.Query<WellList, Well>(_well);
            if (!wlResults.Any())
            {
                _devKit.Add<WellList, Well>(_well);
            }

            _wellbore.Uid = "d3e7d4bf-0f29-4c2b-974d-4871cf8001fd";
            _wellbore.Name = "Test Wellbore 1.4.1.1";
            _wellbore.UidWell = _well.Uid;
            _wellbore.NameWell = _well.Name;

            // check if wellbore already exists
            var wbResults = _devKit.Query<WellboreList, Wellbore>(_wellbore);
            if (!wbResults.Any())
            {
                _devKit.Add<WellboreList, Wellbore>(_wellbore);
            }

            var log = CreateLog("e2401b72-550f-4695-ab27-d5b0589bde17", "Test Depth Log 1.4.1.1", _well, _wellbore);

            // check if log already exists
            var logResults = _devKit.Query<LogList, Log>(log);
            if (!logResults.Any())
            {
                _devKit.InitHeader(log, LogIndexType.measureddepth);
                var response = _devKit.Add<LogList, Log>(log);
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }
        }

        [TestMethod]
        public void Log_can_be_added_without_time_data()
        {
            _well.Uid = "804415d0-b5e7-4389-a3c6-cdb790f5485f";
            _well.Name = "Test Well 1.4.1.1";

            // check if well already exists
            var wlResults = _devKit.Query<WellList, Well>(_well);
            if (!wlResults.Any())
            {
                _devKit.Add<WellList, Well>(_well);
            }

            _wellbore.Uid = "d3e7d4bf-0f29-4c2b-974d-4871cf8001fd";
            _wellbore.Name = "Test Wellbore 1.4.1.1";
            _wellbore.UidWell = _well.Uid;
            _wellbore.NameWell = _well.Name;

            // check if wellbore already exists
            var wbResults = _devKit.Query<WellboreList, Wellbore>(_wellbore);
            if (!wbResults.Any())
            {
                _devKit.Add<WellboreList, Wellbore>(_wellbore);
            }

            var log = CreateLog(
                "e2401b72-550f-4695-ab27-d5b0589bde18", 
                "Test Time Log 1.4.1.1", 
                _well, 
                _wellbore);

            // check if log already exists
            var logResults = _devKit.Query<LogList, Log>(log);
            if (!logResults.Any())
            {
                _devKit.InitHeader(log, LogIndexType.datetime);
                var response = _devKit.Add<LogList, Log>(log);
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }
        }

        [TestMethod]
        public void Log_can_be_added_with_depth_data()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(
                null, 
                _devKit.Name("Log can be added with depth data"), 
                _wellbore.UidWell, 
                _well.Name, 
                response.SuppMsgOut, 
                _wellbore.Name);

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Log_can_be_added_with_time_data()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = CreateLog(
                null, 
                _devKit.Name("Log can be added with time data"), 
                _wellbore.UidWell, 
                _well.Name, 
                uidWellbore, 
                _wellbore.Name);

            _devKit.InitHeader(log, LogIndexType.datetime);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10, 1, false);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
        }

        [TestMethod]
        public void Test_add_unsequenced_increasing_depth_log()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog("", _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.StartIndex = new GenericMeasure(13, "ft");
            log.EndIndex = new GenericMeasure(17, "ft");
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            var logData = log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("17,17.1,17.2");
            
            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(5, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;           
            int index = 13;
            foreach (string row in resultLogData)
            {
                string[] columns = row.Split(',');
                int outIndex = int.Parse(columns[0]);
                Assert.AreEqual(index, outIndex);

                double outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                double outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index++;
            }
        }

        [TestMethod]
        public void Test_add_unsequenced_decreasing_depth_log()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog("", _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.StartIndex = new GenericMeasure(13, "ft");
            log.EndIndex = new GenericMeasure(17, "ft");
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            var logData = log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("17,17.1,17.2");

            _devKit.InitHeader(log, LogIndexType.measureddepth, false);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(5, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;
            int index = 17;
            foreach (string row in resultLogData)
            {
                string[] columns = row.Split(',');
                int outIndex = int.Parse(columns[0]);
                Assert.AreEqual(index, outIndex);

                double outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                double outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index--;
            }
        }

        [TestMethod]
        public void Test_add_unsequenced_increasing_time_log()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog("", _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.StartDateTimeIndex = new Energistics.DataAccess.Timestamp();
            log.EndDateTimeIndex = new Energistics.DataAccess.Timestamp();
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            var logData = log.LogData.First();
            logData.Data.Add("2016-04-13T15:30:42.0000000-05:00,30.1,30.2");
            logData.Data.Add("2016-04-13T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-13T15:34:42.0000000-05:00,34.1,34.2");

            _devKit.InitHeader(log, LogIndexType.datetime);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(6, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;
            int index = 30;
            DateTimeOffset? previousDateTime = null;
            foreach (string row in resultLogData)
            {
                string[] columns = row.Split(',');
                DateTimeOffset outIndex = DateTimeOffset.Parse(columns[0]);
                Assert.AreEqual(index, outIndex.Minute);
                if (previousDateTime.HasValue)
                {
                    Assert.IsTrue((outIndex.ToUnixTimeSeconds() - previousDateTime.Value.ToUnixTimeSeconds()) == 60);
                }
                previousDateTime = outIndex;

                double outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                double outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index++;
            }
        }

        [TestMethod]
        public void Test_add_unsequenced_decreasing_time_log()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog("", _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.StartDateTimeIndex = new Energistics.DataAccess.Timestamp();
            log.EndDateTimeIndex = new Energistics.DataAccess.Timestamp();
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            var logData = log.LogData.First();
            logData.Data.Add("2016-04-13T15:30:42.0000000-05:00,30.1,30.2");
            logData.Data.Add("2016-04-13T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-13T15:34:42.0000000-05:00,34.1,34.2");

            _devKit.InitHeader(log, LogIndexType.datetime, false);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(6, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;
            int index = 35;
            DateTimeOffset? previousDateTime = null;
            foreach (string row in resultLogData)
            {
                string[] columns = row.Split(',');
                DateTimeOffset outIndex = DateTimeOffset.Parse(columns[0]);
                Assert.AreEqual(index, outIndex.Minute);
                if (previousDateTime.HasValue)
                {
                    Assert.IsTrue((outIndex.ToUnixTimeSeconds() - previousDateTime.Value.ToUnixTimeSeconds()) == -60);
                }
                previousDateTime = outIndex;

                double outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                double outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index--;
            }
        }

        [TestMethod]
        public void Test_append_log_data()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(
                null, 
                _devKit.Name("Log Test append log data"), 
                _wellbore.UidWell, 
                _well.Name, 
                response.SuppMsgOut, 
                _wellbore.Name);
            log.StartIndex = new GenericMeasure(5, "m");

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = log.UidWellbore;
            var uidLog = response.SuppMsgOut;

            log = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);
            log.StartIndex = new GenericMeasure(17, "m");

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 6);

            var updateResponse = _devKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.First();
            var logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(16, logData.Data.Count);
        }

        [TestMethod]
        public void Test_prepend_log_data()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(
                null, 
                _devKit.Name("Log Test prepend log data"), 
                _wellbore.UidWell, 
                _well.Name, 
                response.SuppMsgOut, 
                _wellbore.Name);
            log.StartIndex = new GenericMeasure(17, "m");

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = log.UidWellbore;
            var uidLog = response.SuppMsgOut;

            log = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);
            log.StartIndex = new GenericMeasure(5, "m");

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 6);

            var updateResponse = _devKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.First();
            var logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(16, logData.Data.Count);
        }

        [TestMethod]
        public void Test_update_overlapping_log_data()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(
                null, 
                _devKit.Name("Log Test update overlapping log data"), 
                _wellbore.UidWell, 
                _well.Name, 
                response.SuppMsgOut, 
                _wellbore.Name);
            log.StartIndex = new GenericMeasure(1, "m");

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 8);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = log.UidWellbore;
            var uidLog = response.SuppMsgOut;

            log = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);
            log.StartIndex = new GenericMeasure(4.1, "m");

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 3, 0.9);

            var updateResponse = _devKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.First();
            var logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(9, logData.Data.Count);
        }

        [TestMethod]
        public void Test_overwrite_log_data_chunk()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(
                null, 
                _devKit.Name("Log Test overwrite log data chunk"), 
                _wellbore.UidWell, 
                _well.Name, 
                response.SuppMsgOut, 
                _wellbore.Name);
            log.StartIndex = new GenericMeasure(17, "m");

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 6);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = log.UidWellbore;
            var uidLog = response.SuppMsgOut;

            log = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);
            log.StartIndex = new GenericMeasure(4.1, "m");

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 3, 0.9);

            var logData = log.LogData.First();
            logData.Data.Add("21.5, 1, 21.7");

            var updateResponse = _devKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.First();
            logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(5, logData.Data.Count);
        }

        [TestMethod]
        public void Test_update_log_data_with_different_range_for_each_channel()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(
                null, 
                _devKit.Name("Log Test update log data diff range"), 
                _wellbore.UidWell, 
                _well.Name, 
                response.SuppMsgOut, 
                _wellbore.Name);
            log.StartIndex = new GenericMeasure(15, "m");

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 8);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = log.UidWellbore;
            var uidLog = response.SuppMsgOut;

            log = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);
            log.StartIndex = new GenericMeasure(13, "m");

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 6, 0.9);

            var logData = log.LogData.First();
            logData.Data.Clear();

            logData.Data.Add("13,13.1,");
            logData.Data.Add("14,14.1,");
            logData.Data.Add("15,15.1,");
            logData.Data.Add("16,16.1,");
            logData.Data.Add("17,17.1,");
            logData.Data.Add("20,20.1,20.2");
            logData.Data.Add("21,,21.2");
            logData.Data.Add("22,,22.2");
            logData.Data.Add("23,,23.2");

            var updateResponse = _devKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.First();
            logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(11, logData.Data.Count);

            var data = logData.Data;
            Assert.AreEqual("15,15.1,15", data[2]);
        }

        [TestMethod]
        public void Test_update_log_header()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = CreateLog(
                null, 
                _devKit.Name("Log Test update log header"), 
                _wellbore.UidWell, 
                _well.Name, 
                uidWellbore, 
                _wellbore.Name);
            log.Description = "Not updated field";
            log.RunNumber = "101";
            log.BhaRunNumber = 1;

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logAdded = results.FirstOrDefault();

            Assert.IsNotNull(logAdded);
            Assert.AreEqual(log.Description, logAdded.Description);
            Assert.AreEqual(log.RunNumber, logAdded.RunNumber);
            Assert.AreEqual(log.BhaRunNumber, logAdded.BhaRunNumber);
            Assert.IsNull(logAdded.CommonData.ItemState);

            var update = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);
            update.CommonData = new CommonData { ItemState = ItemState.actual };
            update.RunNumber = "102";
            update.BhaRunNumber = 2;

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logUpdated = results.FirstOrDefault();

            Assert.IsNotNull(logUpdated);
            Assert.AreEqual(logAdded.Description, logUpdated.Description);
            Assert.AreEqual(update.RunNumber, logUpdated.RunNumber);
            Assert.AreEqual(update.BhaRunNumber, logUpdated.BhaRunNumber);
            Assert.AreEqual(update.CommonData.ItemState, logUpdated.CommonData.ItemState);
        }

        [TestMethod]
        public void Test_update_log_header_update_curve()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = CreateLog(
                null, 
                _devKit.Name("Log Test update log header update curve"), 
                _wellbore.UidWell, 
                _well.Name, 
                uidWellbore, 
                _wellbore.Name);
            log.Description = "Not updated field";
            log.RunNumber = "101";
            log.BhaRunNumber = 1;

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            log.LogCurveInfo.RemoveAt(2);
            log.LogData.Clear();
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logAdded = results.FirstOrDefault();

            Assert.IsNotNull(logAdded);
            Assert.AreEqual(log.Description, logAdded.Description);
            Assert.AreEqual(log.RunNumber, logAdded.RunNumber);
            Assert.AreEqual(log.BhaRunNumber, logAdded.BhaRunNumber);
            Assert.IsNull(logAdded.CommonData.ItemState);

            var logCurve = _devKit.GetLogCurveInfoByUid(logAdded.LogCurveInfo, "ROP") as LogCurveInfo;
            Assert.IsNull(logCurve.CurveDescription);

            var update = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);
            update.CommonData = new CommonData { ItemState = ItemState.actual };
            update.RunNumber = "102";
            update.BhaRunNumber = 2;

            _devKit.InitHeader(update, LogIndexType.measureddepth);
            update.LogCurveInfo.RemoveAt(2);
            update.LogCurveInfo.RemoveAt(0);
            update.LogData.Clear();
            var updateCurve = update.LogCurveInfo.First();
            updateCurve.CurveDescription = "Updated description";

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logUpdated = results.FirstOrDefault();

            Assert.IsNotNull(logUpdated);
            Assert.AreEqual(logAdded.Description, logUpdated.Description);
            Assert.AreEqual(update.RunNumber, logUpdated.RunNumber);
            Assert.AreEqual(update.BhaRunNumber, logUpdated.BhaRunNumber);
            Assert.AreEqual(update.CommonData.ItemState, logUpdated.CommonData.ItemState);

            logCurve = _devKit.GetLogCurveInfoByUid(logUpdated.LogCurveInfo, "ROP") as LogCurveInfo;
            Assert.AreEqual(updateCurve.CurveDescription, logCurve.CurveDescription);
        }

        [TestMethod]
        public void Test_update_log_header_add_curve()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = CreateLog(
                null, 
                _devKit.Name("Log Test update log header add curve"), 
                _wellbore.UidWell, 
                _well.Name, 
                uidWellbore, 
                _wellbore.Name);

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            log.LogCurveInfo.RemoveRange(1, 2);
            log.LogData.Clear();

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logAdded = results.FirstOrDefault();

            Assert.IsNotNull(logAdded);
            Assert.AreEqual(1, logAdded.LogCurveInfo.Count);
            Assert.AreEqual(log.LogCurveInfo.Count, logAdded.LogCurveInfo.Count);

            var update = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            _devKit.InitHeader(update, LogIndexType.measureddepth);
            update.LogCurveInfo.RemoveAt(2);
            update.LogCurveInfo.RemoveAt(0);
            update.LogData.Clear();

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logUpdated = results.FirstOrDefault();

            Assert.IsNotNull(logUpdated);
            Assert.AreEqual(2, logUpdated.LogCurveInfo.Count);
        }

        [TestMethod]
        public void Test_log_index_direction_default_and_update()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = CreateLog(
                null, 
                _devKit.Name("Log Test log index direction default"), 
                _wellbore.UidWell, 
                _well.Name, 
                uidWellbore, 
                _wellbore.Name);
            log.RunNumber = "101";
            log.IndexCurve = "MD";
            log.IndexType = LogIndexType.measureddepth;

            _devKit.InitHeader(log, log.IndexType.Value);
            log.Direction = null;

            Assert.IsFalse(log.Direction.HasValue);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logAdded = results.FirstOrDefault();

            Assert.IsNotNull(logAdded);
            Assert.AreEqual(LogIndexDirection.increasing, logAdded.Direction);
            Assert.AreEqual(log.RunNumber, logAdded.RunNumber);

            var update = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);
            update.Direction = LogIndexDirection.decreasing;
            update.RunNumber = "102";

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logUpdated = results.FirstOrDefault();

            Assert.IsNotNull(logUpdated);
            Assert.AreEqual(LogIndexDirection.increasing, logAdded.Direction);
            Assert.AreEqual(update.RunNumber, logUpdated.RunNumber);
        }

        [TestMethod]
        public void Test_update_log_data_and_index_range()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(
                null, 
                _devKit.Name("Log Test update log data and index range"), 
                _wellbore.UidWell, 
                _well.Name, 
                response.SuppMsgOut, 
                _wellbore.Name);
            log.StartIndex = new GenericMeasure(15, "m");

            // Initialize a Log with 3 channels where the 2nd channel is blank
            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 8);

            // Make sure there are 3 curves
            var lciUids = log.LogCurveInfo.Select(l => l.Uid).ToArray();
            Assert.AreEqual(3, lciUids.Length);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = log.UidWellbore;
            var uidLog = response.SuppMsgOut;

            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);
            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logAdded = results.FirstOrDefault();

            Assert.IsNotNull(logAdded);
            Assert.AreEqual(15, logAdded.StartIndex.Value);
            Assert.AreEqual(22, logAdded.EndIndex.Value);

            // Check the range of the index curve
            var mdCurve = _devKit.GetLogCurveInfoByUid(logAdded.LogCurveInfo, logAdded.IndexCurve) as LogCurveInfo;
            Assert.AreEqual(logAdded.StartIndex.Value, mdCurve.MinIndex.Value);
            Assert.AreEqual(logAdded.EndIndex.Value, mdCurve.MaxIndex.Value);

            // Look for the 2nd LogCurveInfo by Mnemonic.  It should be filtered out and not exist.
            var curve2 = _devKit.GetLogCurveInfoByUid(logAdded.LogCurveInfo, lciUids[1]) as LogCurveInfo;
            Assert.IsNull(curve2);

            // Check the range of the 3rd curve.
            var curve3 = _devKit.GetLogCurveInfoByUid(logAdded.LogCurveInfo, lciUids[2]) as LogCurveInfo;
            Assert.AreEqual(logAdded.StartIndex.Value, curve3.MinIndex.Value);
            Assert.AreEqual(logAdded.EndIndex.Value, curve3.MaxIndex.Value);

            log = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);
            log.StartIndex = new GenericMeasure(13, "m");

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 6, 0.9);

            var logData = log.LogData.First();
            logData.Data.Clear();

            logData.Data.Add("13,13.1,");
            logData.Data.Add("14,14.1,");
            logData.Data.Add("15,15.1,");
            logData.Data.Add("16,16.1,");
            logData.Data.Add("17,17.1,");
            logData.Data.Add("20,20.1,20.2");
            logData.Data.Add("21,,21.2");
            logData.Data.Add("22,,22.2");
            logData.Data.Add("23,,23.2");

            var updateResponse = _devKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var logUpdated = results.First();
            logData = logUpdated.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(11, logData.Data.Count);
            Assert.AreEqual(13, logUpdated.StartIndex.Value);
            Assert.AreEqual(23, logUpdated.EndIndex.Value);

            mdCurve = _devKit.GetLogCurveInfoByUid(logUpdated.LogCurveInfo, lciUids[0]) as LogCurveInfo;
            Assert.AreEqual(logUpdated.StartIndex.Value, mdCurve.MinIndex.Value);
            Assert.AreEqual(logUpdated.EndIndex.Value, mdCurve.MaxIndex.Value);

            curve2 = _devKit.GetLogCurveInfoByUid(logUpdated.LogCurveInfo, lciUids[1]) as LogCurveInfo;
            Assert.AreEqual(13, curve2.MinIndex.Value);
            Assert.AreEqual(20, curve2.MaxIndex.Value);

            curve3 = _devKit.GetLogCurveInfoByUid(logUpdated.LogCurveInfo, lciUids[2]) as LogCurveInfo;
            Assert.AreEqual(15, curve3.MinIndex.Value);
            Assert.AreEqual(23, curve3.MaxIndex.Value);
        }

        [TestMethod]
        public void Test_log_index_direction_decreasing()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = CreateLog(
                null, 
                _devKit.Name("Log log index direction decreasing"), 
                _wellbore.UidWell, 
                _well.Name, 
                uidWellbore, 
                _wellbore.Name);
            log.RunNumber = "101";
            log.IndexCurve = "MD";
            log.IndexType = LogIndexType.measureddepth;
            log.Direction = LogIndexDirection.decreasing;

            _devKit.InitHeader(log, log.IndexType.Value, increasing: false);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 100, 0.9, increasing: false);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logAdded = results.FirstOrDefault();

            Assert.IsNotNull(logAdded);
            Assert.AreEqual(LogIndexDirection.decreasing, logAdded.Direction);
            Assert.AreEqual(log.RunNumber, logAdded.RunNumber);

            var logData = log.LogData.FirstOrDefault();
            var firstIndex = int.Parse(logData.Data[0].Split(',')[0]);
            var secondIndex = int.Parse(logData.Data[1].Split(',')[0]);
            Assert.IsTrue(firstIndex > secondIndex);
        }

        [TestMethod]
        public void Test_update_with_unsequenced_increasing_depth_log_data_in_same_chunk()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(null, _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.StartIndex = new GenericMeasure(13, "ft");
            log.EndIndex = new GenericMeasure(17, "ft");
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            var logData = log.LogData.First();
            logData.Data.Add("10,10.1,10.2");           
            logData.Data.Add("15,15.1,15.2");         
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("17,17.1,17.2");
            logData.Data.Add("18,18.1,18.2");

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Update
            var updateLog = CreateLog(uidLog, log.Name, log.UidWell, log.NameWell, log.UidWellbore, log.NameWellbore);
            updateLog.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });
            updateLog.LogData[0].MnemonicList = log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("12,12.1,12.2");
            logData.Data.Add("11,11.1,11.2");
            logData.Data.Add("14,14.1,14.2");

            var updateResponse = _devKit.Update<LogList, Log>(updateLog);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query
            var query = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(9, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;
            double index = 10;
            foreach (string row in resultLogData)
            {
                string[] columns = row.Split(',');
                double outIndex = double.Parse(columns[0]);
                Assert.AreEqual(index, outIndex);

                double outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                double outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index++;
            }
        }

        [TestMethod]
        public void Test_update_with_unsequenced_increasing_depth_log_data_in_different_chunk()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(null, _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.StartIndex = new GenericMeasure(13, "ft");
            log.EndIndex = new GenericMeasure(17, "ft");
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            var logData = log.LogData.First();
            logData.Data.Add("1700.0,17.1,17.2");
            logData.Data.Add("1800.0,18.1,18.2");
            logData.Data.Add("1900.0,19.1,19.2");

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Update
            var updateLog = CreateLog(uidLog, log.Name, log.UidWell, log.NameWell, log.UidWellbore, log.NameWellbore);
            updateLog.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });
            updateLog.LogData[0].MnemonicList = log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2000.0,20.1,20.2");
            logData.Data.Add("2300.0,23.1,23.2");
            logData.Data.Add("2200.0,22.1,22.2");
            logData.Data.Add("2100.0,21.1,21.2");
            logData.Data.Add("2400.0,24.1,24.2");

            var updateResponse = _devKit.Update<LogList, Log>(updateLog);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query
            var query = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(8, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;
            double index = 17;
            foreach (string row in resultLogData)
            {
                string[] columns = row.Split(',');
                double outIndex = double.Parse(columns[0]);
                Assert.AreEqual(index * 100, outIndex);

                double outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                double outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index++;
            }
        }

        [TestMethod]
        public void Test_update_with_unsequenced_decreasing_depth_log_data_in_same_chunk()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(null, _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.StartIndex = new GenericMeasure(10, "ft");
            log.EndIndex = new GenericMeasure(18, "ft");
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            _devKit.InitHeader(log, LogIndexType.measureddepth, false);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 5);
            var logData = log.LogData.First();
            logData.Data.Clear();
            logData.Data.Add("19.0,19.1,19.2");
            logData.Data.Add("18.0,18.1,18.2");
            logData.Data.Add("17.0,17.1,17.2");

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Update
            var updateLog = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);
            updateLog.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });
            updateLog.LogData[0].MnemonicList = log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("21.0,21.1,21.2");
            logData.Data.Add("23.0,23.1,23.2");
            logData.Data.Add("22.0,22.1,22.2");
            logData.Data.Add("24.0,24.1,24.2");

            var updateResponse = _devKit.Update<LogList, Log>(updateLog);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query
            var query = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(7, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;

            int start = 0;
            for (int index = 24; index < 20; index--)
            {
                string[] columns = resultLogData[start].Split(',');
                double outIndex = double.Parse(columns[0]);
                Assert.AreEqual(index, outIndex);

                double outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                double outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                start++;
            }
        }

        [TestMethod]
        public void Test_update_with_unsequenced_decreasing_depth_log_data_in_different_chunks()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(null, _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.StartIndex = new GenericMeasure(10, "ft");
            log.EndIndex = new GenericMeasure(18, "ft");
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            _devKit.InitHeader(log, LogIndexType.measureddepth, false);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 5);
            var logData = log.LogData.First();
            logData.Data.Clear();
            logData.Data.Add("1900.0,19.1,19.2");
            logData.Data.Add("1800.0,18.1,18.2");
            logData.Data.Add("1700.0,17.1,17.2");
            logData.Data.Add("1600.0,16.1,16.2");     

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Update
            var updateLog = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);
            updateLog.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });
            updateLog.LogData[0].MnemonicList = log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2000.0,21.1,21.2");
            logData.Data.Add("2100.0,21.1,21.2");
            logData.Data.Add("2300.0,23.1,23.2");
            logData.Data.Add("2200.0,22.1,22.2");
            logData.Data.Add("2400.0,24.1,24.2");
          
            var updateResponse = _devKit.Update<LogList, Log>(updateLog);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query
            var query = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(9, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;
            
            int start = 0;
            for (int index = 24; index<20; index--)
            {
                string[] columns = resultLogData[start].Split(',');
                double outIndex = double.Parse(columns[0]);
                Assert.AreEqual(index*100, outIndex);

                double outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                double outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                start++;
            }
        }

        [TestMethod]
        public void Test_update_with_unsequenced_increasing_time_log_data_in_same_chunk()
        {
            // Set the time range chunk size to number of microseconds equivalent to one day
            WitsmlSettings.TimeRangeSize = 86400000000;

            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog("", _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.StartDateTimeIndex = new Energistics.DataAccess.Timestamp();
            log.EndDateTimeIndex = new Energistics.DataAccess.Timestamp();
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            var logData = log.LogData.First();
            logData.Data.Add("2016-04-13T15:30:42.0000000-05:00,30.1,30.2");
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.1,32.2");

            _devKit.InitHeader(log, LogIndexType.datetime);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Update
            var updateLog = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);
            updateLog.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });
            updateLog.LogData[0].MnemonicList = log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-13T15:35:42.0000000-05:00,35.1,35.2");           
            logData.Data.Add("2016-04-13T15:34:42.0000000-05:00,34.1,34.2");
            logData.Data.Add("2016-04-13T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-13T15:36:42.0000000-05:00,36.1,36.2");

            var updateResponse = _devKit.Update<LogList, Log>(updateLog);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query
            var query = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(7, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;
            int index = 30;
            DateTimeOffset? previousDateTime = null;
            foreach (string row in resultLogData)
            {
                string[] columns = row.Split(',');
                DateTimeOffset outIndex = DateTimeOffset.Parse(columns[0]);
                Assert.AreEqual(index, outIndex.Minute);
                if (previousDateTime.HasValue)
                {
                    Assert.IsTrue((outIndex.ToUnixTimeSeconds() - previousDateTime.Value.ToUnixTimeSeconds()) == 60);
                }
                previousDateTime = outIndex;

                double outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                double outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index++;
            }
        }

        [TestMethod]
        public void Test_update_with_unsequenced_increasing_time_log_data_in_different_chunk()
        {
            // Set the time range chunk size to number of microseconds equivalent to one day
            WitsmlSettings.TimeRangeSize = 86400000000;

            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog("", _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.StartDateTimeIndex = new Energistics.DataAccess.Timestamp();
            log.EndDateTimeIndex = new Energistics.DataAccess.Timestamp();
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            var logData = log.LogData.First();
            logData.Data.Add("2016-04-13T15:30:42.0000000-05:00,30.1,30.2");
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.1,32.2");

            _devKit.InitHeader(log, LogIndexType.datetime);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Update
            var updateLog = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);
            updateLog.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });
            updateLog.LogData[0].MnemonicList = log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-20T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-20T15:34:42.0000000-05:00,34.1,34.2");
            logData.Data.Add("2016-04-20T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-20T15:36:42.0000000-05:00,36.1,36.2");

            var updateResponse = _devKit.Update<LogList, Log>(updateLog);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query
            var query = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(7, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;
            int index = 33;
            DateTimeOffset? previousDateTime = null;
            for (int i = 3; i < resultLogData.Count; i++)
            {
                string[] columns = resultLogData[i].Split(',');
                DateTimeOffset outIndex = DateTimeOffset.Parse(columns[0]);
                Assert.AreEqual(index, outIndex.Minute);
                if (previousDateTime.HasValue)
                {
                    Assert.IsTrue((outIndex.ToUnixTimeSeconds() - previousDateTime.Value.ToUnixTimeSeconds()) == 60);
                }
                previousDateTime = outIndex;

                double outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                double outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index++;
            }
        }

        [TestMethod]
        public void Test_update_with_unsequenced_decreasing_time_log_data_in_same_chunk()
        {
            // Set the time range chunk size to number of microseconds equivalent to one day
            WitsmlSettings.TimeRangeSize = 86400000000;

            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog("", _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.StartDateTimeIndex = new Energistics.DataAccess.Timestamp();
            log.EndDateTimeIndex = new Energistics.DataAccess.Timestamp();
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            var logData = log.LogData.First();
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:30:42.0000000-05:00,30.1,30.2");

            _devKit.InitHeader(log, LogIndexType.datetime, false);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Update
            var updateLog = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);
            updateLog.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });
            updateLog.LogData[0].MnemonicList = log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-13T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-13T15:34:42.0000000-05:00,34.1,34.2");
            logData.Data.Add("2016-04-13T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-13T15:36:42.0000000-05:00,36.1,36.2");

            var updateResponse = _devKit.Update<LogList, Log>(updateLog);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query
            var query = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(7, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;
            int index = 36;
            DateTimeOffset? previousDateTime = null;
            foreach (string row in resultLogData)
            {
                string[] columns = row.Split(',');
                DateTimeOffset outIndex = DateTimeOffset.Parse(columns[0]);
                Assert.AreEqual(index, outIndex.Minute);
                if (previousDateTime.HasValue)
                {
                    Assert.IsTrue((outIndex.ToUnixTimeSeconds() - previousDateTime.Value.ToUnixTimeSeconds()) == -60);
                }
                previousDateTime = outIndex;

                double outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                double outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index--;
            }
        }

        [TestMethod]
        public void Test_update_with_unsequenced_decreasing_time_log_data_in_different_chunk()
        {
            // Set the time range chunk size to number of microseconds equivalent to one day
            WitsmlSettings.TimeRangeSize = 86400000000;

            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog("", _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.StartDateTimeIndex = new Energistics.DataAccess.Timestamp();
            log.EndDateTimeIndex = new Energistics.DataAccess.Timestamp();
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            var logData = log.LogData.First();
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:30:42.0000000-05:00,30.1,30.2");

            _devKit.InitHeader(log, LogIndexType.datetime, false);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Update
            var updateLog = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);
            updateLog.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });
            updateLog.LogData[0].MnemonicList = log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-10T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-10T15:34:42.0000000-05:00,34.1,34.2");
            logData.Data.Add("2016-04-10T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-10T15:36:42.0000000-05:00,36.1,36.2");

            var updateResponse = _devKit.Update<LogList, Log>(updateLog);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query
            var query = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(7, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;
            int index = 36;
            DateTimeOffset? previousDateTime = null;
            for (int i = 3; i < resultLogData.Count; i++)
            {
                string[] columns = resultLogData[i].Split(',');
                DateTimeOffset outIndex = DateTimeOffset.Parse(columns[0]);
                Assert.AreEqual(index, outIndex.Minute);
                if (previousDateTime.HasValue)
                {
                    Assert.IsTrue((outIndex.ToUnixTimeSeconds() - previousDateTime.Value.ToUnixTimeSeconds()) == -60);
                }
                previousDateTime = outIndex;

                double outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                double outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index--;
            }
        }

        [TestMethod]
        [Ignore, Description("Not Implemented")]
        public void Test_error_code_443_invalid_unit_of_measure_value()
        {

            var response = _devKit.Add<WellList, Well>(_well);
            var uidWell = response.SuppMsgOut;
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;
            var logName = "Log Test -443 - Invalid Uom";
            var startIndexUom = "abc";
            var endIndexUom = startIndexUom;
            
            string xmlIn = CreateXmlLog(
                logName, 
                uidWell, 
                _well.Name, 
                uidWellbore, 
                _wellbore.Name, 
                startIndexUom, 
                endIndexUom);
            response = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InvalidUnitOfMeasure, response.Result);
        }

        [TestMethod]
        public void Test_error_code_453_missing_unit_for_measure_data()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            var uidWell = response.SuppMsgOut;
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;
            var logName = "Log Test -453 - Missing Uom";

            string xmlIn = CreateXmlLog(
                logName, 
                uidWell, 
                _well.Name, 
                uidWellbore, 
                _wellbore.Name, 
                startIndexUom: null, 
                endIndexUom: null);
            response = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, response.Result);
        }

        [TestMethod]
        public void Test_error_code_406_missing_parent_uid()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = CreateLog(
                null, 
                _devKit.Name("Log Test error code -406 missing parent"), 
                null, 
                _well.Name, 
                null, 
                _wellbore.Name);
            log.RunNumber = "101";
            log.IndexCurve = "MD";
            log.IndexType = LogIndexType.measureddepth;
            log.Direction = LogIndexDirection.decreasing;

            _devKit.InitHeader(log, log.IndexType.Value, increasing: false);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 100, 0.9, increasing: false);

            response = _devKit.Add<LogList, Log>(log);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingParentUid, response.Result);
        }

        [TestMethod]
        public void Test_error_code_478_parent_uid_case_not_matching()
        {
            // Base uid
            var uid = "well-01-error-478" + _devKit.Uid();

            // Well Uid with uppercase "P"
            _well.Uid = "P" + uid;
            _well.Name = _devKit.Name("Well-to-add-01");
            var response = _devKit.Add<WellList, Well>(_well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Well Uid with uppercase "P"
            _wellbore.Name = _devKit.Name("Wellbore-to-add-02");
            _wellbore.NameWell = _well.Name;
            _wellbore.UidWell = "P" + uid;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            // Well Uid with lowercase "p"
            var log = CreateLog(
                _devKit.Uid(), 
                name: _devKit.Name("Log Test error code -478 parent uid case"), 
                uidWell: "p" + uid, 
                nameWell: _well.Name, 
                uidWellbore: response.SuppMsgOut, 
                nameWellbore: _wellbore.Name);
            log.RunNumber = "101";
            log.IndexCurve = "MD";
            log.IndexType = LogIndexType.measureddepth;
            log.Direction = LogIndexDirection.decreasing;
            _devKit.InitHeader(log, log.IndexType.Value, increasing: false);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 100, 0.9, increasing: false);

            response = _devKit.Add<LogList, Log>(log);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.IncorrectCaseParentUid, response.Result);
        }

        [TestMethod]
        public void Log141DataAdapater_AddToStore_Move_Index_Curve_To_First()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(
                null,
                _devKit.Name("Log can be added with depth data"),
                _wellbore.UidWell,
                _well.Name,
                response.SuppMsgOut,
                _wellbore.Name);

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            var logCurves = log.LogCurveInfo;
            var indexCurve = logCurves.First();
            logCurves.Remove(indexCurve);
            logCurves.Add(indexCurve);
            var firstCurve = log.LogCurveInfo.First();
            Assert.AreNotEqual(indexCurve.Mnemonic.Value, firstCurve.Mnemonic.Value);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logAdded = results.FirstOrDefault();
            Assert.IsNotNull(logAdded);
            firstCurve = logAdded.LogCurveInfo.First();
            Assert.AreEqual(indexCurve.Mnemonic.Value, firstCurve.Mnemonic.Value);
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Supports_NaN_In_Numeric_Fields()
        {
            // Add well
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add log
            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + _wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + _well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + _wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + _devKit.Name("Log 01") + "</name>" + Environment.NewLine +
                        "<indexType>measured depth</indexType>" + Environment.NewLine +
                        "<direction>increasing</direction>" + Environment.NewLine +
                        "<bhaRunNumber>NaN</bhaRunNumber>" + Environment.NewLine +
                        "<indexCurve>MD</indexCurve>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <unit>m</unit>" + Environment.NewLine +
                        "  <classIndex>NaN</classIndex>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"GR\">" + Environment.NewLine +
                        "  <mnemonic>GR</mnemonic>" + Environment.NewLine +
                        "  <unit>gAPI</unit>" + Environment.NewLine +
                        "  <classIndex>NaN</classIndex>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);
            
            var uidLog = result.SuppMsgOut;

            // Query log
            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.IsTrue(results.Any());

            Assert.IsNull(results.First().BhaRunNumber);
            Assert.AreEqual(2, results.First().LogCurveInfo.Count);
            Assert.IsNull(results.First().LogCurveInfo[0].ClassIndex);
            Assert.IsNull(results.First().LogCurveInfo[1].ClassIndex);
        }

        [TestMethod, Description("To test adding a log with special characters")]
        public void Log141DataAdapter_AddToStore_Can_Add_Log_With_Special_Characters()
        {
            // Add well
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add log
            var description         = @"~ ! @ # $ % ^ &amp; * ( ) _ + { } | &lt; > ? ; : ' "" , . / \ [ ] and \b \f \n \r \t \"" \\ ";
            var expectedDescription = @"~ ! @ # $ % ^ & * ( ) _ + { } | < > ? ; : ' "" , . / \ [ ] and \b \f \n \r \t \"" \\";

            var row = @"~ ! @ # $ % ^ &amp; * ( ) _ + { } | &lt; > ? ; : ' "" . / \ [ ] and \b \f \n \r \t \"" \\ ";   // Comma omitted
            var expectedRow = @"~ ! @ # $ % ^ & * ( ) _ + { } | < > ? ; : ' "" . / \ [ ] and \b \f \n \r \t \"" \\";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + _wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + _well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + _wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + _devKit.Name("Test special characters") + "</name>" + Environment.NewLine +
                        "<indexType>measured depth</indexType>" + Environment.NewLine +
                        "<direction>increasing</direction>" + Environment.NewLine +
                        "<description>" + description + "</description>" + Environment.NewLine +
                        "<indexCurve>MD</indexCurve>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <unit>m</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"AAA\">" + Environment.NewLine +
                        "  <mnemonic>AAA</mnemonic>" + Environment.NewLine +
                        "  <unit>unitless</unit>" + Environment.NewLine +
                        "  <typeLogData>string</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"BBB\">" + Environment.NewLine +
                        "  <mnemonic>BBB</mnemonic>" + Environment.NewLine +
                        "  <unit>s</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logData>" + Environment.NewLine +
                        "   <mnemonicList>MD,AAA,BBB</mnemonicList>" + Environment.NewLine +
                        "   <unitList>m,unitless,s</unitList>" + Environment.NewLine +
                        "   <data>5000.0," + row + ", 5.1 </data>" + Environment.NewLine +
                        "</logData>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);
            var uidLog = result.SuppMsgOut;

            // Query log
            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsTrue(results.Any());

            var returnLog = results.First();
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(expectedDescription, returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(expectedRow, channelData[1].Trim());
        }

        [TestMethod, Description("To test adding a log with special characters & (ampersand) and throws error -409")]
        public void Log141DataAdapter_AddToStore_Error_409_Log_With_Special_Characters_Ampersand()
        {
            // Add well
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add log          
            var description = "<description>Header & </description>";
            var row = "<data>5000.1, Data & , 5.1</data>";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + _wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + _well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + _wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + _devKit.Name("Test special characters") + "</name>" + Environment.NewLine +
                        "<indexType>measured depth</indexType>" + Environment.NewLine +
                        "<direction>increasing</direction>" + Environment.NewLine +
                        description + Environment.NewLine +
                        "<indexCurve>MD</indexCurve>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <unit>m</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"AAA\">" + Environment.NewLine +
                        "  <mnemonic>AAA</mnemonic>" + Environment.NewLine +
                        "  <unit>unitless</unit>" + Environment.NewLine +
                        "  <typeLogData>string</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"BBB\">" + Environment.NewLine +
                        "  <mnemonic>BBB</mnemonic>" + Environment.NewLine +
                        "  <unit>s</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logData>" + Environment.NewLine +
                        "   <mnemonicList>MD,AAA,BBB</mnemonicList>" + Environment.NewLine +
                        "   <unitList>m,unitless,s</unitList>"+ Environment.NewLine +
                        row + Environment.NewLine +
                        "</logData>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, result.Result);
        }

        [TestMethod, Description("To test adding a log with special character: &amp; (encoded ampersand)")]
        public void Log141DataAdapter_AddToStore_Can_Add_Log_With_Special_Characters_Encoded_Ampersand()
        {
            // Add well
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add log          
            var description = "<description>Header &amp; </description>";
            var row = "<data>5000.1, Data &amp; , 5.1</data>";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + _wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + _well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + _wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + _devKit.Name("Test special characters") + "</name>" + Environment.NewLine +
                        "<indexType>measured depth</indexType>" + Environment.NewLine +
                        "<direction>increasing</direction>" + Environment.NewLine +
                        description + Environment.NewLine +
                        "<indexCurve>MD</indexCurve>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <unit>m</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"AAA\">" + Environment.NewLine +
                        "  <mnemonic>AAA</mnemonic>" + Environment.NewLine +
                        "  <unit>unitless</unit>" + Environment.NewLine +
                        "  <typeLogData>string</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"BBB\">" + Environment.NewLine +
                        "  <mnemonic>BBB</mnemonic>" + Environment.NewLine +
                        "  <unit>s</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logData>" + Environment.NewLine +
                        "   <mnemonicList>MD,AAA,BBB</mnemonicList>" + Environment.NewLine +
                        "   <unitList>m,unitless,s</unitList>" + Environment.NewLine +
                        row + Environment.NewLine +
                        "</logData>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            var uidLog = result.SuppMsgOut;

            // Query log
            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsTrue(results.Any());

            var returnLog = results.First();
            Assert.IsNotNull(returnLog.Description.Trim());
            Assert.AreEqual("Header &", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual("Data &", channelData[1].Trim());
        }

        [TestMethod, Description("To test adding a log with special characters < (less than) and returning error -409")]
        public void Log141DataAdapter_AddToStore_Error_409_Log_With_Special_Characters_Less_Than()
        {
            // Add well
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add log          
            var description = "<description>Header < </description>";
            var row = "<data>5000.1, Data < , 5.1</data>";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + _wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + _well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + _wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + _devKit.Name("Test special characters") + "</name>" + Environment.NewLine +
                        "<indexType>measured depth</indexType>" + Environment.NewLine +
                        "<direction>increasing</direction>" + Environment.NewLine +
                        description + Environment.NewLine +
                        "<indexCurve>MD</indexCurve>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <unit>m</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"AAA\">" + Environment.NewLine +
                        "  <mnemonic>AAA</mnemonic>" + Environment.NewLine +
                        "  <unit>unitless</unit>" + Environment.NewLine +
                        "  <typeLogData>string</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"BBB\">" + Environment.NewLine +
                        "  <mnemonic>BBB</mnemonic>" + Environment.NewLine +
                        "  <unit>s</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logData>" + Environment.NewLine +
                        "   <mnemonicList>MD,AAA,BBB</mnemonicList>" + Environment.NewLine +
                        "   <unitList>m,unitless,s</unitList>" + Environment.NewLine +
                        row + Environment.NewLine +
                        "</logData>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, result.Result);
        }

        [TestMethod, Description("To test adding a log with special characters &lt; (encoded less than)")]
        public void Log141DataAdapter_AddToStore_Can_Add_Log_With_Special_Characters_Encoded_Less_Than()
        {
            // Add well
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add log          
            var description = "<description>Header &lt; </description>";
            var row = "<data>5000.1, Data &lt; , 5.1</data>";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + _wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + _well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + _wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + _devKit.Name("Test special characters") + "</name>" + Environment.NewLine +
                        "<indexType>measured depth</indexType>" + Environment.NewLine +
                        "<direction>increasing</direction>" + Environment.NewLine +
                        description + Environment.NewLine +
                        "<indexCurve>MD</indexCurve>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <unit>m</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"AAA\">" + Environment.NewLine +
                        "  <mnemonic>AAA</mnemonic>" + Environment.NewLine +
                        "  <unit>unitless</unit>" + Environment.NewLine +
                        "  <typeLogData>string</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"BBB\">" + Environment.NewLine +
                        "  <mnemonic>BBB</mnemonic>" + Environment.NewLine +
                        "  <unit>s</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logData>" + Environment.NewLine +
                        "   <mnemonicList>MD,AAA,BBB</mnemonicList>" + Environment.NewLine +
                        "   <unitList>m,unitless,s</unitList>" + Environment.NewLine +
                        row + Environment.NewLine +
                        "</logData>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            var uidLog = result.SuppMsgOut;

            // Query log
            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsTrue(results.Any());

            var returnLog = results.First();
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual("Header <", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual("Data <", channelData[1].Trim());
        }

        [TestMethod, Description(@"To test adding log data string channel with \ (backslash).")]
    
        public void Log141DataAdapter_AddToStore_Can_Add_Log_With_Special_Character_Backslash()
        {
            // Add well
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add log          
            var description = @"<description>Header \ </description>";
            var row = @"<data>5000.0, Data \ , 5.0</data>";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + _wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + _well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + _wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + _devKit.Name("Test special characters") + "</name>" + Environment.NewLine +
                        "<indexType>measured depth</indexType>" + Environment.NewLine +
                        "<direction>increasing</direction>" + Environment.NewLine +
                        description + Environment.NewLine +
                        "<indexCurve>MD</indexCurve>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <unit>m</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"AAA\">" + Environment.NewLine +
                        "  <mnemonic>AAA</mnemonic>" + Environment.NewLine +
                        "  <unit>unitless</unit>" + Environment.NewLine +
                        "  <typeLogData>string</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"BBB\">" + Environment.NewLine +
                        "  <mnemonic>BBB</mnemonic>" + Environment.NewLine +
                        "  <unit>s</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logData>" + Environment.NewLine +
                        "   <mnemonicList>MD,AAA,BBB</mnemonicList>" + Environment.NewLine +
                        "   <unitList>m,unitless,s</unitList>" + Environment.NewLine +
                        row + Environment.NewLine +
                        "</logData>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null); 
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            var uidLog = result.SuppMsgOut;

            // Query log
            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsTrue(results.Any());

            var returnLog = results.First();
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(@"Header \", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(@"Data \", channelData[1].Trim());
        }

        [TestMethod, Description("As comma is a delimiter, this test is served as a reminder of the problem and will need to be updated to the decided response of the server.")]
        [Ignore]
        public void Log141DataAdapter_AddToStore_Can_Add_Log_With_Special_Character_Comma()
        {
            // Add well
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add log          
            var description = "<description>Test special character , (comma) </description>";
            var row = "<data>5000.0, comma ,, 5.0</data>";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + _wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + _well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + _wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + _devKit.Name("Test special characters") + "</name>" + Environment.NewLine +
                        "<indexType>measured depth</indexType>" + Environment.NewLine +
                        "<direction>increasing</direction>" + Environment.NewLine +
                        description + Environment.NewLine +
                        "<indexCurve>MD</indexCurve>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <unit>m</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"AAA\">" + Environment.NewLine +
                        "  <mnemonic>AAA</mnemonic>" + Environment.NewLine +
                        "  <unit>unitless</unit>" + Environment.NewLine +
                        "  <typeLogData>string</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"BBB\">" + Environment.NewLine +
                        "  <mnemonic>BBB</mnemonic>" + Environment.NewLine +
                        "  <unit>s</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logData>" + Environment.NewLine +
                        "   <mnemonicList>MD,AAA,BBB</mnemonicList>" + Environment.NewLine +
                        "   <unitList>m,unitless,s</unitList>" + Environment.NewLine +
                        row + Environment.NewLine +
                        "</logData>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            var uidLog = result.SuppMsgOut;

            // Query log
            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsTrue(results.Any());

            var returnLog = results.First();
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);
            Assert.AreEqual(3, returnLog.LogData[0].Data[0].Split(',').Length);
        }

        [TestMethod, Description("To test adding log data string channel with JSON special character \f (form feed).")]
        public void Log141DataAdapter_AddToStore_Can_Add_Log_With_Special_Character_FormFeed()
        {
            // Add well
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add log          
            var description = @"<description>Header \f </description>";
            var row = @"<data>5000.0, Data \f , 5.0</data>";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + _wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + _well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + _wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + _devKit.Name("Test special characters") + "</name>" + Environment.NewLine +
                        "<indexType>measured depth</indexType>" + Environment.NewLine +
                        "<direction>increasing</direction>" + Environment.NewLine +
                        description + Environment.NewLine +
                        "<indexCurve>MD</indexCurve>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <unit>m</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"AAA\">" + Environment.NewLine +
                        "  <mnemonic>AAA</mnemonic>" + Environment.NewLine +
                        "  <unit>unitless</unit>" + Environment.NewLine +
                        "  <typeLogData>string</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"BBB\">" + Environment.NewLine +
                        "  <mnemonic>BBB</mnemonic>" + Environment.NewLine +
                        "  <unit>s</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logData>" + Environment.NewLine +
                        "   <mnemonicList>MD,AAA,BBB</mnemonicList>" + Environment.NewLine +
                        "   <unitList>m,unitless,s</unitList>" + Environment.NewLine +
                        row + Environment.NewLine +
                        "</logData>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            var uidLog = result.SuppMsgOut;

            // Query log
            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All); 
            Assert.IsTrue(results.Any());

            var returnLog = results.First();
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(@"Header \f", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(@"Data \f", channelData[1].Trim());
        }

        [TestMethod, Description("To test adding log data string channel with JSON special character \" (backslash double-quote).")]
        public void Log141DataAdapter_AddToStore_Can_Add_Log_With_Special_Character_Backslash_Double_Quote()
        {
            // Add well
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add log          
            var description = @"<description>Header \""  </description>";
            var row = @"<data>5000.0, Data \"" , 5.0</data>";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + _wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + _well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + _wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + _devKit.Name("Test special characters") + "</name>" + Environment.NewLine +
                        "<indexType>measured depth</indexType>" + Environment.NewLine +
                        "<direction>increasing</direction>" + Environment.NewLine +
                        description + Environment.NewLine +
                        "<indexCurve>MD</indexCurve>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <unit>m</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"AAA\">" + Environment.NewLine +
                        "  <mnemonic>AAA</mnemonic>" + Environment.NewLine +
                        "  <unit>unitless</unit>" + Environment.NewLine +
                        "  <typeLogData>string</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"BBB\">" + Environment.NewLine +
                        "  <mnemonic>BBB</mnemonic>" + Environment.NewLine +
                        "  <unit>s</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logData>" + Environment.NewLine +
                        "   <mnemonicList>MD,AAA,BBB</mnemonicList>" + Environment.NewLine +
                        "   <unitList>m,unitless,s</unitList>" + Environment.NewLine +
                        row + Environment.NewLine +
                        "</logData>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);              
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            var uidLog = result.SuppMsgOut;

            // Query log
            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsTrue(results.Any());

            var returnLog = results.First();
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(@"Header \""", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(@"Data \""", channelData[1].Trim());
        }

        [TestMethod, Description("To test adding log data string channel with JSON special character \b (backspace).")]
        public void Log141DataAdapter_AddToStore_Can_Add_Log_With_Special_Character_Backspace()
        {
            // Add well
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            _log.UidWell = _wellbore.UidWell;
            _log.UidWellbore = uidWellbore;
            _log.Name = _devKit.Name("Test special characters");
            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            // Add log          
            var description = @"<description>Header \b  </description>";
            var row = @"<data>5000.0, Data \b , 5.0</data>";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + _wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + _well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + _wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + _devKit.Name("Test special characters") + "</name>" + Environment.NewLine +
                        "<indexType>measured depth</indexType>" + Environment.NewLine +
                        "<direction>increasing</direction>" + Environment.NewLine +
                        description + Environment.NewLine +
                        "<indexCurve>MD</indexCurve>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <unit>m</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"AAA\">" + Environment.NewLine +
                        "  <mnemonic>AAA</mnemonic>" + Environment.NewLine +
                        "  <unit>unitless</unit>" + Environment.NewLine +
                        "  <typeLogData>string</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"BBB\">" + Environment.NewLine +
                        "  <mnemonic>BBB</mnemonic>" + Environment.NewLine +
                        "  <unit>s</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logData>" + Environment.NewLine +
                        "   <mnemonicList>MD,AAA,BBB</mnemonicList>" + Environment.NewLine +
                        "   <unitList>m,unitless,s</unitList>" + Environment.NewLine +
                        row + Environment.NewLine +
                        "</logData>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);    
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);
            var uidLog = result.SuppMsgOut;

            // Query log
            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All); 
            Assert.IsTrue(results.Any());

            var returnLog = results.First();
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(@"Header \b", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(@"Data \b", channelData[1].Trim());
        }

        [TestMethod, Description("To test adding log data string channel with JSON special character \\ (double backslash).")]
        public void Log141DataAdapter_AddToStore_Can_Add_Log_With_Special_Character_Double_Backslash()
        {
            // Add well
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add log          
            var description = @"<description>Header \\ </description>";
            var row = @"Data \\";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + _wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + _well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + _wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + _devKit.Name("Test special characters") + "</name>" + Environment.NewLine +
                        "<indexType>measured depth</indexType>" + Environment.NewLine +
                        "<direction>increasing</direction>" + Environment.NewLine +
                        description + Environment.NewLine +
                        "<indexCurve>MD</indexCurve>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <unit>m</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"AAA\">" + Environment.NewLine +
                        "  <mnemonic>AAA</mnemonic>" + Environment.NewLine +
                        "  <unit>unitless</unit>" + Environment.NewLine +
                        "  <typeLogData>string</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"BBB\">" + Environment.NewLine +
                        "  <mnemonic>BBB</mnemonic>" + Environment.NewLine +
                        "  <unit>s</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logData>" + Environment.NewLine +
                        "   <mnemonicList>MD,AAA,BBB</mnemonicList>" + Environment.NewLine +
                        "   <unitList>m,unitless,s</unitList>" + Environment.NewLine +
                        "<data>5000.0," + row + ", 5.0</data>" + Environment.NewLine +
                        "</logData>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);
            var uidLog = result.SuppMsgOut;

            // Query log
            var query = CreateLog(uidLog, null, _wellbore.UidWell, null, uidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsTrue(results.Any());

            var returnLog = results.First();
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(@"Header \\", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(@"Data \\", channelData[1].Trim());                       
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Rollback_When_Adding_Invalid_Data()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                Uid = _devKit.Uid(),
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01"),
                LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() })
            };

            var logData = log.LogData.First();
            logData.Data.Add("997,13.1,");
            logData.Data.Add("998,14.1,");
            logData.Data.Add("999,15.1,");
            logData.Data.Add("1000,16.1,");
            logData.Data.Add("1001,17.1,");
            logData.Data.Add("1002,,21.2");
            logData.Data.Add("1002,,22.2");
            logData.Data.Add("1003,,23.2");
            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.NodesWithSameIndex, response.Result);

            var query = new Log
            {
                Uid = log.Uid,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Can_Add_With_Null_Indicator()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(null, _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.StartIndex = new GenericMeasure(13, "ft");
            log.EndIndex = new GenericMeasure(17, "ft");
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            log.NullValue = "-999.25";

            var logData = log.LogData.First();
            logData.Data.Add("1700.0,17.1,-999.25");
            logData.Data.Add("1800.0,18.1,-999.25");
            logData.Data.Add("1900.0,19.1,-999.25");
            logData.Data.Add("2000.0,20.1,-999.25");
            logData.Data.Add("2100.0,21.1,-999.25");
            logData.Data.Add("2200.0,22.1,-999.25");

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Query
            var query = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(6, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;
            Assert.AreEqual(2, results[0].LogData[0].MnemonicList.Split(',').Length);
            double index = 17;
            foreach (string row in resultLogData)
            {
                string[] columns = row.Split(',');
                Assert.AreEqual(2, columns.Length);

                double outIndex = double.Parse(columns[0]);
                Assert.AreEqual(index * 100, outIndex);

                double outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                index++;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_With_Null_Indicator_And_An_Empty_Channel_Of_Blanks()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(null, _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            log.NullValue = "-999.25";

            var logData = log.LogData.First();
            logData.Data.Add("1700.0,17.1,");
            logData.Data.Add("1800.0,18.1,");
            logData.Data.Add("1900.0,19.1,");
            logData.Data.Add("2000.0,20.1,");
            logData.Data.Add("2100.0,21.1,");
            logData.Data.Add("2200.0,22.1,");

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Query
            var query = CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(6, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;
            Assert.AreEqual(2, results[0].LogData[0].MnemonicList.Split(',').Length);

            Assert.IsTrue(resultLogData[0].Equals("1700,17.1"));
            Assert.IsTrue(resultLogData[1].Equals("1800,18.1"));
            Assert.IsTrue(resultLogData[2].Equals("1900,19.1"));
            Assert.IsTrue(resultLogData[3].Equals("2000,20.1"));
            Assert.IsTrue(resultLogData[4].Equals("2100,21.1"));
            Assert.IsTrue(resultLogData[5].Equals("2200,22.1"));
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Structural_Ranges_Ignored()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(
                null,
                _devKit.Name("Log can be added with depth data"),
                _wellbore.UidWell,
                _well.Name,
                response.SuppMsgOut,
                _wellbore.Name);

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            log.StartIndex = new GenericMeasure {Uom = "m", Value = 1.0};
            log.EndIndex = new GenericMeasure { Uom = "m", Value = 10.0 };

            foreach (var curve in log.LogCurveInfo)
            {
                curve.MinIndex = log.StartIndex;
                curve.MaxIndex = log.EndIndex;
            }

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var query = new Log
            {
                Uid = response.SuppMsgOut,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.AreEqual(1, results.Count);

            var result = results.First();
            Assert.IsNotNull(result);

            Assert.IsNull(result.StartIndex);
            Assert.IsNull(result.EndIndex);

            Assert.AreEqual(log.LogCurveInfo.Count, result.LogCurveInfo.Count);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.IsNull(curve.MinIndex);
                Assert.IsNull(curve.MaxIndex);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_With_No_LogCurveInfos()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(
                null,
                _devKit.Name("Log can be added with depth data"),
                _wellbore.UidWell,
                _well.Name,
                response.SuppMsgOut,
                _wellbore.Name);

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            log.LogCurveInfo.Clear();
            log.LogData.Clear();

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_With_Custom_Data_Delimiter()
        {
            var delimiter = "|";
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(
                null,
                _devKit.Name("Log can be added with depth data"),
                _wellbore.UidWell,
                _well.Name,
                response.SuppMsgOut,
                _wellbore.Name);

            // Set data delimiter to other charactrer than ","
            log.DataDelimiter = delimiter;

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10, hasEmptyChannel:false);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

            // Assert data delimiter
            Assert.AreEqual(delimiter, result.DataDelimiter);

            var data = result.LogData.FirstOrDefault()?.Data;
            Assert.IsNotNull(data);

            var channelCount = log.LogCurveInfo.Count;

            // Assert data delimiter in log data
            foreach (var row in data)
            {
                var points = ChannelDataReader.Split(row, delimiter);
                Assert.AreEqual(channelCount, points.Length);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Error_1051_Incorrect_Row_Value_Count()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = CreateLog(
                null,
                _devKit.Name("Log can be added with depth data"),
                _wellbore.UidWell,
                _well.Name,
                response.SuppMsgOut,
                _wellbore.Name);


            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10, hasEmptyChannel: false);

            var logData = log.LogData.FirstOrDefault();
            logData?.Data?.Add("20,20.1,20.2,20.3,20.4");

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.ErrorRowDataCount, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Test_Add_With_Blank_Unit_In_LogCurveInfo_And_UnitList()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            // Set the 3rd LogCurveInfo/unit to null and 3rd UnitList entry to an empty string
            _log.LogCurveInfo[2].Unit = null;
            var logData = _log.LogData.First();
            logData.UnitList = "m,m/h,";

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        #region Helper Methods

        private Log CreateLog(string uid, string name, Well well, Wellbore wellbore)
        {
            return CreateLog(uid, name, well.Uid, well.Name, wellbore.Uid, wellbore.Name);
        }

        private Log CreateLog(string uid, string name, string uidWell, string nameWell, string uidWellbore, string nameWellbore)
        {
            return new Log()
            {
                Uid = uid,
                Name = name,
                UidWell = uidWell,
                NameWell = nameWell,
                UidWellbore = uidWellbore,
                NameWellbore = nameWellbore,
            };
        }

        private string CreateXmlLog(string logName, string uidWell, string nameWell, string uidWellbore, string nameWellbore, string startIndexUom, string endIndexUom)
        {
            string xmlIn =
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" +
                "    <log uidWell=\"" + uidWell + "\" uidWellbore=\"" + uidWellbore + "\">" +
                "        <nameWell>" + _well.Name + "</nameWell>" +
                "        <nameWellbore>" + _wellbore.Name + "</nameWellbore>" +
                "        <name>" + logName + "</name>" +
                "        <serviceCompany>Service Company Name</serviceCompany>" +
                "        <indexType>measured depth</indexType>" +
                (string.IsNullOrEmpty(startIndexUom) ? "<startIndex>499</startIndex>" : "<startIndex uom =\"" + startIndexUom + "\">499</startIndex>") +
                (string.IsNullOrEmpty(endIndexUom) ? "<endIndex>509.01</endIndex>" : "<endIndex uom =\"" + endIndexUom + "\">509.01</endIndex>") +
                "        <stepIncrement uom =\"m\">0</stepIncrement>" +
                "        <indexCurve>Mdepth</indexCurve>" +
                "        <logCurveInfo uid=\"lci-1\">" +
                "            <mnemonic>Mdepth</mnemonic>" +
                "            <unit>m</unit>" +
                "            <mnemAlias>md</mnemAlias>" +
                "            <nullValue>-999.25</nullValue>" +
                "            <minIndex uom=\"m\">499</minIndex>" +
                "            <maxIndex uom=\"m\">509.01</maxIndex>" +
                "            <typeLogData>double</typeLogData>" +
                "        </logCurveInfo>" +
                "        <logCurveInfo uid=\"lci-2\">" +
                "            <mnemonic>Vdepth</mnemonic>" +
                "            <unit>m</unit>" +
                "            <mnemAlias>tvd</mnemAlias>" +
                "            <nullValue>-999.25</nullValue>" +
                "            <minIndex uom=\"m\">499</minIndex>" +
                "            <maxIndex uom=\"m\">509.01</maxIndex>" +
                "            <typeLogData>double</typeLogData >" +
                "        </logCurveInfo >" +
                "    </log>" +
                "</logs>";

            return xmlIn;
        }

        private void AddParents()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        #endregion
    }
}
