//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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
using System.IO;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Log141DataAdapter Update tests.
    /// </summary>
    [TestClass]
    public class Log141DataAdapterUpdateTests
    {
        private DevKit141Aspect DevKit;
        private Well Well;
        private Wellbore Wellbore;
        private Log Log;
        private string DataDir;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect(TestContext);

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            // Test data directory
            DataDir = new DirectoryInfo(@".\TestData").FullName;

            Well = new Well()
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
                Name = DevKit.Name("Wellbore 01"),
            };

            Log = new Log()
            {
                UidWell = Well.Uid,
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
            WitsmlOperationContext.Current = null;
            WitsmlSettings.DepthRangeSize = DevKitAspect.DefaultDepthChunkRange;
            WitsmlSettings.TimeRangeSize = DevKitAspect.DefaultTimeChunkRange;
            WitsmlSettings.MaxDataPoints = DevKitAspect.DefaultMaxDataPoints;
            WitsmlSettings.MaxDataNodes = DevKitAspect.DefaultMaxDataNodes;
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Supports_NaN_In_Numeric_Fields()
        {
            // Add well
            var response = DevKit.Add<WellList, Well>(Well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add log
            Log.UidWell = Wellbore.UidWell;
            Log.UidWellbore = uidWellbore;
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 3);
            Log.BhaRunNumber = 123;
            Log.LogCurveInfo[0].ClassIndex = 1;
            Log.LogCurveInfo[1].ClassIndex = 2;

            response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Update log
            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + uidLog + "\" uidWell=\"" + Wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +                        
                        "<bhaRunNumber>NaN</bhaRunNumber>" + Environment.NewLine +                       
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +                                            
                        "  <classIndex>NaN</classIndex>" + Environment.NewLine +                       
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"ROP\">" + Environment.NewLine +                                       
                        "  <classIndex>NaN</classIndex>" + Environment.NewLine +                       
                        "</logCurveInfo>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var result = DevKit.UpdateInStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var query = DevKit.CreateLog(uidLog, null, Wellbore.UidWell, null, uidWellbore, null);

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.IsTrue(results.Any());

            Assert.IsNull(results.First().BhaRunNumber);
            Assert.AreEqual(3, results.First().LogCurveInfo.Count);
            var logCurveInfoList = results.First().LogCurveInfo;

            var mdLogCurveInfo = logCurveInfoList.Where(x => x.Uid.Equals("MD")).FirstOrDefault();
            Assert.IsNotNull(mdLogCurveInfo);
            Assert.IsNull(mdLogCurveInfo.ClassIndex);

            var ropLogCurveInfo = logCurveInfoList.Where(x => x.Uid.Equals("ROP")).FirstOrDefault();
            Assert.IsNotNull(ropLogCurveInfo);
            Assert.IsNull(ropLogCurveInfo.ClassIndex);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Rollback_When_Updating_Invalid_Data()
        {
            var response = DevKit.Add<WellList, Well>(Well);
            Wellbore.UidWell = response.SuppMsgOut;

            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            var logData = log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,");
            logData.Data.Add("15,15.1,15.2");

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);            

            var logAdded = results.First();
            Assert.IsNull(logAdded.Description);

            var logDataAdded = logAdded.LogData.First();
            for (var i = 0; i < logData.Data.Count; i++)
            {
                Assert.AreEqual(logData.Data[i], logDataAdded.Data[i]);
            }

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Description = "Should not be updated"
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);
            logData = update.LogData.First();          
            logData.Data.Add("17,17.1,17.2");
            logData.Data.Add("21,21.1,21.2");
            logData.Data.Add("21,22.1,22.2");

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.NodesWithSameIndex, updateResponse.Result);

            results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var logUpdated = results.First();
            Assert.IsNull(logUpdated.Description);

            var logDataUpdated = logUpdated.LogData.First();
            for (var i = 0; i < logDataAdded.Data.Count; i++)
            {
                Assert.AreEqual(logDataAdded.Data[i], logDataUpdated.Data[i]);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_UpdataInStore_To_Append_With_Null_Indicator_In_Different_Chunks()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = DevKit.CreateLog(null, DevKit.Name("Log 01"), Wellbore.UidWell, Well.Name, response.SuppMsgOut, Wellbore.Name);
            log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            log.NullValue = "-999.25";

            var logData = log.LogData.First();
            logData.Data.Add("1700.0,17.1,-999.25");
            logData.Data.Add("1800.0,18.1,-999.25");
            logData.Data.Add("1900.0,19.1,-999.25");

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            //Update
            var updateLog = DevKit.CreateLog(uidLog, log.Name, log.UidWell, log.NameWell, log.UidWellbore, log.NameWellbore);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2000.0,20.1,-999.25");
            logData.Data.Add("2100.0,21.1,-999.25");
            logData.Data.Add("2200.0,22.1,-999.25");
            logData.Data.Add("2300.0,23.1,23.2");

            var updateResponse = DevKit.Update<LogList, Log>(updateLog);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query
            var query = DevKit.CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);
            query.StartIndex = new GenericMeasure(1700, "ft");
            query.EndIndex = new GenericMeasure(2200, "ft");

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.DataOnly);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(6, results[0].LogData[0].Data.Count);
            Assert.AreEqual(2, results[0].LogData[0].MnemonicList.Split(',').Length);

            var resultLogData = results[0].LogData[0].Data;          
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
        public void Log141DataAdapter_UpdataInStore_Can_Update_With_Null_Indicator_And_Query_In_Range_Covers_Different_Chunks()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = DevKit.CreateLog(null, DevKit.Name("Log 01"), Wellbore.UidWell, Well.Name, response.SuppMsgOut, Wellbore.Name);
            log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            log.NullValue = "-999.25";

            var logData = log.LogData.First();
            logData.Data.Add("1700.0, 17.1, -999.25");
            logData.Data.Add("1800.0, 18.1, -999.25");
            logData.Data.Add("1900.0, 19.1, -999.25");
            logData.Data.Add("2000.0, 20.1,    20.1");
            logData.Data.Add("2100.0, 21.1,    21.1");

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            //Update
            var updateLog = DevKit.CreateLog(uidLog, log.Name, log.UidWell, log.NameWell, log.UidWellbore, log.NameWellbore);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("2000.0, 200.1, -999.25");
            logData.Data.Add("2100.0, 210.1, -999.25");
            logData.Data.Add("2200.0, 220.1,   22.1");

            var updateResponse = DevKit.Update<LogList, Log>(updateLog);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query
            var query = DevKit.CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);
            query.StartIndex = new GenericMeasure(1700, "ft");
            query.EndIndex = new GenericMeasure(2200, "ft");

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.DataOnly);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(6, results[0].LogData[0].Data.Count);
            Assert.AreEqual(3, results[0].LogData[0].MnemonicList.Split(',').Length);

            var resultLogData = results[0].LogData[0].Data;

            Assert.IsTrue(resultLogData[0].Equals("1700,17.1,-999.25"));
            Assert.IsTrue(resultLogData[1].Equals("1800,18.1,-999.25"));
            Assert.IsTrue(resultLogData[2].Equals("1900,19.1,-999.25"));
            Assert.IsTrue(resultLogData[3].Equals("2000,200.1,20.1"));
            Assert.IsTrue(resultLogData[4].Equals("2100,210.1,21.1"));
            Assert.IsTrue(resultLogData[5].Equals("2200,220.1,22.1"));
        }

        [TestMethod]
        public void Log141DataAdapter_UpdataInStore_Can_Replace_Range_In_Different_Chunks_And_With_Null_Indicator()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = DevKit.CreateLog(null, DevKit.Name("Log 01"), Wellbore.UidWell, Well.Name, response.SuppMsgOut, Wellbore.Name);
            log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            log.NullValue = "-999.25";

            var logData = log.LogData.First();
            logData.Data.Add("1700.0, 17.1, 17.2");
            logData.Data.Add("1800.0, 18.1, 18.2");
            logData.Data.Add("1900.0, 19.1, 19.2");
            logData.Data.Add("2000.0, 20.1, 20.1");
            logData.Data.Add("2100.0, 21.1, 21.1");

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            //Update
            var updateLog = DevKit.CreateLog(uidLog, log.Name, log.UidWell, log.NameWell, log.UidWellbore, log.NameWellbore);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = log.LogData.First().UnitList;
            logData = updateLog.LogData.First();
            logData.Data.Add("1800.0, 180.1, -999.25");
            logData.Data.Add("2200.0, 220.1, 22.1");

            var updateResponse = DevKit.Update<LogList, Log>(updateLog);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query
            var query = DevKit.CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);
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
            Well.Uid = "Parent Well - Testing Lock";
            Wellbore.UidWell = Well.Uid;
            Wellbore.Uid = "Parent Wellbore - Testing Lock";
            Log.UidWell = Well.Uid;
            Log.UidWellbore = Wellbore.Uid;
            Log.Uid = "Log - Testing Lock";
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

            var response = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void LogDataAdapter_UpdateInStore_Structural_Ranges_Ignored()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = DevKit.CreateLog(
                null,
                DevKit.Name("Log can be added with depth data"),
                Wellbore.UidWell,
                Well.Name,
                response.SuppMsgOut,
                Wellbore.Name);

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
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

            var update = new Log
            {
                Uid = uidLog,
                UidWell = query.UidWell,
                UidWellbore = query.UidWellbore
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);

            update.StartIndex = new GenericMeasure { Uom = "m", Value = 1.0 };
            update.EndIndex = new GenericMeasure { Uom = "m", Value = 10.0 };

            foreach (var curve in update.LogCurveInfo)
            {
                curve.MinIndex = log.StartIndex;
                curve.MaxIndex = log.EndIndex;
            }

            results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.AreEqual(1, results.Count);

            result = results.First();
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
        public void Log141Validator_UpdateInStore_Index_Curve_Not_First_In_LogCurveInfo()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = DevKit.CreateLog(
                null,
                DevKit.Name("Log can be added with depth data"),
                Wellbore.UidWell,
                Well.Name,
                response.SuppMsgOut,
                Wellbore.Name);

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);
            DevKit.InitDataMany(update, DevKit.Mnemonics(update), DevKit.Units(update), 10);

            var logCurves = update.LogCurveInfo;
            var indexCurve = logCurves.First();
            logCurves.Remove(indexCurve);
            logCurves.Add(indexCurve);

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        [TestMethod]
        public void LogDataAdapter_UpdateInStore_Test_Update_Index_Range()
        {
            const int count = 10;
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = DevKit.CreateLog(
                null,
                DevKit.Name("Log can be added with depth data"),
                Wellbore.UidWell,
                Well.Name,
                response.SuppMsgOut,
                Wellbore.Name);

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), count, hasEmptyChannel:false);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore,
                StartIndex = new GenericMeasure
                {
                    Uom = "m",
                    Value = 11
                }
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);
            DevKit.InitDataMany(update, DevKit.Mnemonics(update), DevKit.Units(update), count, hasEmptyChannel:false);

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.AreEqual(1, results.Count);

            var result = results.First();
            Assert.IsNotNull(result);
            var start = log.StartIndex.Value;
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
        public void Log141DataAdapter_UpdateInStore_Can_Append_Large_Log_Data_In_Different_Chunks()
        {
            // Set the depth range chunk size.
            WitsmlSettings.DepthRangeSize = 1000;

            // Add well
            var response = DevKit.Add<WellList, Well>(Well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;

            // Add wellbore
            Wellbore.UidWell = uidWell;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add large log
            var logXmlIn = File.ReadAllText(Path.Combine(DataDir, "LargeLog.xml"));

            var logList = EnergisticsConverter.XmlToObject<LogList>(logXmlIn);
            Assert.IsNotNull(logList);
            Assert.AreEqual(1, logList.Log.Count);

            var log = logList.Log[0];
            log.UidWell = uidWell;
            log.UidWellbore = uidWellbore; 
                 
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Query added log
            var query = new Log
            {
                Uid = uidLog,
                UidWell = uidWell,
                UidWellbore = uidWellbore
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(5000, results[0].LogData[0].Data.Count);

            // Update log by appending 10000 rows of data
            logXmlIn = File.ReadAllText(Path.Combine(DataDir, "LargeLog_append.xml"));

            logList = EnergisticsConverter.XmlToObject<LogList>(logXmlIn);
            Assert.IsNotNull(logList);
            Assert.AreEqual(1, logList.Log.Count);

            log = logList.Log[0];
            log.UidWell = uidWell;
            log.UidWellbore = uidWellbore;
            log.Uid = uidLog;

            var updateResponse = DevKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query log after appending data
            results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(15000, results[0].LogData[0].Data.Count);
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
        public void Log141DataAdapter_UpdateInStore_Can_Update_LogCurveInfo()
        {
            // Add well
            var response = DevKit.Add<WellList, Well>(Well);

            var uidWell = response.SuppMsgOut;

            // Add wellbore
            Wellbore.UidWell = uidWell;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var uidWellbore = response.SuppMsgOut;

            var log = DevKit.CreateLog(
                null,
                DevKit.Name("Log can be added with depth data"),
                Wellbore.UidWell,
                Well.Name,
                uidWellbore,
                Wellbore.Name);

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            for (var i = log.LogCurveInfo.Count; i < 50; i++)
            {
                var mnemonic = $"Log Curve {i}";
                log.LogCurveInfo.Add(DevKit.LogGenerator.CreateLogCurveInfo(mnemonic, "m", LogDataType.@double));
            }

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            log.Uid = response.SuppMsgOut;

            var updateResponse = DevKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Can_Update_Nested_Recurring_Elements()
        {
            // Add Well and Wellbore
            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);

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
            var addResponse = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, addResponse.Result);

            var query = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            AssertNestedElements(results, curve, extensionName1, extensionName4);

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

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            AssertNestedElements(results, curve, extensionName1, extensionName4);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_With_Custom_Data_Delimiter()
        {
            var delimiter = "|~";
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = DevKit.CreateLog(
                null,
                DevKit.Name("Log can be added with depth data"),
                Wellbore.UidWell,
                Well.Name,
                response.SuppMsgOut,
                Wellbore.Name);

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10, hasEmptyChannel: false);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

            // Assert null data delimiter
            Assert.IsNull(result.DataDelimiter);

            // Update data delimiter
            var update = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore,
                DataDelimiter = delimiter
            };

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            result = results.FirstOrDefault();
            Assert.IsNotNull(result);

            // Assert data delimiter is updated
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
        public void Log141DataAdapter_UpdateInStore_Error_1051_Incorrect_Row_Value_Count()
        {
            const int count = 10;
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = DevKit.CreateLog(
                null,
                DevKit.Name("Log can be added with depth data"),
                Wellbore.UidWell,
                Well.Name,
                response.SuppMsgOut,
                Wellbore.Name);

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), count, hasEmptyChannel: false);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore,
                StartIndex = new GenericMeasure
                {
                    Uom = "m",
                    Value = count
                }
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);
            DevKit.InitDataMany(update, DevKit.Mnemonics(update), DevKit.Units(update), count, hasEmptyChannel: false);

            var logData = update.LogData.FirstOrDefault();
            logData?.Data?.Add("30,30.1,30.2,30.3,30.4");

            update.StartIndex = null;

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.ErrorRowDataCount, updateResponse.Result);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Can_Partial_Update_1()
        {
            var log = AddAnEmptyLogWithFourCurves();

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
            var data = new List<string> {"1,1.2", "2,2.2"};
            logData.Data = data;
            update.LogData.Add(logData);

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

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

            updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

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

            updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

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
        
        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Can_Partial_Update_2()
        {
            var log = AddAnEmptyLogWithFourCurves();

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

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

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

            updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

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

            updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);          
            
            var query = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

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

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Can_Partial_Update_3()
        {
            var log = AddAnEmptyLogWithFourCurves();

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

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

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

            updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

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

            updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

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

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Can_Partial_Update_4()
        {
            var log = AddAnEmptyLogWithFourCurves();

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

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

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

            updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

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

            updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

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
        public void Log141DataAdapter_UpdateInStore_Can_Update_With_Sparse_Data()
        {
            var response = DevKit.Add<WellList, Well>(Well);
            Wellbore.UidWell = response.SuppMsgOut;

            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            var logData = log.LogData.First();
            logData.Data.Add("1,1.1,1.2");
            logData.Data.Add("2,2.1,2.2");
            logData.Data.Add("3,3.1,3.2");
            logData.Data.Add("4,4.1,4.2");

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Description = "Should not be updated"
            };

            var indexCurve = log.LogCurveInfo.FirstOrDefault();
            var channel1 = log.LogCurveInfo[1];
            var channel2 = log.LogCurveInfo[2];

            var logData1 = new LogData
            {
                MnemonicList = $"{indexCurve?.Mnemonic.Value},{channel1.Mnemonic.Value}",
                UnitList = $"{indexCurve?.Unit},{channel1.Unit}",
                Data = new List<string> { "2,2.11"}
            };

            var logData2 = new LogData
            {
                MnemonicList = $"{indexCurve?.Mnemonic.Value},{channel2.Mnemonic.Value}",
                UnitList = $"{indexCurve?.Unit},{channel2.Unit}",
                Data = new List<string> { "3,3.21" }
            };

            update.LogData = new List<LogData> {logData1, logData2};

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.First();
            Assert.IsNotNull(result);

            logData = result.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);

            var data = logData.Data;
            Assert.AreEqual(4, logData.Data.Count);
            Assert.AreEqual("1,1.1,1.2", data[0]);
            Assert.AreEqual("2,2.11,2.2", data[1]);
            Assert.AreEqual("3,3.1,3.21", data[2]);
            Assert.AreEqual("4,4.1,4.2", data[3]);
        }

        #region Helper Functions

        private Log AddAnEmptyLogWithFourCurves()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = DevKit.CreateLog(
                DevKit.Uid(),
                DevKit.Name("Log can be added with depth data"),
                Wellbore.UidWell,
                Well.Name,
                response.SuppMsgOut,
                Wellbore.Name);

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            var channelName = "channel3";
            var curves = log.LogCurveInfo;
            var channel3 = new LogCurveInfo
            {
                Uid = channelName,
                Mnemonic = new ShortNameStruct
                {
                    Value = channelName
                },
                Unit = "ft",
                TypeLogData = LogDataType.@double
            };
            curves.Add(channel3);
            log.LogData = null;

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            return log;
        }

        private void AssertNestedElements(List<Log> results, LogCurveInfo curve, ExtensionNameValue extensionName1, ExtensionNameValue extensionName4)
        {
            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

            var lastCurve = result.LogCurveInfo.Last();
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

        #endregion
    }
}
