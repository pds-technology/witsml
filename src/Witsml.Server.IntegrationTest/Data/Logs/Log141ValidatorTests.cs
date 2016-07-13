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
        public void Test_error_code_459_bad_column_identifier_in_LogCurveInfo()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10);

            // Test all Illegal characters => { "'", "\"", "<", ">", "/", "\\", "&", "," }

            // Test &
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + "&";
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test "
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + "\"";
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test '
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + "'";
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test >
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + ">";
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test <
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + "<";
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test \
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + "\\";
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test /
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + "/";
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test ,
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + ",";
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_459_Bad_Char_In_Mnemonics()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10);

            // Test all Illegal characters => { "'", "\"", "<", ">", "/", "\\", "&", "," }
            var mnemonics = log.LogData.FirstOrDefault().MnemonicList.Split(',');

            // Test &
            mnemonics[1] = "&";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test "
            mnemonics[1] = "\"";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test '
            mnemonics[1] = "'";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test >
            mnemonics[1] = ">";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test <
            mnemonics[1] = "<";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test \
            mnemonics[1] = "\\";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test /
            mnemonics[1] = "/";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);
        }

        [TestMethod]
        public void Test_error_code_442_optionsIn_keyword_not_supported()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10);

            response = _devKit.Add<LogList, Log>(log, optionsIn: "compressionMethod=gzip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByServer, response.Result);
        }

        [TestMethod]
        public void Test_error_code_464_child_uids_not_unique()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            // Make all child uids the same for LogCurveInfos
            log.LogCurveInfo.ForEach(lci => lci.Uid = "lci1");

            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10);

            response = _devKit.Add<LogList, Log>(log);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, response.Result);
        }

        [TestMethod]
        public void Test_error_code_486_data_object_types_dont_match()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10);

            var logs = new LogList { Log = _devKit.List(log) };
            var xmlIn = EnergisticsConverter.ObjectToXml(logs);
            response = _devKit.AddToStore(ObjectTypes.Wellbore, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypesDontMatch, response.Result);
        }

        [TestMethod]
        public void Test_error_code_433_object_not_exist()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                Uid = _devKit.Uid(),
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            var update = _devKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotExist, update.Result);
        }

        [TestMethod]
        public void Test_error_code_415_missing_uid()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            var update = _devKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidMissing, update.Result);
        }

        [TestMethod]
        public void MongoDbUpdate_UpdateInStore_Error_484_Empty_Value_For_Mandatory_Field()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var xmlIn = "<logs version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" +
                "<log uidWell=\"" + log.UidWell + "\" uidWellbore=\"" + log.UidWellbore + "\" uid=\"" + uidLog + "\">" +
                    "<nameWell />" +
                "</log>" +
                "</logs>";

            var updateResponse = _devKit.UpdateInStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingRequiredData, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_445_empty_new_element()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = _wellbore.UidWell,
                UidWellbore = uidWellbore
            };

            update.LogCurveInfo = new List<LogCurveInfo>
            {
                new LogCurveInfo { Uid = "ExtraCurve" }
            };

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.EmptyNewElementsOrAttributes, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_464_unique_curve_uid_add()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            log.LogCurveInfo.ForEach(l => l.Uid = "uid01");

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, response.Result);
        }

        [TestMethod]
        public void Test_error_code_464_unique_curve_uid_update()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = _wellbore.UidWell,
                UidWellbore = uidWellbore,
                NameWell = string.Empty
            };

            update.LogCurveInfo = log.LogCurveInfo;
            update.LogCurveInfo.ForEach(l => l.Uid = "abc");

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_448_missing_curve_uid()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = _wellbore.UidWell,
                UidWellbore = uidWellbore,
                NameWell = string.Empty
            };

            update.LogCurveInfo = log.LogCurveInfo;
            update.LogCurveInfo.Last().Uid = string.Empty;

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MissingElementUid, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_463_duplicate_index_value()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = _wellbore.UidWell,
                UidWellbore = uidWellbore
            };

            _devKit.InitHeader(update, LogIndexType.measureddepth);
            var logData = update.LogData.First();
            logData.Data.Add("13,13.1,");
            logData.Data.Add("14,14.1,");
            logData.Data.Add("15,15.1,");
            logData.Data.Add("15,16.1,");
            logData.Data.Add("17,17.1,");
            logData.Data.Add("21,,21.2");
            logData.Data.Add("22,,22.2");
            logData.Data.Add("23,,23.2");

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.NodesWithSameIndex, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_480_adding_updating_curves_simultaneously()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = _wellbore.UidWell,
                UidWellbore = uidWellbore
            };

            update.LogCurveInfo = log.LogCurveInfo;
            update.LogCurveInfo.Last().Uid = "NewCurve";

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.AddingUpdatingLogCurveAtTheSameTime, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_434_missing_mnemonics_when_updating_log_data()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = _wellbore.UidWell,
                UidWellbore = uidWellbore
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
        public void Log141Validator_Error_434_Missing_Mnemonics_In_LogCurveInfo()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            ///////////////////////////////////////////////////////////////
            // Add a Log with only the index channel in the LogCurveInfo //
            ///////////////////////////////////////////////////////////////

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            // Remove all LogCurveInfo except for the index channel
            log.LogCurveInfo.RemoveAt(2);
            log.LogCurveInfo.RemoveAt(1);
            log.LogData.Clear();

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            ////////////////////////////////////////////////////////////////////
            // Update the Log with data for two channels in the mnemonic list //
            ////////////////////////////////////////////////////////////////////
            var update = new Log()
            {
                Uid = uidLog,
                UidWell = _wellbore.UidWell,
                UidWellbore = uidWellbore
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
        public void Log141Validator_Error_434_Mnemonics_Do_Not_Match_LogCurveInfo()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            /////////////////////////////////////////////////////
            // Add a Log with two channels in the LogCurveInfo //
            /////////////////////////////////////////////////////

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            // Remove the last channel from LogCurveInfo, that should leave MD and ROP
            log.LogCurveInfo.RemoveAt(2);
            log.LogData.Clear();

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Update the Log with data for two channels in the mnemonic list, but one channel does not match LogCurveInfo //
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var update = new Log()
            {
                Uid = uidLog,
                UidWell = _wellbore.UidWell,
                UidWellbore = uidWellbore
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

        [TestMethod]
        public void Test_error_code_436_index_range_should_not_be_specified_when_updating_data()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = _wellbore.UidWell,
                UidWellbore = uidWellbore
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

        [TestMethod]
        public void Test_error_code_451_missing_units_when_updating_log_data()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = _wellbore.UidWell,
                UidWellbore = uidWellbore
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
        public void Test_error_code_452_units_not_match_when_updating_log_data()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = _wellbore.UidWell,
                UidWellbore = uidWellbore
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

        [TestMethod]
        public void Test_error_code_449_missing_index_curve_when_updating_log_data()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = _wellbore.UidWell,
                UidWellbore = uidWellbore
            };

            _devKit.InitHeader(update, LogIndexType.measureddepth);

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,");
            var mnemonics = logData.MnemonicList.Split(',');
            logData.MnemonicList = string.Join(",", mnemonics.Where(m => m != log.IndexCurve));

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.IndexCurveNotFound, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_453_units_not_specified_for_log_data_add()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10);

            // Set the 3rd mnemonic to the 2nd in LogCurveInfo
            var logData = log.LogData.First();
            logData.UnitList = "m,";

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, response.Result);
        }

        [TestMethod]
        public void Log141Validator_Test_Add_With_Blank_Unit_In_LogCurveInfo_And_UnitList()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10);

            // Set the 3rd LogCurveInfo/unit to null and 3rd UnitList entry to an empty string
            log.LogCurveInfo[2].Unit = null;
            var logData = log.LogData.First();
            logData.UnitList = "m,m/h,";

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Log141Validator_Test_Add_With_Unit_In_LogCurveInfo_And_Blank_In_UnitList()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10);

            // Set the 3rd UnitList entry to an empty string
            var logData = log.LogData.First();
            logData.UnitList = "m,m/h,";

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, response.Result);
        }

        [TestMethod]
        public void Test_error_code_453_units_not_specified_for_log_data_update()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            log.Uid = uidLog;
            _devKit.InitDataMany(log, _devKit.Mnemonics(log), _devKit.Units(log), 10);

            // Set the 3rd mnemonic to the 2nd in LogCurveInfo
            var logData = log.LogData.First();
            logData.UnitList = "m,";

            var update = _devKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, update.Result);
        }

        [TestMethod]
        public void Test_error_code_448_missing_log_param_uid_add()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            var logParam = new IndexedObject
            {
                Description = "Md Index"
            };

            log.LogParam = new List<IndexedObject> { logParam };

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.MissingElementUid, response.Result);
        }

        [Ignore, Description("No longer a valid test as we are now validating recurring elements for uid")]
        [TestMethod]
        public void Test_log_curve_uid_default_to_mnemonic()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            var indexCurve = log.LogCurveInfo.First();
            indexCurve.Uid = string.Empty;

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All.ToString());
            Assert.AreEqual(1, results.Count);

            var logAdded = results.First();
            indexCurve = logAdded.LogCurveInfo.First();
            Assert.AreEqual(indexCurve.Mnemonic.Value, indexCurve.Uid);
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
        public void Log141Validator_GetFromStore_Error_458_Has_Mixed_Structural_Range_Indices()
        {
            _log.StartIndex = new GenericMeasure(1000.0, "ft");
            _log.EndDateTimeIndex = new Timestamp();

            var result = _devKit.Get<LogList, Log>(_devKit.List(_log), ObjectTypes.Log, null, optionsIn: OptionsIn.ReturnElements.Requested);

            Assert.AreEqual((short)ErrorCodes.MixedStructuralRangeIndices, result.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_450_mnemonics_not_unique()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log
            {
                Uid = uidLog,
                UidWell = _wellbore.UidWell,
                UidWellbore = uidWellbore,
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
            var log = new Log
            {
                UidWell = _devKit.Uid(),
                UidWellbore = _devKit.Uid(),
                NameWell = "Well 01",
                NameWellbore = "Wellbore 01",
                Name = "Log missing well parent"
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.MissingParentDataObject, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_405_Log_Already_Exists()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            _wellbore.UidWell = response.SuppMsgOut;

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            log.Uid = uidLog;

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
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
        public void Log141Validator_AddToStore_Success_DataDelimiter()
        {
            TestAddLogWithDelimiter("#", ErrorCodes.Success);
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
        public void Log141Validator_UpdateInStore_Success_DataDelimiter()
        {
            TestUpdateLogWithDelimiter("#", ErrorCodes.Success);
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

        #endregion Helper Methods
    }
}
