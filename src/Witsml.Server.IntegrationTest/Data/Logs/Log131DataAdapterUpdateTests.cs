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

using System.Linq;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Log131DataAdapter Update tests.
    /// </summary>
    public partial class Log131DataAdapterUpdateTests
    {
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
        public void Log141DataAdapter_UpdateInStore_UpdateLog_Data_And_Index_Range()
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
