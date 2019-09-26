//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Compatibility;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data.GrowingObjects;
using PDS.WITSMLstudio.Store.Jobs;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    /// <summary>
    /// Log141DataAdapter Update tests.
    /// </summary>
    [TestClass]
    public partial class Log141DataAdapterUpdateTests : Log141TestBase
    {
        private const int MicrosecondsPerSecond = 1000000;
        private const int GrowingTimeoutPeriod = 10;
        private string _dataDir;

        protected override void OnTestSetUp()
        {
            base.OnTestSetUp();

            // Test data directory
            _dataDir = new DirectoryInfo(@".\TestData").FullName;
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Supports_NaN_In_Numeric_Fields()
        {
            AddParents();
            // Add log
            Log.BhaRunNumber = 123;

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 3, hasEmptyChannel: false);

            Log.LogCurveInfo[0].ClassIndex = 1;
            Log.LogCurveInfo[1].ClassIndex = 2;

            DevKit.AddAndAssert(Log);

            // Update log
            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + Log.Uid + "\" uidWell=\"" + Log.UidWell + "\" uidWellbore=\"" + Log.UidWellbore + "\">" + Environment.NewLine +
                        "<bhaRunNumber>NaN</bhaRunNumber>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <classIndex>NaN</classIndex>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"ROP\">" + Environment.NewLine +
                        "  <classIndex>NaN</classIndex>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var updateResponse = DevKit.UpdateInStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query log
            var result = DevKit.GetAndAssert(Log);

            Assert.IsNull(result.BhaRunNumber);
            Assert.AreEqual(3, result.LogCurveInfo.Count);
            var logCurveInfoList = result.LogCurveInfo;

            var mdLogCurveInfo = logCurveInfoList.FirstOrDefault(x => x.Uid.Equals("MD"));
            Assert.IsNotNull(mdLogCurveInfo);
            Assert.IsNull(mdLogCurveInfo.ClassIndex);

            var ropLogCurveInfo = logCurveInfoList.FirstOrDefault(x => x.Uid.Equals("ROP"));
            Assert.IsNotNull(ropLogCurveInfo);
            Assert.IsNull(ropLogCurveInfo.ClassIndex);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Rollback_When_Updating_Invalid_Data()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var logData = Log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,");
            logData.Data.Add("15,15.1,15.2");

            DevKit.AddAndAssert(Log);

            var logAdded = DevKit.GetAndAssert(Log);
            Assert.IsNull(logAdded.Description);

            var logDataAdded = logAdded.LogData.First();
            for (var i = 0; i < logData.Data.Count; i++)
            {
                Assert.AreEqual(logData.Data[i], logDataAdded.Data[i]);
            }

            var update = new Log()
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                Description = "Should not be updated"
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);
            logData = update.LogData.First();
            logData.Data.Add("17,17.1,17.2");
            logData.Data.Add("21,21.1,21.2");
            logData.Data.Add("21,22.1,22.2");

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.NodesWithSameIndex, updateResponse.Result);

            var logUpdated = DevKit.GetAndAssert(Log);

            var logDataUpdated = logUpdated.LogData.First();
            for (var i = 0; i < logDataAdded.Data.Count; i++)
            {
                Assert.AreEqual(logDataAdded.Data[i], logDataUpdated.Data[i]);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_To_Append_With_Null_Indicator_In_Different_Chunks()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            AddParents();

            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            Log.NullValue = "-999.25";

            var logData = Log.LogData.First();
            logData.Data.Add("1700.0,17.1,-999.25");
            logData.Data.Add("1800.0,18.1,-999.25");
            logData.Data.Add("1900.0,19.1,-999.25");

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            DevKit.AddAndAssert(Log);

            //Update
            var updateLog = DevKit.CreateLog(Log.Uid, Log.Name, Log.UidWell, Log.NameWell, Log.UidWellbore, Log.NameWellbore);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2000.0,20.1,-999.25");
            logData.Data.Add("2100.0,21.1,-999.25");
            logData.Data.Add("2200.0,22.1,-999.25");
            logData.Data.Add("2300.0,23.1,23.2");

            var updateResponse = DevKit.Update<LogList, Log>(updateLog);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query
            var query = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            query.StartIndex = new GenericMeasure(1700, "ft");
            query.EndIndex = new GenericMeasure(2200, "ft");

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsNotNull(results);
            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(6, result.LogData[0].Data.Count);
            Assert.AreEqual(2, result.LogData[0].MnemonicList.Split(',').Length);

            var resultLogData = result.LogData[0].Data;
            double index = 17;
            foreach (var row in resultLogData)
            {
                var columns = row.Split(',');
                Assert.AreEqual(2, columns.Length);

                var outIndex = double.Parse(columns[0]);
                Assert.AreEqual(index * 100, outIndex);

                var outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                index++;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_With_Null_Indicator_And_Query_In_Range_Covers_Different_Chunks()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            AddParents();

            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            Log.NullValue = "-999.25";

            var logData = Log.LogData.First();
            logData.Data.Add("1700.0, 17.1, -999.25");
            logData.Data.Add("1800.0, 18.1, -999.25");
            logData.Data.Add("1900.0, 19.1, -999.25");
            logData.Data.Add("2000.0, 20.1,    20.1");
            logData.Data.Add("2100.0, 21.1,    21.1");

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            DevKit.AddAndAssert(Log);

            //Update
            var updateLog = DevKit.CreateLog(Log.Uid, Log.Name, Log.UidWell, Log.NameWell, Log.UidWellbore, Log.NameWellbore);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2000.0, 200.1, -999.25");
            logData.Data.Add("2100.0, 210.1, -999.25");
            logData.Data.Add("2200.0, 220.1,   22.1");

            var updateResponse = DevKit.Update<LogList, Log>(updateLog);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query
            var query = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            query.StartIndex = new GenericMeasure(1700, "ft");
            query.EndIndex = new GenericMeasure(2200, "ft");

            var result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(6, result.LogData[0].Data.Count);
            Assert.AreEqual(3, result.LogData[0].MnemonicList.Split(',').Length);

            var resultLogData = result.LogData[0].Data;

            Assert.IsTrue(resultLogData[0].Equals("1700,17.1,-999.25"));
            Assert.IsTrue(resultLogData[1].Equals("1800,18.1,-999.25"));
            Assert.IsTrue(resultLogData[2].Equals("1900,19.1,-999.25"));
            Assert.IsTrue(resultLogData[3].Equals("2000,200.1,20.1"));
            Assert.IsTrue(resultLogData[4].Equals("2100,210.1,21.1"));
            Assert.IsTrue(resultLogData[5].Equals("2200,220.1,22.1"));
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Replace_Range_In_Different_Chunks_And_With_Null_Indicator()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            AddParents();

            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            Log.NullValue = "-999.25";

            var logData = Log.LogData.First();
            logData.Data.Add("1700.0, 17.1, 17.2");
            logData.Data.Add("1800.0, 18.1, 18.2");
            logData.Data.Add("1900.0, 19.1, 19.2");
            logData.Data.Add("2000.0, 20.1, 20.1");
            logData.Data.Add("2100.0, 21.1, 21.1");

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            DevKit.AddAndAssert(Log);

            //Update
            var updateLog = DevKit.CreateLog(Log.Uid, Log.Name, Log.UidWell, Log.NameWell, Log.UidWellbore, Log.NameWellbore);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("1800.0, 180.1, -999.25");
            logData.Data.Add("2200.0, 220.1, 22.1");

            var updateResponse = DevKit.Update<LogList, Log>(updateLog);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query
            var query = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            query.StartIndex = new GenericMeasure(1700, "ft");
            query.EndIndex = new GenericMeasure(2200, "ft");

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.DataOnly);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(6, results[0].LogData[0].Data.Count);
            Assert.AreEqual(3, results[0].LogData[0].MnemonicList.Split(',').Length);

            var resultLogData = results[0].LogData[0].Data;

            Assert.IsTrue(resultLogData[0].Equals("1700,17.1,17.2"));
            Assert.IsTrue(resultLogData[1].Equals("1800,180.1,18.2"));
            Assert.IsTrue(resultLogData[2].Equals("1900,-999.25,19.2"));
            Assert.IsTrue(resultLogData[3].Equals("2000,-999.25,20.1"));
            Assert.IsTrue(resultLogData[4].Equals("2100,-999.25,21.1"));
            Assert.IsTrue(resultLogData[5].Equals("2200,220.1,22.1"));
        }

        /// <summary>
        /// To test concurrency lock for update: open 2 visual studio and debug the following test at the same time;
        /// lock one test, i.e. break at the commit statement and check if the 2nd thread is repeatedly checking if
        /// the transaction has been released every 2 seconds
        /// </summary>
        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Lock_Transaction()
        {
            Well.Uid = "ParentWell_Testing_Lock";
            Wellbore.UidWell = Well.Uid;
            Wellbore.Uid = "ParentWellbore_Testing_Lock";
            Log.UidWell = Well.Uid;
            Log.UidWellbore = Wellbore.Uid;
            Log.Uid = "Log_Testing_Lock";
            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var queryWell = new Well { Uid = Well.Uid };
            var resultWell = DevKit.Query<WellList, Well>(queryWell, optionsIn: OptionsIn.ReturnElements.All);
            if (resultWell.Count == 0)
            {
                DevKit.Add<WellList, Well>(Well);
            }

            var queryWellbore = new Wellbore { Uid = Wellbore.Uid, UidWell = Wellbore.UidWell };
            var resultWellbore = DevKit.Query<WellboreList, Wellbore>(queryWellbore, optionsIn: OptionsIn.ReturnElements.All);
            if (resultWellbore.Count == 0)
            {
                DevKit.Add<WellboreList, Wellbore>(Wellbore);
            }

            var queryLog = new Log { Uid = Log.Uid, UidWell = Log.UidWell, UidWellbore = Log.UidWellbore };
            var resultLog = DevKit.Query<LogList, Log>(queryLog, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            if (resultLog.Count == 0)
            {
                DevKit.Add<LogList, Log>(Log);
            }

            var update = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                Description = "Update Description"
            };

            DevKit.UpdateAndAssert(update);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Structural_Ranges_Ignored()
        {
            AddLogHeader(Log, LogIndexType.measureddepth);

            var result = DevKit.GetAndAssert(Log);
            Assert.IsNull(result.StartIndex);
            Assert.IsNull(result.EndIndex);

            Assert.AreEqual(Log.LogCurveInfo.Count, result.LogCurveInfo.Count);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.IsNull(curve.MinIndex);
                Assert.IsNull(curve.MaxIndex);
            }

            var update = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            DevKit.InitHeader(update, LogIndexType.measureddepth);

            update.StartIndex = new GenericMeasure { Uom = "m", Value = 1.0 };
            update.EndIndex = new GenericMeasure { Uom = "m", Value = 10.0 };

            foreach (var curve in update.LogCurveInfo)
            {
                curve.MinIndex = Log.StartIndex;
                curve.MaxIndex = Log.EndIndex;
            }

            DevKit.UpdateAndAssert(update);

            result = DevKit.GetAndAssert(Log);
            Assert.IsNotNull(result);
            Assert.IsNull(result.StartIndex);
            Assert.IsNull(result.EndIndex);

            Assert.AreEqual(Log.LogCurveInfo.Count, result.LogCurveInfo.Count);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.IsNull(curve.MinIndex);
                Assert.IsNull(curve.MaxIndex);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Index_Curve_Not_First_InLogCurveInfo()
        {
            AddLogHeader(Log, LogIndexType.measureddepth);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, null, 10);
            var logCurves = update.LogCurveInfo;
            var indexCurve = logCurves.First();
            logCurves.Remove(indexCurve);
            logCurves.Add(indexCurve);

            DevKit.UpdateAndAssert(update);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_Index_Range()
        {
            const int count = 10;
            AddLogWithData(Log, LogIndexType.measureddepth, 10, false);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure { Uom = "m", Value = 11 }, count, hasEmptyChannel: false);
            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);
            var start = Log.StartIndex.Value;
            var end = update.StartIndex.Value + count - 1;
            Assert.AreEqual(start, result.StartIndex.Value);
            Assert.AreEqual(end, result.EndIndex.Value);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(start, curve.MinIndex.Value);
                Assert.AreEqual(end, curve.MaxIndex.Value);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Append_LargeLog_Data_In_Different_Chunks()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            AddParents();

            // Add large log
            var logXmlIn = File.ReadAllText(Path.Combine(_dataDir, "LargeLog.xml"));

            var logList = EnergisticsConverter.XmlToObject<LogList>(logXmlIn);
            Assert.IsNotNull(logList);
            Assert.AreEqual(1, logList.Log.Count);

            var uidLog = DevKit.Uid();

            var log = logList.Log[0];
            log.Uid = uidLog;
            log.UidWell = Well.Uid;
            log.UidWellbore = Wellbore.Uid;

            DevKit.AddAndAssert(log);

            var result = DevKit.GetAndAssert(log);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(5000, result.LogData[0].Data.Count);

            // Update log by appending 10000 rows of data
            logXmlIn = File.ReadAllText(Path.Combine(_dataDir, "LargeLog_append.xml"));

            logList = EnergisticsConverter.XmlToObject<LogList>(logXmlIn);
            Assert.IsNotNull(logList);
            Assert.AreEqual(1, logList.Log.Count);

            log = logList.Log[0];
            log.UidWell = Well.Uid;
            log.UidWellbore = Wellbore.Uid;
            log.Uid = uidLog;

            DevKit.UpdateAndAssert(log);

            // Query log after appending data
            result = DevKit.GetAndAssert(log, errorCode: ErrorCodes.ParialSuccess);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(WitsmlSettings.LogMaxDataNodesGet, result.LogData[0].Data.Count);
        }

        /// <summary>
        /// This test is for bug 5782, 
        /// which is caused by incorrect updating of recurring elements, i.e. logCurveInfo.
        /// The updated log ends up with 1 logCurve being replaced by another logCurveInfo, hence
        /// there were 2 identical logCurveInfos in the log and it cause the exception
        /// when duplicated mnemonic is being added to the index map.
        /// The test log below has 50 curves.
        /// </summary>
        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_UpdateLogCurveInfo()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            for (var i = Log.LogCurveInfo.Count; i < 50; i++)
            {
                var mnemonic = $"Log-Curve-{i}";
                Log.LogCurveInfo.Add(DevKit.LogGenerator.CreateLogCurveInfo(mnemonic, "m", LogDataType.@double));
            }

            DevKit.AddAndAssert(Log);

            DevKit.UpdateAndAssert(Log);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_Nested_Recurring_Elements()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            // Create nested array elements
            var curve = Log.LogCurveInfo.Last();

            var extensionName1 = DevKit.ExtensionNameValue("Ext-1", "1.0", "m");
            var extensionName4 = DevKit.ExtensionNameValue("Ext-4", "4.0", "cm");

            curve.AxisDefinition = new List<AxisDefinition>
            {
                new AxisDefinition()
                {
                    Uid = "1",
                    Order = 1,
                    Count = 3,
                    DoubleValues = "1 2 3",
                    ExtensionNameValue = new List<ExtensionNameValue>
                    {
                        extensionName1,
                        DevKit.ExtensionNameValue("Ext-2", "2.0", "ft")
                    }
                }
            };

            curve.ExtensionNameValue = new List<ExtensionNameValue>
            {
                DevKit.ExtensionNameValue("Ext-3", "3.0", "mm"),
                extensionName4
            };

            // Add Log
            DevKit.AddAndAssert(Log);

            var result = DevKit.GetAndAssert(Log);
            AssertNestedElements(result, curve, extensionName1, extensionName4);

            // Update Log
            extensionName1 = DevKit.ExtensionNameValue("Ext-1", "1.1", "m");
            extensionName4 = DevKit.ExtensionNameValue("Ext-4", "4.4", "cm");

            var update = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore
            };

            curve.ExtensionNameValue = new List<ExtensionNameValue>
            {
                extensionName4
            };

            curve.AxisDefinition = new List<AxisDefinition>
            {
                new AxisDefinition()
                {
                    Uid = "1",
                    Order = 1,
                    Count = 3,
                    DoubleValues = "1 2 3",
                    ExtensionNameValue = new List<ExtensionNameValue>
                    {
                        extensionName1
                    }
                }
            };

            update.LogCurveInfo = new List<LogCurveInfo>
            {
                curve
            };

            DevKit.UpdateAndAssert(update);

            result = DevKit.GetAndAssert(Log);
            AssertNestedElements(result, curve, extensionName1, extensionName4);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_With_Custom_Data_Delimiter()
        {
            var delimiter = "|~";
            AddLogWithData(Log, LogIndexType.measureddepth, 10, false);

            var result = DevKit.GetAndAssert(Log);
            Assert.IsNotNull(result);

            // Assert null data delimiter
            Assert.IsNull(result.DataDelimiter);

            // Update data delimiter
            var update = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                DataDelimiter = delimiter
            };

            DevKit.UpdateAndAssert(update);

            result = DevKit.GetAndAssert(Log);

            // Assert data delimiter is updated
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


        [TestMethod, Description("A string value with a comma delimiter inside quotes.")]
        public void Log141DataAdapter_UpdateInStore_UpdateLog_With_Special_Character_Comma()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            Log.LogCurveInfo[1].TypeLogData = LogDataType.@string;
            Log.LogCurveInfo[1].Unit = "unitless";
            Log.LogData = new List<LogData>();

            // Add log
            DevKit.AddAndAssert(Log);

            var update = DevKit.CreateLog(Log);

            update.LogData = new List<LogData>()
            {
                new LogData()
                {
                    MnemonicList = string.Join(",", Log.LogCurveInfo.Select(x => x.Mnemonic.Value)),
                    UnitList = string.Join(",", Log.LogCurveInfo.Select(x => x.Unit)),
                    Data = new List<string>()
                    {
                        @"5000.0, ""A comma, in the value"",30.0",
                        @"5000.1, ""Another comma, in the value"",31.0",
                        @"5000.2, ""No comma in the value"",32.0",
                    }
                }
            };

            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.LogData.Count);

            result.LogData[0].Data.ForEach(x => DevKit.AssertDataRowWithQuotes(x, ",", 3));
        }

        [TestMethod, Description("A string value with a custom delimiter inside quotes.")]
        public void Log141DataAdapter_UpdateInStore_UpdateLog_With_Special_Character_Custom_Delimiter()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            Log.DataDelimiter = "|";
            Log.LogCurveInfo[1].TypeLogData = LogDataType.@string;
            Log.LogCurveInfo[1].Unit = "unitless";
            Log.LogData = new List<LogData>();

            // Add log
            DevKit.AddAndAssert(Log);

            var update = DevKit.CreateLog(Log);

            update.LogData = new List<LogData>()
            {
                new LogData()
                {
                    MnemonicList = string.Join(",", Log.LogCurveInfo.Select(x => x.Mnemonic.Value)),
                    UnitList = string.Join(",", Log.LogCurveInfo.Select(x => x.Unit)),
                    Data = new List<string>()
                    {
                        @"5000.0| ""A pipe| in the value""|30.0",
                        @"5000.1| ""Another pipe | in the value""|31.0",
                        @"5000.2| ""No comma in the value""|32.0",
                    }
                }
            };

            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.LogData.Count);

            result.LogData[0].Data.ForEach(x => DevKit.AssertDataRowWithQuotes(x, Log.DataDelimiter, 3));
        }


        /// <summary>
        /// This partial update tests the following scenario:
        /// A log has 1 index curve and 3 channels is added with only channel 2 has initial data [chunk index {1, 5000)];
        /// the 1st update add data for channel 1 [chunk index {5000, 10000)];
        /// the 2nd update add data for channel 3 [chunk index {1, 5000); {5000, 10000)]
        /// </summary>
        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Partial_Update_1()
        {
            var log = AddAnEmptyLogWithMoreCurves();

            var curves = log.LogCurveInfo;

            var indexCurve = curves[0];
            var channel1 = curves[1];
            var channel2 = curves[2];
            var channel3 = curves[3];

            var update = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);

            // Update 2rd channel
            update.LogData = new List<LogData>();

            var logData = new LogData
            {
                MnemonicList = indexCurve.Mnemonic.Value + "," + channel2.Mnemonic.Value,
                UnitList = indexCurve.Unit + "," + channel2.Unit
            };
            var data = new List<string> { "1,1.2", "2,2.2" };
            logData.Data = data;
            update.LogData.Add(logData);

            DevKit.UpdateAndAssert(update);

            // Update 1st channel with a different chunk
            update = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.LogData = new List<LogData>();

            logData = new LogData
            {
                MnemonicList = indexCurve.Mnemonic.Value + "," + channel1.Mnemonic.Value,
                UnitList = indexCurve.Unit + "," + channel1.Unit
            };
            data = new List<string> { "5002,5002.1", "5003,5003.1" };
            logData.Data = data;
            update.LogData.Add(logData);

            DevKit.UpdateAndAssert(update);

            // Update 3rd channel spanning the previous 2 chunks
            update = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.LogData = new List<LogData>();

            logData = new LogData
            {
                MnemonicList = indexCurve.Mnemonic.Value + "," + channel3.Mnemonic.Value,
                UnitList = indexCurve.Unit + "," + channel3.Unit
            };
            data = new List<string> { "1001,1001.3", "5001,5001.3", "5003,5003.3" };
            logData.Data = data;
            update.LogData.Add(logData);

            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);

            // Assert log data
            logData = result.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);

            data = logData.Data;
            Assert.IsNotNull(data);
            Assert.AreEqual(6, data.Count);
            Assert.AreEqual("1,,1.2,", data[0]);
            Assert.AreEqual("2,,2.2,", data[1]);
            Assert.AreEqual("1001,,,1001.3", data[2]);
            Assert.AreEqual("5001,,,5001.3", data[3]);
            Assert.AreEqual("5002,5002.1,,", data[4]);
            Assert.AreEqual("5003,5003.1,,5003.3", data[5]);
        }

        /// <summary>
        /// This partial update tests the following scenario:
        /// A log has 1 index curve and 3 channels is added with only channel 1 has initial data [chunk index {5000, 10000)];
        /// the 1st update add data for channel 3 [chunk index {1, 5000); {5000, 10000)];
        /// the 2nd update add data for channel 2 [chunk index {1, 5000)]
        /// </summary>
        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Partial_Update_2()
        {
            var log = AddAnEmptyLogWithMoreCurves();

            var curves = log.LogCurveInfo;

            var indexCurve = curves[0];
            var channel1 = curves[1];
            var channel2 = curves[2];
            var channel3 = curves[3];

            // Update 1st channel
            var update = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.LogData = new List<LogData>();

            var logData = new LogData
            {
                MnemonicList = indexCurve.Mnemonic.Value + "," + channel1.Mnemonic.Value,
                UnitList = indexCurve.Unit + "," + channel1.Unit
            };
            var data = new List<string> { "5002,5002.1", "5003,5003.1" };
            logData.Data = data;
            update.LogData.Add(logData);

            DevKit.UpdateAndAssert(update);

            // Update 3rd channel spanning the previous 2 chunks
            update = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.LogData = new List<LogData>();

            logData = new LogData
            {
                MnemonicList = indexCurve.Mnemonic.Value + "," + channel3.Mnemonic.Value,
                UnitList = indexCurve.Unit + "," + channel3.Unit
            };
            data = new List<string> { "1001,1001.3", "5001,5001.3", "5003,5003.3" };
            logData.Data = data;
            update.LogData.Add(logData);

            DevKit.UpdateAndAssert(update);

            // Update 2rd channel with a different chunk
            update = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.LogData = new List<LogData>();

            logData = new LogData
            {
                MnemonicList = indexCurve.Mnemonic.Value + "," + channel2.Mnemonic.Value,
                UnitList = indexCurve.Unit + "," + channel2.Unit
            };
            data = new List<string> { "1,1.2", "2,2.2" };
            logData.Data = data;
            update.LogData.Add(logData);

            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);

            // Assert log data
            logData = result.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);

            data = logData.Data;
            Assert.IsNotNull(data);
            Assert.AreEqual(6, data.Count);
            Assert.AreEqual("1,,1.2,", data[0]);
            Assert.AreEqual("2,,2.2,", data[1]);
            Assert.AreEqual("1001,,,1001.3", data[2]);
            Assert.AreEqual("5001,,,5001.3", data[3]);
            Assert.AreEqual("5002,5002.1,,", data[4]);
            Assert.AreEqual("5003,5003.1,,5003.3", data[5]);
        }

        /// <summary>
        /// This partial update tests the following scenario:
        /// A log has 1 index curve and 3 channels is added with only channel 3 has initial data [chunk index {1, 5000); {5000, 10000)];
        /// the 1st update add data for channel 3 [chunk index {1, 5000)];
        /// the 2nd update add data for channel 1 [chunk index {5000, 10000)] 
        /// </summary>
        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Partial_Update_3()
        {
            var log = AddAnEmptyLogWithMoreCurves();

            var curves = log.LogCurveInfo;

            var indexCurve = curves[0];
            var channel1 = curves[1];
            var channel2 = curves[2];
            var channel3 = curves[3];

            // Update 3rd channel spanning the previous 2 chunks
            var update = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.LogData = new List<LogData>();

            var logData = new LogData
            {
                MnemonicList = indexCurve.Mnemonic.Value + "," + channel3.Mnemonic.Value,
                UnitList = indexCurve.Unit + "," + channel3.Unit
            };
            var data = new List<string> { "1001,1001.3", "5001,5001.3", "5003,5003.3" };
            logData.Data = data;
            update.LogData.Add(logData);

            DevKit.UpdateAndAssert(update);

            // Update 2rd channel
            update = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.LogData = new List<LogData>();

            logData = new LogData
            {
                MnemonicList = indexCurve.Mnemonic.Value + "," + channel2.Mnemonic.Value,
                UnitList = indexCurve.Unit + "," + channel2.Unit
            };
            data = new List<string> { "1,1.2", "2,2.2" };
            logData.Data = data;
            update.LogData.Add(logData);

            DevKit.UpdateAndAssert(update);

            // Update 1st channel with a different chunk
            update = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.LogData = new List<LogData>();

            logData = new LogData
            {
                MnemonicList = indexCurve.Mnemonic.Value + "," + channel1.Mnemonic.Value,
                UnitList = indexCurve.Unit + "," + channel1.Unit
            };
            data = new List<string> { "5002,5002.1", "5003,5003.1" };
            logData.Data = data;
            update.LogData.Add(logData);

            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);

            // Assert log data
            logData = result.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);

            data = logData.Data;
            Assert.IsNotNull(data);
            Assert.AreEqual(6, data.Count);
            Assert.AreEqual("1,,1.2,", data[0]);
            Assert.AreEqual("2,,2.2,", data[1]);
            Assert.AreEqual("1001,,,1001.3", data[2]);
            Assert.AreEqual("5001,,,5001.3", data[3]);
            Assert.AreEqual("5002,5002.1,,", data[4]);
            Assert.AreEqual("5003,5003.1,,5003.3", data[5]);
        }

        /// <summary>
        /// This partial update tests the following scenario:
        /// A log has 1 index curve and 3 channels is added with only channel 3 has initial data [chunk index {1, 5000); {5000, 10000)];
        /// the 1st update add data for all channels [chunk index {1, 5000)];
        /// the 2nd update add data for channel 1 [chunk index {5000, 10000)] 
        /// </summary>
        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Partial_Update_4()
        {
            var log = AddAnEmptyLogWithMoreCurves();

            var curves = log.LogCurveInfo;

            var indexCurve = curves[0];
            var channel1 = curves[1];
            var channel3 = curves[3];

            // Update 3rd channel spanning the previous 2 chunks
            var update = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.LogData = new List<LogData>();

            var logData = new LogData
            {
                MnemonicList = indexCurve.Mnemonic.Value + "," + channel3.Mnemonic.Value,
                UnitList = indexCurve.Unit + "," + channel3.Unit
            };
            var data = new List<string> { "1001,1001.3", "5001,5001.3", "5003,5003.3" };
            logData.Data = data;
            update.LogData.Add(logData);

            DevKit.UpdateAndAssert(update);

            // Update All channel for 1st chunk
            update = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.LogData = new List<LogData>();

            logData = new LogData
            {
                MnemonicList = DevKit.Mnemonics(log),
                UnitList = DevKit.Units(log)
            };
            data = new List<string> { "1,1.1,1.2,", "2,2.1,2.2,2.3" };
            logData.Data = data;
            update.LogData.Add(logData);

            DevKit.UpdateAndAssert(update);

            // Update 1st channel with a different chunk
            update = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.LogData = new List<LogData>();

            logData = new LogData
            {
                MnemonicList = indexCurve.Mnemonic.Value + "," + channel1.Mnemonic.Value,
                UnitList = indexCurve.Unit + "," + channel1.Unit
            };
            data = new List<string> { "5002,5002.1", "5003,5003.1" };
            logData.Data = data;
            update.LogData.Add(logData);

            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);

            // Assert log data
            logData = result.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);

            data = logData.Data;
            Assert.IsNotNull(data);
            Assert.AreEqual(6, data.Count);
            Assert.AreEqual("1,1.1,1.2,", data[0]);
            Assert.AreEqual("2,2.1,2.2,2.3", data[1]);
            Assert.AreEqual("1001,,,1001.3", data[2]);
            Assert.AreEqual("5001,,,5001.3", data[3]);
            Assert.AreEqual("5002,5002.1,,", data[4]);
            Assert.AreEqual("5003,5003.1,,5003.3", data[5]);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_With_Sparse_Data()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var logData = Log.LogData.First();
            logData.Data.Add("1,1.1,1.2");
            logData.Data.Add("2,2.1,2.2");
            logData.Data.Add("3,3.1,3.2");
            logData.Data.Add("4,4.1,4.2");

            DevKit.AddAndAssert(Log);

            var update = new Log()
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                Description = "Should not be updated"
            };

            var indexCurve = Log.LogCurveInfo.FirstOrDefault();
            var channel1 = Log.LogCurveInfo[1];
            var channel2 = Log.LogCurveInfo[2];

            var logData1 = new LogData
            {
                MnemonicList = $"{indexCurve?.Mnemonic.Value},{channel1.Mnemonic.Value}",
                UnitList = $"{indexCurve?.Unit},{channel1.Unit}",
                Data = new List<string> { "2,2.11" }
            };

            var logData2 = new LogData
            {
                MnemonicList = $"{indexCurve?.Mnemonic.Value},{channel2.Mnemonic.Value}",
                UnitList = $"{indexCurve?.Unit},{channel2.Unit}",
                Data = new List<string> { "3,3.21" }
            };

            update.LogData = new List<LogData> { logData1, logData2 };

            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);

            logData = result.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);

            var data = logData.Data;
            Assert.AreEqual(4, logData.Data.Count);
            Assert.AreEqual("1,1.1,1.2", data[0]);
            Assert.AreEqual("2,2.11,2.2", data[1]);
            Assert.AreEqual("3,3.1,3.21", data[2]);
            Assert.AreEqual("4,4.1,4.2", data[3]);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Success_DataDelimiter()
        {
            var dataDelimiter = "#";

            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            DevKit.AddAndAssert(Log);

            var update = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                DataDelimiter = dataDelimiter
            };

            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(dataDelimiter, result.DataDelimiter);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_AppendLog_Data()
        {
            Log.StartIndex = new GenericMeasure(5, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(17, "m"), 6);
            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);
            var logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(16, logData.Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_PrependLog_Data()
        {
            Log.StartIndex = new GenericMeasure(17, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(5, "m"), 6);
            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);
            var logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(16, logData.Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_OverlappingLog_Data()
        {
            Log.StartIndex = new GenericMeasure(1, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 8);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(4.1, "m"), 3, 0.9);
            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);
            var logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(9, logData.Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Merge_Overlapping_Data()
        {
            AddParents();

            Log.StartDateTimeIndex = new Timestamp();
            Log.EndDateTimeIndex = new Timestamp();
            Log.LogData = DevKit.List(new LogData { Data = DevKit.List<string>() });

            //add data for cureves ROP, GR
            var logData = Log.LogData.First();
            logData.Data.Add("2016-12-16T15:17:14.5100106+00:00, 30.1, 40.2");
            logData.Data.Add("2016-12-16T15:17:15.5100106+00:00, 31.1, 41.2");
            logData.Data.Add("2016-12-16T15:17:16.5100106+00:00, 32.1, 42.2");
            logData.Data.Add("2016-12-16T15:17:17.5100106+00:00, 33.1, 43.2");
            logData.Data.Add("2016-12-16T15:17:18.5100106+00:00, 34.1, 44.2");

            DevKit.InitHeader(Log, LogIndexType.datetime);

            var additionalCurves = new List<LogCurveInfo>
            {
                DevKit.LogGenerator.CreateDoubleLogCurveInfo("MFIA", "galUS/min"),
                DevKit.LogGenerator.CreateDoubleLogCurveInfo("RPMA", "rpm")
            };
            Log.LogCurveInfo.AddRange(additionalCurves);

            DevKit.AddAndAssert(Log);
            var resultAdd = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, resultAdd.LogData.Count);
            Assert.AreEqual(5, resultAdd.LogData[0].Data.Count);

            var timeIndex = Log.LogCurveInfo.Find(c => c.Mnemonic.Value == Log.IndexCurve);
            var mnemonicListForUpdate = timeIndex.Mnemonic + "," + string.Join(",", additionalCurves.Select(x => x.Mnemonic));
            var unitListForUpdate = (timeIndex.TypeLogData.HasValue ? timeIndex.TypeLogData.Value + "," : string.Empty)
                + string.Join(",", additionalCurves.Select(x => x.Unit ?? string.Empty));

            // Update with data for additional curves MFIA, RPMA
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = mnemonicListForUpdate;
            updateLog.LogData[0].UnitList = unitListForUpdate;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-12-16T15:17:14.5100106+00:00, 335.1, 35.7");
            logData.Data.Add("2016-12-16T15:17:18.5100106+00:00, 335.1 ,");
            logData.Data.Add("2016-12-16T15:17:20.5100106+00:00, 335.1, 35.2");

            DevKit.UpdateAndAssert(updateLog);
            var resultUpdate = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, resultUpdate.LogData.Count);
            Assert.AreEqual(6, resultUpdate.LogData[0].Data.Count);

            // Update with overlapping data for merging
            var updateLogMerge = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLogMerge.LogData = DevKit.List(new LogData { Data = DevKit.List<string>() });
            updateLogMerge.LogData[0].MnemonicList = mnemonicListForUpdate;
            updateLogMerge.LogData[0].UnitList = unitListForUpdate;
            logData = updateLogMerge.LogData.First();
            logData.Data.Add("2016-12-16T15:17:17.5100106+00:00, 335.1, 35.7");
            logData.Data.Add("2016-12-16T15:17:24.5100106+00:00, 335.1, 33.5");

            DevKit.UpdateAndAssert(updateLogMerge);
            var resultMerge = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, resultMerge.LogData.Count);
            Assert.AreEqual(6, resultMerge.LogData[0].Data.Count);
            Assert.IsFalse(resultMerge.LogData[0].Data[4].Split(',').Contains("null"));
            Assert.IsTrue(resultMerge.LogData[0].Data[4].Split(',')[3] == string.Empty
                , "Set to empty for 2016-12-16T15:17:18.5100106+00:00 while merging at start: 2016-12-16T15:17:17.5100106+00:00");
            Assert.IsTrue(resultMerge.LogData[0].Data[5].Split(',')[0] == "2016-12-16T15:17:24.5100106+00:00"
                , "Row deleted for 2016-12-16T15:17:20.5100106+00:00 and new row added for 2016-12-16T15:17:24.5100106+00:00");
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_OverwriteLog_Data_Chunk()
        {
            Log.StartIndex = new GenericMeasure(17, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 6);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(4.1, "m"), 3, 0.9);
            var logData = update.LogData.First();
            logData.Data.Add("21.5, 1, 21.7");
            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);
            logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(5, logData.Data.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_Different_Data_Range_For_Each_Channel()
        {
            Log.StartIndex = new GenericMeasure(15, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 8);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(13, "m"), 6, 0.9);
            var logData = update.LogData.First();
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

            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);
            logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(11, logData.Data.Count);

            var data = logData.Data;
            Assert.AreEqual("15,15.1,15", data[2]);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_UpdateLog_Header()
        {
            AddParents();

            Log.Description = "Not updated field";
            Log.RunNumber = "101";
            Log.BhaRunNumber = 1;

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            DevKit.AddAndAssert(Log);

            var logAdded = DevKit.GetAndAssert(Log);
            Assert.IsNotNull(logAdded);
            Assert.AreEqual(Log.Description, logAdded.Description);
            Assert.AreEqual(Log.RunNumber, logAdded.RunNumber);
            Assert.AreEqual(Log.BhaRunNumber, logAdded.BhaRunNumber);
            Assert.IsNull(logAdded.CommonData.ItemState);

            var update = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            update.CommonData = new CommonData { ItemState = ItemState.actual };
            update.RunNumber = "102";
            update.BhaRunNumber = 2;

            DevKit.UpdateAndAssert(update);

            var logUpdated = DevKit.GetAndAssert(Log);
            Assert.IsNotNull(logUpdated);
            Assert.AreEqual(logAdded.Description, logUpdated.Description);
            Assert.AreEqual(update.RunNumber, logUpdated.RunNumber);
            Assert.AreEqual(update.BhaRunNumber, logUpdated.BhaRunNumber);
            Assert.AreEqual(update.CommonData.ItemState, logUpdated.CommonData.ItemState);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_UpdateLog_Curve()
        {
            AddParents();

            Log.Description = "Not updated field";
            Log.RunNumber = "101";
            Log.BhaRunNumber = 1;

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogCurveInfo.RemoveAt(2);
            Log.LogData.Clear();

            DevKit.AddAndAssert(Log);

            var logAdded = DevKit.GetAndAssert(Log);
            Assert.IsNotNull(logAdded);
            Assert.AreEqual(Log.Description, logAdded.Description);
            Assert.AreEqual(Log.RunNumber, logAdded.RunNumber);
            Assert.AreEqual(Log.BhaRunNumber, logAdded.BhaRunNumber);
            Assert.IsNull(logAdded.CommonData.ItemState);

            var logCurve = DevKit.GetLogCurveInfoByUid(logAdded.LogCurveInfo, "ROP") as LogCurveInfo;
            Assert.IsNotNull(logCurve);
            Assert.IsNull(logCurve.CurveDescription);

            var update = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            update.CommonData = new CommonData { ItemState = ItemState.actual };
            update.RunNumber = "102";
            update.BhaRunNumber = 2;

            DevKit.InitHeader(update, LogIndexType.measureddepth);
            update.LogCurveInfo.RemoveAt(2);
            update.LogCurveInfo.RemoveAt(0);
            update.LogData.Clear();
            var updateCurve = update.LogCurveInfo.First();
            updateCurve.CurveDescription = "Updated description";

            DevKit.UpdateAndAssert(update);

            var logUpdated = DevKit.GetAndAssert(Log);

            Assert.IsNotNull(logUpdated);
            Assert.AreEqual(logAdded.Description, logUpdated.Description);
            Assert.AreEqual(update.RunNumber, logUpdated.RunNumber);
            Assert.AreEqual(update.BhaRunNumber, logUpdated.BhaRunNumber);
            Assert.AreEqual(update.CommonData.ItemState, logUpdated.CommonData.ItemState);

            logCurve = DevKit.GetLogCurveInfoByUid(logUpdated.LogCurveInfo, "ROP") as LogCurveInfo;
            Assert.IsNotNull(logCurve);
            Assert.AreEqual(updateCurve.CurveDescription, logCurve.CurveDescription);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Add_curve()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogCurveInfo.RemoveRange(1, 2);
            Log.LogData.Clear();

            DevKit.AddAndAssert(Log);

            var logAdded = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, logAdded.LogCurveInfo.Count);
            Assert.AreEqual(Log.LogCurveInfo.Count, logAdded.LogCurveInfo.Count);

            var update = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            DevKit.InitHeader(update, LogIndexType.measureddepth);
            update.LogCurveInfo.RemoveAt(2);
            update.LogCurveInfo.RemoveAt(0);
            update.LogData.Clear();

            DevKit.UpdateAndAssert(update);

            var logUpdated = DevKit.GetAndAssert(Log);
            Assert.AreEqual(2, logUpdated.LogCurveInfo.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Index_Direction_Default_And_Update()
        {
            AddParents();

            Log.RunNumber = "101";
            Log.IndexCurve = "MD";
            Log.IndexType = LogIndexType.measureddepth;

            DevKit.InitHeader(Log, Log.IndexType.Value);
            Log.Direction = null;

            Assert.IsFalse(Log.Direction.HasValue);

            DevKit.AddAndAssert(Log);

            var logAdded = DevKit.GetAndAssert(Log);
            Assert.AreEqual(LogIndexDirection.increasing, logAdded.Direction);
            Assert.AreEqual(Log.RunNumber, logAdded.RunNumber);

            var update = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            update.Direction = LogIndexDirection.decreasing;
            update.RunNumber = "102";

            DevKit.UpdateAndAssert(update);

            var logUpdated = DevKit.GetAndAssert(Log);
            Assert.AreEqual(LogIndexDirection.increasing, logAdded.Direction);
            Assert.AreEqual(update.RunNumber, logUpdated.RunNumber);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_UpdateLog_Data_And_Index_Range()
        {
            Log.StartIndex = new GenericMeasure(15, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 8);

            // Make sure there are 3 curves
            var lciUids = Log.LogCurveInfo.Select(l => l.Uid).ToArray();
            Assert.AreEqual(3, lciUids.Length);

            var logAdded = DevKit.GetAndAssert(Log);
            Assert.AreEqual(15, logAdded.StartIndex.Value);
            Assert.AreEqual(22, logAdded.EndIndex.Value);

            // Check the range of the index curve
            var mdCurve = DevKit.GetLogCurveInfoByUid(logAdded.LogCurveInfo, logAdded.IndexCurve) as LogCurveInfo;
            Assert.IsNotNull(mdCurve);
            Assert.AreEqual(logAdded.StartIndex.Value, mdCurve.MinIndex.Value);
            Assert.AreEqual(logAdded.EndIndex.Value, mdCurve.MaxIndex.Value);

            // Look for the 2nd LogCurveInfo by Mnemonic.  It should be filtered out and not exist.
            var curve2 = DevKit.GetLogCurveInfoByUid(logAdded.LogCurveInfo, lciUids[1]) as LogCurveInfo;
            Assert.IsNull(curve2);

            // Check the range of the 3rd curve.
            var curve3 = DevKit.GetLogCurveInfoByUid(logAdded.LogCurveInfo, lciUids[2]) as LogCurveInfo;
            Assert.IsNotNull(curve3);
            Assert.AreEqual(logAdded.StartIndex.Value, curve3.MinIndex.Value);
            Assert.AreEqual(logAdded.EndIndex.Value, curve3.MaxIndex.Value);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(13, "m"), 6, 0.9);
            var logData = update.LogData.First();
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

            DevKit.UpdateAndAssert(update);

            var logUpdated = DevKit.GetAndAssert(Log);
            logData = logUpdated.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(11, logData.Data.Count);
            Assert.AreEqual(13, logUpdated.StartIndex.Value);
            Assert.AreEqual(23, logUpdated.EndIndex.Value);

            mdCurve = DevKit.GetLogCurveInfoByUid(logUpdated.LogCurveInfo, lciUids[0]) as LogCurveInfo;
            Assert.IsNotNull(mdCurve);
            Assert.AreEqual(logUpdated.StartIndex.Value, mdCurve.MinIndex.Value);
            Assert.AreEqual(logUpdated.EndIndex.Value, mdCurve.MaxIndex.Value);

            curve2 = DevKit.GetLogCurveInfoByUid(logUpdated.LogCurveInfo, lciUids[1]) as LogCurveInfo;
            Assert.IsNotNull(curve2);
            Assert.AreEqual(13, curve2.MinIndex.Value);
            Assert.AreEqual(20, curve2.MaxIndex.Value);

            curve3 = DevKit.GetLogCurveInfoByUid(logUpdated.LogCurveInfo, lciUids[2]) as LogCurveInfo;
            Assert.IsNotNull(curve3);
            Assert.AreEqual(15, curve3.MinIndex.Value);
            Assert.AreEqual(23, curve3.MaxIndex.Value);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_With_Unsequenced_Increasing_DepthLog_Data_In_Same_Chunk()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            AddParents();

            Log.StartIndex = new GenericMeasure(13, "ft");
            Log.EndIndex = new GenericMeasure(17, "ft");
            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            var logData = Log.LogData.First();
            logData.Data.Add("10,10.1,10.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("17,17.1,17.2");
            logData.Data.Add("18,18.1,18.2");

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            DevKit.AddAndAssert(Log);

            // Update
            var updateLog = DevKit.CreateLog(Log.Uid, Log.Name, Log.UidWell, Log.NameWell, Log.UidWellbore, Log.NameWellbore);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("12,12.1,12.2");
            logData.Data.Add("11,11.1,11.2");
            logData.Data.Add("14,14.1,14.2");

            DevKit.UpdateAndAssert(updateLog);

            // Query
            var result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(9, result.LogData[0].Data.Count);

            var resultLogData = result.LogData[0].Data;
            double index = 10;
            foreach (var row in resultLogData)
            {
                var columns = row.Split(',');
                var outIndex = double.Parse(columns[0]);
                Assert.AreEqual(index, outIndex);

                var outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                var outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index++;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_With_Unsequenced_Increasing_DepthLog_Data_In_Differnet_Chunk()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            AddParents();

            Log.StartIndex = new GenericMeasure(13, "ft");
            Log.EndIndex = new GenericMeasure(17, "ft");
            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            var logData = Log.LogData.First();
            logData.Data.Add("1700.0,17.1,17.2");
            logData.Data.Add("1800.0,18.1,18.2");
            logData.Data.Add("1900.0,19.1,19.2");

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            DevKit.AddAndAssert(Log);

            // Update
            var updateLog = DevKit.CreateLog(Log.Uid, Log.Name, Log.UidWell, Log.NameWell, Log.UidWellbore, Log.NameWellbore);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2000.0,20.1,20.2");
            logData.Data.Add("2300.0,23.1,23.2");
            logData.Data.Add("2200.0,22.1,22.2");
            logData.Data.Add("2100.0,21.1,21.2");
            logData.Data.Add("2400.0,24.1,24.2");

            DevKit.UpdateAndAssert(updateLog);

            // Query
            var result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(8, result.LogData[0].Data.Count);

            var resultLogData = result.LogData[0].Data;
            double index = 17;
            foreach (var row in resultLogData)
            {
                var columns = row.Split(',');
                var outIndex = double.Parse(columns[0]);
                Assert.AreEqual(index * 100, outIndex);

                var outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                var outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index++;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_With_Unsequenced_Decreasing_DepthLog_Data_In_Same_Chunk()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            AddParents();

            Log.StartIndex = new GenericMeasure(10, "ft");
            Log.EndIndex = new GenericMeasure(18, "ft");
            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            DevKit.InitHeader(Log, LogIndexType.measureddepth, false);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 5);
            var logData = Log.LogData.First();
            logData.Data.Clear();
            logData.Data.Add("19.0,19.1,19.2");
            logData.Data.Add("18.0,18.1,18.2");
            logData.Data.Add("17.0,17.1,17.2");

            DevKit.AddAndAssert(Log);

            // Update
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("21.0,21.1,21.2");
            logData.Data.Add("23.0,23.1,23.2");
            logData.Data.Add("22.0,22.1,22.2");
            logData.Data.Add("24.0,24.1,24.2");

            DevKit.UpdateAndAssert(updateLog);

            // Query
            var result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(7, result.LogData[0].Data.Count);

            var resultLogData = result.LogData[0].Data;

            var start = 0;
            for (var index = 24; index < 20; index--)
            {
                var columns = resultLogData[start].Split(',');
                var outIndex = double.Parse(columns[0]);
                Assert.AreEqual(index, outIndex);

                var outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                var outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                start++;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_With_Unsequenced_Decreasing_DepthLog_Data_In_Different_Chunk()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            AddParents();
            Log.StartIndex = new GenericMeasure(10, "ft");
            Log.EndIndex = new GenericMeasure(18, "ft");
            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            DevKit.InitHeader(Log, LogIndexType.measureddepth, false);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 5);
            var logData = Log.LogData.First();
            logData.Data.Clear();
            logData.Data.Add("1900.0,19.1,19.2");
            logData.Data.Add("1800.0,18.1,18.2");
            logData.Data.Add("1700.0,17.1,17.2");
            logData.Data.Add("1600.0,16.1,16.2");

            DevKit.AddAndAssert(Log);

            // Update
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2000.0,21.1,21.2");
            logData.Data.Add("2100.0,21.1,21.2");
            logData.Data.Add("2300.0,23.1,23.2");
            logData.Data.Add("2200.0,22.1,22.2");
            logData.Data.Add("2400.0,24.1,24.2");

            DevKit.UpdateAndAssert(updateLog);

            // Query
            var result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(9, result.LogData[0].Data.Count);

            var resultLogData = result.LogData[0].Data;

            var start = 0;
            for (var index = 24; index < 20; index--)
            {
                var columns = resultLogData[start].Split(',');
                var outIndex = double.Parse(columns[0]);
                Assert.AreEqual(index * 100, outIndex);

                var outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                var outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                start++;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_With_Unsequenced_Increasing_TimeLog_Data_In_Same_Chunk()
        {
            // Set the time range chunk size to number of microseconds equivalent to one day
            WitsmlSettings.TimeRangeSize = 86400000000;

            AddParents();

            Log.StartDateTimeIndex = new Timestamp();
            Log.EndDateTimeIndex = new Timestamp();
            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            var logData = Log.LogData.First();
            logData.Data.Add("2016-04-13T15:30:42.0000000-05:00,30.1,30.2");
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.1,32.2");

            DevKit.InitHeader(Log, LogIndexType.datetime);

            DevKit.AddAndAssert(Log);

            // Update
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-13T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-13T15:34:42.0000000-05:00,34.1,34.2");
            logData.Data.Add("2016-04-13T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-13T15:36:42.0000000-05:00,36.1,36.2");

            DevKit.UpdateAndAssert(updateLog);

            // Query
            var result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(7, result.LogData[0].Data.Count);

            var resultLogData = result.LogData[0].Data;
            var index = 30;
            DateTimeOffset? previousDateTime = null;
            foreach (var row in resultLogData)
            {
                var columns = row.Split(',');
                var outIndex = DateTimeOffset.Parse(columns[0]);
                Assert.AreEqual(index, outIndex.Minute);
                if (previousDateTime.HasValue)
                {
                    Assert.IsTrue((outIndex.ToUnixTimeMicroseconds() - previousDateTime.Value.ToUnixTimeMicroseconds()) == 60 * MicrosecondsPerSecond);
                }
                previousDateTime = outIndex;

                var outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                var outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index++;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_With_Unsequenced_Increasing_TimeLog_Data_In_Different_Chunk()
        {
            // Set the time range chunk size to number of microseconds equivalent to one day
            WitsmlSettings.TimeRangeSize = 86400000000;

            AddParents();

            Log.StartDateTimeIndex = new Timestamp();
            Log.EndDateTimeIndex = new Timestamp();
            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            var logData = Log.LogData.First();
            logData.Data.Add("2016-04-13T15:30:42.0000000-05:00,30.1,30.2");
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.1,32.2");

            DevKit.InitHeader(Log, LogIndexType.datetime);

            DevKit.AddAndAssert(Log);

            // Update
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-20T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-20T15:34:42.0000000-05:00,34.1,34.2");
            logData.Data.Add("2016-04-20T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-20T15:36:42.0000000-05:00,36.1,36.2");

            DevKit.UpdateAndAssert(updateLog);

            // Query
            var result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(7, result.LogData[0].Data.Count);

            var resultLogData = result.LogData[0].Data;
            var index = 33;
            DateTimeOffset? previousDateTime = null;
            for (var i = 3; i < resultLogData.Count; i++)
            {
                var columns = resultLogData[i].Split(',');
                var outIndex = DateTimeOffset.Parse(columns[0]);
                Assert.AreEqual(index, outIndex.Minute);
                if (previousDateTime.HasValue)
                {
                    Assert.IsTrue((outIndex.ToUnixTimeMicroseconds() - previousDateTime.Value.ToUnixTimeMicroseconds()) == 60 * MicrosecondsPerSecond);
                }
                previousDateTime = outIndex;

                var outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                var outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index++;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_With_Unsequenced_Decreasing_TimeLog_Data_In_Same_Chunk()
        {
            // Set the time range chunk size to number of microseconds equivalent to one day
            WitsmlSettings.TimeRangeSize = 86400000000;

            AddParents();

            Log.StartDateTimeIndex = new Timestamp();
            Log.EndDateTimeIndex = new Timestamp();
            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            var logData = Log.LogData.First();
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:30:42.0000000-05:00,30.1,30.2");

            DevKit.InitHeader(Log, LogIndexType.datetime, false);

            DevKit.AddAndAssert(Log);

            // Update
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-13T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-13T15:34:42.0000000-05:00,34.1,34.2");
            logData.Data.Add("2016-04-13T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-13T15:36:42.0000000-05:00,36.1,36.2");

            DevKit.UpdateAndAssert(updateLog);

            // Query
            var result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(7, result.LogData[0].Data.Count);

            var resultLogData = result.LogData[0].Data;
            var index = 36;
            DateTimeOffset? previousDateTime = null;
            foreach (var row in resultLogData)
            {
                var columns = row.Split(',');
                var outIndex = DateTimeOffset.Parse(columns[0]);
                Assert.AreEqual(index, outIndex.Minute);
                if (previousDateTime.HasValue)
                {
                    Assert.IsTrue((outIndex.ToUnixTimeMicroseconds() - previousDateTime.Value.ToUnixTimeMicroseconds()) == -60 * MicrosecondsPerSecond);
                }
                previousDateTime = outIndex;

                var outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                var outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index--;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_With_Unsequenced_Decreasing_TimeLog_Data_In_Different_Chunk()
        {
            // Set the time range chunk size to number of microseconds equivalent to one day
            WitsmlSettings.TimeRangeSize = 86400000000;

            AddParents();
            Log.StartDateTimeIndex = new Timestamp();
            Log.EndDateTimeIndex = new Timestamp();
            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            var logData = Log.LogData.First();
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:30:42.0000000-05:00,30.1,30.2");

            DevKit.InitHeader(Log, LogIndexType.datetime, false);

            DevKit.AddAndAssert(Log);

            // Update
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-10T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-10T15:34:42.0000000-05:00,34.1,34.2");
            logData.Data.Add("2016-04-10T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-10T15:36:42.0000000-05:00,36.1,36.2");

            DevKit.UpdateAndAssert(updateLog);

            // Query
            var result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(7, result.LogData[0].Data.Count);

            var resultLogData = result.LogData[0].Data;
            var index = 36;
            DateTimeOffset? previousDateTime = null;
            for (var i = 3; i < resultLogData.Count; i++)
            {
                var columns = resultLogData[i].Split(',');
                var outIndex = DateTimeOffset.Parse(columns[0]);
                Assert.AreEqual(index, outIndex.Minute);
                if (previousDateTime.HasValue)
                {
                    Assert.IsTrue((outIndex.ToUnixTimeMicroseconds() - previousDateTime.Value.ToUnixTimeMicroseconds()) == -60 * MicrosecondsPerSecond);
                }
                previousDateTime = outIndex;

                var outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                var outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index--;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Add_Complex_Element_During_Update()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var logData = Log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,");
            logData.Data.Add("15,15.1,15.2");

            DevKit.AddAndAssert(Log);

            var logAdded = DevKit.GetAndAssert(Log);
            Assert.IsNull(logAdded.StepIncrement);

            var logDataAdded = logAdded.LogData.First();
            for (var i = 0; i < logData.Data.Count; i++)
            {
                Assert.AreEqual(logData.Data[i], logDataAdded.Data[i]);
            }

            var update = new Log()
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                StepIncrement = new RatioGenericMeasure()
                {
                    Value = 1.0,
                    Uom = "m"
                }
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);

            DevKit.UpdateAndAssert(update);

            var logUpdated = DevKit.GetAndAssert(Log);

            var logDataUpdated = logUpdated.LogData.First();
            for (var i = 0; i < logDataAdded.Data.Count; i++)
            {
                Assert.AreEqual(logDataAdded.Data[i], logDataUpdated.Data[i]);
            }
            var stepIncrement = logUpdated.StepIncrement;
            Assert.IsNotNull(stepIncrement);
            Assert.AreEqual("m", stepIncrement.Uom);
            Assert.AreEqual(1.0, stepIncrement.Value);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_AppendLog_Data_Set_ObjectGrowing_And_IsActive_State()
        {
            Log.StartIndex = new GenericMeasure(5, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            // Query for objectGrowing is 0 (false)
            var logQuery = string.Format(BasicXMLTemplate, Log.UidWell, Log.UidWellbore, Log.Uid, "<objectGrowing>0</objectGrowing>");

            var logResult = DevKit.GetFromStore(ObjectTypes.Log, logQuery, string.Empty, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(logResult);

            var logList = EnergisticsConverter.XmlToObject<LogList>(logResult.XMLout);
            Assert.IsNotNull(logList);

            var log = logList.Log.FirstOrDefault();
            Assert.IsNotNull(log);
            Assert.IsFalse(log.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");

            // Query for isActive is 0 (false)
            var wellboreQuery = GenerateWellboreIsActiveQuery(0);
            var wellboreResult = DevKit.GetFromStore(ObjectTypes.Wellbore, wellboreQuery, string.Empty, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(wellboreResult);

            var wellboreList = EnergisticsConverter.XmlToObject<WellboreList>(wellboreResult.XMLout);
            Assert.IsNotNull(wellboreList);

            var wellbore = wellboreList.Wellbore.FirstOrDefault();
            Assert.IsNotNull(wellbore);
            Assert.IsFalse(wellbore.IsActive.GetValueOrDefault(), "IsActive");

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(17, "m"), 6);
            DevKit.UpdateAndAssert(update);

            // Query for objectGrowing is 0 (false)
            logQuery = string.Format(BasicXMLTemplate, Log.UidWell, Log.UidWellbore, Log.Uid, "<objectGrowing>0</objectGrowing>");
            logResult = DevKit.GetFromStore(ObjectTypes.Log, logQuery, string.Empty, OptionsIn.ReturnElements.IdOnly);
            Assert.IsNotNull(logResult);

            logList = EnergisticsConverter.XmlToObject<LogList>(logResult.XMLout);
            Assert.IsNotNull(logList);
            Assert.AreEqual(0, logList.Log.Count);

            // Query for objectGrowing is 1 (true)
            logQuery = string.Format(BasicXMLTemplate, Log.UidWell, Log.UidWellbore, Log.Uid, "<objectGrowing>1</objectGrowing>");
            logResult = DevKit.GetFromStore(ObjectTypes.Log, logQuery, string.Empty, OptionsIn.ReturnElements.HeaderOnly);
            Assert.IsNotNull(logResult);

            logList = EnergisticsConverter.XmlToObject<LogList>(logResult.XMLout);
            Assert.IsNotNull(logList);

            log = logList.Log.FirstOrDefault();
            Assert.IsNotNull(log);
            Assert.IsTrue(log.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");

            // Query for isActive is 0 (false)
            wellboreQuery = GenerateWellboreIsActiveQuery(0);
            wellboreResult = DevKit.GetFromStore(ObjectTypes.Wellbore, wellboreQuery, string.Empty, OptionsIn.ReturnElements.All);
            Assert.IsNotNull(wellboreResult);

            wellboreList = EnergisticsConverter.XmlToObject<WellboreList>(wellboreResult.XMLout);
            Assert.IsNotNull(wellboreList);
            Assert.AreEqual(0, wellboreList.Wellbore.Count);

            // Query for isActive is 1 (true)
            wellboreQuery = GenerateWellboreIsActiveQuery(1);
            wellboreResult = DevKit.GetFromStore(ObjectTypes.Wellbore, wellboreQuery, string.Empty, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(wellboreResult);

            wellboreList = EnergisticsConverter.XmlToObject<WellboreList>(wellboreResult.XMLout);
            Assert.IsNotNull(wellboreList);

            wellbore = wellboreList.Wellbore.FirstOrDefault();
            Assert.IsNotNull(wellbore);
            Assert.IsTrue(wellbore.IsActive.GetValueOrDefault(), "IsActive");
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_UpdateLog_Empty_Channel_Unchanged_ObjectGrowing_And_IsActive_State()
        {
            Log.StartIndex = new GenericMeasure(5, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            var addedLog = DevKit.GetAndAssert(Log);
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault());

            // Update
            var updateLog = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(8, "m"), 3, 0.2);
            DevKit.UpdateAndAssert(updateLog);

            var result = DevKit.GetAndAssert(updateLog);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_UpdateLog_Unchanged_ObjectGrowing_And_IsActive_State()
        {
            Log.StartIndex = new GenericMeasure(5, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            var addedLog = DevKit.GetAndAssert(Log);
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault());
            Assert.IsFalse(Wellbore.IsActive.GetValueOrDefault());

            // Update
            var updateLog = DevKit.CreateLog(addedLog.Uid, addedLog.Name, addedLog.UidWell, addedLog.NameWell, addedLog.UidWellbore, addedLog.NameWellbore);
            updateLog.LogData = DevKit.List(new LogData { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = addedLog.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = addedLog.LogData.First().UnitList;
            var logData = updateLog.LogData.First();
            logData.Data.Add("10.1,25");
            logData.Data.Add("10.5,11");

            DevKit.UpdateAndAssert(updateLog);

            var result = DevKit.GetAndAssert(updateLog);
            var wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_TimeLog_Data_Unchanged_ObjectGrowing_And_IsActive_State()
        {
            AddParents();
            Log.StartDateTimeIndex = new Timestamp();
            Log.EndDateTimeIndex = new Timestamp();

            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            var logData = Log.LogData.First();
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:38:42.0000000-05:00,30.1,30.2");

            DevKit.InitHeader(Log, LogIndexType.datetime, false);

            DevKit.AddAndAssert(Log);
            var addedLog = DevKit.GetAndAssert(Log);
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsFalse(Wellbore.IsActive.GetValueOrDefault(), "IsActive");

            // Update
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-13T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-13T15:34:42.0000000-05:00,34.1,34.2");
            logData.Data.Add("2016-04-13T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-13T15:36:42.0000000-05:00,36.1,36.2");

            DevKit.UpdateAndAssert(updateLog);

            var result = DevKit.GetAndAssert(Log);
            var wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Append_TimeLog_Data_Set_ObjectGrowing_And_IsActive_State_ExpireGrowingObjects()
        {
            AddParents();
            Log.StartDateTimeIndex = new Timestamp();
            Log.EndDateTimeIndex = new Timestamp();

            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            var logData = Log.LogData.First();
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:38:42.0000000-05:00,30.1,30.2");

            DevKit.InitHeader(Log, LogIndexType.datetime, false);

            DevKit.AddAndAssert(Log);
            var addedLog = DevKit.GetAndAssert(Log);
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsFalse(Wellbore.IsActive.GetValueOrDefault(), "IsActive");

            // Update
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-15T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-15T15:34:42.0000000-05:00,34.1,34.2");
            logData.Data.Add("2016-04-15T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-15T15:36:42.0000000-05:00,36.1,36.2");

            DevKit.UpdateAndAssert(updateLog);

            var result = DevKit.GetAndAssert(Log);
            var wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");

            WitsmlSettings.LogGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            result = DevKit.GetAndAssert(Log);
            wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_AppendLog_Data_ExpireGrowingObjects()
        {
            Log.StartIndex = new GenericMeasure(5, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            var addedLog = DevKit.GetAndAssert(Log);
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault());
            Assert.IsFalse(Wellbore.IsActive.GetValueOrDefault());

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(17, "m"), 6);
            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);
            var wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault());
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault());

            WitsmlSettings.LogGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            result = DevKit.GetAndAssert(Log);
            wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Can_Expire_Growing_Object_After_Delete()
        {
            // Add a Log with data
            Log.StartIndex = new GenericMeasure(5, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            // Veryify that Object Growing is false after an Add with Log data
            var addedLog = DevKit.GetAndAssert(Log);
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault());
            Assert.IsFalse(Wellbore.IsActive.GetValueOrDefault());

            // Append data to the log
            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(17, "m"), 6);
            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);
            var uri = addedLog.GetUri();
            var wellboreResult = DevKit.GetAndAssert(Wellbore);

            // Verify that the append set the objectGrowing flag to true.
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault());
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault());
            Assert.IsTrue(DevKit.Container.Resolve<IGrowingObjectDataProvider>().Exists(uri));

            // Delete the growing object
            var deleteLog = DevKit.CreateLog(result.Uid, result.Name, result.UidWell, result.NameWell,
                result.UidWellbore, result.NameWellbore);
            DevKit.DeleteAndAssert(deleteLog);

            // Wait until we're past the GrowingTimeoutPeriod
            WitsmlSettings.LogGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            // Expire the growing objects. By calling this after the delete of the log 
            // ... we're testing that an Exception wasn't raised.
            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            // The dbGrowingObject should have been deleted after the Log was deleted.
            Assert.IsFalse(DevKit.Container.Resolve<IGrowingObjectDataProvider>().Exists(uri));
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_AppendLogs_ExpireGrowingObjects()
        {
            //Add log
            Log.StartIndex = new GenericMeasure(5, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            var addedLog = DevKit.GetAndAssert(Log);
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault());
            Assert.IsFalse(Wellbore.IsActive.GetValueOrDefault());

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(17, "m"), 6);
            DevKit.UpdateAndAssert(update);

            var resultLog = DevKit.GetAndAssert(Log);
            var wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.IsTrue(resultLog.ObjectGrowing.GetValueOrDefault());
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault());

            //Change settings and wait
            WitsmlSettings.LogGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            //Add log2
            var log2 = new Log { Uid = DevKit.Uid(), Name = DevKit.Name("Log2"), UidWell = Well.Uid, NameWell = Well.Name, UidWellbore = Wellbore.Uid, NameWellbore = Wellbore.Name };
            Log.StartIndex = new GenericMeasure(5, "m");
            DevKit.InitHeader(log2, LogIndexType.measureddepth);
            DevKit.InitDataMany(log2, DevKit.Mnemonics(log2), DevKit.Units(log2), 5);
            DevKit.AddAndAssert(log2);

            var addedLog2 = DevKit.GetAndAssert(log2);
            Assert.IsFalse(addedLog2.ObjectGrowing.GetValueOrDefault());
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault());

            var updateLog2 = CreateLogDataUpdate(log2, LogIndexType.measureddepth, new GenericMeasure(10, "m"), 6);
            DevKit.UpdateAndAssert(updateLog2);

            var resultLog2 = DevKit.GetAndAssert(log2);
            wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.IsTrue(resultLog2.ObjectGrowing.GetValueOrDefault());
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault());

            //Expire objects
            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            resultLog = DevKit.GetAndAssert(Log);
            resultLog2 = DevKit.GetAndAssert(log2);
            wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.IsFalse(resultLog.ObjectGrowing.GetValueOrDefault(), "Log ObjectGrowing");
            Assert.IsTrue(resultLog2.ObjectGrowing.GetValueOrDefault(), "Log2 ObjectGrowing");
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");

            WitsmlSettings.LogGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            //Expire objects again
            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            resultLog = DevKit.GetAndAssert(Log);
            resultLog2 = DevKit.GetAndAssert(log2);
            wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.IsFalse(resultLog.ObjectGrowing.GetValueOrDefault(), "Log ObjectGrowing");
            Assert.IsFalse(resultLog2.ObjectGrowing.GetValueOrDefault(), "Log2 ObjectGrowing");
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");
        }

        [TestMethod]
        public void Log141DataAdapter_ChangeLog_Track_Append_DepthLog_Data_And_Expire()
        {
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(17, "m"), 6);
            DevKit.UpdateAndAssert(update);

            // Assert log is growing
            var log = DevKit.GetAndAssert(Log);
            Assert.IsTrue(log.ObjectGrowing.GetValueOrDefault(), "Log ObjectGrowing");

            var current = DevKit.GetAndAssert(Log);
            var changeLog = DevKit.AssertChangeLog(current, 2, ChangeInfoType.update);

            var lastChange = changeLog.ChangeHistory.LastOrDefault();
            Assert.IsNotNull(lastChange);
            DevKit.AssertChangeHistoryIndexRange(lastChange, 17, 22);

            update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(24, "m"), 6);
            DevKit.UpdateAndAssert(update);

            WitsmlSettings.LogGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            // Fetch the latest changeLog
            current = DevKit.GetAndAssert(Log);
            changeLog = DevKit.AssertChangeLog(current, 3, ChangeInfoType.update);

            lastChange = changeLog.ChangeHistory.LastOrDefault();
            Assert.IsNotNull(lastChange);
            DevKit.AssertChangeHistoryFlags(lastChange, false, false);
        }

        [TestMethod]
        public void Log141DataAdapter_ChangeLog_Track_Append_TimeLog_Data_And_Expire()
        {
            AddParents();
            Log.StartDateTimeIndex = new Timestamp();
            Log.EndDateTimeIndex = new Timestamp();

            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            var logData = Log.LogData.First();
            logData.Data.Add("2016-04-13T15:31:42.0000123-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:38:42.0000000-05:00,30.1,30.2");

            DevKit.InitHeader(Log, LogIndexType.datetime);

            DevKit.AddAndAssert(Log);

            // Update
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-15T15:35:42.0000040-05:00,35.1,35.2");
            logData.Data.Add("2016-04-15T15:36:42.0000000-05:00,34.1,34.2");
            logData.Data.Add("2016-04-15T15:37:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-15T15:38:42.0000600-05:00,36.1,36.2");

            DevKit.UpdateAndAssert(updateLog);

            // Assert log is growing
            var log = DevKit.GetAndAssert(Log);
            Assert.IsTrue(log.ObjectGrowing.GetValueOrDefault(), "Log ObjectGrowing");

            var current = DevKit.GetAndAssert(Log);
            var changeLog = DevKit.AssertChangeLog(current, 2, ChangeInfoType.update);

            var lastChange = changeLog.ChangeHistory.LastOrDefault();

            Assert.IsNotNull(lastChange);
            DevKit.AssertChangeHistoryIndexRange(lastChange, DateTimeOffset.Parse("2016-04-15T15:35:42.0000040-05:00"),
                DateTimeOffset.Parse("2016-04-15T15:38:42.0000600-05:00"));
            DevKit.AssertChangeLogMnemonics(DevKit.GetMnemonics(current), lastChange.Mnemonics);

            // Send 2nd update of data with some overlap
            updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-15T15:37:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-15T15:38:42.0000000-05:00,34.1,34.2");
            logData.Data.Add("2016-04-15T15:39:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-15T15:40:42.0000000-05:00,36.1,36.2");

            DevKit.UpdateAndAssert(updateLog);

            WitsmlSettings.LogGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            // Fetch the latest changeLog
            current = DevKit.GetAndAssert(Log);
            changeLog = DevKit.AssertChangeLog(current, 3, ChangeInfoType.update);
            lastChange = changeLog.ChangeHistory.LastOrDefault();
            Assert.IsNotNull(lastChange);
            DevKit.AssertChangeHistoryFlags(lastChange, false, false);
        }

        [TestMethod]
        public void Log141DataAdapter_ChangeLog_Track_Add_Curve()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogCurveInfo.RemoveRange(1, 2);
            Log.LogData.Clear();

            DevKit.AddAndAssert(Log);

            var logAdded = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, logAdded.LogCurveInfo.Count);
            Assert.AreEqual(Log.LogCurveInfo.Count, logAdded.LogCurveInfo.Count);

            var update = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            DevKit.InitHeader(update, LogIndexType.measureddepth);
            update.LogCurveInfo.RemoveAt(2);
            update.LogCurveInfo.RemoveAt(0);
            update.LogData.Clear();

            DevKit.UpdateAndAssert(update);

            var logUpdated = DevKit.GetAndAssert(Log);
            Assert.AreEqual(2, logUpdated.LogCurveInfo.Count);

            // Fetch the changeLog for the entity just added
            var current = DevKit.GetAndAssert(Log);
            var changeLog = DevKit.AssertChangeLog(current, 2, ChangeInfoType.update);

            var lastChange = changeLog.ChangeHistory.LastOrDefault();
            Assert.IsNotNull(lastChange);
            Assert.AreEqual(changeLog.LastChangeInfo, lastChange.ChangeInfo);
            DevKit.AssertChangeHistoryFlags(lastChange, true, false);
            Assert.AreEqual("Mnemonics added: ROP", lastChange.ChangeInfo);
            DevKit.AssertChangeLogMnemonics(new[] { update.LogCurveInfo[0].Mnemonic.Value }, lastChange.Mnemonics);
        }

        [TestMethod]
        public void Log141DataAdapter_ChangeLog_Track_Add_Data_To_Middle_Of_Single_Mnemonic()
        {
            AddParents();
            Log.StartDateTimeIndex = new Timestamp();
            Log.EndDateTimeIndex = new Timestamp();

            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            var logData = Log.LogData.First();
            logData.Data.Add("2016-04-13T15:31:42.0000123-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.2,31.2");
            logData.Data.Add("2016-04-13T15:38:42.0000000-05:00,30.1,30.2");

            DevKit.InitHeader(Log, LogIndexType.datetime);

            DevKit.AddAndAssert(Log);

            // Update
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = "TIME,ROP";
            updateLog.LogData[0].UnitList = "s,m/h";
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-13T15:32:42.0000100-05:00,100");
            logData.Data.Add("2016-04-13T15:32:42.0020000-05:00,100");

            DevKit.UpdateAndAssert(updateLog);

            // Assert log is not growing
            var log = DevKit.GetAndAssert(Log);
            Assert.IsFalse(log.ObjectGrowing.GetValueOrDefault(), "Log ObjectGrowing");

            var current = DevKit.GetAndAssert(Log);
            var changeLog = DevKit.AssertChangeLog(current, 2, ChangeInfoType.update);

            var lastChange = changeLog.ChangeHistory.LastOrDefault();

            Assert.IsNotNull(lastChange);
            DevKit.AssertChangeHistoryIndexRange(lastChange, DateTimeOffset.Parse("2016-04-13T15:32:42.0000100-05:00"),
                DateTimeOffset.Parse("2016-04-13T15:32:42.0020000-05:00"));
            DevKit.AssertChangeLogMnemonics(updateLog.LogData[0].MnemonicList.Split(','), lastChange.Mnemonics);
        }

        [TestMethod]
        public void Log141DataAdapter_ChangeLog_Track_Updating_Last_Existing_Row_While_Growing()
        {
            AddParents();
            Log.StartDateTimeIndex = new Timestamp();
            Log.EndDateTimeIndex = new Timestamp();

            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            var logData = Log.LogData.First();
            logData.Data.Add("2016-04-13T15:31:42.0000123-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.2,31.2");
            logData.Data.Add("2016-04-13T15:38:42.0000000-05:00,30.1,30.2");

            DevKit.InitHeader(Log, LogIndexType.datetime);

            DevKit.AddAndAssert(Log);

            // Send update modifying the last row
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-13T15:38:42.0000000-05:00,0,0");

            DevKit.UpdateAndAssert(updateLog);

            // Assert log is not growing
            var log = DevKit.GetAndAssert(Log);
            Assert.IsFalse(log.ObjectGrowing.GetValueOrDefault(), "Log ObjectGrowing");

            var current = DevKit.GetAndAssert(Log);
            var changeLog = DevKit.AssertChangeLog(current, 2, ChangeInfoType.update);
            var lastChange = changeLog.ChangeHistory.LastOrDefault();
            Assert.IsNotNull(lastChange);
            DevKit.AssertChangeHistoryIndexRange(lastChange, DateTimeOffset.Parse("2016-04-13T15:38:42.0000000-05:00"),
                DateTimeOffset.Parse("2016-04-13T15:38:42.0000000-05:00"));
            DevKit.AssertChangeLogMnemonics(DevKit.GetMnemonics(Log), lastChange.Mnemonics);

            // Send 3rd update of data appending
            updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-13T15:39:42.0000000-05:00,0,0");

            DevKit.UpdateAndAssert(updateLog);

            // Assert log is growing
            log = DevKit.GetAndAssert(Log);
            Assert.IsTrue(log.ObjectGrowing.GetValueOrDefault(), "Log ObjectGrowing");

            changeLog = DevKit.AssertChangeLog(log, 3, ChangeInfoType.update);
            lastChange = changeLog.ChangeHistory.LastOrDefault();
            Assert.IsNotNull(lastChange);
            DevKit.AssertChangeHistoryFlags(lastChange, true, true);
            DevKit.AssertChangeHistoryIndexRange(lastChange, DateTimeOffset.Parse("2016-04-13T15:39:42.0000000-05:00"),
                DateTimeOffset.Parse("2016-04-13T15:39:42.0000000-05:00"));
            DevKit.AssertChangeLogMnemonics(DevKit.GetMnemonics(Log), lastChange.Mnemonics);

            // Send 4th update only modifying the last row again
            updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2016-04-13T15:39:42.0000000-05:00,1,1");

            DevKit.UpdateAndAssert(updateLog);

            // Assert log is still growing
            log = DevKit.GetAndAssert(Log);
            Assert.IsTrue(log.ObjectGrowing.GetValueOrDefault(), "Log ObjectGrowing");

            // Verify that a change history was made even though the object is growing
            changeLog = DevKit.AssertChangeLog(log, 4, ChangeInfoType.update);
            lastChange = changeLog.ChangeHistory.LastOrDefault();
            Assert.IsNotNull(lastChange);
            DevKit.AssertChangeHistoryFlags(lastChange, true, true);
            DevKit.AssertChangeHistoryIndexRange(lastChange, DateTimeOffset.Parse("2016-04-13T15:39:42.0000000-05:00"),
                DateTimeOffset.Parse("2016-04-13T15:39:42.0000000-05:00"));
            DevKit.AssertChangeLogMnemonics(DevKit.GetMnemonics(Log), lastChange.Mnemonics);
        }

        [TestMethod, Description("Implements test60.py from the Energistics certification tests")]
        public void Log141DataAdapter_ChangeLog_Tracks_Historical_Changes_To_Mnemonics()
        {
            AddParents();

            // Initialize log with 4 channels with 4 data rows.
            Log.IndexType = LogIndexType.measureddepth;
            Log.IndexCurve = "BDEP";
            Log.Direction = LogIndexDirection.increasing;

            Log.LogCurveInfo = new List<LogCurveInfo>
            {
                DevKit.LogGenerator.CreateDoubleLogCurveInfo(Log.IndexCurve, "m"),
                DevKit.LogGenerator.CreateDoubleLogCurveInfo("CURVE1", "m/h"),
                DevKit.LogGenerator.CreateDoubleLogCurveInfo("CURVE2", "m/h"),
                DevKit.LogGenerator.CreateDoubleLogCurveInfo("CURVE3", "m/h")
            };
            Log.LogCurveInfo.ForEach(x => x.TypeLogData = LogDataType.@int);
            DevKit.InitData(Log, DevKit.Mnemonics(Log), DevKit.Units(Log));
            Log.LogData[0].Data.Add("0,0,0,0");
            Log.LogData[0].Data.Add("1,1,1,1");
            Log.LogData[0].Data.Add("2,2,2,2");
            Log.LogData[0].Data.Add("4,4,4,4");

            // Add the log and check the success
            DevKit.AddAndAssert(Log);

            // UpdateInStore to add a single datapoint to a curve to the existing curve range
            var updateLog = DevKit.CreateLog(Log);
            updateLog.LogData = new List<LogData>
            {
                new LogData()
                {
                    MnemonicList = "BDEP,CURVE1",
                    UnitList = "m, m/h",
                    Data = new List<string>()
                }
            };
            updateLog.LogData[0].Data.Add("3,3");

            DevKit.UpdateAndAssert(updateLog);

            // Call DeleteFrom Store to delete a datapoint in a curve
            var deleteLog = DevKit.CreateLog(Log);
            deleteLog.StartIndex = new GenericMeasure(2, "m");
            deleteLog.EndIndex = new GenericMeasure(2, "m");
            deleteLog.LogCurveInfo = new List<LogCurveInfo>
            {
                new LogCurveInfo() { Mnemonic = new ShortNameStruct() {Value = "CURVE2"} }
            };

            DevKit.DeleteAndAssert(deleteLog, partialDelete: true);

            // Call UpdateInStore to delete a datapoint in a curve
            var partialDeleteLog = DevKit.CreateLog(Log);
            partialDeleteLog.LogData = new List<LogData>
            {
                new LogData()
                {
                    MnemonicList = "BDEP,CURVE3",
                    UnitList = "m, m/h",
                    Data = new List<string>()
                }
            };
            partialDeleteLog.LogData[0].Data.Add("1,1");
            partialDeleteLog.LogData[0].Data.Add("4,4");

            DevKit.UpdateAndAssert(partialDeleteLog);

            var tenMinutesAgo = DateTimeOffset.UtcNow.AddSeconds(-600);
            var changeLogQuery = DevKit.CreateChangeLog(Log.GetUri());
            changeLogQuery.ChangeHistory = new List<ChangeHistory>
            {
                new ChangeHistory()
                {
                    DateTimeChange = tenMinutesAgo,
                    ChangeType = ChangeInfoType.update
                }
            };

            var changeLog = DevKit.QueryAndAssert<ChangeLogList, ChangeLog>(changeLogQuery);

            Assert.AreEqual(3, changeLog.ChangeHistory.Count, "ChangeHistory results should only contain updates");
            var history = changeLog.ChangeHistory;
            var firstUpdateHistory = history.FirstOrDefault(h => h.Mnemonics != null && h.Mnemonics.Equals("BDEP,CURVE1"));
            var partialDeleteHistory = history.FirstOrDefault(h => h.Mnemonics != null && h.Mnemonics.Equals("CURVE2"));
            var secondUpdateHistory = history.FirstOrDefault(h => h.Mnemonics != null && h.Mnemonics.Equals("BDEP,CURVE3"));

            Assert.IsNotNull(firstUpdateHistory);
            Assert.AreEqual(ChangeInfoType.update, firstUpdateHistory.ChangeType);
            Assert.AreEqual(3, firstUpdateHistory.StartIndex.Value);
            Assert.AreEqual(3, firstUpdateHistory.EndIndex.Value);
            Assert.IsTrue(tenMinutesAgo < firstUpdateHistory.DateTimeChange.Value);

            Assert.IsNotNull(partialDeleteHistory);
            Assert.AreEqual(2, partialDeleteHistory.StartIndex.Value);
            Assert.AreEqual(2, partialDeleteHistory.EndIndex.Value);
            Assert.IsTrue(tenMinutesAgo < partialDeleteHistory.DateTimeChange.Value);

            Assert.IsNotNull(secondUpdateHistory);
            Assert.AreEqual(ChangeInfoType.update, secondUpdateHistory.ChangeType);
            Assert.AreEqual(1, secondUpdateHistory.StartIndex.Value);
            Assert.AreEqual(4, secondUpdateHistory.EndIndex.Value);
            Assert.IsTrue(tenMinutesAgo < secondUpdateHistory.DateTimeChange.Value);
        }

        [TestMethod]
        public async Task Log141DataAdapter_UpdateInStore_Sparsely_Update_Log_Curves_With_Multiple_Threads()
        {
            AddAnEmptyLogWithMoreCurves();

            var iterations = 10;
            var indexValue = 10.0;
            var logCount = 10;
            var logs = new List<Log>();

            for (var i = 0; i < logCount; i++)
            {
                var clone = new Log()
                {
                    Uid = Log.Uid,
                    UidWell = Log.UidWell,
                    UidWellbore = Log.UidWellbore,
                    Name = Log.Name,
                    NameWell = Log.NameWell,
                    NameWellbore = Log.NameWellbore,
                    IndexCurve = Log.IndexCurve,
                    IndexType = Log.IndexType,
                    LogCurveInfo = Log.LogCurveInfo
                };
                clone.Uid = $"Log-{i}";
                logs.Add(clone);
            }

            logs.ForEach(x =>
            {
                DevKit.AddAndAssert(x);
                x.LogCurveInfo = new List<LogCurveInfo>();
            });

            var counter = 0;
            while (counter < iterations)
            {
                indexValue += 0.1;
                var tasks = new List<Task>();
                var value = indexValue;

                logs.ForEach((x, i) =>
                {
                    var indexCurve = Log.IndexCurve;
                    var mnemonics = Log.LogCurveInfo.Where(c => c.Mnemonic.Value != indexCurve).ToList();
                    var logDatas = GenerateSparseLogData(value, indexCurve, mnemonics);

                    x.LogData = new List<LogData>() { logDatas[0], logDatas[1] };
                    tasks.Add(new Task(() => DevKit.UpdateAndAssert(x)));

                    var clone = new Log
                    {
                        Uid = x.Uid,
                        UidWell = x.UidWell,
                        UidWellbore = x.UidWellbore,
                        LogData = new List<LogData>() { logDatas[0], logDatas[2] }
                    };
                    tasks.Add(new Task(() => DevKit.UpdateAndAssert(clone)));
                });

                tasks.ForEach(x => x.Start());
                await Task.WhenAll(tasks);
                counter++;
            }

            // Assert all data was written
            logs.ForEach(x =>
            {
                var result = DevKit.GetAndAssert(x);
                Assert.IsNotNull(result);
                Assert.AreEqual(4, result.LogCurveInfo.Count);
                Assert.AreEqual(1, result.LogData.Count);
                Assert.AreEqual(30, result.LogData[0].Data.Count);
                Assert.AreEqual(8.1, result.LogCurveInfo[0].MinIndex.Value);
                Assert.AreEqual(11, result.LogCurveInfo[0].MaxIndex.Value);
                Assert.AreEqual(10.1, result.LogCurveInfo[1].MinIndex.Value);
                Assert.AreEqual(11, result.LogCurveInfo[1].MaxIndex.Value);
                Assert.AreEqual(9.1, result.LogCurveInfo[2].MinIndex.Value);
                Assert.AreEqual(10, result.LogCurveInfo[2].MaxIndex.Value);
                Assert.AreEqual(8.1, result.LogCurveInfo[3].MinIndex.Value);
                Assert.AreEqual(9, result.LogCurveInfo[3].MaxIndex.Value);
            });
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Invalid_Data_Rows()
        {
            CompatibilitySettings.InvalidDataRowSetting = InvalidDataRowSetting.Error;

            AddParents();

            // Initialize Log Header
            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            // Initialize data 
            DevKit.InitData(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 5, 5, 5);

            DevKit.AddAndAssert(Log);

            var updateLog = new Log()
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore
            };
            DevKit.InitHeader(updateLog, LogIndexType.measureddepth);

            // Initialize with invalid data 
            DevKit.InitData(updateLog, DevKit.Mnemonics(updateLog), DevKit.Units(updateLog), 5);

            // Update with invalid data
            DevKit.UpdateAndAssert(updateLog, ErrorCodes.ErrorRowDataCount);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_Channels_With_Blank_Data()
        {
            AddParents();

            // Initialize Log Header
            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var mnemonicList = DevKit.Mnemonics(Log);
            var unitList = DevKit.Units(Log);

            // Initialize data 
            DevKit.InitData(Log, mnemonicList, unitList, 0, 0, 0);
            DevKit.InitData(Log, mnemonicList, unitList, 1, 1, 1);
            DevKit.InitData(Log, mnemonicList, unitList, 2, 2, 2);
            DevKit.InitData(Log, mnemonicList, unitList, 3, 3, 3);
            DevKit.InitData(Log, mnemonicList, unitList, 4, 4, 4);

            DevKit.AddAndAssert(Log);

            var updateLog = DevKit.CreateLog(Log);
            DevKit.InitData(updateLog, mnemonicList, unitList, 0, 0, 0);
            DevKit.InitData(updateLog, mnemonicList, unitList, 1, null, 1);
            DevKit.InitData(updateLog, mnemonicList, unitList, 2, null, 2);
            DevKit.InitData(updateLog, mnemonicList, unitList, 3, 3, null);
            DevKit.InitData(updateLog, mnemonicList, unitList, 4, 4, null);

            DevKit.UpdateAndAssert(updateLog);

            var queryLog = DevKit.CreateLog(Log);
            var results = DevKit.QueryAndAssert<LogList, Log>(queryLog);

            Assert.IsNotNull(results);
            Assert.IsNotNull(results.LogData);
            Assert.AreEqual(Log.LogData.Count, results.LogData.Count);
            Assert.AreEqual(Log.LogData[0].Data.Count, results.LogData[0].Data.Count);

            var row1 = results.LogData[0].Data[1].Split(',');
            var row2 = results.LogData[0].Data[2].Split(',');
            var row3 = results.LogData[0].Data[3].Split(',');
            var row4 = results.LogData[0].Data[4].Split(',');

            // Row 1 & 2, Curve 1 should be empty now
            Assert.IsTrue(string.IsNullOrEmpty(row1[1]));
            Assert.IsTrue(string.IsNullOrEmpty(row2[1]));

            // Row 3 & 4, Curve 2 should still have a value
            Assert.IsTrue(!string.IsNullOrEmpty(row3[2]));
            Assert.IsTrue(!string.IsNullOrEmpty(row4[2]));
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Multi_Update_Merge_With_File_Storage()
        {
            WitsmlSettings.DepthRangeSize = 5000;
            WitsmlSettings.MaxDataLength = 5000000;

            // Add Well
            var response = DevKit.Add_Well_from_file(Path.Combine(_dataDir, "Test-chunk-file-merge-well141-add.xml"));
            // There is no response if the Well already exists in the database
            if (response != null)
            {
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }

            // Add Wellbore
            response = DevKit.Add_Wellbore_from_file(Path.Combine(_dataDir, "Test-chunk-file-merge-wellbore141-add.xml"));
            if (response != null)
            {
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }

            // Add Log Header
            response = DevKit.Add_Log_from_file(Path.Combine(_dataDir, "Test-chunk-file-merge-log141-add.xml"));
            if (response != null)
            {
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);

                // Update Log with data ranging from 4.9 - 1304.8
                var updateResponse = DevKit.Update_Log_from_file(Path.Combine(_dataDir, "Test-chunk-file-merge-log141-update1.xml"));
                if (updateResponse != null)
                {
                    Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
                }

                // Update Log with data ranging from 1304.9 - 2604.8
                updateResponse = DevKit.Update_Log_from_file(Path.Combine(_dataDir, "Test-chunk-file-merge-log141-update2.xml"));
                if (updateResponse != null)
                {
                    Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
                }
            }

            // Query the log using the last index from the first update to the first index of the last update
            var queryLog = new Log()
            {
                UidWell = "TestChunkFileMergeWell",
                UidWellbore = "TestChunkFileMergeWellbore",
                Uid = "TestChunkFileMergeLog",
                StartIndex = new GenericMeasure() { Uom = "ft", Value = 1304.8 },
                EndIndex = new GenericMeasure() { Uom = "ft", Value = 1304.9 }
            };

            var result = DevKit.GetAndAssert(queryLog, optionsIn: OptionsIn.ReturnElements.DataOnly, queryByExample: true);
            var logData = result.LogData;

            // Both updates should fit into a single data chunk, if so we should get two records back
            Assert.AreEqual(1, logData.Count);
            Assert.AreEqual(2, logData[0].Data.Count);
        }

        [Ignore]
        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Null_Value_Compare_With_Trailing_Zeros_Returns_No_Data()
        {
            AddParents();

            // Initialize Log Header
            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var mnemonicList = DevKit.Mnemonics(Log);
            var unitList = DevKit.Units(Log);

            // Initialize data 
            DevKit.InitData(Log, mnemonicList, unitList, 10, 10, 0);
            DevKit.InitData(Log, mnemonicList, unitList, 11, 11, 1);
            DevKit.InitData(Log, mnemonicList, unitList, 12, 12, 2);
            DevKit.InitData(Log, mnemonicList, unitList, 13, 13, 3);
            DevKit.InitData(Log, mnemonicList, unitList, 14, 14, 4);

            // Add a Log with some data
            DevKit.AddAndAssert(Log);

            var updateLog = DevKit.CreateLog(Log);

            // Add a new curve to the log
            updateLog.NullValue = "-999.2500";
            updateLog.LogCurveInfo = new List<LogCurveInfo>();
            updateLog.LogCurveInfo.Add(new LogCurveInfo()
            {
                Uid = "XXX",
                Mnemonic = new ShortNameStruct() { Value = "XXX" },
                Unit = "m/h",
                TypeLogData = LogDataType.@int
            });
            DevKit.UpdateAndAssert(updateLog);

            updateLog.LogCurveInfo.Clear();
            DevKit.InitData(updateLog, "MD,XXX", "m,m/h", 10, -999.25);
            DevKit.UpdateAndAssert(updateLog);

            updateLog.LogData[0].Data.Clear();
            updateLog.LogData[0].UnitList = null;
            var result = DevKit.Query<LogList, Log>(updateLog, ObjectTypes.Log, null, OptionsIn.ReturnElements.DataOnly);

            Assert.AreEqual(0, result[0].LogData.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Null_Reference_Exception()
        {
            WitsmlSettings.TimeRangeSize = 600000000;
            const int logCount = 4;
            const int dataRowCount = 200;

            AddParents();

            DevKit.InitHeader(Log, LogIndexType.datetime);

            Log.LogData = null;

            DevKit.AddAndAssert(Log);

            var logs = new List<Log>();
            var index = new DateTime(2000, 1, 1);
            var mnemonicList = string.Join(",", Log.LogCurveInfo.Select(x => x.Mnemonic.Value));
            var unitList = string.Join(",", Log.LogCurveInfo.Select(x => x.Unit));

            for (var i = 0; i < logCount; i++)
            {
                var clone = new Log()
                {
                    Uid = Log.Uid,
                    UidWell = Log.UidWell,
                    UidWellbore = Log.UidWellbore,
                    Name = Log.Name,
                    NameWell = Log.NameWell,
                    NameWellbore = Log.NameWellbore,
                    IndexCurve = Log.IndexCurve,
                    IndexType = Log.IndexType,
                    LogData = new List<LogData>()
                    {
                        new LogData()
                        {
                            MnemonicList = mnemonicList,
                            UnitList = unitList,
                            Data = new List<string>()
                        }
                    }
                };

                for (var r = 0; r < dataRowCount; r++)
                {
                    var data = string.Join(",", Enumerable.Repeat($"{r}", 2));
                    clone.LogData[0].Data.Add($"{index:O},{data}");
                    index = index.AddSeconds(1);
                }

                logs.Add(clone);
            }

            logs.Reverse();
            logs.ForEach(x => DevKit.UpdateAndAssert(x));
        }

        [Ignore]
        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Can_Update_With_Multiple_Threads_No_Deadlocks()
        {
            const int logCount = 10;
            const int dataRowCount = 10000;
            const int numberOfExtraCurves = 100;

            AddAnEmptyLogWithMoreCurves(LogIndexType.datetime, numberOfExtraCurves);

            var logs = new List<Log>();
            var index = new DateTime(2000, 1, 1);
            var mnemonicList = string.Join(",", Log.LogCurveInfo.Select(x => x.Mnemonic.Value));
            var unitList = string.Join(",", Log.LogCurveInfo.Select(x => x.Unit));

            for (var i = 0; i < logCount; i++)
            {
                var clone = new Log()
                {
                    Uid = Log.Uid,
                    UidWell = Log.UidWell,
                    UidWellbore = Log.UidWellbore,
                    Name = Log.Name,
                    NameWell = Log.NameWell,
                    NameWellbore = Log.NameWellbore,
                    IndexCurve = Log.IndexCurve,
                    IndexType = Log.IndexType,
                    LogData = new List<LogData>()
                    {
                        new LogData()
                        {
                            MnemonicList = mnemonicList,
                            UnitList = unitList,
                            Data = new List<string>()
                        }
                    }
                };

                for (var r = 0; r < dataRowCount; r++)
                {
                    var data = string.Join(",", Enumerable.Repeat($"{r}", numberOfExtraCurves + 2));
                    clone.LogData[0].Data.Add($"{index:O},{data}");
                    index = index.AddSeconds(1);
                }

                logs.Add(clone);
            }

            var tasks = new List<Task>();
            logs.ForEach(x => tasks.Add(new Task(() => DevKit.UpdateAndAssert(x))));

            var random = new Random();
            tasks = tasks.OrderBy(x => random.Next()).ToList();
            tasks.ForEach(x => x.Start());
            Task.WaitAll(tasks.ToArray());

            var headerQuery = new Log { UidWell = Log.UidWell, UidWellbore = Log.UidWellbore, Uid = Log.Uid };
            var result = DevKit.GetAndAssert(headerQuery, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.IsNotNull(result);
            Assert.AreEqual(new DateTime(2000, 1, 1).ToUnixTimeMicroseconds(), result.StartDateTimeIndex.ToUnixTimeMicroseconds(), "Start Index does not match");
            Assert.AreEqual(index.AddSeconds(-1).ToUnixTimeMicroseconds(), result.EndDateTimeIndex.ToUnixTimeMicroseconds(), "End Index does not match");
        }

        [TestMethod, Description("As Unit is not required (can be blank) on unit list when unit is not specfied on logCurveInfo")]
        public void Log141DataAdapter_UpdateInStore_With_Empty_Unit_On_Curve()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            //Remove the unit of the last logCurveInfo
            var lci = Log.LogCurveInfo.LastOrDefault();
            Assert.IsNotNull(lci);

            lci.Unit = string.Empty;

            // Set the 3rd UnitList entry to an empty string
            var logData = Log.LogData.First();
            logData.UnitList = "m,m/h,";

            DevKit.AddAndAssert(Log);

            DevKit.UpdateAndAssert(Log);

            var result = DevKit.GetAndAssert(Log, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.IsNotNull(result);

            // Verify the logCurveInfo unit
            var resultLci = result.LogCurveInfo.LastOrDefault();
            Assert.IsNotNull(resultLci);
            Assert.IsNull(resultLci.Unit);

            // Verify the log data unitList matches
            result = DevKit.GetAndAssert(Log, optionsIn: OptionsIn.ReturnElements.DataOnly);
            Assert.IsNotNull(result.LogData[0]);
            Assert.AreEqual(1, result.LogData.Count);

            var resultLogData = result.LogData[0];
            Assert.AreEqual("m,", resultLogData.UnitList, "Unit list of the result doesn't match");
        }

        #region Helper Functions

        private Log AddAnEmptyLogWithMoreCurves(LogIndexType indexType = LogIndexType.measureddepth, int numberOfExtraCurves = 1)
        {
            AddParents();

            DevKit.InitHeader(Log, indexType);

            for (var i = 0; i < numberOfExtraCurves; i++)
            {
                var channelName = $"channel{i}";
                var curves = Log.LogCurveInfo;
                var newChannel = new LogCurveInfo
                {
                    Uid = channelName,
                    Mnemonic = new ShortNameStruct
                    {
                        Value = channelName
                    },
                    Unit = "ft",
                    TypeLogData = LogDataType.@double
                };
                curves.Add(newChannel);
            }

            Log.LogData = null;

            DevKit.AddAndAssert(Log);

            return Log;
        }

        private void AssertNestedElements(Log log, LogCurveInfo curve, ExtensionNameValue extensionName1, ExtensionNameValue extensionName4)
        {
            var lastCurve = log.LogCurveInfo.Last();
            Assert.IsNotNull(lastCurve);

            var extensionNames = lastCurve.ExtensionNameValue;

            var resultExtensionName = extensionNames.Last();
            Assert.IsNotNull(resultExtensionName);
            Assert.AreEqual(extensionName4.Value.Value, resultExtensionName.Value.Value);

            var axisDefinition = curve.AxisDefinition.FirstOrDefault();
            Assert.IsNotNull(axisDefinition);

            var resultAxisDefinition = lastCurve.AxisDefinition.FirstOrDefault();
            Assert.IsNotNull(resultAxisDefinition);

            resultExtensionName = resultAxisDefinition.ExtensionNameValue.First();
            Assert.IsNotNull(resultExtensionName);

            Assert.AreEqual(extensionName1.Value.Value, resultExtensionName.Value.Value);
        }

        private void AddLogHeader(Log log, LogIndexType indexType)
        {
            AddParents();

            DevKit.InitHeader(log, indexType);

            DevKit.AddAndAssert(Log);
        }

        private void AddLogWithData(Log log, LogIndexType indexType, int numOfRows, bool hasEmptyChannel = true)
        {
            AddParents();

            DevKit.InitHeader(log, indexType);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), numOfRows, hasEmptyChannel: hasEmptyChannel);

            DevKit.AddAndAssert(Log);
        }

        private Log CreateLogDataUpdate(Log log, LogIndexType indexType, GenericMeasure startIndex, int numOfRows, double factor = 1, bool hasEmptyChannel = true)
        {
            var update = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.StartIndex = startIndex;

            DevKit.InitHeader(update, indexType);
            DevKit.InitDataMany(update, DevKit.Mnemonics(update), DevKit.Units(update), numOfRows, factor, hasEmptyChannel: hasEmptyChannel);

            return update;
        }

        private string GenerateWellboreIsActiveQuery(int isActive)
        {
            return "<wellbores xmlns =\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                                $"<wellbore uidWell =\"{Log.UidWell}\" uid=\"{Log.UidWellbore}\">" + Environment.NewLine +
                                $"<isActive>{isActive}</isActive>" + Environment.NewLine +
                                "</wellbore></wellbores>";
        }
        #endregion
    }
}
