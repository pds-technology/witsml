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
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    public class Log141DataAdapterGetTests
    {
        private DevKit141Aspect DevKit;
        private Well _well;
        private Wellbore _wellbore;
        private Log _log;
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

            _log = new Log()
            {
                NameWell = _well.Name,
                NameWellbore = _wellbore.Name,
                Name = DevKit.Name("Log 01")
            };
        }

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

        [TestMethod]
        [Ignore, Description("Waiting for requestLatestValues to be implemented in ChannelDataReader")]
        public void Log141DataAdapter_GetFromStore_Decreasing_RequestLatestValue_OptionsIn()
        {
            var response = DevKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);

            _log.UidWell = _wellbore.UidWell;
            _log.UidWellbore = response.SuppMsgOut;

            var row = 10;
            DevKit.InitHeader(_log, LogIndexType.measureddepth, false);

            var startIndex = new GenericMeasure { Uom = "m", Value = 100 };
            _log.StartIndex = startIndex;
            DevKit.InitDataMany(_log, DevKit.Mnemonics(_log), DevKit.Units(_log), row, 1, true, false, false);
            var logData = _log.LogData.First();
            logData.Data.Clear();
            logData.Data.Add("100,1,");
            logData.Data.Add("99,2,");
            logData.Data.Add("98,3,");
            logData.Data.Add("97,4,");

            // Our return data set should be all of these values.
            logData.Data.Add("96,5,"); // The latest 1 value for the 2nd channel
            logData.Data.Add("95,,6");      // should not be returned
            logData.Data.Add("94,,7");      // should not be returned
            logData.Data.Add("93,,8");      // should not be returned
            logData.Data.Add("92,,9");      // should not be returned
            logData.Data.Add("91,,10"); // The latest 1 value for the last channel

            // Add a decreasing log with several values
            response = DevKit.Add<LogList, Log>(_log);

            // Assert that the log was added successfully
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);


            var uidLog = response.SuppMsgOut;
            logData = new LogData { MnemonicList = DevKit.Mnemonics(_log) };
            var query = new Log
            {
                Uid = uidLog,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore,
            };

            var result = DevKit.Query<LogList, Log>(
                query,
                ObjectTypes.Log, null, OptionsIn.ReturnElements.All + ';' +
                OptionsIn.RequestLatestValues.Eq(1)); // Request the latest 1 value (for each channel)


            // Verify that a Log was returned with the results
            Assert.IsNotNull(result, "No results returned from Log query");
            Assert.IsNotNull(result.First(), "No Logs returned in results from Log query");

            // Verify that a LogData element was returned with the results.
            var queryLogData = result.First().LogData;
            Assert.IsTrue(queryLogData != null && queryLogData.First() != null, "No LogData returned in results from Log query");

            // Verify that data rows were returned with the LogData
            var queryData = queryLogData.First().Data;
            Assert.IsTrue(queryData != null && queryData.Count > 0, "No data returned in results from Log query");

            // Verify that only the rows of data (referenced above) for index values 96 and 91 were returned 
            //... in order to get the latest 1 value for each channel.
            Assert.AreEqual(2, queryData.Count, "Only rows for index values 96 and 91 should be returned.");
            Assert.AreEqual("96", queryData[0].Split(',')[0], "The first data row should be for index value 96");
            Assert.AreEqual("91", queryData[1].Split(',')[0], "The second data row should be for index value 91");
        }

        [TestMethod]
        [Ignore, Description("Waiting for requestLatestValues to be implemented in ChannelDataReader")]
        public void Log141DataAdapter_GetFromStore_Increasing_RequestLatestValue_OptionsIn()
        {
            var response = DevKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);

            _log.UidWell = _wellbore.UidWell;
            _log.UidWellbore = response.SuppMsgOut;

            var row = 10;
            DevKit.InitHeader(_log, LogIndexType.measureddepth, increasing: true);

            // Add a 4th Log Curve
            _log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("GR2", "gAPI"));

            var startIndex = new GenericMeasure { Uom = "m", Value = 100 };
            _log.StartIndex = startIndex;
            DevKit.InitDataMany(_log, DevKit.Mnemonics(_log), DevKit.Units(_log), row, 1, true, false, increasing: true);

            // Reset for custom LogData
            var logData = _log.LogData.First();
            logData.Data.Clear();

            logData.Data.Add("1,1,,");
            logData.Data.Add("2,2,,");
            logData.Data.Add("3,3,,");
            logData.Data.Add("4,4,,"); // returned
            logData.Data.Add("5,5,,"); // returned

            logData.Data.Add("6,,1,");
            logData.Data.Add("7,,2,");
            logData.Data.Add("8,,3,");
            logData.Data.Add("9,,4,"); // returned
            logData.Data.Add("10,,5,"); // returned

            logData.Data.Add("11,,,1");
            logData.Data.Add("12,,,2");
            logData.Data.Add("13,,,3");
            logData.Data.Add("14,,,4"); // returned
            logData.Data.Add("15,,,5"); // returned

            // Add a decreasing log with several values
            response = DevKit.Add<LogList, Log>(_log);

            // Assert that the log was added successfully
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);


            var uidLog = response.SuppMsgOut;
            logData = new LogData { MnemonicList = DevKit.Mnemonics(_log) };
            var query = new Log
            {
                Uid = uidLog,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore,
            };

            var result = DevKit.Query<LogList, Log>(
                query,
                ObjectTypes.Log, null, OptionsIn.ReturnElements.All + ';' +
                OptionsIn.RequestLatestValues.Eq(2)); // Request the latest 2 values (for each channel)


            // Verify that a Log was returned with the results
            Assert.IsNotNull(result, "No results returned from Log query");
            Assert.IsNotNull(result.First(), "No Logs returned in results from Log query");

            // Verify that a LogData element was returned with the results.
            var queryLogData = result.First().LogData;
            Assert.IsTrue(queryLogData != null && queryLogData.First() != null, "No LogData returned in results from Log query");

            // Verify that data rows were returned with the LogData
            var queryData = queryLogData.First().Data;
            Assert.IsTrue(queryData != null && queryData.Count > 0, "No data returned in results from Log query");

            // Verify that only the rows of data (referenced above) for index values 96 and 91 were returned 
            //... in order to get the latest 1 value for each channel.
            Assert.AreEqual(6, queryData.Count, "Only rows for index values 4,5,9,10,14 and 15 should be returned.");
            Assert.AreEqual("4", queryData[0].Split(',')[0], "The first data row should be for index value 4");
            Assert.AreEqual("15", queryData[5].Split(',')[0], "The last data row should be for index value 15");
        }

        [TestMethod]
        [Ignore, Description("Waiting for requestLatestValues to be implemented in ChannelDataReader")]
        public void Log141DataAdapter_GetFromStore_Increasing_Time_RequestLatestValue_OptionsIn()
        {
            var response = DevKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);

            _log.UidWell = _wellbore.UidWell;
            _log.UidWellbore = response.SuppMsgOut;

            //var row = 10;
            DevKit.InitHeader(_log, LogIndexType.datetime, increasing: true);
            DevKit.InitDataMany(_log, DevKit.Mnemonics(_log), DevKit.Units(_log), numRows: 1,factor: 1, isDepthLog: false, hasEmptyChannel: false, increasing: true);

            // Reset for custom LogData
            var logData = _log.LogData.First();
            logData.Data.Clear();

            logData.Data.Add("2012-07-26T15:17:20.0000000+00:00,1,0");
            logData.Data.Add("2012-07-26T15:17:30.0000000+00:00,2,1");
            logData.Data.Add("2012-07-26T15:17:40.0000000+00:00,,2");
            logData.Data.Add("2012-07-26T15:17:50.0000000+00:00,,3");

            // Add a decreasing log with several values
            response = DevKit.Add<LogList, Log>(_log);

            // Assert that the log was added successfully
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = new Log
            {
                Uid = uidLog,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore,
            };

            var result = DevKit.Query<LogList, Log>(
                query,
                ObjectTypes.Log, null, OptionsIn.ReturnElements.All + ';' +
                OptionsIn.RequestLatestValues.Eq(1)); // Request the latest 1 values (for each channel)



            // Verify that a Log was returned with the results
            Assert.IsNotNull(result, "No results returned from Log query");
            Assert.IsNotNull(result.First(), "No Logs returned in results from Log query");

            var logInfos = result.First().LogCurveInfo;
            Assert.AreEqual(_log.LogCurveInfo.Count, logInfos.Count, "There should be 3 LogCurveInfos");

            // Verify that a LogData element was returned with the results.
            var queryLogData = result.First().LogData;
            Assert.IsTrue(queryLogData != null && queryLogData.First() != null, "No LogData returned in results from Log query");

            // Verify that data rows were returned with the LogData
            var queryData = queryLogData.First().Data;
            Assert.IsTrue(queryData != null && queryData.Count > 0, "No data returned in results from Log query");

            // Verify that only the rows of data (referenced above) for index values 96 and 91 were returned 
            //... in order to get the latest 1 value for each channel.
            Assert.AreEqual(2, queryData.Count, "Only rows for index values 4,5,9,10,14 and 15 should be returned.");
            Assert.AreEqual("2012-07-26T15:17:30.0000000+00:00", queryData[0].Split(',')[0], "The first data row should be for index value 2012-07-26T15:17:30.0000000+00:00");
            Assert.AreEqual("2012-07-26T15:17:50.0000000+00:00", queryData[1].Split(',')[0], "The last data row should be for index value 2012-07-26T15:17:50.0000000+00:00");

            // Validate the Min and Max of each LogCurveInfo #1
            Assert.AreEqual(logInfos[0].MinDateTimeIndex.Value, new Energistics.DataAccess.Timestamp(DateTimeOffset.Parse(queryData[0].Split(',')[0])));
            Assert.AreEqual(logInfos[0].MaxDateTimeIndex.Value, new Energistics.DataAccess.Timestamp(DateTimeOffset.Parse(queryData[1].Split(',')[0])));

            // Validate the Min and Max of each LogCurveInfo #2
            Assert.AreEqual(logInfos[1].MinDateTimeIndex.Value, new Energistics.DataAccess.Timestamp(DateTimeOffset.Parse(queryData[0].Split(',')[0])));
            Assert.AreEqual(logInfos[1].MaxDateTimeIndex.Value, new Energistics.DataAccess.Timestamp(DateTimeOffset.Parse(queryData[0].Split(',')[0])));

            // Validate the Min and Max of each LogCurveInfo #3
            Assert.AreEqual(logInfos[2].MinDateTimeIndex.Value, new Energistics.DataAccess.Timestamp(DateTimeOffset.Parse(queryData[1].Split(',')[0])));
            Assert.AreEqual(logInfos[2].MaxDateTimeIndex.Value, new Energistics.DataAccess.Timestamp(DateTimeOffset.Parse(queryData[1].Split(',')[0])));
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Error_402_MaxReturnNodes_Not_Greater_Than_Zero()
        {
            var result = DevKit.Get<LogList, Log>(DevKit.List(_log), ObjectTypes.Log, optionsIn: "maxReturnNodes=0");

            Assert.AreEqual((short)ErrorCodes.InvalidMaxReturnNodes, result.Result);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Error_438_Recurring_Elements_Have_Inconsistent_Selection()
        {
            _log.LogCurveInfo = new List<LogCurveInfo>();
            _log.LogCurveInfo.Add(new LogCurveInfo() { Uid = "MD", DataSource = "abc" });
            _log.LogCurveInfo.Add(new LogCurveInfo() { Uid = "GR", CurveDescription = "efg" });

            var list = DevKit.New<LogList>(x => x.Log = DevKit.List(_log));
            var queryIn = WitsmlParser.ToXml(list);
            var result = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");

            Assert.AreEqual((short)ErrorCodes.RecurringItemsInconsistentSelection, result.Result);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Error_439_Recurring_Elements_Has_Empty_Selection_Value()
        {
            _log.LogCurveInfo = new List<LogCurveInfo>();
            _log.LogCurveInfo.Add(new LogCurveInfo() { Uid = "MD", Mnemonic = new ShortNameStruct("MD") });
            _log.LogCurveInfo.Add(new LogCurveInfo() { Uid = string.Empty, Mnemonic = new ShortNameStruct("ROP") });

            var result = DevKit.Get<LogList, Log>(DevKit.List(_log), ObjectTypes.Log, null, optionsIn: OptionsIn.ReturnElements.Requested);

            Assert.AreEqual((short)ErrorCodes.RecurringItemsEmptySelection, result.Result);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Error_475_No_Subset_When_Getting_Growing_Object()
        {
            var response = DevKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add first log
            _log.UidWell = _wellbore.UidWell;
            _log.UidWellbore = response.SuppMsgOut;
            DevKit.InitHeader(_log, LogIndexType.measureddepth);
            response = DevKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add second log
            var log2 = DevKit.CreateLog(null, DevKit.Name("Log 02"), _log.UidWell, _log.NameWell, _log.UidWellbore, _log.NameWellbore);
            DevKit.InitHeader(log2, LogIndexType.measureddepth);

            response = DevKit.Add<LogList, Log>(log2);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Query
            var query = DevKit.CreateLog(null, null, log2.UidWell, null, log2.UidWellbore, null);

            var result = DevKit.Get<LogList, Log>(DevKit.List(query), ObjectTypes.Log, null, OptionsIn.ReturnElements.DataOnly);
            Assert.AreEqual((short)ErrorCodes.MissingSubsetOfGrowingDataObject, result.Result);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_ReturnElements_DataOnly_Supports_Multiple_Queries()
        {
            var response = DevKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add first log
            _log.UidWell = _wellbore.UidWell;
            _log.UidWellbore = response.SuppMsgOut;
            DevKit.InitHeader(_log, LogIndexType.measureddepth);
            DevKit.InitDataMany(_log, DevKit.Mnemonics(_log), DevKit.Units(_log), 3, hasEmptyChannel:false);

            response = DevKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog1 = response.SuppMsgOut;

            // Add second log
            var log2 = DevKit.CreateLog(null, DevKit.Name("Log 02"), _log.UidWell, _log.NameWell, _log.UidWellbore, _log.NameWellbore);
            DevKit.InitHeader(log2, LogIndexType.datetime);
            log2.LogCurveInfo.Clear();
            log2.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("TIME", "s"));
            log2.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("AAA", "m/h"));
            log2.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("BBB", "gAPI"));
            log2.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("CCC", "gAPI"));

            var logData = log2.LogData.First();
            logData.Data.Clear();
            logData.MnemonicList = "TIME,AAA,BBB,CCC";
            logData.UnitList = "s,m/h,gAPI,gAPI";
            logData.Data.Add("2012-07-26T15:17:20.0000000+00:00,1,0,1");
            logData.Data.Add("2012-07-26T15:17:30.0000000+00:00,2,1,2");
            logData.Data.Add("2012-07-26T15:17:40.0000000+00:00,3,2,3");
            logData.Data.Add("2012-07-26T15:17:50.0000000+00:00,4,3,4");

            response = DevKit.Add<LogList, Log>(log2);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog2 = response.SuppMsgOut;

            // Query
            var query1 = DevKit.CreateLog(uidLog1, null, log2.UidWell, null, log2.UidWellbore, null);
            var query2 = DevKit.CreateLog(uidLog2, null, log2.UidWell, null, log2.UidWellbore, null);

            var result = DevKit.Get<LogList, Log>(DevKit.List(query1, query2), ObjectTypes.Log, null, OptionsIn.ReturnElements.DataOnly);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            Assert.AreEqual(2, logList.Log.Count);

            // Query result of Depth log
            var mnemonicList1 = logList.Log[0].LogData[0].MnemonicList.Split(',');
            Assert.AreEqual(3, mnemonicList1.Length);
            Assert.IsTrue(!mnemonicList1.Except(new List<string>() { "MD", "ROP", "GR" }).Any());
            Assert.AreEqual(3, logList.Log[0].LogData[0].Data.Count);

            // Query result of Time log
            var mnemonicList2 = logList.Log[1].LogData[0].MnemonicList.Split(',');
            Assert.AreEqual(4, mnemonicList2.Length);
            Assert.IsFalse(mnemonicList2.Except(new List<string>() { "TIME", "AAA", "BBB", "CCC" }).Any());
            Assert.AreEqual(4, logList.Log[1].LogData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_ReturnElements_Requested_Supports_Multiple_Queries()
        {
            var response = DevKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add first log
            _log.UidWell = _wellbore.UidWell;
            _log.UidWellbore = response.SuppMsgOut;
            DevKit.InitHeader(_log, LogIndexType.measureddepth);
            DevKit.InitDataMany(_log, DevKit.Mnemonics(_log), DevKit.Units(_log), 3, hasEmptyChannel: false);

            response = DevKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog1 = response.SuppMsgOut;

            // Add second log
            var log2 = DevKit.CreateLog(null, DevKit.Name("Log 02"), _log.UidWell, _log.NameWell, _log.UidWellbore, _log.NameWellbore);
            DevKit.InitHeader(log2, LogIndexType.datetime);
            log2.LogCurveInfo.Clear();
            log2.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("TIME", "s"));
            log2.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("AAA", "m/h"));
            log2.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("BBB", "gAPI"));
            log2.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("CCC", "gAPI"));

            var logData = log2.LogData.First();
            logData.Data.Clear();
            logData.MnemonicList = "TIME,AAA,BBB,CCC";
            logData.UnitList = "s,m/h,gAPI,gAPI";
            logData.Data.Add("2012-07-26T15:17:20.0000000+00:00,1,0,1");
            logData.Data.Add("2012-07-26T15:17:30.0000000+00:00,2,1,2");
            logData.Data.Add("2012-07-26T15:17:40.0000000+00:00,3,2,3");
            logData.Data.Add("2012-07-26T15:17:50.0000000+00:00,4,3,4");

            response = DevKit.Add<LogList, Log>(log2);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog2 = response.SuppMsgOut;

            // Query
            var query1 = DevKit.CreateLog(uidLog1, null, log2.UidWell, null, log2.UidWellbore, null);
            query1.LogCurveInfo = new List<LogCurveInfo>();
            query1.LogCurveInfo.Add(new LogCurveInfo() { Uid = "MD", Mnemonic = new ShortNameStruct("MD") });
            query1.LogCurveInfo.Add(new LogCurveInfo() { Uid = "ROP", Mnemonic = new ShortNameStruct("ROP") });
            query1.LogCurveInfo.Add(new LogCurveInfo() { Uid = "GR", Mnemonic = new ShortNameStruct("GR") });
            query1.LogData = new List<LogData>() { new LogData() };
            query1.LogData.First().MnemonicList = "MD,ROP,GR";

            var query2 = DevKit.CreateLog(uidLog2, null, log2.UidWell, null, log2.UidWellbore, null);
            query2.LogCurveInfo = new List<LogCurveInfo>();
            query2.LogCurveInfo.Add(new LogCurveInfo() { Uid = "TIME", Mnemonic = new ShortNameStruct("TIME") });
            query2.LogCurveInfo.Add(new LogCurveInfo() { Uid = "AAA", Mnemonic = new ShortNameStruct("AAA") });
            query2.LogCurveInfo.Add(new LogCurveInfo() { Uid = "BBB", Mnemonic = new ShortNameStruct("BBB") });
            query2.LogCurveInfo.Add(new LogCurveInfo() { Uid = "CCC", Mnemonic = new ShortNameStruct("CCC") });
            query2.LogData = new List<LogData>() { new LogData() };
            query2.LogData.First().MnemonicList = "TIME,AAA,BBB,CCC";

            var list = DevKit.New<LogList>(x => x.Log = DevKit.List(query1, query2));
            var queryIn = WitsmlParser.ToXml(list);
            var result = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            Assert.AreEqual(2, logList.Log.Count);

            // Query result of Depth log
            var mnemonicList1 = logList.Log[0].LogData[0].MnemonicList.Split(',');
            Assert.AreEqual(3, mnemonicList1.Length);
            Assert.IsTrue(!mnemonicList1.Except(new List<string>() { "MD", "ROP", "GR" }).Any());
            Assert.AreEqual(3, logList.Log[0].LogData[0].Data.Count);

            // Query result of Time log
            var mnemonicList2 = logList.Log[1].LogData[0].MnemonicList.Split(',');
            Assert.AreEqual(4, mnemonicList2.Length);
            Assert.IsFalse(mnemonicList2.Except(new List<string>() { "TIME", "AAA", "BBB", "CCC" }).Any());
            Assert.AreEqual(4, logList.Log[1].LogData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Supports_NaN_In_Numeric_Fields()
        {
            // Add well
            var response = DevKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add log
            _log.UidWell = _wellbore.UidWell;
            _log.UidWellbore = uidWellbore;
            DevKit.InitHeader(_log, LogIndexType.measureddepth);
            DevKit.InitDataMany(_log, DevKit.Mnemonics(_log), DevKit.Units(_log), 3);
            _log.BhaRunNumber = 123;
            _log.LogCurveInfo[0].ClassIndex = 1;
            _log.LogCurveInfo[1].ClassIndex = 2;

            response = DevKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Query log
            var queryIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + uidLog + "\" uidWell=\"" + _wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +
                        "<bhaRunNumber>NaN</bhaRunNumber>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <classIndex>NaN</classIndex>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"ROP\">" + Environment.NewLine +
                        "  <mnemonic>ROP</mnemonic>" + Environment.NewLine +
                        "  <classIndex>NaN</classIndex>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);
        
            Assert.AreEqual((short)123, logList.Log.First().BhaRunNumber);
            Assert.AreEqual(2, logList.Log.First().LogCurveInfo.Count);
            Assert.AreEqual((short)1, logList.Log.First().LogCurveInfo[0].ClassIndex);
            Assert.AreEqual((short)2, logList.Log.First().LogCurveInfo[1].ClassIndex);
        }
    }
}
