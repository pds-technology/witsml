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
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data.Channels;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    public class Log141DataAdapterGetTests
    {
        private DevKit141Aspect DevKit;
        private Well _well;
        private Wellbore _wellbore;
        private DatabaseProvider _databaseProvider;

        [TestInitialize]
        public void TestSetUp()
        {
            _databaseProvider = new DatabaseProvider(new MongoDbClassMapper());

            DevKit = new DevKit141Aspect();

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _well = new Well { Name = DevKit.Name("Well 01"), TimeZone = DevKit.TimeZone };
            _wellbore = new Wellbore()
            {
                NameWell = _well.Name,
                Name = DevKit.Name("Wellbore 01")
            };
        }

        [Ignore]
        [TestMethod]
        public void Log_can_be_retrieved_with_all_data()
        {
            var response = DevKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            var row = 10;
            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), row);
            var columnCountBeforeSave = log.LogData.First().Data.First().Split(',').Length;
            response = DevKit.Add<LogList, Log>(log);

            // Test that a Log was Added successfully
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore,
            };
            var result = DevKit.Query<LogList, Log>(query, ObjectTypes.Log, null, OptionsIn.ReturnElements.DataOnly);
            var queriedLog = result.FirstOrDefault();

            // Test that Log was returned
            Assert.IsNotNull(queriedLog);

            var logData = queriedLog.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);

            var data = logData.Data;
            var firstRow = data.First().Split(',');
            var mnemonics = logData.MnemonicList.Split(',').ToList();

            // Test that all of the rows of data saved are returned.
            Assert.AreEqual(row, data.Count);

            // Test that the number of mnemonics matches the number of data values per row
            Assert.AreEqual(firstRow.Length, mnemonics.Count);

            // Update Test to verify that a column of LogData.Data with no values is NOT returned with the results.
            Assert.AreEqual(columnCountBeforeSave - 1, firstRow.Length);
        }

        [Ignore]
        [TestMethod]
        public void Log_column_with_one_value_returned()
        {
            var response = DevKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            var row = 10;
            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), row);

            // Replace the third data row with a value where there is none
            log.LogData.First().Data[2] = log.LogData.First().Data[2].Replace(",,", ",0,");
            var columnCountBeforeSave = log.LogData.First().Data.First().Split(',').Length;

            // Save the Log
            response = DevKit.Add<LogList, Log>(log);

            var uidLog = response.SuppMsgOut;
            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore,
            };
            var result = DevKit.Query<LogList, Log>(query, ObjectTypes.Log, null, OptionsIn.ReturnElements.DataOnly);
            var queriedLog = result.FirstOrDefault();

            // Test that Log was returned
            Assert.IsNotNull(queriedLog);

            var logData = queriedLog.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);

            var data = logData.Data;
            var firstRow = data.First().Split(',');

            // Update Test to verify that a column of LogData.Data with no values is NOT returned with the results.
            Assert.AreEqual(columnCountBeforeSave, firstRow.Length);
        }

        [TestMethod]
        public void Log_can_be_retrieved_with_increasing_log_data()
        {
            var response = DevKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            var row = 10;
            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), row);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;
            var logData = new LogData { MnemonicList = DevKit.Mnemonics(log) };
            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore,
                StartIndex = new GenericMeasure(2.0, "m"),
                EndIndex = new GenericMeasure(6.0, "m"),
                LogData = new List<LogData> { logData }
            };
            var result = DevKit.Query<LogList, Log>(query, ObjectTypes.Log, null, OptionsIn.ReturnElements.DataOnly);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Log_can_be_retrieved_with_decreasing_log_data()
        {
            var response = DevKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            var row = 10;
            DevKit.InitHeader(log, LogIndexType.measureddepth, false);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), row, 1, true, true, false);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;
            var logData = new LogData { MnemonicList = DevKit.Mnemonics(log) };
            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore,
                Direction = LogIndexDirection.decreasing,
                StartIndex = new GenericMeasure(-3.0, "m"),
                EndIndex = new GenericMeasure(-6.0, "m"),
                LogData = new List<LogData> { logData }
            };
            var result = DevKit.Query<LogList, Log>(query, ObjectTypes.Log, null, OptionsIn.ReturnElements.DataOnly);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Log_empty_elements_are_removed()
        {
            var response = DevKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth, false);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;
            
            var query = new Log()
            {
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore,
                Uid = uidLog
            };
            var returnLog = DevKit.Query<LogList, Log>(query, ObjectTypes.Log, null, OptionsIn.ReturnElements.All).FirstOrDefault();
            Assert.IsNotNull(returnLog);
            Assert.IsNotNull(returnLog.IndexType);
            Assert.AreEqual(log.IndexType, returnLog.IndexType);


            var queryIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + uidLog + "\" uidWell=\"" + log.UidWell + "\" uidWellbore=\"" + log.UidWellbore + "\">" + Environment.NewLine +
                        "<nameWell />" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";
            var result = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, null);
            var xmlOut = result.XMLout;
            Assert.IsNotNull(result);
            var context = new RequestContext(Functions.GetFromStore, ObjectTypes.Log, xmlOut, null, null);
            var parser = new WitsmlQueryParser(context);
            Assert.IsFalse(parser.HasElements("indexType"));
        }

        [TestMethod]
        public void Log_header_index_value_sorted_for_decreasing_log()
        {
            var response = DevKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            var row = 10;
            DevKit.InitHeader(log, LogIndexType.measureddepth, false);

            var startIndex = new GenericMeasure { Uom = "m", Value = 100 };
            log.StartIndex = startIndex;
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), row, 1, true, false, false);
            var logData = log.LogData.First();
            logData.Data.Clear();
            logData.Data.Add("100,1,");
            logData.Data.Add("99,2,");
            logData.Data.Add("98,3,");
            logData.Data.Add("97,4,");
            logData.Data.Add("96,5,");
            logData.Data.Add("95,,6");
            logData.Data.Add("94,,7");
            logData.Data.Add("93,,8");
            logData.Data.Add("92,,9");
            logData.Data.Add("91,,10");
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;
            logData = new LogData { MnemonicList = DevKit.Mnemonics(log) };
            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore,
                Direction = LogIndexDirection.decreasing,
                StartIndex = new GenericMeasure(98.0, "m"),
                EndIndex = new GenericMeasure(94.0, "m")
            };
            var result = DevKit.Query<LogList, Log>(query, ObjectTypes.Log, null, OptionsIn.ReturnElements.All);
            Assert.IsNotNull(result);
            var logAdded = result.First();
            var indexCurve = logAdded.LogCurveInfo.First();
            Assert.AreEqual(query.EndIndex.Value, indexCurve.MinIndex.Value);
            Assert.AreEqual(query.StartIndex.Value, indexCurve.MaxIndex.Value);
            Assert.AreEqual(query.StartIndex.Value, logAdded.StartIndex.Value);
            Assert.AreEqual(query.EndIndex.Value, logAdded.EndIndex.Value);
            var firstChannel = logAdded.LogCurveInfo[1];
            Assert.AreEqual(96, firstChannel.MinIndex.Value);
            Assert.AreEqual(query.StartIndex.Value, firstChannel.MaxIndex.Value);
            var secondChannel = logAdded.LogCurveInfo[2];
            Assert.AreEqual(query.EndIndex.Value, secondChannel.MinIndex.Value);
            Assert.AreEqual(95, secondChannel.MaxIndex.Value);
        }
    }
}
