//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2017.2
//
// Copyright 2017 PDS Americas LLC
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
using System.Threading;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Compatibility;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data.GrowingObjects;
using PDS.WITSMLstudio.Store.Jobs;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    /// <summary>
    /// Log131DataAdapter Update tests.
    /// </summary>
    [TestClass]
    public partial class Log131DataAdapterUpdateTests : Log131TestBase
    {
        private const int GrowingTimeoutPeriod = 10;
        private string _dataDir = new DirectoryInfo(@".\TestData").FullName;

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_AppendLog_Data()
        {
            Log.StartIndex = new GenericMeasure(5, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(17, "m"), 6);
            UpdateLogData(update);

            var result = GetLog(Log);
            var logData = result.LogData;

            Assert.IsNotNull(logData);
            Assert.AreEqual(16, logData.Count);
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_PrependLog_Data()
        {
            Log.StartIndex = new GenericMeasure(17, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(5, "m"), 6);
            UpdateLogData(update);

            var result = GetLog(Log);
            var logData = result.LogData;

            Assert.IsNotNull(logData);
            Assert.AreEqual(16, logData.Count);
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_Update_OverlappingLog_Data()
        {
            Log.StartIndex = new GenericMeasure(1, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 8);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(4.1, "m"), 3, 0.9);
            UpdateLogData(update);

            var result = GetLog(Log);
            var logData = result.LogData;

            Assert.IsNotNull(logData);
            Assert.AreEqual(9, logData.Count);
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_OverwriteLog_Data_Chunk()
        {
            Log.StartIndex = new GenericMeasure(17, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 6);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(4.1, "m"), 3, 0.9);
            var logData = update.LogData;
            logData.Add("21.5, 1, 21.7");
            UpdateLogData(update);

            var result = GetLog(Log);
            logData = result.LogData;

            Assert.IsNotNull(logData);
            Assert.AreEqual(5, logData.Count);
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_Update_Different_Data_Range_For_Each_Channel()
        {
            Log.StartIndex = new GenericMeasure(15, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 8);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(13, "m"), 6, 0.9);
            var logData = update.LogData;
            logData.Clear();

            logData.Add("13,13.1,");
            logData.Add("14,14.1,");
            logData.Add("15,15.1,");
            logData.Add("16,16.1,");
            logData.Add("17,17.1,");
            logData.Add("20,20.1,20.2");
            logData.Add("21,,21.2");
            logData.Add("22,,22.2");
            logData.Add("23,,23.2");

            UpdateLogData(update);

            var result = GetLog(Log);
            logData = result.LogData;

            Assert.IsNotNull(logData);
            Assert.AreEqual(11, logData.Count);

            Assert.AreEqual("15,15.1,15", logData[2]);
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_UpdateLog_Data_And_Index_Range()
        {
            Log.StartIndex = new GenericMeasure(15, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 8);

            // Make sure there are 3 curves
            var lciUids = Log.LogCurveInfo.Select(l => l.Uid).ToArray();
            Assert.AreEqual(3, lciUids.Length);

            var logAdded = GetLog(Log);
            Assert.IsNotNull(logAdded);
            Assert.AreEqual(15, logAdded.StartIndex.Value);
            Assert.AreEqual(22, logAdded.EndIndex.Value);

            var mdCurve = DevKit.GetLogCurveInfoByUid(logAdded.LogCurveInfo, lciUids[0]) as LogCurveInfo;
            Assert.IsNotNull(mdCurve);
            Assert.AreEqual(logAdded.StartIndex.Value, mdCurve.MinIndex.Value);
            Assert.AreEqual(logAdded.EndIndex.Value, mdCurve.MaxIndex.Value);

            var curve2 = DevKit.GetLogCurveInfoByUid(logAdded.LogCurveInfo, lciUids[1]) as LogCurveInfo;
            Assert.IsNull(curve2);

            var curve3 = DevKit.GetLogCurveInfoByUid(logAdded.LogCurveInfo, lciUids[2]) as LogCurveInfo;
            Assert.IsNotNull(curve3);
            Assert.AreEqual(logAdded.StartIndex.Value, curve3.MinIndex.Value);
            Assert.AreEqual(logAdded.EndIndex.Value, curve3.MaxIndex.Value);

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(13, "m"), 6, 0.9);
            var logData = update.LogData;
            logData.Clear();

            logData.Add("13,13.1,");
            logData.Add("14,14.1,");
            logData.Add("15,15.1,");
            logData.Add("16,16.1,");
            logData.Add("17,17.1,");
            logData.Add("20,20.1,20.2");
            logData.Add("21,,21.2");
            logData.Add("22,,22.2");
            logData.Add("23,,23.2");

            UpdateLogData(update);

            var logUpdated = GetLog(Log);
            logData = logUpdated.LogData;

            Assert.IsNotNull(logData);
            Assert.AreEqual(11, logData.Count);
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
        public void Log131DataAdapter_UpdateInStore_AppendLog_Data_Set_ObjectGrowing_And_IsActive_State()
        {
            Log.StartIndex = new GenericMeasure(5, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            var addedLog = DevKit.GetAndAssert(Log);
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(17, "m"), 6);
            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_UpdateLog_Empty_Channel_Unchanged_ObjectGrowing_And_IsActive_State()
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
        public void Log131DataAdapter_UpdateInStore_UpdateLog_Unchanged_ObjectGrowing_And_IsActive_State()
        {
            AddParents();
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 5);

            var logData = Log.LogData;
            logData.Clear();
            logData.Add("1,11.1,10");
            logData.Add("2,13.1,11");
            logData.Add("3,13.1,11");
            logData.Add("4,13.1,11");

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var addedLog = DevKit.GetAndAssert(Log);
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault());

            // Update
            var updateLog = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(8, "m"), 3, 0.2);
            logData = updateLog.LogData;
            logData.Clear();

            logData.Add("2.1,11.1,10");
            logData.Add("2.2,13.1,11");
            DevKit.UpdateAndAssert(updateLog);

            var result = DevKit.GetAndAssert(updateLog);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_AppendLog_Data_ExpireGrowingObjects()
        {
            Log.StartIndex = new GenericMeasure(5, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            var addedLog = DevKit.GetAndAssert(Log);
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault());

            var update = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(17, "m"), 6);
            DevKit.UpdateAndAssert(update);

            var result = DevKit.GetAndAssert(Log);
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault());

            WitsmlSettings.LogGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            result = DevKit.GetAndAssert(Log);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_Can_Expire_Growing_Object_After_Delete()
        {
            // Add parent well and wellbore and create a log
            AddParents();
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 5);

            // Clear default log data and create Log Data specific to our test
            var logData = Log.LogData;
            logData.Clear();
            logData.Add("1,11.1,10");
            logData.Add("2,13.1,11");
            logData.Add("3,13.1,11");
            logData.Add("4,13.1,11");

            // Add Log with Log Data
            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Veryify that Object Growing is false after an Add with Log data
            var addedLog = DevKit.GetAndAssert(Log);
            var uri = addedLog.GetUri();
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault());

            // Create a Log for update
            var updateLog = CreateLogDataUpdate(Log, LogIndexType.measureddepth, new GenericMeasure(8, "m"), 3, 0.2);
            logData = updateLog.LogData;
            logData.Clear();

            // Add data that will append to the log
            logData.Add("5.1,11.1,10");
            logData.Add("6.2,13.1,11");
            DevKit.UpdateAndAssert(updateLog);

            // Verify that the append set the objectGrowing flag to true.
            var result = DevKit.GetAndAssert(updateLog);
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsTrue(DevKit.Container.Resolve<IGrowingObjectDataProvider>().Exists(uri));

            // Delete the growing object
            var deleteLog = DevKit.CreateLog(result.Uid, result.Name, result.UidWell, result.NameWell,
                result.UidWellbore, result.NameWellbore);
            DevKit.DeleteAndAssert(deleteLog);

            // Wait until we're past the GrowingTimeoutPeriod
            WitsmlSettings.LogGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            // Expire the growing objects.  By calling this after the delete of the log 
            // ... we're testing that an Exception wasn't raised.
            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            // The dbGrowingObject should have been deleted after the Log was deleted.
            Assert.IsFalse(DevKit.Container.Resolve<IGrowingObjectDataProvider>().Exists(uri));
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_Update_TimeLog_Data_Unchanged_ObjectGrowing_And_IsActive_State()
        {
            AddParents();
            DevKit.InitHeader(Log, LogIndexType.datetime);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 5, 1, false, false);

            var logData = Log.LogData;
            logData.Clear();
            logData.Add("2016-04-13T15:31:42.0000000-05:00,32.1,32.2");
            logData.Add("2016-04-13T15:32:42.0000000-05:00,31.1,31.2");
            logData.Add("2016-04-13T15:38:42.0000000-05:00,30.1,30.2");

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var addedLog = DevKit.GetAndAssert(Log);
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault());

            // Update
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            DevKit.InitHeader(updateLog, LogIndexType.datetime);
            DevKit.InitDataMany(updateLog, DevKit.Mnemonics(Log), DevKit.Units(Log), 5, 1, false, false);

            logData = updateLog.LogData;
            logData.Clear();
            logData.Add("2016-04-13T15:35:42.0000000-05:00,35.1,35.2");
            logData.Add("2016-04-13T15:34:42.0000000-05:00,34.1,34.2");
            logData.Add("2016-04-13T15:33:42.0000000-05:00,33.1,33.2");
            logData.Add("2016-04-13T15:36:42.0000000-05:00,36.1,36.2");

            DevKit.UpdateAndAssert(updateLog);

            var result = DevKit.GetAndAssert(updateLog);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_Append_TimeLog_Data_Set_ObjectGrowing_And_IsActive_State_ExpireGrowingObjects()
        {
            AddParents();
            DevKit.InitHeader(Log, LogIndexType.datetime);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 5, 1, false, false);

            var logData = Log.LogData;
            logData.Clear();
            logData.Add("2016-04-13T15:31:42.0000000-05:00,32.1,32.2");
            logData.Add("2016-04-13T15:32:42.0000000-05:00,31.1,31.2");
            logData.Add("2016-04-13T15:38:42.0000000-05:00,30.1,30.2");

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var addedLog = DevKit.GetAndAssert(Log);
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault());

            // Update
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            DevKit.InitHeader(updateLog, LogIndexType.datetime);
            DevKit.InitDataMany(updateLog, DevKit.Mnemonics(Log), DevKit.Units(Log), 5, 1, false, false);

            logData = updateLog.LogData;
            logData.Clear();
            logData.Add("2016-04-13T15:30:42.0000000-05:00,35.1,35.2");
            logData.Add("2016-04-13T15:34:42.0000000-05:00,34.1,34.2");
            logData.Add("2016-04-13T15:33:42.0000000-05:00,33.1,33.2");
            logData.Add("2016-04-13T15:39:42.0000000-05:00,36.1,36.2");

            DevKit.UpdateAndAssert(updateLog);

            var result = DevKit.GetAndAssert(updateLog);
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");

            WitsmlSettings.LogGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            result = DevKit.GetAndAssert(Log);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_Invalid_Data_Rows()
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
        public void Log131DataAdapter_UpdateInStore_With_New_Row_Updates_DataRowCount()
        {
            AddParents();

            // Add a Log with dataRowCount Rows
            const int dataRowCount = 10;
            DevKit.AddLogWithData(Log, LogIndexType.measureddepth, dataRowCount, false);

            // Create an Update log with totalUpdateRows
            const int totalUpdateRows = 1;
            var updateLog = DevKit.CreateUpdateLogWithRows(Log, totalUpdateRows);

            // Update the Log with a new Row
            DevKit.UpdateAndAssert(updateLog);

            DevKit.GetAndAssertDataRowCount(DevKit.CreateLog(Log), dataRowCount + totalUpdateRows);
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_Updates_Existing_Row_Does_Not_Change_DataRowCount()
        {
            AddParents();

            // Add a Log with dataRowCount Rows
            const int dataRowCount = 10;
            DevKit.AddLogWithData(Log, LogIndexType.measureddepth, dataRowCount, false);

            // Create an Update log that updates the last row of LogData
            var updateLog = DevKit.CreateLog(Log);
            updateLog.LogCurveInfo = Log.LogCurveInfo;
            updateLog.LogData = new List<string> {Log.LogData[Log.LogData.Count - 1]};

            // Update the Log with a new Row
            DevKit.UpdateAndAssert(updateLog);

            DevKit.GetAndAssertDataRowCount(DevKit.CreateLog(Log), dataRowCount);
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_Updates_Existing_And_New_Row_Updates_DataRowCount()
        {
            AddParents();

            // Add a Log with dataRowCount Rows
            const int dataRowCount = 10;
            DevKit.AddLogWithData(Log, LogIndexType.measureddepth, dataRowCount, false);

            // Create an Update log with totalUpdateRows
            const int totalUpdateRows = 1;
            var updateLog = DevKit.CreateUpdateLogWithRows(Log, totalUpdateRows);

            // Add an existing row to the top of the updateLog's LogData
            updateLog.LogData.Insert(0, Log.LogData[Log.LogData.Count - 1]);

            // Update the Log with a new Row
            DevKit.UpdateAndAssert(updateLog);

            DevKit.GetAndAssertDataRowCount(DevKit.CreateLog(Log), dataRowCount + totalUpdateRows);
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_Multi_Update_Merge_With_File_Storage()
        {
            WitsmlSettings.DepthRangeSize = 5000;
            WitsmlSettings.MaxDataLength = 5000000;

            // Add Well
            var response = DevKit.Add_Well_from_file(Path.Combine(_dataDir, "Test-chunk-file-merge-well131-add.xml"));
            // There is no response if the Well already exists in the database
            if (response != null)
            {
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }

            // Add Wellbore
            response = DevKit.Add_Wellbore_from_file(Path.Combine(_dataDir, "Test-chunk-file-merge-wellbore131-add.xml"));
            if (response != null)
            {
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }

            // Add Log Header
            response = DevKit.Add_Log_from_file(Path.Combine(_dataDir, "Test-chunk-file-merge-log131-add.xml"));
            if (response != null)
            {
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);

                // Update Log with data ranging from 4.9 - 1304.8
                var updateResponse = DevKit.Update_Log_from_file(Path.Combine(_dataDir, "Test-chunk-file-merge-log131-update1.xml"));
                if (updateResponse != null)
                {
                    Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
                }

                // Update Log with data ranging from 1304.9 - 2604.8
                updateResponse = DevKit.Update_Log_from_file(Path.Combine(_dataDir, "Test-chunk-file-merge-log131-update2.xml"));
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
            Assert.AreEqual(2, logData.Count);
        }

        #region Helper Functions

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

        private void AddLogWithData(Log log, LogIndexType indexType, int numOfRows)
        {
            AddParents();

            DevKit.InitHeader(log, indexType);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), numOfRows);

            var response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        private Log CreateLogDataUpdate(Log log, LogIndexType indexType, GenericMeasure startIndex, int numOfRows, double factor = 1)
        {
            var update = CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.StartIndex = startIndex;

            DevKit.InitHeader(update, indexType);
            DevKit.InitDataMany(update, DevKit.Mnemonics(update), DevKit.Units(update), numOfRows, factor);

            return update;
        }

        private void UpdateLogData(Log log)
        {
            var updateResponse = DevKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        private Log GetLog(Log log)
        {
            return DevKit.GetAndAssert(log);
        }

        #endregion
    }
}
