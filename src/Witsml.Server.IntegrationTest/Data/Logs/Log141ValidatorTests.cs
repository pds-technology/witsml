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
    public class Log141ValidatorTests
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

            _devKit = null;
        }       

        [TestMethod]
        public void Log141Validator_AddToStore_Error_447_Duplicate_Column_Identifiers_In_LogCurveInfo()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            // Set the 3rd mnemonic to the 2nd in LogCurveInfo
            _log.LogCurveInfo[2].Mnemonic.Value = _log.LogCurveInfo[1].Mnemonic.Value;

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.DuplicateColumnIdentifiers, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_450_mnemonics_not_unique()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            // Set the 3rd mnemonic to the 2nd in the LogData.MnemonicList
            var logData = _log.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);

            var mnemonics = logData.MnemonicList.Split(',');
            mnemonics[2] = mnemonics[1];
            
            logData.MnemonicList = string.Join(",", mnemonics);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.MnemonicsNotUnique, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_449_Index_Curve_Not_Found_In_LogData_MnemonicList()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            // Remove the index curve from the LogData.MnemonicList
            var logData = _log.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);

            var mnemonics = logData.MnemonicList.Split(',');
            logData.MnemonicList = string.Join(",", mnemonics.Where(m => m != _log.IndexCurve));

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.IndexCurveNotFound, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_456_Max_Data_Exceeded_For_Nodes()
        {
            var maxDataNodes = 5;
            WitsmlSettings.MaxDataNodes = maxDataNodes;

            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            // Create a Data set with one more row than maxNodes
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), maxDataNodes + 1);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.MaxDataExceeded, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_456_Max_Data_Exceeded_For_Points()
        {
            var maxDataPoints = 20;
            WitsmlSettings.MaxDataPoints = maxDataPoints;

            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            // Create a Data set with one more row than maxNodes
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), (maxDataPoints / _log.LogCurveInfo.Count) + 1);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.MaxDataExceeded, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_458_Mixed_Index_Types_In_Log()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            // Add a StartDateTimeIndex to the Depth Log
            _log.StartDateTimeIndex = DateTimeOffset.Now;

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.MixedStructuralRangeIndices, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_459_Bad_Column_Identifier_In_LogCurveInfo()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            // Test all Illegal characters => { "'", "\"", "<", ">", "/", "\\", "&", "," }

            // Test &
            AddLogBadColumnIdentifier(_log, "&");

            // Test "
            AddLogBadColumnIdentifier(_log, "\"");

            // Test '
            AddLogBadColumnIdentifier(_log, "'");

            // Test >
            AddLogBadColumnIdentifier(_log, ">");

            // Test <
            AddLogBadColumnIdentifier(_log, "<");

            // Test \
            AddLogBadColumnIdentifier(_log, "\\");

            // Test /
            AddLogBadColumnIdentifier(_log, "/");

            // Test ,
            AddLogBadColumnIdentifier(_log, ",");
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_459_Bad_Char_In_Mnemonics()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            // Test all Illegal characters => { "'", "\"", "<", ">", "/", "\\", "&", "," }
            var logData = _log.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);
            var mnemonics = logData.MnemonicList.Split(',');

            // Test &
            AddLogBadCharInMnemonics(_log, mnemonics, "&");

            // Test "
            AddLogBadCharInMnemonics(_log, mnemonics, "\"");

            // Test '
            AddLogBadCharInMnemonics(_log, mnemonics, "'");

            // Test >
            AddLogBadCharInMnemonics(_log, mnemonics, ">");

            // Test <
            AddLogBadCharInMnemonics(_log, mnemonics, "<");

            // Test \
            AddLogBadCharInMnemonics(_log, mnemonics, "\\");

            // Test /
            AddLogBadCharInMnemonics(_log, mnemonics, "/");
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_442_OptionsIn_Keyword_Not_Supported()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            var response = _devKit.Add<LogList, Log>(_log, optionsIn: "compressionMethod=gzip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByServer, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_464_Child_Uids_Not_Unique()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            // Make all child uids the same for LogCurveInfos
            _log.LogCurveInfo.ForEach(lci => lci.Uid = "lci1");

            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            var response = _devKit.Add<LogList, Log>(_log);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_486_Data_Object_Types_Dont_Match()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            var logs = new LogList { Log = _devKit.List(_log) };
            var xmlIn = EnergisticsConverter.ObjectToXml(logs);
            var response = _devKit.AddToStore(ObjectTypes.Wellbore, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypesDontMatch, response.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_433_Object_Not_Exist()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var update = _devKit.Update<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotExist, update.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_415_Missing_Uid()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            _log.Uid = null;
            var update = _devKit.Update<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidMissing, update.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_464_Curve_Uid_Not_Unique()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _log.LogCurveInfo.ForEach(l => l.Uid = "uid01");

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, response.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_464_Curve_Uid_Not_Unique()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log
            {
                Uid = _log.Uid,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore,
                NameWell = string.Empty,
                LogCurveInfo = _log.LogCurveInfo
            };

            update.LogCurveInfo.ForEach(l => l.Uid = "abc");

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, updateResponse.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_448_Missing_Curve_Uid()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log
            {
                Uid = _log.Uid,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore,
                NameWell = string.Empty,
                LogCurveInfo = _log.LogCurveInfo
            };

            update.LogCurveInfo.Last().Uid = string.Empty;

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForUpdate, updateResponse.Result);
        }
       
        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_480_Adding_Updating_Curves_Simultaneously()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log
            {
                Uid = _log.Uid,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore,
                LogCurveInfo = _log.LogCurveInfo
            };

            update.LogCurveInfo.Last().Uid = "NewCurve";

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.AddingUpdatingLogCurveAtTheSameTime, updateResponse.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_434_Missing_Mnemonics_When_Updating_Log_Data()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log()
            {
                Uid = _log.Uid,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore
            };

            _devKit.InitHeader(update, LogIndexType.measureddepth);

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1,");
            logData.Data.Add("14,14.1,");
            logData.MnemonicList = string.Empty;

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MissingColumnIdentifiers, updateResponse.Result);
        }

        [TestMethod, Description("Test Error 434 LogCurveInfo has fewer channels than the Mnemonic list")]
        public void Log141Validator_UpdateInStore_Error_434_Missing_Mnemonics_In_LogCurveInfo()
        {
            AddParents();

            ///////////////////////////////////////////////////////////////
            // Add a Log with only the index channel in the LogCurveInfo //
            ///////////////////////////////////////////////////////////////

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            // Remove all LogCurveInfo except for the index channel
            _log.LogCurveInfo.RemoveAt(2);
            _log.LogCurveInfo.RemoveAt(1);
            _log.LogData.Clear();

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            ////////////////////////////////////////////////////////////////////
            // Update the Log with data for two channels in the mnemonic list //
            ////////////////////////////////////////////////////////////////////
            var update = new Log()
            {
                Uid = _log.Uid,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore
            };

            _devKit.InitHeader(update, LogIndexType.measureddepth);
            update.LogCurveInfo.Clear();

            // Add data for index channel and one other channel
            var logData = update.LogData.First();
            logData.Data.Add("13,13.1");
            logData.Data.Add("14,14.1");
            logData.MnemonicList = "MD,ROP";
            logData.UnitList = "m,m/h";

            // Assert -434 error
            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MissingColumnIdentifiers, updateResponse.Result);
        }

        [TestMethod, Description("Test Error 434 LogCurveInfo and Mnemonic list have the same count but one channel does not match")]
        public void Log141Validator_UpdateInStore_Error_434_Mnemonics_Do_Not_Match_LogCurveInfo()
        {
            AddParents();

            /////////////////////////////////////////////////////
            // Add a Log with two channels in the LogCurveInfo //
            /////////////////////////////////////////////////////

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            // Remove the last channel from LogCurveInfo, that should leave MD and ROP
            _log.LogCurveInfo.RemoveAt(2);
            _log.LogData.Clear();

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Update the Log with data for two channels in the mnemonic list, but one channel does not match LogCurveInfo //
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var update = new Log()
            {
                Uid = _log.Uid,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore
            };

            _devKit.InitHeader(update, LogIndexType.measureddepth);
            update.LogCurveInfo.Clear();

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1");
            logData.Data.Add("14,14.1");
            logData.MnemonicList = "MD,ROP1"; // Last channel does not match what's in LogCurveInfo
            logData.UnitList = "m,m/h";

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MissingColumnIdentifiers, updateResponse.Result);
        }

        [TestMethod, Description("Index range should not be specified when updating log data")]
        public void Log141Validator_UpdateInStore_Error_436_Index_Range_Specified()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log()
            {
                Uid = _log.Uid,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore
            };

            _devKit.InitHeader(update, LogIndexType.measureddepth);
            update.LogCurveInfo.RemoveAt(2);
            var lastCurve = update.LogCurveInfo.Last();
            lastCurve.Uid = "NewCurve";
            lastCurve.Mnemonic.Value = "NewCurve";
            lastCurve.MinIndex = new GenericMeasure { Value = 13, Uom = "m" };

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1");
            logData.Data.Add("14,14.1");
            logData.MnemonicList = _devKit.Mnemonics(update);

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.IndexRangeSpecified, updateResponse.Result);
        }

        [TestMethod, Description("Unit list is missing in log data when updating log data")]
        public void Log141Validator_UpdateInStore_Error_451_Missing_Units()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log()
            {
                Uid = _log.Uid,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore
            };

            _devKit.InitHeader(update, LogIndexType.measureddepth);

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1,");
            logData.Data.Add("14,14.1,");
            logData.UnitList = string.Empty;

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MissingUnitList, updateResponse.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_452_Units_Not_Match()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log()
            {
                Uid = _log.Uid,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore
            };

            _devKit.InitHeader(update, LogIndexType.measureddepth);
            var indexCurve = update.LogCurveInfo.First();
            indexCurve.Unit = "ft";

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,");

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.UnitListNotMatch, updateResponse.Result);
        }

        [TestMethod, Description("Index curve is missing when updating log data")]
        public void Log141Validator_UpdateInStore_Error_449_Index_Curve_Missing()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log()
            {
                Uid = _log.Uid,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore
            };

            _devKit.InitHeader(update, LogIndexType.measureddepth);

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,");
            var mnemonics = logData.MnemonicList.Split(',');
            logData.MnemonicList = string.Join(",", mnemonics.Where(m => m != _log.IndexCurve));

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.IndexCurveNotFound, updateResponse.Result);
        }

        [TestMethod, Description("Unit list in logData is not specified when add log date")]
        public void Log141Validator_AddToStore_Error_453_Units_Not_Specified()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            // Set the 3rd mnemonic to the 2nd in LogCurveInfo
            var logData = _log.LogData.First();
            logData.UnitList = "m,";

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, response.Result);
        }

        [TestMethod, Description("Mismatch in units between log curve and log data when adding log data")]
        public void Log141Validator_AddToStore_Error_453_Mismatched_UnitList()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            // Set the 3rd UnitList entry to an empty string
            var logData = _log.LogData.First();
            logData.UnitList = "m,m/h,";

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, response.Result);
        }

        [TestMethod, Description("Mismatch in units between log curve and log data when updating log data")]
        public void Log141Validator_UpdateInStore_Error_453_Mismatched_UnitList()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            // Set the 3rd mnemonic to the 2nd in LogCurveInfo
            var logData = _log.LogData.First();
            logData.UnitList = "m,";

            var update = _devKit.Update<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, update.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_448_Missing_Log_Param_Uid_Add()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var logParam = new IndexedObject
            {
                Description = "Md Index"
            };

            _log.LogParam = new List<IndexedObject> { logParam };

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForAdd, response.Result);
        }

        [TestMethod]
        public void Log141Validator_GetFromStore_Error_429_Has_Recurring_Data_Section()
        {
            _log.LogData = new List<LogData>() { new LogData() { MnemonicList = "MD,GR" }, new LogData() { MnemonicList = "MD,ROP" } };

            var result = _devKit.Get<LogList, Log>(_devKit.List(_log), ObjectTypes.Log, null, optionsIn: OptionsIn.ReturnElements.Requested);

            Assert.AreEqual((short)ErrorCodes.RecurringLogData, result.Result);
        }

        [TestMethod]
        public void Log141Validator_GetFromStore_Error_482_LogData_Has_Duplicate_Mnemonics()
        {
            _log.LogData = new List<LogData>() { new LogData() { MnemonicList = "MD,GR,MD" } };

            var result = _devKit.Get<LogList, Log>(_devKit.List(_log), ObjectTypes.Log, null, optionsIn: OptionsIn.ReturnElements.Requested);

            Assert.AreEqual((short)ErrorCodes.DuplicateMnemonics, result.Result);
        }

        [TestMethod]
        public void Log141Validator_GetFromStore_Error_458_Mixed_Structural_Range_Indices_Allowed_When_Not_Requesting_Log_Data()
        {
            _log.StartIndex = new GenericMeasure();
            _log.EndDateTimeIndex = new Timestamp();

            var result = _devKit.Get<LogList, Log>(_devKit.List(_log), ObjectTypes.Log, null, optionsIn: OptionsIn.ReturnElements.Requested);

            Assert.AreEqual((short)ErrorCodes.Success, result.Result);
        }

        [TestMethod]
        public void Log141Validator_GetFromStore_Error_458_Mixed_Structural_Range_Indices_Not_Allowed_When_Requesting_Log_Data()
        {
            _log.StartIndex = new GenericMeasure(1000.0, "ft");
            _log.EndDateTimeIndex = new Timestamp();
            _log.LogData = new List<LogData>
            {
                new LogData
                {
                    Data = new List<string>()
                }
            };

            var result = _devKit.Get<LogList, Log>(_devKit.List(_log), ObjectTypes.Log, null, optionsIn: OptionsIn.ReturnElements.Requested);

            Assert.AreEqual((short)ErrorCodes.MixedStructuralRangeIndices, result.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_450_mnemonics_not_unique()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log
            {
                Uid = _log.Uid,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore,
            };

            update.LogData = new List<LogData> { new LogData
            {
                MnemonicList = "MD,MD,GR",
                UnitList = "m,m,gAPI",
                Data = new List<string> {"1,1,1" }
            } };

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MnemonicsNotUnique, updateResponse.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_481_Well_Missing()
        {
            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.MissingParentDataObject, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_405_Log_Already_Exists()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidAlreadyExists, response.Result);
        }

        [TestMethod]
        public void Log141Validator_GetFromStore_Error_460_Column_Identifiers_In_Header_And_Data_Not_Same()
        {
            _log.LogCurveInfo = new List<LogCurveInfo>();
            _log.LogCurveInfo.Add(new LogCurveInfo() { Uid = "MD", Mnemonic = new ShortNameStruct("MD") });
            _log.LogCurveInfo.Add(new LogCurveInfo() { Uid = "GR", Mnemonic = new ShortNameStruct("GR") });

            _log.LogData = new List<LogData>() { new LogData() { MnemonicList = "MD" } };

            var list = _devKit.New<LogList>(x => x.Log = _devKit.List(_log));
            var queryIn = WitsmlParser.ToXml(list);
            var result = _devKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");

            Assert.AreEqual((short)ErrorCodes.ColumnIdentifiersNotSame, result.Result);
        }

        [TestMethod]
        public void Log141Validator_GetFromStore_Error_461_Missing_Mnemonic_Element_In_Column_Definition()
        {
            _log.LogCurveInfo = new List<LogCurveInfo>();
            _log.LogCurveInfo.Add(new LogCurveInfo() { Uid = "MD" });

            var list = _devKit.New<LogList>(x => x.Log = _devKit.List(_log));
            var queryIn = WitsmlParser.ToXml(list);
            var result = _devKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");

            Assert.AreEqual((short)ErrorCodes.MissingMnemonicElement, result.Result);
        }

        [TestMethod]
        public void Log141Validator_GetFromStore_Error_462_Missing_MnemonicList_In_Data_Section()
        {
            string queryIn = "<logs version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                     "<log uidWell = \"abc\" uidWellbore = \"abc\" uid = \"abc\">" + Environment.NewLine +
                     "    <logData/>" + Environment.NewLine +
                     "</log>" + Environment.NewLine +
                     "</logs>";

            var result = _devKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");

            Assert.AreEqual((short)ErrorCodes.MissingMnemonicList, result.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_409_DataDelimiter_Max_Size_Exceeded()
        {
            TestAddLogWithDelimiter("123", ErrorCodes.InputTemplateNonConforming);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_409_DataDelimiter_With_Bad_Characters()
        {
            TestAddLogWithDelimiter("0", ErrorCodes.InputTemplateNonConforming);
            TestAddLogWithDelimiter("1", ErrorCodes.InputTemplateNonConforming);
            TestAddLogWithDelimiter("5", ErrorCodes.InputTemplateNonConforming);
            TestAddLogWithDelimiter("9", ErrorCodes.InputTemplateNonConforming);
            TestAddLogWithDelimiter("+", ErrorCodes.InputTemplateNonConforming);
            TestAddLogWithDelimiter("-", ErrorCodes.InputTemplateNonConforming);
            TestAddLogWithDelimiter("# ", ErrorCodes.InputTemplateNonConforming);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_483_DataDelimiter_Max_Size_Exceeded()
        {
            TestUpdateLogWithDelimiter("123", ErrorCodes.UpdateTemplateNonConforming);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_483_DataDelimiter_With_Bad_Charaters()
        {
            var log = TestAddLogWithDelimiter(",", ErrorCodes.Success);
            TestUpdateLogWithDelimiter("0", ErrorCodes.UpdateTemplateNonConforming, log);
            TestUpdateLogWithDelimiter("2", ErrorCodes.UpdateTemplateNonConforming, log);
            TestUpdateLogWithDelimiter("6", ErrorCodes.UpdateTemplateNonConforming, log);
            TestUpdateLogWithDelimiter("8", ErrorCodes.UpdateTemplateNonConforming, log);
            TestUpdateLogWithDelimiter("+", ErrorCodes.UpdateTemplateNonConforming, log);
            TestUpdateLogWithDelimiter("-", ErrorCodes.UpdateTemplateNonConforming, log);
            TestUpdateLogWithDelimiter("# ", ErrorCodes.UpdateTemplateNonConforming, log);
        }

        [TestMethod]
        [Ignore, Description("Not Implemented")]
        public void WitsmlValidator_UpdateInStore_Error_443_Invalid_Uom_Value()
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
        public void WitsmlValidator_UpdateInStore_Error_453_Missing_Uom_Data()
        {
            AddParents();

            var xmlIn = CreateXmlLog(
                _log.Name,
                _log.UidWell,
                _well.Name,
                _log.UidWellbore,
                _wellbore.Name,
                startIndexUom: null,
                endIndexUom: null);
            var response = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, response.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_406_Missing_Parent_Uid()
        {
            AddParents();

            _log.UidWell = null;
            _log.RunNumber = "101";
            _log.IndexCurve = "MD";
            _log.IndexType = LogIndexType.measureddepth;
            _log.Direction = LogIndexDirection.decreasing;

            _devKit.InitHeader(_log, _log.IndexType.Value, increasing: false);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 100, 0.9, increasing: false);

            var response = _devKit.Add<LogList, Log>(_log);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForAdd, response.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_478_Parent_Uid_Case_Not_Matching()
        {
            // Base uid
            var uid = "well-01-error-478" + _devKit.Uid();

            // Well Uid with uppercase "P"
            _well.Uid = "P" + uid;

            // Well Uid with uppercase "P"
            _wellbore.UidWell = _well.Uid;

            AddParents();

            _log.UidWell = "p" + uid;
            _log.UidWellbore = _wellbore.Uid;

            // Well Uid with lowercase "p"

            _log.RunNumber = "101";
            _log.IndexCurve = "MD";
            _log.IndexType = LogIndexType.measureddepth;
            _log.Direction = LogIndexDirection.decreasing;
            _devKit.InitHeader(_log, _log.IndexType.Value, increasing: false);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 100, 0.9, increasing: false);

            var response = _devKit.Add<LogList, Log>(_log);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.IncorrectCaseParentUid, response.Result);
        }

        [TestMethod, Description("To test adding a log with special characters & (ampersand) and throws error -409")]
        public void WitsmlValidator_AddToStore_Error_409_Log_With_Special_Characters_Ampersand()
        {
            // Add well
            AddParents();

            // Add log          
            var description = "<description>Header & </description>";
            var row = "<data>5000.1, Data & , 5.1</data>";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + _log.UidWell + "\" uidWellbore=\"" + _log.UidWellbore + "\">" + Environment.NewLine +
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

        [TestMethod, Description("To test adding a log with special characters < (less than) and returning error -409")]
        public void Log141Validator_AddToStore_Error_409_Log_With_Special_Characters_Less_Than()
        {
            AddParents();

            // Add log          
            var description = "<description>Header < </description>";
            var row = "<data>5000.1, Data < , 5.1</data>";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + _log.UidWell + "\" uidWellbore=\"" + _log.UidWellbore + "\">" + Environment.NewLine +
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

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_402_MaxReturnNodes_Not_Greater_Than_Zero()
        {
            var result = _devKit.Get<LogList, Log>(_devKit.List(_log), ObjectTypes.Log, optionsIn: "maxReturnNodes=0");

            Assert.AreEqual((short)ErrorCodes.InvalidMaxReturnNodes, result.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_438_Recurring_Elements_Have_Inconsistent_Selection()
        {
            _log.LogCurveInfo = new List<LogCurveInfo>
            {
                new LogCurveInfo() {Uid = "MD", DataSource = "abc"},
                new LogCurveInfo() {Uid = "GR", CurveDescription = "efg"}
            };

            var list = _devKit.New<LogList>(x => x.Log = _devKit.List(_log));
            var queryIn = WitsmlParser.ToXml(list);
            var result = _devKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");

            Assert.AreEqual((short)ErrorCodes.RecurringItemsInconsistentSelection, result.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_439_Recurring_Elements_Has_Empty_Selection_Value()
        {
            _log.LogCurveInfo = new List<LogCurveInfo>();
            _log.LogCurveInfo.Add(new LogCurveInfo() { Uid = "MD", Mnemonic = new ShortNameStruct("MD") });
            _log.LogCurveInfo.Add(new LogCurveInfo() { Uid = string.Empty, Mnemonic = new ShortNameStruct("ROP") });

            var result = _devKit.Get<LogList, Log>(_devKit.List(_log), ObjectTypes.Log, null,
                optionsIn: OptionsIn.ReturnElements.Requested);

            Assert.AreEqual((short)ErrorCodes.RecurringItemsEmptySelection, result.Result);
        }

        [TestMethod]
        public void ChannelDataReader_AddToStore_Error_1051_Incorrect_Row_Value_Count()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10, hasEmptyChannel: false);

            var logData = _log.LogData.FirstOrDefault();
            logData?.Data?.Add("20,20.1,20.2,20.3,20.4");

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.ErrorRowDataCount, response.Result);
        }

        [TestMethod]
        public void ChannelDataReader_UpdateInStore_Error_1051_Incorrect_Row_Value_Count()
        {
            const int count = 10;
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), count, hasEmptyChannel: false);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            
            var update = new Log
            {
                Uid = _log.Uid,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore,
                StartIndex = new GenericMeasure
                {
                    Uom = "m",
                    Value = count
                }
            };

            _devKit.InitHeader(update, LogIndexType.measureddepth);
            _devKit.InitDataMany(update, _devKit.Mnemonics(update), _devKit.Units(update), count, hasEmptyChannel: false);

            var logData = update.LogData.FirstOrDefault();
            logData?.Data?.Add("30,30.1,30.2,30.3,30.4");

            update.StartIndex = null;

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.ErrorRowDataCount, updateResponse.Result);
        }


        #region Helper Methods

        private Log TestAddLogWithDelimiter(string dataDelimiter, ErrorCodes expectedReturnCode)
        {
            _well.Uid = null;
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.Uid = null;
            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01"),
                DataDelimiter = dataDelimiter
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)expectedReturnCode, response.Result);
            log.Uid = response.SuppMsgOut;

            return log;
        }

        private void TestUpdateLogWithDelimiter(string dataDelimiter, ErrorCodes expectedReturnCode, Log log = null)
        {
            if (log == null)
            {
                log = TestAddLogWithDelimiter(",", ErrorCodes.Success);
            }
            log.DataDelimiter = dataDelimiter;
            var update = _devKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)expectedReturnCode, update.Result);
        }

        private void AddParents()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        private void AddLogBadColumnIdentifier(Log log, string badChar)
        {
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + badChar;
            var response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);
        }

        private void AddLogBadCharInMnemonics(Log log, string[] mnemonics, string badChar)
        {
            mnemonics[1] = badChar;
            var logData = log.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);
            logData.MnemonicList = string.Join(",", mnemonics);
            var response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);
        }

        private string CreateXmlLog(string logName, string uidWell, string nameWell, string uidWellbore, string nameWellbore, string startIndexUom, string endIndexUom)
        {
            string xmlIn =
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" +
                "    <log uidWell=\"" + uidWell + "\" uidWellbore=\"" + uidWellbore + "\">" +
                "        <nameWell>" + nameWell + "</nameWell>" +
                "        <nameWellbore>" + nameWellbore + "</nameWellbore>" +
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

        #endregion Helper Methods
    }
}
