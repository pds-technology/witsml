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
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    public class Log141ValidationTests
    {
        private DevKit141Aspect DevKit;
        private Well Well;
        private Wellbore Wellbore;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect();

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            Well = new Well { Name = DevKit.Name("Well 01"), TimeZone = DevKit.TimeZone };

            Wellbore = new Wellbore()
            {
                NameWell = Well.Name,
                Name = DevKit.Name("Wellbore 01")
            };
        }

        [TestMethod]
        public void Test_error_code_463_nodes_with_same_index()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01"),
                LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() })
            };

            var logData = log.LogData.First();
            logData.Data.Add("13,13.1,");
            logData.Data.Add("14,14.1,");
            logData.Data.Add("15,15.1,");
            logData.Data.Add("15,16.1,");
            logData.Data.Add("17,17.1,");
            logData.Data.Add("21,,21.2");
            logData.Data.Add("22,,22.2");
            logData.Data.Add("23,,23.2");
            DevKit.InitHeader(log, LogIndexType.measureddepth);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.NodesWithSameIndex, response.Result);
        }

        [TestMethod]
        public void Test_error_code_447_duplicate_column_identifiers_in_LogCurveInfo()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);

            // Set the 3rd mnemonic to the 2nd in LogCurveInfo
            log.LogCurveInfo[2].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value;

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.DuplicateColumnIdentifiers, response.Result);
        }

        [TestMethod]
        public void Test_error_code_447_duplicate_column_identifiers_in_LogData_MnemonicList()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);

            // Set the 3rd mnemonic to the 2nd in the LogData.MnemonicList
            var mnemonics = log.LogData.FirstOrDefault().MnemonicList.Split(',');
            mnemonics[2] = mnemonics[1];
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.DuplicateColumnIdentifiers, response.Result);
        }

        [TestMethod]
        public void Test_error_code_449_index_curve_not_found_in_LogCurveInfo()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);

            // Remove LogCurveInfo for IndexCurve
            log.LogCurveInfo.Remove(log.LogCurveInfo.Where(l => l.Mnemonic.Value == log.IndexCurve).FirstOrDefault());

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.IndexCurveNotFound, response.Result);
        }

        [TestMethod]
        public void Test_error_code_449_index_curve_not_found_in_LogData_MnemonicList()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);

            // Remove the index curve from the LogData.MnemonicList
            var mnemonics = log.LogData.FirstOrDefault().MnemonicList.Split(',');
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics.Where(m => m != log.IndexCurve));

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.IndexCurveNotFound, response.Result);
        }

        [TestMethod]
        public void Test_error_code_456_max_data_exceeded_for_nodes()
        {
            var response = DevKit.Add<WellList, Well>(Well);
            var maxDataNodes = Settings.Default.MaxDataNodes;

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            // Create a Data set with one more row than maxNodes
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), maxDataNodes + 1);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.MaxDataExceeded, response.Result);
        }

        [TestMethod]
        public void Test_error_code_456_max_data_exceeded_for_points()
        {
            var response = DevKit.Add<WellList, Well>(Well);
            var maxDataPoints = Settings.Default.MaxDataPoints;

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            
            // Create a Data set with one more row than maxNodes
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), (maxDataPoints / log.LogCurveInfo.Count) + 1);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.MaxDataExceeded, response.Result);
        }

        [TestMethod]
        public void Test_error_code_457_index_not_first_in_LogCurveInfo()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);

            // Move the last LogCurveInfo before the index LogCurveInfo
            var lastLogCurveInfo = log.LogCurveInfo.LastOrDefault();
            log.LogCurveInfo.Remove(lastLogCurveInfo);
            log.LogCurveInfo.Insert(0, lastLogCurveInfo);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.IndexNotFirstInDataColumnList, response.Result);
        }

        [TestMethod]
        public void Test_error_code_458_mixed_index_types_in_Log()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);

            // Add a StartDateTimeIndex to the Depth Log
            log.StartDateTimeIndex = DateTimeOffset.Now;

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.MixedStructuralRangeIndices, response.Result);
        }

        [TestMethod]
        public void Test_error_code_459_bad_column_identifier_in_LogCurveInfo()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);

            // Test all Illegal characters => { "'", "\"", "<", ">", "/", "\\", "&", "," }

            // Test &
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + "&";
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test "
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + "\"";
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test '
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + "'";
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test >
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + ">";
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test <
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + "<";
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test \
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + "\\";
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test /
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + "/";
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test ,
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + ",";
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);
        }

        [TestMethod]
        public void Test_error_code_459_bad_column_identifier_in_LogData()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);

            // Test all Illegal characters => { "'", "\"", "<", ">", "/", "\\", "&", "," }
            var mnemonics = log.LogData.FirstOrDefault().MnemonicList.Split(',');

            // Test &
            mnemonics[1] = "&";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test "
            mnemonics[1] = "\"";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test '
            mnemonics[1] = "'";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test >
            mnemonics[1] = ">";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test <
            mnemonics[1] = "<";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test \
            mnemonics[1] = "\\";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test /
            mnemonics[1] = "/";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);

            // Test ,
            mnemonics[1] = ",";
            log.LogData.FirstOrDefault().MnemonicList = string.Join(",", mnemonics);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);
        }

        [TestMethod]
        public void Test_error_code_442_optionsIn_keyword_not_supported()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);

            response = DevKit.Add<LogList, Log>(log, optionsIn: "compressionMethod=gzip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByServer, response.Result);
        }

        [TestMethod]
        public void Test_error_code_464_child_uids_not_unique()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            // Make all child uids the same for LogCurveInfos
            log.LogCurveInfo.ForEach(lci => lci.Uid = "lci1");

            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);

            response = DevKit.Add<LogList, Log>(log);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, response.Result);
        }

        [TestMethod]
        public void Test_error_code_486_data_object_types_dont_match()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);

            var logs = new LogList { Log = DevKit.List(log) };
            var xmlIn = EnergisticsConverter.ObjectToXml(logs);
            response = DevKit.AddToStore(ObjectTypes.Wellbore, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypesDontMatch, response.Result);
        }

        [TestMethod]
        public void Test_error_code_433_object_not_exist()
        {
            var response = DevKit.Add<WellList, Well>(Well);
            Wellbore.UidWell = response.SuppMsgOut;

            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                Uid = DevKit.Uid(),
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            var update = DevKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotExist, update.Result);          
        }

        [TestMethod]
        public void Test_error_code_415_missing_uid()
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

            var update = DevKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidMissing, update.Result);
        }

        [TestMethod]
        public void Test_error_code_484_missing_mandatory_field()
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

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                NameWell = string.Empty
            };

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MissingRequiredData, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_445_empty_new_element()
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

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore
            };

            update.LogCurveInfo = new List<LogCurveInfo>
            {
                new LogCurveInfo { Uid = "ExtraCurve" }
            };

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.EmptyNewElementsOrAttributes, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_464_unique_curve_uid_add()
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
            log.LogCurveInfo.ForEach(l => l.Uid = "uid01");

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, response.Result);
        }

        [TestMethod]
        public void Test_error_code_464_unique_curve_uid_update()
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

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                NameWell = string.Empty
            };

            update.LogCurveInfo = log.LogCurveInfo;
            update.LogCurveInfo.ForEach(l => l.Uid = "abc");

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_448_missing_curve_uid()
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

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                NameWell = string.Empty
            };

            update.LogCurveInfo = log.LogCurveInfo;
            update.LogCurveInfo.Last().Uid = string.Empty;

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MissingElementUid, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_463_duplicate_index_value()
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

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);
            var logData = update.LogData.First();
            logData.Data.Add("13,13.1,");
            logData.Data.Add("14,14.1,");
            logData.Data.Add("15,15.1,");
            logData.Data.Add("15,16.1,");
            logData.Data.Add("17,17.1,");
            logData.Data.Add("21,,21.2");
            logData.Data.Add("22,,22.2");
            logData.Data.Add("23,,23.2");

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.NodesWithSameIndex, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_480_adding_updating_curves_simultaneously()
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

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore
            };

            update.LogCurveInfo = log.LogCurveInfo;
            update.LogCurveInfo.Last().Uid = "NewCurve";

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.AddingUpdatingLogCurveAtTheSameTime, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_434_missing_mnemonics_when_updating_log_data()
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

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);          

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1,");
            logData.Data.Add("14,14.1,");
            logData.MnemonicList = string.Empty;

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MissingColumnIdentifiers, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_436_index_range_should_not_be_specified_when_updating_data()
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

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);
            update.LogCurveInfo.RemoveAt(2);
            var lastCurve = update.LogCurveInfo.Last();
            lastCurve.Uid = "NewCurve";
            lastCurve.Mnemonic.Value = "NewCurve";
            lastCurve.MinIndex = new GenericMeasure { Value = 13, Uom = "m" };

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1");
            logData.Data.Add("14,14.1");
            logData.MnemonicList = DevKit.Mnemonics(update);

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.IndexRangeSpecified, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_451_missing_units_when_updating_log_data()
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

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1,");
            logData.Data.Add("14,14.1,");
            logData.UnitList = string.Empty;

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MissingUnitList, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_452_units_not_match_when_updating_log_data()
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

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);
            var indexCurve = update.LogCurveInfo.First();
            indexCurve.Unit = "ft";

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,");

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.UnitListNotMatch, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_449_missing_index_curve_when_updating_log_data()
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

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,");
            var mnemonics = logData.MnemonicList.Split(',');
            logData.MnemonicList = string.Join(",", mnemonics.Where(m => m != log.IndexCurve));

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.IndexCurveNotFound, updateResponse.Result);
        }
    }
}
