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
    [TestClass]
    public class Log131DataAdapterUpdateTests
    {
        private DevKit131Aspect _devKit;
        private Well _well;
        private Wellbore _wellbore;
        private Log _log;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit131Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version131.Value)
                .ToArray();

            _well = new Well { Uid = _devKit.Uid(), Name = _devKit.Name("Well 01"), TimeZone = _devKit.TimeZone };

            _wellbore = new Wellbore()
            {
                Uid = _devKit.Uid(),
                UidWell = _well.Uid,
                NameWell = _well.Name,
                Name = _devKit.Name("Wellbore 01")
            };

            _log = CreateLog(_devKit.Uid(), _devKit.Name("Log 01"), _well.Uid, _well.Name, _wellbore.Uid, _wellbore.Name);
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_Append_Log_Data()
        {
            _log.StartIndex = new GenericMeasure(5, "m");
            AddLogWithData(_log, LogIndexType.measureddepth, 10);

            var update = CreateLogDataUpdate(_log, LogIndexType.measureddepth, new GenericMeasure(17, "m"), 6);          
            UpdateLogData(update);

            var result = GetLog(_log);
            var logData = result.LogData;

            Assert.IsNotNull(logData);
            Assert.AreEqual(16, logData.Count);
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_Prepend_Log_Data()
        {
            _log.StartIndex = new GenericMeasure(17, "m");
            AddLogWithData(_log, LogIndexType.measureddepth, 10);

            var update = CreateLogDataUpdate(_log, LogIndexType.measureddepth, new GenericMeasure(5, "m"), 6);
            UpdateLogData(update);

            var result = GetLog(_log);
            var logData = result.LogData;

            Assert.IsNotNull(logData);
            Assert.AreEqual(16, logData.Count);
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_Update_Overlapping_Log_Data()
        {
            _log.StartIndex = new GenericMeasure(1, "m");
            AddLogWithData(_log, LogIndexType.measureddepth, 8);

            var update = CreateLogDataUpdate(_log, LogIndexType.measureddepth, new GenericMeasure(4.1, "m"), 3, 0.9);
            UpdateLogData(update);

            var result = GetLog(_log);
            var logData = result.LogData;

            Assert.IsNotNull(logData);
            Assert.AreEqual(9, logData.Count);
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_Overwrite_Log_Data_Chunk()
        {
            _log.StartIndex = new GenericMeasure(17, "m");
            AddLogWithData(_log, LogIndexType.measureddepth, 6);

            var update = CreateLogDataUpdate(_log, LogIndexType.measureddepth, new GenericMeasure(4.1, "m"), 3, 0.9);
            var logData = update.LogData;
            logData.Add("21.5, 1, 21.7");
            UpdateLogData(update);

            var result = GetLog(_log);
            logData = result.LogData;

            Assert.IsNotNull(logData);
            Assert.AreEqual(5, logData.Count);
        }

        [TestMethod]
        public void Log131DataAdapter_UpdateInStore_Update_Different_Data_Range_For_Each_Channel()
        {
            _log.StartIndex = new GenericMeasure(15, "m");
            AddLogWithData(_log, LogIndexType.measureddepth, 8);

            var update = CreateLogDataUpdate(_log, LogIndexType.measureddepth, new GenericMeasure(13, "m"), 6, 0.9);
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

            var result = GetLog(_log);
            logData = result.LogData;

            Assert.IsNotNull(logData);
            Assert.AreEqual(11, logData.Count);

            Assert.AreEqual("15,15.1,15", logData[2]);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Update_Log_Data_And_Index_Range()
        {
            _log.StartIndex = new GenericMeasure(15, "m");
            AddLogWithData(_log, LogIndexType.measureddepth, 8);

            // Make sure there are 3 curves
            var lciUids = _log.LogCurveInfo.Select(l => l.Uid).ToArray();
            Assert.AreEqual(3, lciUids.Length);
           
            var logAdded = GetLog(_log);
            Assert.IsNotNull(logAdded);
            Assert.AreEqual(15, logAdded.StartIndex.Value);
            Assert.AreEqual(22, logAdded.EndIndex.Value);

            var mdCurve = _devKit.GetLogCurveInfoByUid(logAdded.LogCurveInfo, lciUids[0]) as LogCurveInfo;
            Assert.IsNotNull(mdCurve);
            Assert.AreEqual(logAdded.StartIndex.Value, mdCurve.MinIndex.Value);
            Assert.AreEqual(logAdded.EndIndex.Value, mdCurve.MaxIndex.Value);

            var curve2 = _devKit.GetLogCurveInfoByUid(logAdded.LogCurveInfo, lciUids[1]) as LogCurveInfo;
            Assert.IsNull(curve2);

            var curve3 = _devKit.GetLogCurveInfoByUid(logAdded.LogCurveInfo, lciUids[2]) as LogCurveInfo;
            Assert.IsNotNull(curve3);
            Assert.AreEqual(logAdded.StartIndex.Value, curve3.MinIndex.Value);
            Assert.AreEqual(logAdded.EndIndex.Value, curve3.MaxIndex.Value);

            var update = CreateLogDataUpdate(_log, LogIndexType.measureddepth, new GenericMeasure(13, "m"), 6, 0.9);
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

            var logUpdated = GetLog(_log);
            logData = logUpdated.LogData;

            Assert.IsNotNull(logData);
            Assert.AreEqual(11, logData.Count);
            Assert.AreEqual(13, logUpdated.StartIndex.Value);
            Assert.AreEqual(23, logUpdated.EndIndex.Value);

            mdCurve = _devKit.GetLogCurveInfoByUid(logUpdated.LogCurveInfo, lciUids[0]) as LogCurveInfo;
            Assert.IsNotNull(mdCurve);
            Assert.AreEqual(logUpdated.StartIndex.Value, mdCurve.MinIndex.Value);
            Assert.AreEqual(logUpdated.EndIndex.Value, mdCurve.MaxIndex.Value);

            curve2 = _devKit.GetLogCurveInfoByUid(logUpdated.LogCurveInfo, lciUids[1]) as LogCurveInfo;
            Assert.IsNotNull(curve2);
            Assert.AreEqual(13, curve2.MinIndex.Value);
            Assert.AreEqual(20, curve2.MaxIndex.Value);

            curve3 = _devKit.GetLogCurveInfoByUid(logUpdated.LogCurveInfo, lciUids[2]) as LogCurveInfo;
            Assert.IsNotNull(curve3);
            Assert.AreEqual(15, curve3.MinIndex.Value);
            Assert.AreEqual(23, curve3.MaxIndex.Value);
        }

        #region Helper Functions

        private void AddParents()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
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

        private void AddLogWithData(Log log, LogIndexType indexType, int numOfRows)
        {
            AddParents();

            _devKit.InitHeader(log, indexType);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), numOfRows);

            var response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        private Log CreateLogDataUpdate(Log log, LogIndexType indexType, GenericMeasure startIndex, int numOfRows, double factor = 1)
        {
            var update = CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.StartIndex = startIndex;

            _devKit.InitHeader(update, indexType);
            _devKit.InitDataMany(update, _devKit.Mnemonics(update), _devKit.Units(update), numOfRows, factor);

            return update;
        }

        private void UpdateLogData(Log log)
        {
            var updateResponse = _devKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        private Log GetLog(Log log)
        {
            var query = CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

            return result;
        }

        #endregion
    }
}
