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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    [TestClass]
    public partial class Log141DataAdapterGetTests : Log141TestBase
    {
        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Be_Retrieved_With_All_Data()
        {
            // Initialize the Log
            var row = 10;
            int columnCountBeforeSave = 0;
            var response = AddLogWithAction( row, () =>
            {
                columnCountBeforeSave = Log.LogData.First().Data.First().Split(',').Length;
            });

            // Test that a Log was Added successfully
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            // Create a query log
            var query = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
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
        public void Log141DataAdapter_GetFromStore_Column_With_One_Value_Returned()
        {
            var row = 10;
            int columnCountBeforeSave = 0;
            AddLogWithAction(row, () =>
            {
                Log.LogData.First().Data[2] = Log.LogData.First().Data[2].Replace(",,", ",0,");
                columnCountBeforeSave = Log.LogData.First().Data.First().Split(',').Length;
            });

            // Create a query log
            var query = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
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
        public void Log141DataAdapter_GetFromStore_Can_Be_Retrieved_With_IncreasingLog_Data()
        {
            var row = 10;
            var response = AddLogWithAction(row);
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            var logData = new LogData {MnemonicList = DevKit.Mnemonics(Log)};
            var query = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                StartIndex = new GenericMeasure(2.0, "m"),
                EndIndex = new GenericMeasure(6.0, "m"),
                LogData = new List<LogData> {logData}
            };
            var result = DevKit.Query<LogList, Log>(query, ObjectTypes.Log, null, OptionsIn.ReturnElements.DataOnly);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Be_Retrieved_With_DecreasingLog_Data()
        {
            var row = 10;
            var response = AddLogWithAction(row, null, 1, true, true, false);
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            //var uidLog = response.SuppMsgOut;
            var logData = new LogData {MnemonicList = DevKit.Mnemonics(Log)};
            var query = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                Direction = LogIndexDirection.decreasing,
                StartIndex = new GenericMeasure(-3.0, "m"),
                EndIndex = new GenericMeasure(-6.0, "m"),
                LogData = new List<LogData> {logData}
            };
            var result = DevKit.Query<LogList, Log>(query, ObjectTypes.Log, null, OptionsIn.ReturnElements.DataOnly);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Empty_Elements_Are_Removed()
        {
            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);

            DevKit.InitHeader(Log, LogIndexType.measureddepth, false);
            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            // Query all of the log and Assert
            var query = new Log()
            {
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                Uid = Log.Uid
            };
            var returnLog =
                DevKit.Query<LogList, Log>(query, ObjectTypes.Log, null, OptionsIn.ReturnElements.All).FirstOrDefault();
            Assert.IsNotNull(returnLog);
            Assert.IsNotNull(returnLog.IndexType);
            Assert.AreEqual(Log.IndexType, returnLog.IndexType);

            var queryIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                          "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                          "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" +
                          Environment.NewLine +
                          "<log uid=\"" + Log.Uid + "\" uidWell=\"" + Log.UidWell + "\" uidWellbore=\"" + Log.UidWellbore +
                          "\">" + Environment.NewLine +
                          "<nameWell />" + Environment.NewLine +
                          "</log>" + Environment.NewLine +
                          "</logs>";

            // Query log, requested by default.
            var result = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, null);
            Assert.IsNotNull(result);

            var document = WitsmlParser.Parse(result.XMLout);
            var parser = new WitsmlQueryParser(document.Root, ObjectTypes.Log, null);
            Assert.IsFalse(parser.HasElements("indexType"));
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Header_Index_Value_Sorted_For_DecreasingLog()
        {
            var row = 10;
            var response = AddLogWithAction(row, () =>
            {
                Log.StartIndex = new GenericMeasure { Uom = "m", Value = 100 };
                var logData = Log.LogData.First();
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
            }, 1, true, false, false);
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            //var uidLog = response.SuppMsgOut;
            var query = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
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

        [TestMethod, Description("Tests selecting the last column for RequestLatestValue returns the correct data")]
        public void Log141DataAdapter_GetFromStore_Sliced_RequestLatestValue_OptionsIn()
        {
            var row = 10;
            var response = AddLogWithAction(row, () =>
            {
                Log.StartIndex = new GenericMeasure { Uom = "m", Value = 100 };

                var logData = Log.LogData.First();
                logData.Data.Clear();
                logData.Data.Add("1,10,100");
                logData.Data.Add("2,20,200");
                logData.Data.Add("3,30,300");
                logData.Data.Add("4,40,400");
                logData.Data.Add("5,50,500"); 
                logData.Data.Add("6,60,600"); 
                logData.Data.Add("7,70,700"); 
                logData.Data.Add("8,80,800"); 
                logData.Data.Add("9,90,900");                             
                logData.Data.Add("10,100,1000"); // The Latest Values
            }, 1, true, false, true);

            // Assert that the log was added successfully
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var firstAndLast = Log.LogCurveInfo[0].Mnemonic + "," + Log.LogCurveInfo[2].Mnemonic;
            var uidLog = response.SuppMsgOut;
            var query = new Log
            {
                Uid = uidLog,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                LogData = new List<LogData>() { new LogData() { MnemonicList = firstAndLast} }
            };

            var result = DevKit.Query<LogList, Log>(
                query,
                ObjectTypes.Log, null, OptionsIn.ReturnElements.DataOnly + ';' +
                                       OptionsIn.RequestLatestValues.Eq(1));

            // Assert that Log Data was returned
            var queryLogData = result.First().LogData;
            Assert.IsTrue(queryLogData != null && queryLogData.First() != null,
                "No LogData returned in results from Log query");

            // Assert that only one row was returned
            var queryData = queryLogData.First().Data;
            Assert.AreEqual(1, queryData.Count, "No data returned in results from Log query");

            // Assert that the data for the last column of the latest values is correct
            Assert.AreEqual("1000", queryData[0].Split(',')[1], "The last data value row should be 1000");

        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Decreasing_RequestLatestValue_OptionsIn()
        {
            var row = 10;
            var response = AddLogWithAction(row, () =>
            {
                Log.StartIndex = new GenericMeasure {Uom = "m", Value = 100};

                var logData = Log.LogData.First();
                logData.Data.Clear();
                logData.Data.Add("100,1,");
                logData.Data.Add("99,2,");
                logData.Data.Add("98,3,");
                logData.Data.Add("97,4,");

                // Our return data set should be all of these values.
                logData.Data.Add("96,5,"); // The latest 1 value for the 2nd channel
                logData.Data.Add("95,,6"); // should not be returned
                logData.Data.Add("94,,7"); // should not be returned
                logData.Data.Add("93,,8"); // should not be returned
                logData.Data.Add("92,,9"); // should not be returned
                logData.Data.Add("91,,10"); // The latest 1 value for the last channel
            }, 1, true, false, false);

            // Assert that the log was added successfully
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);


            var uidLog = response.SuppMsgOut;
            var query = new Log
            {
                Uid = uidLog,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
            };

            var result = DevKit.Query<LogList, Log>(
                query,
                ObjectTypes.Log, null, OptionsIn.ReturnElements.All + ';' +
                                       OptionsIn.RequestLatestValues.Eq(1));
                // Request the latest 1 value (for each channel)


            // Verify that a Log was returned with the results
            Assert.IsNotNull(result, "No results returned from Log query");
            Assert.IsNotNull(result.First(), "No Logs returned in results from Log query");

            // Verify that a LogData element was returned with the results.
            var queryLogData = result.First().LogData;
            Assert.IsTrue(queryLogData != null && queryLogData.First() != null,
                "No LogData returned in results from Log query");

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
        public void Log141DataAdapter_GetFromStore_Increasing_RequestLatestValue_OptionsIn()
        {
            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var row = 10;
            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            // Add a 4th Log Curve
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("GR2", "gAPI"));

            var startIndex = new GenericMeasure { Uom = "m", Value = 100 };
            Log.StartIndex = startIndex;
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), row, 1, true, false);

            // Reset for custom LogData
            var logData = Log.LogData.First();
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
            var response = DevKit.Add<LogList, Log>(Log);

            // Assert that the log was added successfully
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);


            var uidLog = response.SuppMsgOut;
            var query = new Log
            {
                Uid = uidLog,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
            };

            var result = DevKit.Query<LogList, Log>(
                query,
                ObjectTypes.Log, null, OptionsIn.ReturnElements.All + ';' +
                                       OptionsIn.RequestLatestValues.Eq(2));
                // Request the latest 2 values (for each channel)


            // Verify that a Log was returned with the results
            Assert.IsNotNull(result, "No results returned from Log query");
            Assert.IsNotNull(result.First(), "No Logs returned in results from Log query");

            // Verify that a LogData element was returned with the results.
            var queryLogData = result.First().LogData;
            Assert.IsTrue(queryLogData != null && queryLogData.First() != null,
                "No LogData returned in results from Log query");

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
        public void Log141DataAdapter_GetFromStore_Increasing_Time_RequestLatestValue_OptionsIn()
        {
            var row = 1;
            var response = AddLogWithAction(row, () =>
            {
                // Reset for custom LogData
                var logData = Log.LogData.First();
                logData.Data.Clear();

                logData.Data.Add("2012-07-26T15:17:20.0000000+00:00,1,0");
                logData.Data.Add("2012-07-26T15:17:30.0000000+00:00,2,1");
                logData.Data.Add("2012-07-26T15:17:40.0000000+00:00,,2");
                logData.Data.Add("2012-07-26T15:17:50.0000000+00:00,,3");
            }, 1, false, false);

            // Assert that the log was added successfully
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = new Log
            {
                Uid = uidLog,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
            };

            var result = DevKit.Query<LogList, Log>(
                query,
                ObjectTypes.Log, null, OptionsIn.ReturnElements.All + ';' +
                                       OptionsIn.RequestLatestValues.Eq(1));
                // Request the latest 1 values (for each channel)



            // Verify that a Log was returned with the results
            Assert.IsNotNull(result, "No results returned from Log query");
            Assert.IsNotNull(result.First(), "No Logs returned in results from Log query");

            var logInfos = result.First().LogCurveInfo;
            Assert.AreEqual(Log.LogCurveInfo.Count, logInfos.Count, "There should be 3 LogCurveInfos");

            // Verify that a LogData element was returned with the results.
            var queryLogData = result.First().LogData;
            Assert.IsTrue(queryLogData != null && queryLogData.First() != null,
                "No LogData returned in results from Log query");

            // Verify that data rows were returned with the LogData
            var queryData = queryLogData.First().Data;
            Assert.IsTrue(queryData != null && queryData.Count > 0, "No data returned in results from Log query");

            // Verify that only the rows of data (referenced above) for index values 96 and 91 were returned 
            //... in order to get the latest 1 value for each channel.
            Assert.AreEqual(2, queryData.Count, "Only rows for index values 4,5,9,10,14 and 15 should be returned.");
            Assert.AreEqual("2012-07-26T15:17:30.0000000+00:00", queryData[0].Split(',')[0],
                "The first data row should be for index value 2012-07-26T15:17:30.0000000+00:00");
            Assert.AreEqual("2012-07-26T15:17:50.0000000+00:00", queryData[1].Split(',')[0],
                "The last data row should be for index value 2012-07-26T15:17:50.0000000+00:00");

            Assert.IsNotNull(logInfos);

            // Validate the Min and Max of each LogCurveInfo #1
            var minDateTimeIndex0 = logInfos[0].MinDateTimeIndex;
            var maxDateTimeIndex0 = logInfos[0].MaxDateTimeIndex;
            var minDateTimeIndex1 = logInfos[1].MinDateTimeIndex;
            var maxDateTimeIndex1 = logInfos[1].MaxDateTimeIndex;
            var minDateTimeIndex2 = logInfos[2].MinDateTimeIndex;
            var maxDateTimeIndex2 = logInfos[2].MaxDateTimeIndex;

            Assert.IsNotNull(minDateTimeIndex0);
            Assert.IsNotNull(maxDateTimeIndex0);
            Assert.AreEqual(minDateTimeIndex0.Value,
                new Timestamp(DateTimeOffset.Parse(queryData[0].Split(',')[0])));
            Assert.AreEqual(maxDateTimeIndex0.Value,
                new Timestamp(DateTimeOffset.Parse(queryData[1].Split(',')[0])));

            // Validate the Min and Max of each LogCurveInfo #2
            Assert.IsNotNull(minDateTimeIndex1);
            Assert.IsNotNull(maxDateTimeIndex1);
            Assert.AreEqual(minDateTimeIndex1.Value,
                new Timestamp(DateTimeOffset.Parse(queryData[0].Split(',')[0])));
            Assert.AreEqual(maxDateTimeIndex1.Value,
                new Timestamp(DateTimeOffset.Parse(queryData[0].Split(',')[0])));

            // Validate the Min and Max of each LogCurveInfo #3
            Assert.IsNotNull(minDateTimeIndex2);
            Assert.IsNotNull(maxDateTimeIndex2);
            Assert.AreEqual(minDateTimeIndex2.Value,
                new Timestamp(DateTimeOffset.Parse(queryData[1].Split(',')[0])));
            Assert.AreEqual(maxDateTimeIndex2.Value,
                new Timestamp(DateTimeOffset.Parse(queryData[1].Split(',')[0])));
        }
        
        [TestMethod]
        public void Log141DataAdapter_GetFromStore_ReturnElements_DataOnly_Supports_Multiple_Queries()
        {
            var row = 3;
            var response = AddLogWithAction(row, null, hasEmptyChannel: false);
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            var uidLog1 = response.SuppMsgOut;

            // Add second log
            var log2 = DevKit.CreateLog(null, DevKit.Name("Log 02"), Log.UidWell, Log.NameWell, Log.UidWellbore,
                Log.NameWellbore);
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
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            var uidLog2 = response.SuppMsgOut;

            // Query
            var query1 = DevKit.CreateLog(uidLog1, null, log2.UidWell, null, log2.UidWellbore, null);
            var query2 = DevKit.CreateLog(uidLog2, null, log2.UidWell, null, log2.UidWellbore, null);

            var result = DevKit.Get<LogList, Log>(DevKit.List(query1, query2), ObjectTypes.Log, null,
                OptionsIn.ReturnElements.DataOnly);
            Assert.AreEqual((short) ErrorCodes.Success, result.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            Assert.AreEqual(2, logList.Log.Count);

            // Query result of Depth log
            var mnemonicList1 = logList.Log[0].LogData[0].MnemonicList.Split(',');
            Assert.AreEqual(3, mnemonicList1.Length);
            Assert.IsTrue(!mnemonicList1.Except(new List<string>() {"MD", "ROP", "GR"}).Any());
            Assert.AreEqual(3, logList.Log[0].LogData[0].Data.Count);

            // Query result of Time log
            var mnemonicList2 = logList.Log[1].LogData[0].MnemonicList.Split(',');
            Assert.AreEqual(4, mnemonicList2.Length);
            Assert.IsFalse(mnemonicList2.Except(new List<string>() {"TIME", "AAA", "BBB", "CCC"}).Any());
            Assert.AreEqual(4, logList.Log[1].LogData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_ReturnElements_Requested_Supports_Multiple_Queries()
        {
            var row = 3;
            var response = AddLogWithAction(row, null, hasEmptyChannel: false);
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            var uidLog1 = response.SuppMsgOut;

            // Add second log
            var log2 = DevKit.CreateLog(null, DevKit.Name("Log 02"), Log.UidWell, Log.NameWell, Log.UidWellbore,
                Log.NameWellbore);
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
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            var uidLog2 = response.SuppMsgOut;

            // Query
            var query1 = DevKit.CreateLog(uidLog1, null, log2.UidWell, null, log2.UidWellbore, null);
            query1.LogCurveInfo = new List<LogCurveInfo>();
            query1.LogCurveInfo.Add(new LogCurveInfo() {Uid = "MD", Mnemonic = new ShortNameStruct("MD")});
            query1.LogCurveInfo.Add(new LogCurveInfo() {Uid = "ROP", Mnemonic = new ShortNameStruct("ROP")});
            query1.LogCurveInfo.Add(new LogCurveInfo() {Uid = "GR", Mnemonic = new ShortNameStruct("GR")});
            query1.LogData = new List<LogData>() {new LogData()};
            query1.LogData.First().MnemonicList = "MD,ROP,GR";

            var query2 = DevKit.CreateLog(uidLog2, null, log2.UidWell, null, log2.UidWellbore, null);
            query2.LogCurveInfo = new List<LogCurveInfo>();
            query2.LogCurveInfo.Add(new LogCurveInfo() {Uid = "TIME", Mnemonic = new ShortNameStruct("TIME")});
            query2.LogCurveInfo.Add(new LogCurveInfo() {Uid = "AAA", Mnemonic = new ShortNameStruct("AAA")});
            query2.LogCurveInfo.Add(new LogCurveInfo() {Uid = "BBB", Mnemonic = new ShortNameStruct("BBB")});
            query2.LogCurveInfo.Add(new LogCurveInfo() {Uid = "CCC", Mnemonic = new ShortNameStruct("CCC")});
            query2.LogData = new List<LogData>() {new LogData()};
            query2.LogData.First().MnemonicList = "TIME,AAA,BBB,CCC";

            var list = DevKit.New<LogList>(x => x.Log = DevKit.List(query1, query2));
            var queryIn = WitsmlParser.ToXml(list);
            var result = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short) ErrorCodes.Success, result.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            Assert.AreEqual(2, logList.Log.Count);

            // Query result of Depth log
            var mnemonicList1 = logList.Log[0].LogData[0].MnemonicList.Split(',');
            Assert.AreEqual(3, mnemonicList1.Length);
            Assert.IsTrue(!mnemonicList1.Except(new List<string>() {"MD", "ROP", "GR"}).Any());
            Assert.AreEqual(3, logList.Log[0].LogData[0].Data.Count);

            // Query result of Time log
            var mnemonicList2 = logList.Log[1].LogData[0].MnemonicList.Split(',');
            Assert.AreEqual(4, mnemonicList2.Length);
            Assert.IsFalse(mnemonicList2.Except(new List<string>() {"TIME", "AAA", "BBB", "CCC"}).Any());
            Assert.AreEqual(4, logList.Log[1].LogData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Supports_NaN_In_Numeric_Fields()
        {
            var row = 3;
            var response = AddLogWithAction(row, () =>
            {
                Log.BhaRunNumber = 123;
                Log.LogCurveInfo[0].ClassIndex = 1;
                Log.LogCurveInfo[1].ClassIndex = 2;
            });
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            // Query log
            var queryIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                          "<logs version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" +
                          Environment.NewLine +
                          "<log uid=\"" + Log.Uid + "\" uidWell=\"" + Log.UidWell + "\" uidWellbore=\"" +
                          Log.UidWellbore + "\">" + Environment.NewLine +
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
            Assert.AreEqual((short) ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);

            Assert.AreEqual((short) 123, logList.Log.First().BhaRunNumber);
            Assert.AreEqual(2, logList.Log.First().LogCurveInfo.Count);
            Assert.AreEqual((short) 1, logList.Log.First().LogCurveInfo[0].ClassIndex);
            Assert.AreEqual((short) 2, logList.Log.First().LogCurveInfo[1].ClassIndex);
        }


        [TestMethod]
        public void Log141DataAdapter_GetFromStore_With_Start_And_End_Index_On_Increasing_DepthLog_Data_In_Different_Chunk()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);

            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            var logData = Log.LogData.First();
            logData.Data.Add("1700.0,17.1,17.2");
            logData.Data.Add("1800.0,18.1,18.2");
            logData.Data.Add("1900.0,19.1,19.2");
            logData.Data.Add("2700.0,27.1,27.2");
            logData.Data.Add("2800.0,28.1,28.2");
            logData.Data.Add("2900.0,29.1,29.2");

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Query
            var query = DevKit.CreateLog(uidLog, null, Log.UidWell, null, Log.UidWellbore, null);
            query.StartIndex = new GenericMeasure(1800, "ft");
            query.EndIndex = new GenericMeasure(2700, "ft");

            var result = DevKit.Get<LogList, Log>(DevKit.List(query), ObjectTypes.Log, null,
                OptionsIn.ReturnElements.DataOnly);
            Assert.AreEqual((short) ErrorCodes.Success, result.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            var resultLog = logList.Log;
            Assert.AreEqual(1, resultLog.Count);
            Assert.AreEqual(3, resultLog[0].LogData[0].Data.Count);
            Assert.AreEqual(1800, Convert.ToDouble(resultLog[0].LogData[0].Data[0].Split(',').First()));
            Assert.AreEqual(1900, Convert.ToDouble(resultLog[0].LogData[0].Data[1].Split(',').First()));
            Assert.AreEqual(2700, Convert.ToDouble(resultLog[0].LogData[0].Data[2].Split(',').First()));
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_With_Start_And_End_Index_On_decreasing_DepthLog_Data_In_Different_Chunk()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);

            Log.LogData = DevKit.List(new LogData() {Data = DevKit.List<string>()});
            var logData = Log.LogData.First();
            logData.Data.Add("2300.0,23.1,23.2");
            logData.Data.Add("2200.0,22.1,22.2");
            logData.Data.Add("2100.0,21.1,21.2");
            logData.Data.Add("2000.0,20.1,20.2");
            logData.Data.Add("1900.0,19.1,19.2");
            logData.Data.Add("1800.0,18.1,18.2");

            DevKit.InitHeader(Log, LogIndexType.measureddepth, false);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Query
            var query = DevKit.CreateLog(uidLog, null, Log.UidWell, null, Log.UidWellbore, null);
            query.StartIndex = new GenericMeasure(2200, "ft");
            query.EndIndex = new GenericMeasure(1800, "ft");

            var result = DevKit.Get<LogList, Log>(DevKit.List(query), ObjectTypes.Log, null,
                OptionsIn.ReturnElements.DataOnly);
            Assert.AreEqual((short) ErrorCodes.Success, result.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            var resultLog = logList.Log;
            Assert.AreEqual(1, resultLog.Count);
            Assert.AreEqual(5, resultLog[0].LogData[0].Data.Count);
            Assert.AreEqual(2200, Convert.ToDouble(resultLog[0].LogData[0].Data[0].Split(',').First()));
            Assert.AreEqual(2100, Convert.ToDouble(resultLog[0].LogData[0].Data[1].Split(',').First()));
            Assert.AreEqual(2000, Convert.ToDouble(resultLog[0].LogData[0].Data[2].Split(',').First()));
            Assert.AreEqual(1900, Convert.ToDouble(resultLog[0].LogData[0].Data[3].Split(',').First()));
            Assert.AreEqual(1800, Convert.ToDouble(resultLog[0].LogData[0].Data[4].Split(',').First()));
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_With_Start_And_End_Index_On_Channel_With_Null_Values()
        {
            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            Log.LogCurveInfo.Clear();
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("MD", "ft"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("AAA", "m/h"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("BBB", "gAPI"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("CCC", "gAPI"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("DDD", "s"));

            Log.LogData = DevKit.List(new LogData() {Data = DevKit.List<string>()});
            var logData = Log.LogData.First();
            logData.Data.Clear();
            logData.MnemonicList = "MD,AAA,BBB,CCC,DDD";
            logData.UnitList = "ft,m/h,gAPI,gAPI,s";
            logData.Data.Add("1700.0, 17.1, 17.2, null, 17.4");
            logData.Data.Add("1800.0, 18.1, 18.2, null, 18.4");
            logData.Data.Add("1900.0, 19.1, 19.2, null, 19.4");
            logData.Data.Add("2000.0, 20.1, 20.2, null, 20.4");
            logData.Data.Add("2100.0, 21.1, 21.2, null, 21.4");
            logData.Data.Add("2200.0, 22.1, 22.2, null, 22.4");
            logData.Data.Add("2300.0, 23.1, 23.2, 23.3, 23.4");

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Query
            var query = DevKit.CreateLog(uidLog, null, Log.UidWell, null, Log.UidWellbore, null);
            query.StartIndex = new GenericMeasure(1800, "ft");
            query.EndIndex = new GenericMeasure(2200, "ft");

            var result = DevKit.Get<LogList, Log>(DevKit.List(query), ObjectTypes.Log, null,
                OptionsIn.ReturnElements.All);
            Assert.AreEqual((short) ErrorCodes.Success, result.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            var resultLog = logList.Log;
            Assert.AreEqual(1, resultLog.Count);

            // Result log header
            var logCurveInfoList = resultLog[0].LogCurveInfo;
            Assert.AreEqual(4, logCurveInfoList.Count());
            var lciMnemonics = logCurveInfoList.Select(x => x.Mnemonic.Value).ToArray();
            Assert.IsFalse(lciMnemonics.Except(new List<string>() {"MD", "AAA", "BBB", "DDD"}).Any());

            // Result log data
            var mnemonicList = resultLog[0].LogData[0].MnemonicList.Split(',');
            Assert.AreEqual(4, mnemonicList.Count());
            Assert.IsFalse(mnemonicList.Except(new List<string>() {"MD", "AAA", "BBB", "DDD"}).Any());
            var unitList = resultLog[0].LogData[0].UnitList.Split(',');
            Assert.AreEqual(4, unitList.Count());
            Assert.IsFalse(unitList.Except(new List<string>() {"ft", "m/h", "gAPI", "s"}).Any());

            var data = resultLog[0].LogData[0].Data;
            Assert.AreEqual(5, data.Count);

            double value = 18;
            foreach (string r in data)
            {
                var row = r.Split(',');
                Assert.AreEqual(4, row.Count());
                Assert.AreEqual(value*100, Convert.ToDouble(row[0]));
                Assert.AreEqual(value + 0.1, Convert.ToDouble(row[1]));
                Assert.AreEqual(value + 0.2, Convert.ToDouble(row[2]));
                Assert.AreEqual(value + 0.4, Convert.ToDouble(row[3]));
                value++;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Slice_On_Channel_On_Range_Of_Null_Indicator_Values_In_Different_Chunks()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            Log.NullValue = "-999.25";

            Log.LogCurveInfo.Clear();
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("MD", "ft"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("AAA", "m/h"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("BBB", "gAPI"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("CCC", "gAPI"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("DDD", "s"));

            Log.LogData = DevKit.List(new LogData() {Data = DevKit.List<string>()});
            var logData = Log.LogData.First();
            logData.Data.Clear();
            logData.MnemonicList = "MD,AAA,BBB,CCC,DDD";
            logData.UnitList = "ft,m/h,gAPI,gAPI,s";
            logData.Data.Add("1700.0, 17.1, 17.2, -999.25, 17.4");
            logData.Data.Add("1800.0, 18.1, 18.2, -999.25, 18.4");
            logData.Data.Add("1900.0, 19.1, 19.2, -999.25, 19.4");
            logData.Data.Add("2000.0, 20.1, 20.2, -999.25, 20.4");
            logData.Data.Add("2100.0, 21.1, 21.2, -999.25, 21.4");
            logData.Data.Add("2200.0, 22.1, 22.2, -999.25, 22.4");
            logData.Data.Add("2300.0, 23.1, 23.2,    23.3, 23.4");

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Query
            var query = DevKit.CreateLog(uidLog, null, Log.UidWell, null, Log.UidWellbore, null);
            query.StartIndex = new GenericMeasure(1800, "ft");
            query.EndIndex = new GenericMeasure(2200, "ft");

            var result = DevKit.Get<LogList, Log>(DevKit.List(query), ObjectTypes.Log, null,
                OptionsIn.ReturnElements.All);
            Assert.AreEqual((short) ErrorCodes.Success, result.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            var resultLog = logList.Log;
            Assert.AreEqual(1, resultLog.Count);

            // Result log header
            var logCurveInfoList = resultLog[0].LogCurveInfo;
            Assert.AreEqual(4, logCurveInfoList.Count());
            var lciMnemonics = logCurveInfoList.Select(x => x.Mnemonic.Value).ToArray();
            Assert.IsFalse(lciMnemonics.Except(new List<string>() {"MD", "AAA", "BBB", "DDD"}).Any());

            // Result log data
            var mnemonicList = resultLog[0].LogData[0].MnemonicList.Split(',');
            Assert.AreEqual(4, mnemonicList.Count());
            Assert.IsFalse(mnemonicList.Except(new List<string>() {"MD", "AAA", "BBB", "DDD"}).Any());
            var unitList = resultLog[0].LogData[0].UnitList.Split(',');
            Assert.AreEqual(4, unitList.Count());
            Assert.IsFalse(unitList.Except(new List<string>() {"ft", "m/h", "gAPI", "s"}).Any());

            var data = resultLog[0].LogData[0].Data;
            Assert.AreEqual(5, data.Count);

            double value = 18;
            foreach (string s in data)
            {
                var row = s.Split(',');
                Assert.AreEqual(4, row.Count());
                Assert.AreEqual(value*100, Convert.ToDouble(row[0]));
                Assert.AreEqual(value + 0.1, Convert.ToDouble(row[1]));
                Assert.AreEqual(value + 0.2, Convert.ToDouble(row[2]));
                Assert.AreEqual(value + 0.4, Convert.ToDouble(row[3]));
                value++;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Calculate_Channels_Range_With_Different_Null_Indicators_In_Different_Chunks()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            Log.NullValue = "-999.25";

            Log.LogCurveInfo.Clear();
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("MD", "ft"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("AAA", "m/h"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("BBB", "gAPI"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("CCC", "gAPI"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("DDD", "s"));

            // Set channels null value except channel "CCC"
            Log.LogCurveInfo[1].NullValue = "-1111.1";
            Log.LogCurveInfo[2].NullValue = "-2222.2";
            Log.LogCurveInfo[4].NullValue = "-4444.4";

            Log.LogData = DevKit.List(new LogData { Data = DevKit.List<string>() });
            var logData = Log.LogData.First();
            logData.Data.Clear();

            logData.MnemonicList = "MD,AAA,BBB,CCC,DDD";
            logData.UnitList = "ft,m/h,gAPI,gAPI,s";

            logData.Data.Add("1700.0, -1111.1,    17.2, -999.25, -4444.4");
            logData.Data.Add("1800.0,    18.1,    18.2, -999.25, -4444.4");
            logData.Data.Add("1900.0,    19.1,    19.2, -999.25,    19.4");
            logData.Data.Add("2000.0,    20.1,    20.2, -999.25,    20.4");
            logData.Data.Add("2100.0,    21.1,    21.2, -999.25,    21.4");
            logData.Data.Add("2200.0,    22.1,    22.2, -999.25, -4444.4");
            logData.Data.Add("2300.0,    23.1, -2222.2,    23.3, -4444.4");

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Query
            var query = DevKit.CreateLog(uidLog, null, Log.UidWell, null, Log.UidWellbore, null);
            query.StartIndex = new GenericMeasure(1700.0, "ft");
            query.EndIndex = new GenericMeasure(2300.0, "ft");

            var result = DevKit.Get<LogList, Log>(DevKit.List(query), ObjectTypes.Log, null, OptionsIn.ReturnElements.All);
            Assert.AreEqual((short) ErrorCodes.Success, result.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            var resultLog = logList.Log;
            Assert.AreEqual(1, resultLog.Count);

            // Result log header
            var logCurveInfoList = resultLog[0].LogCurveInfo;
            Assert.AreEqual(5, logCurveInfoList.Count());

            var lciMnemonics = logCurveInfoList.Select(x => x.Mnemonic.Value).ToArray();
            Assert.IsFalse(lciMnemonics.Except(new[] { "MD", "AAA", "BBB", "CCC", "DDD" }).Any());

            Assert.AreEqual(1800.0, logCurveInfoList[1].MinIndex.Value);
            Assert.AreEqual(2300.0, logCurveInfoList[1].MaxIndex.Value);
            Assert.AreEqual(1700.0, logCurveInfoList[2].MinIndex.Value);
            Assert.AreEqual(2200.0, logCurveInfoList[2].MaxIndex.Value);
            Assert.AreEqual(2300.0, logCurveInfoList[3].MinIndex.Value);
            Assert.AreEqual(2300.0, logCurveInfoList[3].MaxIndex.Value);
            Assert.AreEqual(1900.0, logCurveInfoList[4].MinIndex.Value);
            Assert.AreEqual(2100.0, logCurveInfoList[4].MaxIndex.Value);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_With_Null_Indicator_Empty_Row_Should_Not_Be_Returned()
        {
            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.NullValue = "-999.25";
            Log.LogCurveInfo.Clear();
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("MD", "ft"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("AAA", "m/h"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("BBB", "gAPI"));
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("CCC", "gAPI"));

            Log.LogData = DevKit.List(new LogData() {Data = DevKit.List<string>()});
            var logData = Log.LogData.First();
            logData.Data.Clear();
            logData.MnemonicList = "MD,AAA,BBB,CCC";
            logData.UnitList = "ft,m/h,gAPI,gAPI";
            logData.Data.Add("1700.0, 17.1, 17.2, -999.25");
            logData.Data.Add("1800.0, 18.1, 18.2, -999.25");
            logData.Data.Add("1900.0, -999.25, -999.25, -999.25");
            logData.Data.Add("2000.0, 20.1, 20.2, -999.25");

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Query
            var query = DevKit.CreateLog(uidLog, null, Log.UidWell, null, Log.UidWellbore, null);

            var result = DevKit.Get<LogList, Log>(DevKit.List(query), ObjectTypes.Log, null,
                OptionsIn.ReturnElements.All);
            Assert.AreEqual((short) ErrorCodes.Success, result.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            var resultLog = logList.Log;
            Assert.AreEqual(1, resultLog.Count);

            // Result log header
            var logCurveInfoList = resultLog[0].LogCurveInfo;
            Assert.AreEqual(3, logCurveInfoList.Count());
            var lciMnemonics = logCurveInfoList.Select(x => x.Mnemonic.Value).ToArray();
            Assert.IsFalse(lciMnemonics.Except(new List<string>() {"MD", "AAA", "BBB", "CCC"}).Any());

            // Result log data
            var mnemonicList = resultLog[0].LogData[0].MnemonicList.Split(',');
            Assert.AreEqual(3, mnemonicList.Count());
            Assert.IsFalse(mnemonicList.Except(new List<string>() {"MD", "AAA", "BBB"}).Any());
            var unitList = resultLog[0].LogData[0].UnitList.Split(',');
            Assert.AreEqual(3, unitList.Count());
            Assert.IsFalse(unitList.Except(new List<string>() {"ft", "m/h", "gAPI"}).Any());

            var data = resultLog[0].LogData[0].Data;
            Assert.AreEqual(3, data.Count);

            Assert.IsTrue(data[0].Equals("1700,17.1,17.2"));
            Assert.IsTrue(data[1].Equals("1800,18.1,18.2"));
            Assert.IsTrue(data[2].Equals("2000,20.1,20.2"));
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_MaxReturnNodes_OptionsIn()
        {
            var numRows = 20;
            var maxReturnNodes = 5;

            // Add the Setup Well, Wellbore and Log to the store.
            WMLS_AddToStoreResponse logResponse =
                AddSetupWellWellboreLog(numRows, isDepthLog: true, hasEmptyChannel: false, increasing: true);

            // Assert that the log was added successfully
            Assert.AreEqual((short) ErrorCodes.Success, logResponse.Result);

            var query = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
            };

            short errorCode;
            var result = DevKit.QueryWithErrorCode<LogList, Log>(
                query, out errorCode,
                ObjectTypes.Log, null, OptionsIn.ReturnElements.All + ';' +
                                       OptionsIn.MaxReturnNodes.Eq(maxReturnNodes));

            Assert.AreEqual((short) ErrorCodes.ParialSuccess, errorCode, "Error code should indicate partial success");

            // Verify that a Log was returned with the results
            Assert.IsNotNull(result, "No results returned from Log query");

            var queriedLog = result.First();
            Assert.IsNotNull(queriedLog, "No Logs returned in results from Log query");

            // Verify that a LogData element was returned with the results.
            var queryLogData = queriedLog.LogData;
            Assert.IsTrue(queryLogData != null && queryLogData.First() != null,
                "No LogData returned in results from Log query");

            // Verify that data rows were returned with the LogData
            var queryData = queryLogData.First().Data;
            Assert.IsTrue(queryData != null && queryData.Count == maxReturnNodes,
                string.Format("Expected {0} rows returned because MaxReturnNodes = {0}", maxReturnNodes));
        }


        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Slice_Empty_Channel_MaxReturnNodes_OptionsIn()
        {
            var numRows = 20;
            var maxReturnNodes = 5;

            // Add the Setup Well, Wellbore and Log to the store.
            WMLS_AddToStoreResponse logResponse =
                AddSetupWellWellboreLog(numRows, isDepthLog: true, hasEmptyChannel: true, increasing: true);

            // Assert that the log was added successfully
            Assert.AreEqual((short) ErrorCodes.Success, logResponse.Result);

            var query = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
            };

            short errorCode;
            var result = DevKit.QueryWithErrorCode<LogList, Log>(
                query, out errorCode,
                ObjectTypes.Log, null, OptionsIn.ReturnElements.All + ';' +
                                       OptionsIn.MaxReturnNodes.Eq(maxReturnNodes));

            Assert.AreEqual((short) ErrorCodes.ParialSuccess, errorCode, "Error code should indicate partial success");

            // Verify that a Log was returned with the results
            Assert.IsNotNull(result, "No results returned from Log query");

            var queriedLog = result.First();
            Assert.IsNotNull(queriedLog, "No Logs returned in results from Log query");

            // Test that the column count returned is reduced by one.
            Assert.AreEqual(Log.LogCurveInfo.Count - 1, queriedLog.LogCurveInfo.Count);

            // Verify that a LogData element was returned with the results.
            var queryLogData = queriedLog.LogData;
            Assert.IsTrue(queryLogData != null && queryLogData.First() != null,
                "No LogData returned in results from Log query");

            // Verify that data rows were returned with the LogData
            var queryData = queryLogData.First().Data;
            Assert.IsTrue(queryData != null && queryData.Count == maxReturnNodes,
                string.Format("Expected {0} rows returned because MaxReturnNodes = {0}", maxReturnNodes));
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Multiple_MaxReturnNodes_OptionsIn()
        {
            var numRows = 20;
            var maxReturnNodes = 5;

            // Add the Setup Well, Wellbore and Log to the store.
            WMLS_AddToStoreResponse log1Response =
                AddSetupWellWellboreLog(numRows, isDepthLog: true, hasEmptyChannel: false, increasing: true);

            Assert.AreEqual((short) ErrorCodes.Success, log1Response.Result);

            // Add a second Log to the same wellbore as Setup log (Log)
            var log2 = DevKit.CreateLog(null, DevKit.Name("Log 02"), Log.UidWell, Well.Name, Log.UidWellbore,
                Wellbore.Name);
            DevKit.InitHeader(log2, LogIndexType.measureddepth);
            DevKit.InitDataMany(log2, DevKit.Mnemonics(log2), DevKit.Units(log2), numRows, hasEmptyChannel: false);

            // Add the 2nd log
            var log2Response = DevKit.Add<LogList, Log>(log2);

            var query1 = DevKit.CreateLog(log1Response.SuppMsgOut, null, Log.UidWell, null, Log.UidWellbore, null);
            var query2 = DevKit.CreateLog(log2Response.SuppMsgOut, null, log2.UidWell, null, log2.UidWellbore, null);

            // Perform a GetFromStore with multiple log queries
            var result = DevKit.Get<LogList, Log>(
                DevKit.List(query1, query2),
                ObjectTypes.Log,
                null,
                OptionsIn.ReturnElements.All + ';' + OptionsIn.MaxReturnNodes.Eq(maxReturnNodes));
            Assert.AreEqual((short) ErrorCodes.ParialSuccess, result.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            Assert.AreEqual(2, logList.Items.Count, "Two logs should be returned");

            // Test that each log has maxRetunNodes number of log data rows.
            foreach (var l in logList.Items)
            {
                var log = l as Log;
                Assert.IsNotNull(log);
                Assert.AreEqual(maxReturnNodes, log.LogData[0].Data.Count);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Multiple_MaxDataNodes_MaxReturnNodes_OptionsIn()
        {
            var numRows = 20;
            var maxReturnNodes = 5;

            // Add the Setup Well, Wellbore and Log to the store.
            WMLS_AddToStoreResponse log1Response =
                AddSetupWellWellboreLog(numRows, isDepthLog: true, hasEmptyChannel: false, increasing: true);

            Assert.AreEqual((short) ErrorCodes.Success, log1Response.Result);

            // Add a second Log to the same wellbore as Setup log (Log)
            var log2 = DevKit.CreateLog(null, DevKit.Name("Log 02"), Log.UidWell, Well.Name, Log.UidWellbore,
                Wellbore.Name);
            DevKit.InitHeader(log2, LogIndexType.measureddepth);
            DevKit.InitDataMany(log2, DevKit.Mnemonics(log2), DevKit.Units(log2), numRows, hasEmptyChannel: false);

            // Add the 2nd log
            var log2Response = DevKit.Add<LogList, Log>(log2);

            var query1 = DevKit.CreateLog(log1Response.SuppMsgOut, null, Log.UidWell, null, Log.UidWellbore, null);
            var query2 = DevKit.CreateLog(log2Response.SuppMsgOut, null, log2.UidWell, null, log2.UidWellbore, null);

            // This will cap the total response nodes to 8 instead of 10 if this was not specified.
            var previousMaxDataNodes = WitsmlSettings.LogMaxDataNodesGet;
            WitsmlSettings.LogMaxDataNodesGet = 8;

            try
            {
                // Perform a GetFromStore with multiple log queries
                var result = DevKit.Get<LogList, Log>(
                    DevKit.List(query1, query2),
                    ObjectTypes.Log,
                    null,
                    OptionsIn.ReturnElements.All + ';' + OptionsIn.MaxReturnNodes.Eq(maxReturnNodes));
                Assert.AreEqual((short) ErrorCodes.ParialSuccess, result.Result);

                var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
                Assert.IsNotNull(logList);
                Assert.IsNotNull(logList.Items);
                Assert.AreEqual(2, logList.Items.Count, "Two logs should be returned");

                // The first log should have maxReturnNodes log data rows
                var log0 = (logList.Items[0] as Log);
                Assert.IsNotNull(log0);
                Assert.AreEqual(maxReturnNodes, log0.LogData[0].Data.Count);

                // Since there is a total cap of 8 rows the last log should have only 3 rows.
                var log1 = (logList.Items[1] as Log);
                Assert.IsNotNull(log1);
                Assert.AreEqual(WitsmlSettings.LogMaxDataNodesGet - maxReturnNodes,
                    log1.LogData[0].Data.Count);

                WitsmlSettings.LogMaxDataNodesGet = previousMaxDataNodes;
            }
            catch
            {
                WitsmlSettings.LogMaxDataNodesGet = previousMaxDataNodes;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Returns_Less_Than_MaxDataPoints()
        {
            int maxDataPoints = 10;
            int numRows = 10;

            // Add the Setup Well, Wellbore and Log to the store.
            var logResponse = AddSetupWellWellboreLog(numRows, isDepthLog: true, hasEmptyChannel: false,
                increasing: true);
            Assert.AreEqual((short) ErrorCodes.Success, logResponse.Result);

            var query = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);

            // Query the log and it returns the whole log data
            short errorCode;
            var result = DevKit.QueryWithErrorCode<LogList, Log>(query, out errorCode, ObjectTypes.Log, null,
                OptionsIn.ReturnElements.All);

            Assert.AreEqual((short) ErrorCodes.Success, errorCode);
            Assert.IsNotNull(result);

            var returnLog = result.First();
            Assert.IsNotNull(returnLog);
            Assert.AreEqual(1, returnLog.LogData.Count);

            var returnDataPoints = returnLog.LogData[0].Data[0].Split(',').Length*returnLog.LogData[0].Data.Count;
            Assert.IsTrue(maxDataPoints < returnDataPoints);

            // Change the MaxDataPoints in Settings to a small number and query the log again
            WitsmlSettings.LogMaxDataPointsGet = maxDataPoints;

            result = DevKit.QueryWithErrorCode<LogList, Log>(query, out errorCode, ObjectTypes.Log, null,
                OptionsIn.ReturnElements.All);

            Assert.AreEqual((short) ErrorCodes.ParialSuccess, errorCode, "Returning partial data.");
            Assert.IsNotNull(result);

            returnLog = result.First();
            Assert.IsNotNull(returnLog);
            Assert.AreEqual(1, returnLog.LogData.Count);

            returnDataPoints = returnLog.LogData[0].Data[0].Split(',').Length*returnLog.LogData[0].Data.Count;
            Assert.IsFalse(maxDataPoints < returnDataPoints);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Empty_MneMonicList_And_ReturnElement_DataOnly()
        {
            // Add well
            var response = DevKit.Add<WellList, Well>(Well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add log
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 3);

            response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Query log
            var queryIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + Log.Uid + "\" uidWell=\"" + Log.UidWell + "\" uidWellbore=\"" + Log.UidWellbore + "\">" + Environment.NewLine +
                        "<logData>" + Environment.NewLine +
                        "  <mnemonicList/>" + Environment.NewLine +
                        "</logData>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=data-only");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);
            Assert.AreEqual(1, logList.Log[0].LogData.Count);
            Assert.AreEqual(3, logList.Log[0].LogData[0].Data.Count);           
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Empty_MneMonicList_And_ReturnElement_Requested()
        {
            // Add well
            var response = DevKit.Add<WellList, Well>(Well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add log
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 3);

            response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Query log
            var queryIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + Log.Uid + "\" uidWell=\"" + Log.UidWell + "\" uidWellbore=\"" + Log.UidWellbore + "\">" + Environment.NewLine +
                        "<logData>" + Environment.NewLine +
                        "  <mnemonicList/>" + Environment.NewLine +
                        "</logData>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);
            Assert.AreEqual(1, logList.Log[0].LogData.Count);
            Assert.AreEqual(3, logList.Log[0].LogData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Specified_MnemonicList_With_Requested_Elements_Requested()
        {
            new SampleDataTests()
                .AddSampleData(TestContext);
            
            var queryIn = @"
                           <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">
                                    <logData>
                                        <mnemonicList>TIME,SSAS,WDSSA</mnemonicList>
                                    </logData>
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);

            var logData = logList.Log[0].LogData;
            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(3, logData[0].MnemonicList.Split(',').Length);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Specified_MnemonicList_With_Requested_Elements_All()
        {
            new SampleDataTests()
                .AddSampleData(TestContext);

            var queryIn = @"
                           <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">
                                    <logData>
                                        <mnemonicList>TIME,SSAS,WDSSA</mnemonicList>
                                    </logData>
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=all");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);

            var logData = logList.Log[0].LogData;            
            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(3, logData[0].MnemonicList.Split(',').Length);           
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Specified_StartIndex_With_Requested_Elements_Requested()
        {
            new SampleDataTests()
                .AddSampleData(TestContext);

            var queryIn = @"
                           <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">
                                    <startDateTimeIndex>2016-12-16T06:15:45.0000000+00:00</startDateTimeIndex>                                    
                                    <logData>            
                                        <mnemonicList/>
                                    </logData>
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);
            Assert.IsTrue(logList.Log[0].StartDateTimeIndex.HasValue);
            Assert.IsFalse(logList.Log[0].EndDateTimeIndex.HasValue);

            var logData = logList.Log[0].LogData;
            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(20, logData[0].MnemonicList.Split(',').Length);
            Assert.AreEqual(27, logData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Specified_StartIndex_With_Requested_Elements_All()
        {
            new SampleDataTests()
                .AddSampleData(TestContext);

            var queryIn = @"
                           <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">
                                    <startDateTimeIndex>2016-12-16T06:15:45.0000000+00:00</startDateTimeIndex>                                                                       
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=all");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);
            Assert.IsTrue(logList.Log[0].StartDateTimeIndex.HasValue);
            Assert.IsTrue(logList.Log[0].EndDateTimeIndex.HasValue);

            var logData = logList.Log[0].LogData;
            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(20, logData[0].MnemonicList.Split(',').Length);
            Assert.AreEqual(27, logData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Specified_EndIndex_With_Requested_Elements_Requested()
        {
            new SampleDataTests()
                .AddSampleData(TestContext);

            var queryIn = @"
                           <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">
                                    <endDateTimeIndex>2016-12-15T21:51:15.0000000+00:00</endDateTimeIndex>                                    
                                    <logData>            
                                        <mnemonicList/>
                                    </logData>
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);
            Assert.IsTrue(logList.Log[0].EndDateTimeIndex.HasValue);
            Assert.IsFalse(logList.Log[0].StartDateTimeIndex.HasValue);

            var logData = logList.Log[0].LogData;
            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(9, logData[0].MnemonicList.Split(',').Length);
            Assert.AreEqual(24, logData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Specified_EndIndex_With_Requested_Elements_All()
        {
            new SampleDataTests()
                .AddSampleData(TestContext);

            var queryIn = @"
                           <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">
                                    <endDateTimeIndex>2016-12-15T21:51:15.0000000+00:00</endDateTimeIndex>                                                                        
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=all");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);
            Assert.IsTrue(logList.Log[0].EndDateTimeIndex.HasValue);
            Assert.IsTrue(logList.Log[0].StartDateTimeIndex.HasValue);

            var logData = logList.Log[0].LogData;
            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(9, logData[0].MnemonicList.Split(',').Length);
            Assert.AreEqual(24, logData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Specified_MnemonicList_And_StartIndex_With_Requested_Elements_Requested()
        {
            new SampleDataTests()
                .AddSampleData(TestContext);

            var queryIn = @"
                           <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">                                    
                                    <startDateTimeIndex>2016-12-16T06:15:45.0000000+00:00</startDateTimeIndex>                                    
                                    <logData>            
                                        <mnemonicList>TIME,SSAS,WDSSA,RCBROP</mnemonicList>
                                    </logData>
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);
            Assert.IsTrue(logList.Log[0].StartDateTimeIndex.HasValue);
            Assert.IsFalse(logList.Log[0].EndDateTimeIndex.HasValue);

            var logData = logList.Log[0].LogData;
            Assert.AreEqual(1, logData.Count);            
            Assert.AreEqual(4, logData[0].MnemonicList.Split(',').Length);
            Assert.AreEqual(27, logData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Specified_MnemonicList_And_StartIndex_With_Requested_Elements_All()
        {
            new SampleDataTests()
                .AddSampleData(TestContext);

            var queryIn = @"
                           <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">
                                    <startDateTimeIndex>2016-12-16T06:15:45.0000000+00:00</startDateTimeIndex>                                    
                                    <logData>            
                                        <mnemonicList>TIME,SSAS,WDSSA,RCBROP</mnemonicList>
                                    </logData>
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=all");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);
            Assert.IsTrue(logList.Log[0].StartDateTimeIndex.HasValue);
            Assert.IsTrue(logList.Log[0].EndDateTimeIndex.HasValue);

            var logData = logList.Log[0].LogData;
            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(4, logData[0].MnemonicList.Split(',').Length);
            Assert.AreEqual(27, logData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Specified_MnemonicList_And_EndIndex_With_Requested_Elements_Requested()
        {
            new SampleDataTests()
                .AddSampleData(TestContext);

            var queryIn = @"
                           <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">
                                    <endDateTimeIndex>2016-12-15T21:51:30.0000000+00:00</endDateTimeIndex>
                                    <logData>
                                        <mnemonicList>TIME,SSAS,WDSSA,RCBROP,BAS,WDBA</mnemonicList>
                                    </logData>
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);
            
            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.IsFalse(logList.Log[0].StartDateTimeIndex.HasValue);
            Assert.IsTrue(logList.Log[0].EndDateTimeIndex.HasValue);
            Assert.AreEqual(1, logList.Log.Count);

            var logData = logList.Log[0].LogData;
            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(5, logData[0].MnemonicList.Split(',').Length);
            Assert.AreEqual(39, logData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Specified_MnemonicList_And_EndIndex_With_Requested_Elements_All()
        {
            new SampleDataTests()
                .AddSampleData(TestContext);

            var queryIn = @"
                           <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">
                                    <endDateTimeIndex>2016-12-15T21:51:30.0000000+00:00</endDateTimeIndex>
                                    <logData>
                                        <mnemonicList>TIME,SSAS,WDSSA,RCBROP,BAS,WDBA</mnemonicList>
                                    </logData>
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=all");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);
            Assert.IsTrue(logList.Log[0].StartDateTimeIndex.HasValue);
            Assert.IsTrue(logList.Log[0].EndDateTimeIndex.HasValue);

            var logData = logList.Log[0].LogData;
            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(5, logData[0].MnemonicList.Split(',').Length);
            Assert.AreEqual(39, logData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Specified_StartIndex_And_EndIndex_With_Requested_Elements_Requested()
        {
            new SampleDataTests()
                .AddSampleData(TestContext);

            var queryIn = @"
                            <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">
                                    <startDateTimeIndex>2016-12-15T21:50:59.0000000+00:00</startDateTimeIndex>  
                                    <endDateTimeIndex>2016-12-15T21:51:11.0000000+00:00</endDateTimeIndex>                                    
                                    <logData>
                                        <mnemonicList/>
                                    </logData>
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);
            
            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);

            var logData = logList.Log[0].LogData;
            Assert.AreEqual(1, logList.Log.Count);
            Assert.IsTrue(logList.Log[0].StartDateTimeIndex.HasValue);
            Assert.IsTrue(logList.Log[0].EndDateTimeIndex.HasValue);
            Assert.IsFalse(logList.Log[0].Direction.HasValue);

            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(9, logData[0].MnemonicList.Split(',').Length);
            Assert.AreEqual(13, logData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Specified_StartIndex_And_EndIndex_With_Requested_Elements_All()
        {
            new SampleDataTests()
                .AddSampleData(TestContext);

            var queryIn = @"
                            <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">
                                    <startDateTimeIndex>2016-12-15T21:50:59.0000000+00:00</startDateTimeIndex>  
                                    <endDateTimeIndex>2016-12-15T21:51:11.0000000+00:00</endDateTimeIndex>                                                                        
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=all");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);

            var logData = logList.Log[0].LogData;
            Assert.AreEqual(1, logList.Log.Count);
            Assert.IsTrue(logList.Log[0].StartDateTimeIndex.HasValue);
            Assert.IsTrue(logList.Log[0].EndDateTimeIndex.HasValue);
            Assert.IsTrue(logList.Log[0].Direction.HasValue);

            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(9, logData[0].MnemonicList.Split(',').Length);
            Assert.AreEqual(13, logData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Specified_MnemonicList_And_StartIndex_And_EndIndex_With_Requested_Elements_Requested()
        {
            new SampleDataTests()
                 .AddSampleData(TestContext);

            var queryIn = @"
                            <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">
                                    <startDateTimeIndex>2016-12-15T21:50:59.0000000+00:00</startDateTimeIndex>  
                                    <endDateTimeIndex>2016-12-15T21:51:11.0000000+00:00</endDateTimeIndex>                                    
                                    <logData>
                                        <mnemonicList>TIME,SSAS,WDSSA,ROPS</mnemonicList>
                                    </logData>
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);

            var logData = logList.Log[0].LogData;
            Assert.AreEqual(1, logList.Log.Count);
            Assert.IsTrue(logList.Log[0].StartDateTimeIndex.HasValue);
            Assert.IsTrue(logList.Log[0].EndDateTimeIndex.HasValue);
            Assert.IsFalse(logList.Log[0].Direction.HasValue);

            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(4, logData[0].MnemonicList.Split(',').Length);
            Assert.AreEqual(13, logData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_Specified_MnemonicList_And_StartIndex_And_EndIndex_With_Requested_Elements_All()
        {
            new SampleDataTests()
                 .AddSampleData(TestContext);

            var queryIn = @"
                            <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">
                                    <startDateTimeIndex>2016-12-15T21:50:59.0000000+00:00</startDateTimeIndex>  
                                    <endDateTimeIndex>2016-12-15T21:51:11.0000000+00:00</endDateTimeIndex>                                    
                                    <logData>
                                        <mnemonicList>TIME,SSAS,WDSSA,ROPS</mnemonicList>
                                    </logData>
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=all");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);

            var logData = logList.Log[0].LogData;
            Assert.AreEqual(1, logList.Log.Count);
            Assert.IsTrue(logList.Log[0].StartDateTimeIndex.HasValue);
            Assert.IsTrue(logList.Log[0].EndDateTimeIndex.HasValue);
            Assert.IsTrue(logList.Log[0].Direction.HasValue);

            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(4, logData[0].MnemonicList.Split(',').Length);
            Assert.AreEqual(13, logData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Query_Empty_And_Data_Curves_From_Log_With_Empty_Channel()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 5);

            DevKit.AddAndAssert(Log);

            var query = DevKit.CreateLog(Log);
            query.LogData = new List<LogData>() { new LogData() };
            query.LogData.First().MnemonicList = "ROP, GR";

            var resultLog = DevKit.GetAndAssert(query, true, null, true);
            var logData = resultLog.LogData;

            Assert.IsNotNull(logData);
            Assert.AreEqual(1, logData.Count);

            var mnemonicList = logData[0].MnemonicList.Split(',');
            Assert.AreEqual(2, mnemonicList.Length);
            Assert.AreEqual(5, logData[0].Data.Count);
            Assert.AreEqual("MD", mnemonicList[0]);
            Assert.AreEqual("GR", mnemonicList[1]);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Header_Only_No_Data()
        {
            int numRows = 10;

            // Add the Setup Well, Wellbore and Log to the store.
            var logResponse = AddSetupWellWellboreLog(numRows, isDepthLog: true, hasEmptyChannel: false,
                increasing: true);
            Assert.AreEqual((short)ErrorCodes.Success, logResponse.Result);

            var queryHeaderOnly = DevKit.CreateLog(logResponse.SuppMsgOut, null, Log.UidWell, null, Log.UidWellbore, null);

            // Perform a GetFromStore with multiple log queries
            var result = DevKit.Get<LogList, Log>(
                DevKit.List(queryHeaderOnly),
                ObjectTypes.Log,
                null,
                OptionsIn.ReturnElements.HeaderOnly);

            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            Assert.IsNotNull(logList);
            Assert.AreEqual(1, logList.Log.Count);
            Assert.AreEqual(0, logList.Log[0].LogData.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Header_Only_Query_As_Requested()
        {
            int numRows = 10;

            // Add the Setup Well, Wellbore and Log to the store.
            var logResponse = AddSetupWellWellboreLog(numRows, isDepthLog: true, hasEmptyChannel: false,
                increasing: true);
            Assert.AreEqual((short)ErrorCodes.Success, logResponse.Result);
            var queryIn = "<logs version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                            $"  <log uidWell=\"{Log.UidWell}\" uidWellbore=\"{Log.UidWellbore}\" uid=\"{logResponse.SuppMsgOut}\">" + Environment.NewLine +
                            "    <nameWell />" + Environment.NewLine +
                            "    <nameWellbore />" + Environment.NewLine +
                            "    <name />" + Environment.NewLine +
                            "    <objectGrowing />" + Environment.NewLine +
                            "    <serviceCompany />" + Environment.NewLine +
                            "    <runNumber />" + Environment.NewLine +
                            "    <creationDate />" + Environment.NewLine +
                            "    <indexType />" + Environment.NewLine +
                            "    <startIndex uom=\"\" />" + Environment.NewLine +
                            "    <endIndex uom=\"\" />" + Environment.NewLine +
                            "    <startDateTimeIndex />" + Environment.NewLine +
                            "    <endDateTimeIndex />" + Environment.NewLine +
                            "    <direction />" + Environment.NewLine +
                            "    <indexCurve />" + Environment.NewLine +
                            "    <logCurveInfo uid=\"\">" + Environment.NewLine +
                            "      <mnemonic />" + Environment.NewLine +
                            "      <unit />" + Environment.NewLine +
                            "      <minIndex uom=\"\" />" + Environment.NewLine +
                            "      <maxIndex uom=\"\" />" + Environment.NewLine +
                            "      <minDateTimeIndex />" + Environment.NewLine +
                            "      <maxDateTimeIndex />" + Environment.NewLine +
                            "      <curveDescription />" + Environment.NewLine +
                            "      <typeLogData />" + Environment.NewLine +
                            "    </logCurveInfo>" + Environment.NewLine +
                            "  </log>" + Environment.NewLine +
                            "</logs>";
            var result = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);
            Assert.IsNotNull(result);
            var logs = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            Assert.IsNotNull(logs);
            var log = logs.Log.FirstOrDefault();
            Assert.IsNotNull(log);
            Assert.AreEqual(log.LogCurveInfo.Count, 3);
            Assert.AreEqual(log.LogData.Count, 0);
        }

        [TestMethod]
        public void LogDataAdapter_GetFromStore_Return_Latest_N_Values()
        {
            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var channel3 = Log.LogCurveInfo[1];
            var channel4 = Log.LogCurveInfo[2];

            Log.LogCurveInfo.Add(new LogCurveInfo
            {
                Uid = "ROP1",
                Unit = channel3.Unit,
                TypeLogData = LogDataType.@double,
                Mnemonic = new ShortNameStruct
                {
                    Value = "ROP1"
                }
            });

            Log.LogCurveInfo.Add(new LogCurveInfo
            {
                Uid = "GR1",
                Unit = channel4.Unit,
                TypeLogData = LogDataType.@double,
                Mnemonic = new ShortNameStruct
                {
                    Value = "GR1"
                }
            });

            var logData = new LogData
            {
                MnemonicList = DevKit.Mnemonics(Log),
                UnitList = DevKit.Units(Log),
                Data = new List<string> {"0,,0.2,0.3,", "1,,1.2,,1.4", "2,,2.2,,2.4", "3,,3.2,,", "4,,4.2,,"}
            };
            Log.LogData = new List<LogData> {logData};

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var query = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All + ';' + OptionsIn.RequestLatestValues.Eq(1));
            Assert.IsNotNull(results);

            var result = results.First();
            Assert.IsNotNull(result);

            logData = result.LogData.First();
            Assert.IsNotNull(logData);
            Assert.IsTrue(logData.Data.Count > 0);

            var data = new Dictionary<int, List<string>>();
            foreach (var row in logData.Data)
            {
                var points = row.Split(',');
                for (var i = 1; i < points.Length; i++)
                {
                    if (!data.ContainsKey(i))
                        data.Add(i, new List<string>());

                    if (!string.IsNullOrWhiteSpace(points[i]))
                        data[i].Add(points[i]);
                }
            }

            foreach (KeyValuePair<int, List<string>> pairs in data)
            {
                Assert.AreEqual(1, pairs.Value.Count);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_With_Custom_Data_Delimiter()
        {
            var delimiter = "|";
            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);

            // Set data delimiter to other charactrer than ","
            Log.DataDelimiter = delimiter;

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10, hasEmptyChannel: false);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var query = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                DataDelimiter = delimiter
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

            // Assert data delimiter
            Assert.AreEqual(delimiter, result.DataDelimiter);

            var data = result.LogData.FirstOrDefault()?.Data;
            Assert.IsNotNull(data);

            var channelCount = Log.LogCurveInfo.Count;

            // Assert data delimiter in log data
            foreach (var row in data)
            {
                var points = ChannelDataReader.Split(row, delimiter);
                Assert.AreEqual(channelCount, points.Length);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Request_Multiple_Recurring_Items_With_Empty_Value()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth, false);
            var responseAddLog = DevKit.Add<LogList, Log>(Log);
            Assert.IsNotNull(responseAddLog);
            Assert.AreEqual((short)ErrorCodes.Success, responseAddLog.Result);

            var queryIn = @"
                            <logs xmlns=""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">      
                                <log uid=""" + Log.Uid + @""">
                                    <logCurveInfo> 
                                        <mnemonic>ROP</mnemonic> 
                                        <unit /> 
                                    </logCurveInfo>
                                    <logCurveInfo>
                                        <mnemonic>{0}</mnemonic> 
                                        <unit /> 
                                    </logCurveInfo>
                                </log>
                            </logs>";

            var response = DevKit.GetFromStore(ObjectTypes.Log, string.Format(queryIn, "GR"), null, null);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var responseError = DevKit.GetFromStore(ObjectTypes.Log, string.Format(queryIn, ""), null, null);
            Assert.IsNotNull(responseError);
            Assert.AreEqual((short)ErrorCodes.RecurringItemsEmptySelection, responseError.Result);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Can_Get_Data_With_WhiteSpace_In_MnemonicList()
        {
            new SampleDataTests()
                .AddSampleData(TestContext);

            var queryIn = @"
                           <logs xmlns =""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <log uidWell=""490251090200"" uidWellbore=""62-TpX-11"" uid=""490251090200_13346"">
                                    <logData>
                                        <mnemonicList>TIME,  SSAS ,  WDSSA,RCBROP  ,
                                        BAS,WDBA
                                        </mnemonicList>
                                    </logData>
                                </log>
                           </logs>";

            var results = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=data-only");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(results.XMLout);
            Assert.AreEqual(1, logList.Log.Count);

            var logData = logList.Log[0].LogData;
            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(6, logData[0].MnemonicList.Split(',').Length);
            Assert.AreEqual(510, logData[0].Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Recurring_By_AxisDefinition()
        {
            AddParents();

            // Create a log with a 16 count arrayCurve1
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("arrayCurve", "unitless"));
            var arrayCurve1 = Log.LogCurveInfo.First(x => x.Mnemonic.Value == "arrayCurve");
            arrayCurve1.AxisDefinition = new List<AxisDefinition>()
            {
                new AxisDefinition()
                {
                    Uid = DevKit.Uid(),
                    Order = 1,
                    Count = 16,
                    DoubleValues = "1 2"
                }
            };

            DevKit.AddAndAssert(Log);

            // Create a log with a 64 count arrayCurve2
            var log2 = DevKit.CreateLog(Log);
            log2.Uid = DevKit.Uid();
            DevKit.InitHeader(log2, LogIndexType.measureddepth);
            log2.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("arrayCurve2", "g/cm3"));
            var arrayCurve2 = log2.LogCurveInfo.First(x => x.Mnemonic.Value == "arrayCurve2");
            arrayCurve2.AxisDefinition = new List<AxisDefinition>()
            {
                new AxisDefinition()
                {
                    Uid = DevKit.Uid(),
                    Order = 1,
                    Count = 64,
                    DoubleValues = "1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31 32 33 34 35 36 37 38 39 40 41 42 43 44 45 46 47 48 49 50 51 52 53 54 55 56 57 58 59 60 61 62 63 64"
                }
                ,
                new AxisDefinition()
                {
                    Uid = DevKit.Uid(),
                    Order = 2,
                    Count = 64,
                }
            };

            DevKit.AddAndAssert(log2);

            // Create another log with a 64 count arrayCurve2
            var log3 = DevKit.CreateLog(Log);
            log3.Uid = DevKit.Uid();
            DevKit.InitHeader(log3, LogIndexType.measureddepth);
            log3.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("arrayCurve2", "g/cm3"));
            var arrayCurve3 = log3.LogCurveInfo.First(x => x.Mnemonic.Value == "arrayCurve2");
            arrayCurve3.AxisDefinition = new List<AxisDefinition>()
            {
                new AxisDefinition()
                {
                    Uid = DevKit.Uid(),
                    Order = 1,
                    Count = 64,
                    DoubleValues = "1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31 32 33 34 35 36 37 38 39 40 41 42 43 44 45 46 47 48 49 50 51 52 53 54 55 56 57 58 59 60 61 62 63 64"
                }
            };

            DevKit.AddAndAssert(log3);

            // Query for log 1
            var objectTemplate = CreateLogTemplateQuery(Log);

            // Set the count element value to 16
            DevKit.Template.Set(objectTemplate, "//count", 16);

            var result = DevKit.GetFromStore(ObjectTypes.Log, objectTemplate.ToString(), null, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            AssertAxisDefintion(Log, arrayCurve1, result.XMLout);

            // Create a new log template
            objectTemplate = CreateLogTemplateQuery();

            // Set the count element value to 64
            DevKit.Template.Set(objectTemplate, "//count", 64);

            result = DevKit.GetFromStore(ObjectTypes.Log, objectTemplate.ToString(), null, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            AssertAxisDefintion(log2, arrayCurve2, result.XMLout);
            AssertAxisDefintion(log3, arrayCurve3, result.XMLout);

            // Create a new log template
            objectTemplate = CreateLogTemplateQuery();

            // Query for logs that contain curve ROP or arrayCurve2
            DevKit.Template.Set(objectTemplate, "//mnemonic", "ROP");

            var lci = DevKit.Template.Clone(objectTemplate, "//logCurveInfo");
            DevKit.Template.Set(lci, "//mnemonic", arrayCurve2.Mnemonic.Value);
            DevKit.Template.Push(objectTemplate, "//logCurveInfo", lci.Root);

            result = DevKit.GetFromStore(ObjectTypes.Log, objectTemplate.ToString(), null, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            AssertAxisDefintion(log2, arrayCurve2, result.XMLout, 3);
            AssertAxisDefintion(log3, arrayCurve3, result.XMLout, 3);
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Recurring_By_LogParam()
        {
            AddParents();

            // Create a log with a 2 logParams
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogParam = new List<IndexedObject>()
            {
                DevKit.IndexedObject(1, 1),
                DevKit.IndexedObject(2, 2)
            };

            DevKit.AddAndAssert(Log);

            // Create a log with a 2 logParams
            var log2 = DevKit.CreateLog(Log);
            log2.Uid = DevKit.Uid();
            DevKit.InitHeader(log2, LogIndexType.measureddepth);
            log2.LogParam = new List<IndexedObject>()
            {
                DevKit.IndexedObject(3, 1),
                DevKit.IndexedObject(2, 2)
            };

            DevKit.AddAndAssert(log2);

            // Create a log with a 2 logParams
            var log3 = DevKit.CreateLog(Log);
            log3.Uid = DevKit.Uid();
            DevKit.InitHeader(log3, LogIndexType.measureddepth);
            log3.LogParam = new List<IndexedObject>()
            {
                DevKit.IndexedObject(3, 1),
                DevKit.IndexedObject(4, 2)
            };

            DevKit.AddAndAssert(log3);

            var logParam1 = Log.LogParam.FirstOrDefault(x => x.Name == "Test1");
            var logParam2 = Log.LogParam.FirstOrDefault(x => x.Name == "Test2");
            var logParam3 = log3.LogParam.FirstOrDefault(x => x.Name == "Test3");
            var logParam4 = log3.LogParam.FirstOrDefault(x => x.Name == "Test4");

            Assert.IsNotNull(logParam1);
            Assert.IsNotNull(logParam2);
            Assert.IsNotNull(logParam3);
            Assert.IsNotNull(logParam4);

            // Query for log 1
            var objectTemplate = CreateLogTemplateQuery(Log);

            // Set the param value to test1
            DevKit.Template.Set(objectTemplate, "//logParam/@name", logParam1.Name);

            var result = DevKit.GetFromStore(ObjectTypes.Log, objectTemplate.ToString(), null, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            AssertLogParam(Log, logParam1, result.XMLout);

            // Query for logs that have logParam2
            objectTemplate = CreateLogTemplateQuery();

            // Set the param value to test1
            DevKit.Template.Set(objectTemplate, "//logParam/@name", logParam2.Name);

            result = DevKit.GetFromStore(ObjectTypes.Log, objectTemplate.ToString(), null, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            AssertLogParam(Log, logParam2, result.XMLout);
            AssertLogParam(log2, logParam2, result.XMLout);

            // Query for logs that have logParams with the value of 13.0 or 14.0
            objectTemplate = CreateLogTemplateQuery();

            // Set the param value to test1
            var logParam = DevKit.Template.Clone(objectTemplate, "//logParam");
            DevKit.Template.Set(logParam, "//logParam", logParam4.Value);
            DevKit.Template.Push(objectTemplate, "//logParam", logParam.Root);

            DevKit.Template.Set(objectTemplate, "//logParam", logParam3.Value);

            result = DevKit.GetFromStore(ObjectTypes.Log, objectTemplate.ToString(), null, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            AssertLogParam(log2, logParam3, result.XMLout);
            AssertLogParam(log3, logParam3, result.XMLout);
            AssertLogParam(log3, logParam4, result.XMLout);
        }
        
        #region Helper Methods

        private XDocument CreateLogTemplateQuery(Log log = null, bool includeData = false)
        {
            var document = DevKit.Template.Create<LogList>();

            Assert.IsNotNull(document);
            Assert.IsNotNull(document.Root);

            // Remove log data
            if (!includeData)
                DevKit.Template.Remove(document, "//logData");

            // If log is not null set the UIDs
            if (log != null)
                DevKit.SetDocumentUids(log, document);

            return document;
        }

        private void AssertAxisDefintion(Log expectedLog, LogCurveInfo expectedCurve, string xmlOut, int expectedCurveCount = 2)
        {
            var logList = EnergisticsConverter.XmlToObject<LogList>(xmlOut);
            Assert.IsNotNull(logList);
            Assert.IsTrue(logList.Log.Count > 0);

            var log = logList.Log.FirstOrDefault(x => x.Uid == expectedLog.Uid);
            Assert.IsNotNull(log);
            Assert.AreEqual(expectedCurveCount, log.LogCurveInfo.Count);

            var logCurveInfo = log.LogCurveInfo.FirstOrDefault(x => x.Mnemonic.Value == expectedCurve.Mnemonic.Value);
            Assert.IsNotNull(logCurveInfo);
            Assert.IsTrue(logCurveInfo.AxisDefinition.Count > 0);

            var axisDef = logCurveInfo.AxisDefinition.FirstOrDefault(x => x.Uid == expectedCurve.AxisDefinition[0].Uid);
            Assert.IsNotNull(axisDef);
            Assert.AreEqual(expectedCurve.AxisDefinition[0].Count, axisDef.Count);
            Assert.AreEqual(expectedCurve.AxisDefinition[0].DoubleValues, axisDef.DoubleValues);
        }

        private static void AssertLogParam(Log expectedLog, IndexedObject expectedLogParam, string xmlOut)
        {
            var logList = EnergisticsConverter.XmlToObject<LogList>(xmlOut);
            Assert.IsNotNull(logList);
            Assert.IsTrue(logList.Log.Count > 0);

            var log = logList.Log.FirstOrDefault(x => x.Uid == expectedLog.Uid);
            Assert.IsNotNull(log);

            var logParam = log.LogParam.FirstOrDefault(x => x.Name == expectedLogParam.Name);
            Assert.IsNotNull(logParam);
            Assert.AreEqual(expectedLogParam.Value, logParam.Value);
            Assert.AreEqual(expectedLogParam.Uom, logParam.Uom);
            Assert.AreEqual(expectedLogParam.Description, logParam.Description);
            Assert.AreEqual(expectedLogParam.Index, logParam.Index);
        }

        private WMLS_AddToStoreResponse AddSetupWellWellboreLog(int numRows, bool isDepthLog, bool hasEmptyChannel, bool increasing)
        {
            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);

            DevKit.InitHeader(Log, LogIndexType.measureddepth, increasing);

            var startIndex = new GenericMeasure { Uom = "m", Value = 100 };
            Log.StartIndex = startIndex;
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), numRows, 1, isDepthLog, hasEmptyChannel, increasing);

            // Add a log
            return DevKit.Add<LogList, Log>(Log);
        }

        private WMLS_AddToStoreResponse AddLogWithAction(int row, Action executeLogChanges = null,
            double factor = 1D, bool isDepthLog = true, bool hasEmptyChannel = true, bool increasing = true)
        {
            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);

            // Initialize the Log
            //var row = 10;
            DevKit.InitHeader(Log, isDepthLog ? LogIndexType.measureddepth : LogIndexType.datetime, increasing);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), row, factor, isDepthLog, hasEmptyChannel, increasing);

            executeLogChanges?.Invoke();

            return DevKit.Add<LogList, Log>(Log);
        }
        #endregion Helper Methods
    }
}
