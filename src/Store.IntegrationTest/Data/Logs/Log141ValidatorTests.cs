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
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Compatibility;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    public partial class Log141ValidatorTests
    {
        [TestMethod]
        public void Log141Validator_AddToStore_Error_447_Duplicate_Column_Identifiers_InLogCurveInfo()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            // Set the 3rd mnemonic to the 2nd in LogCurveInfo
            Log.LogCurveInfo[2].Mnemonic.Value = Log.LogCurveInfo[1].Mnemonic.Value;

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.DuplicateColumnIdentifiers, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_450_mnemonics_not_unique()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            // Set the 3rd mnemonic to the 2nd in the LogData.MnemonicList
            var logData = Log.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);

            var mnemonics = logData.MnemonicList.Split(',');
            mnemonics[2] = mnemonics[1];

            logData.MnemonicList = string.Join(",", mnemonics);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.MnemonicsNotUnique, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_449_Index_Curve_Not_Found_InLogData_MnemonicList()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            // Remove the index curve from the LogData.MnemonicList
            var logData = Log.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);

            logData.Data = new List<string> { "13.1, 14.1", "13.3, 14.3" };

            var mnemonics = logData.MnemonicList.Split(',');
            logData.MnemonicList = string.Join(",", mnemonics.Where(m => m != Log.IndexCurve));

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.IndexCurveNotFound, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_456_Max_Data_Exceeded_For_Nodes()
        {
            var maxDataNodes = 5;
            WitsmlSettings.LogMaxDataPointsAdd = maxDataNodes;

            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            // Create a Data set with one more row than maxNodes
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), maxDataNodes + 1);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.MaxDataExceeded, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_456_Max_Data_Exceeded_For_Points()
        {
            var maxDataPoints = 20;
            WitsmlSettings.LogMaxDataPointsAdd = maxDataPoints;

            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            // Create a Data set with one more row than maxDataPoints
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), (maxDataPoints / Log.LogCurveInfo.Count) + 1);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.MaxDataExceeded, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_458_Mixed_Index_Types_InLog()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            // Add a StartDateTimeIndex to the Depth Log
            Log.StartDateTimeIndex = DateTimeOffset.Now;

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.MixedStructuralRangeIndices, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_459_Bad_Column_Identifier_InLogCurveInfo()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            // Test all Illegal characters => { "'", "\"", "<", ">", "/", "\\", "&", "," }

            // Test &
            AddLogBadColumnIdentifier(Log, "&");

            // Test "
            AddLogBadColumnIdentifier(Log, "\"");

            // Test '
            AddLogBadColumnIdentifier(Log, "'");

            // Test >
            AddLogBadColumnIdentifier(Log, ">");

            // Test <
            AddLogBadColumnIdentifier(Log, "<");

            // Test \
            AddLogBadColumnIdentifier(Log, "\\");

            // Test /
            AddLogBadColumnIdentifier(Log, "/");

            // Test ,
            AddLogBadColumnIdentifier(Log, ",");
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_459_Bad_Char_In_Mnemonics()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            // Test all Illegal characters => { "'", "\"", "<", ">", "/", "\\", "&", "," }
            var logData = Log.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);
            var mnemonics = logData.MnemonicList.Split(',');

            // Test &
            AddLogBadCharInMnemonics(Log, mnemonics, "&");

            // Test "
            AddLogBadCharInMnemonics(Log, mnemonics, "\"");

            // Test '
            AddLogBadCharInMnemonics(Log, mnemonics, "'");

            // Test >
            AddLogBadCharInMnemonics(Log, mnemonics, ">");

            // Test <
            AddLogBadCharInMnemonics(Log, mnemonics, "<");

            // Test \
            AddLogBadCharInMnemonics(Log, mnemonics, "\\");

            // Test /
            AddLogBadCharInMnemonics(Log, mnemonics, "/");
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_442_OptionsIn_Keyword_Not_Supported()
        {
            WitsmlSettings.IsRequestCompressionEnabled = false;

            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            var response = DevKit.Add<LogList, Log>(Log, optionsIn: "compressionMethod=gzip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByServer, response.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_464_Child_Uids_Not_Unique()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            // Make all child uids the same for LogCurveInfos
            Log.LogCurveInfo.ForEach(lci => lci.Uid = "lci1");

            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            var response = DevKit.Add<LogList, Log>(Log);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_486_Data_Object_Types_Dont_Match()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            var logs = new LogList { Log = DevKit.List(Log) };
            var xmlIn = EnergisticsConverter.ObjectToXml(logs);
            var response = DevKit.AddToStore(ObjectTypes.Wellbore, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypesDontMatch, response.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_433_Object_Not_Exist()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var update = DevKit.Update<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotExist, update.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_415_Missing_Uid()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            Log.Uid = null;
            var update = DevKit.Update<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidMissing, update.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_464_Curve_Uid_Not_Unique()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogCurveInfo.ForEach(l => l.Uid = "uid01");

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, response.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_464_Curve_Uid_Not_Unique()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                NameWell = string.Empty,
                LogCurveInfo = Log.LogCurveInfo
            };

            update.LogCurveInfo.ForEach(l => l.Uid = "abc");

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, updateResponse.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_448_Missing_Curve_Uid()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                NameWell = string.Empty,
                LogCurveInfo = Log.LogCurveInfo
            };

            update.LogCurveInfo.Last().Uid = string.Empty;

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForUpdate, updateResponse.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_480_Adding_Updating_Curves_Simultaneously()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                LogData = new List<LogData>()
                {
                    new LogData()
                    {
                        Data = new List<string>()
                    }

                }
            };

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,13.3");
            logData.MnemonicList = "MD,ROP,XXX";

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.AddingUpdatingLogCurveAtTheSameTime, updateResponse.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_434_Missing_Mnemonics_When_UpdatingLog_Data()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log()
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1,");
            logData.Data.Add("14,14.1,");
            logData.MnemonicList = string.Empty;

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MissingColumnIdentifiers, updateResponse.Result);
        }

        [TestMethod, Description("Test Error 434 LogCurveInfo has fewer channels than the Mnemonic list")]
        public void Log141Validator_UpdateInStore_Error_434_Missing_Mnemonics_In_LogCurveInfo()
        {
            AddParents();

            ///////////////////////////////////////////////////////////////
            // Add a Log with only the index channel in the LogCurveInfo //
            ///////////////////////////////////////////////////////////////

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            Log.LogData.Clear();

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            ////////////////////////////////////////////////////////////////////
            // Update the Log with data for two channels in the mnemonic list //
            ////////////////////////////////////////////////////////////////////
            var update = new Log()
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);
            //update.LogCurveInfo.Clear();

            // Remove all LogCurveInfo except for the index channel
            update.LogCurveInfo.RemoveAt(2);
            update.LogCurveInfo.RemoveAt(1);

            // Add data for index channel and one other channel
            var logData = update.LogData.First();
            logData.Data.Add("13,13.1");
            logData.Data.Add("14,14.1");
            logData.MnemonicList = "MD,ROP";
            logData.UnitList = "m,m/h";

            // Assert -434 error
            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MissingColumnIdentifiers, updateResponse.Result);
        }

        [TestMethod, Description("Test Error 434 LogCurveInfo and Mnemonic list have the same count but one channel does not match")]
        public void Log141Validator_UpdateInStore_Error_434_Mnemonics_Do_Not_MatchLogCurveInfo()
        {
            AddParents();

            /////////////////////////////////////////////////////
            // Add a Log with two channels in the LogCurveInfo //
            /////////////////////////////////////////////////////

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogData.Clear();

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Update the Log with data for two channels in the mnemonic list, but one channel does not match LogCurveInfo //
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var update = new Log()
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);

            // Remove the last channel from LogCurveInfo, that should leave MD and ROP
            update.LogCurveInfo.RemoveAt(2);

            var logData = update.LogData.First();
            logData.Data.Add("13,13.1");
            logData.Data.Add("14,14.1");
            logData.MnemonicList = "MD,GR"; // Last channel does not match what's in LogCurveInfo
            logData.UnitList = "m,m/h";

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MissingColumnIdentifiers, updateResponse.Result);
        }

        [TestMethod, Description("Index range should not be specified when updating log data")]
        public void Log141Validator_UpdateInStore_Error_436_Index_Range_Specified()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log()
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore
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

        [TestMethod, Description("Unit list is missing in log data when updating log data")]
        public void Log141Validator_UpdateInStore_Error_451_Missing_Units()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log()
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore
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
        public void Log141Validator_UpdateInStore_Error_452_Units_Not_Match()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log()
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore
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

        [TestMethod, Description("Index curve is missing when updating log data")]
        public void Log141Validator_UpdateInStore_Error_449_Index_Curve_Missing()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log()
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);

            var logData = update.LogData.First();
            logData.Data.Add("13.1,13.2");
            logData.Data.Add("14.1,");
            var mnemonics = logData.MnemonicList.Split(',');
            logData.MnemonicList = string.Join(",", mnemonics.Where(m => m != Log.IndexCurve));

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.IndexCurveNotFound, updateResponse.Result);
        }

        [TestMethod, Description("Unit list in logData is not specified when add log date")]
        public void Log141Validator_AddToStore_Error_453_Units_Not_Specified()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            // Set the 3rd mnemonic to the 2nd in LogCurveInfo
            var logData = Log.LogData.First();
            logData.UnitList = "m,";

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, response.Result);
        }

        [TestMethod, Description("Mismatch in units between log curve and log data when adding log data")]
        public void Log141Validator_AddToStore_Error_453_Mismatched_UnitList()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            // Set the 3rd UnitList entry to an empty string
            var logData = Log.LogData.First();
            logData.UnitList = "m,m/h,";

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, response.Result);
        }

        [TestMethod, Description("Mismatch in units between log curve and log data when updating log data")]
        public void Log141Validator_UpdateInStore_Error_453_Mismatched_UnitList()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            // Set the 3rd mnemonic to the 2nd in LogCurveInfo
            var logData = Log.LogData.First();
            logData.UnitList = "m,";

            var update = DevKit.Update<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, update.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_448_MissingLog_Param_Uid_Add()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var logParam = new IndexedObject
            {
                Description = "Md Index"
            };

            Log.LogParam = new List<IndexedObject> { logParam };

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForAdd, response.Result);
        }

        [TestMethod]
        public void Log141Validator_GetFromStore_Error_429_Has_Recurring_Data_Section()
        {
            Log.LogData = new List<LogData>() { new LogData() { MnemonicList = "MD,GR" }, new LogData() { MnemonicList = "MD,ROP" } };

            var result = DevKit.Get<LogList, Log>(DevKit.List(Log), ObjectTypes.Log, null, optionsIn: OptionsIn.ReturnElements.Requested);

            Assert.AreEqual((short)ErrorCodes.RecurringLogData, result.Result);
        }

        [TestMethod]
        public void Log141Validator_GetFromStore_Error_482LogData_Has_Duplicate_Mnemonics()
        {
            Log.LogData = new List<LogData>() { new LogData() { MnemonicList = "MD,GR,MD" } };

            var result = DevKit.Get<LogList, Log>(DevKit.List(Log), ObjectTypes.Log, null, optionsIn: OptionsIn.ReturnElements.Requested);

            Assert.AreEqual((short)ErrorCodes.DuplicateMnemonics, result.Result);
        }

        [TestMethod]
        public void Log141Validator_GetFromStore_Error_458_Mixed_Structural_Range_Indices_Allowed_When_Not_RequestingLog_Data()
        {
            Log.StartIndex = new GenericMeasure();
            Log.EndDateTimeIndex = new Timestamp();

            var result = DevKit.Get<LogList, Log>(DevKit.List(Log), ObjectTypes.Log, null, optionsIn: OptionsIn.ReturnElements.Requested);

            Assert.AreEqual((short)ErrorCodes.Success, result.Result);
        }

        [TestMethod]
        public void Log141Validator_GetFromStore_Error_458_Mixed_Structural_Range_Indices_Not_Allowed_When_RequestingLog_Data()
        {
            Log.StartIndex = new GenericMeasure(1000.0, "ft");
            Log.EndDateTimeIndex = new Timestamp();
            Log.LogData = new List<LogData>
            {
                new LogData
                {
                    Data = new List<string>()
                }
            };

            var result = DevKit.Get<LogList, Log>(DevKit.List(Log), ObjectTypes.Log, null, optionsIn: OptionsIn.ReturnElements.Requested);

            Assert.AreEqual((short)ErrorCodes.MixedStructuralRangeIndices, result.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_450_mnemonics_not_unique()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
            };

            update.LogData = new List<LogData>
            {
                new LogData
                {
                    MnemonicList = "MD,MD,GR",
                    UnitList = "m,m,gAPI",
                    Data = new List<string> { "1,1,1" }
                }
            };

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.MnemonicsNotUnique, updateResponse.Result);
        }

        [TestMethod]
        public void Log141Validator_AddToStore_Error_481_Well_Missing()
        {
            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.MissingParentDataObject, response.Result);
        }

        [TestMethod]
        public void Log141Validator_GetFromStore_Error_460_Column_Identifiers_In_Header_And_Data_Not_Same()
        {
            Log.LogCurveInfo = new List<LogCurveInfo>();
            Log.LogCurveInfo.Add(new LogCurveInfo() { Uid = "MD", Mnemonic = new ShortNameStruct("MD") });
            Log.LogCurveInfo.Add(new LogCurveInfo() { Uid = "GR", Mnemonic = new ShortNameStruct("GR") });

            Log.LogData = new List<LogData>() { new LogData() { MnemonicList = "MD" } };

            var list = DevKit.New<LogList>(x => x.Log = DevKit.List(Log));
            var queryIn = WitsmlParser.ToXml(list);
            var result = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");

            Assert.AreEqual((short)ErrorCodes.ColumnIdentifiersNotSame, result.Result);
        }

        [TestMethod]
        public void Log141Validator_GetFromStore_Error_461_Missing_Mnemonic_Element_In_Column_Definition()
        {
            Log.LogCurveInfo = new List<LogCurveInfo>();
            Log.LogCurveInfo.Add(new LogCurveInfo() { Uid = "MD" });

            var list = DevKit.New<LogList>(x => x.Log = DevKit.List(Log));
            var queryIn = WitsmlParser.ToXml(list);
            var result = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");

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

            var result = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");

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

            var response = DevKit.Add<WellList, Well>(Well);
            var uidWell = response.SuppMsgOut;
            Wellbore.UidWell = response.SuppMsgOut;

            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            var uidWellbore = response.SuppMsgOut;
            var logName = "Log Test -443 - Invalid Uom";
            var startIndexUom = "abc";
            var endIndexUom = startIndexUom;

            string xmlIn = CreateXmlLog(
                logName,
                uidWell,
                Well.Name,
                uidWellbore,
                Wellbore.Name,
                startIndexUom,
                endIndexUom);
            response = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InvalidUnitOfMeasure, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_UpdateInStore_Error_453_Missing_Uom_Data()
        {
            AddParents();

            var xmlIn = CreateXmlLog(
                Log.Name,
                Log.UidWell,
                Well.Name,
                Log.UidWellbore,
                Wellbore.Name,
                startIndexUom: null,
                endIndexUom: null);
            var response = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_406_Missing_Parent_Uid()
        {
            AddParents();

            Log.UidWell = null;
            Log.RunNumber = "101";
            Log.IndexCurve = "MD";
            Log.IndexType = LogIndexType.measureddepth;
            Log.Direction = LogIndexDirection.decreasing;

            DevKit.InitHeader(Log, Log.IndexType.Value, increasing: false);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 100, 0.9, increasing: false);

            var response = DevKit.Add<LogList, Log>(Log);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForAdd, response.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_478_Parent_Uid_Case_Not_Matching()
        {
            // Base uid
            var uid = "well-01-error-478" + DevKit.Uid();

            // Well Uid with uppercase "P"
            Well.Uid = "P" + uid;

            // Well Uid with uppercase "P"
            Wellbore.UidWell = Well.Uid;

            AddParents();

            Log.UidWell = "p" + uid;
            Log.UidWellbore = Wellbore.Uid;

            // Well Uid with lowercase "p"

            Log.RunNumber = "101";
            Log.IndexCurve = "MD";
            Log.IndexType = LogIndexType.measureddepth;
            Log.Direction = LogIndexDirection.decreasing;
            DevKit.InitHeader(Log, Log.IndexType.Value, increasing: false);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 100, 0.9, increasing: false);

            var response = DevKit.Add<LogList, Log>(Log);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.IncorrectCaseParentUid, response.Result);
        }

        [TestMethod, Description("To test adding a log with special characters & (ampersand) and throws error -409")]
        public void WitsmlValidator_AddToStore_Error_409Log_With_Special_Characters_Ampersand()
        {
            // Add well
            AddParents();

            // Add log          
            var description = "<description>Header & </description>";
            var row = "<data>5000.1, Data & , 5.1</data>";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + Log.UidWell + "\" uidWellbore=\"" + Log.UidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + Well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + Wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + DevKit.Name("Test special characters") + "</name>" + Environment.NewLine +
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

            var result = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, result.Result);
        }

        [TestMethod, Description("To test adding a log with special characters < (less than) and returning error -409")]
        public void Log141Validator_AddToStore_Error_409Log_With_Special_Characters_Less_Than()
        {
            AddParents();

            // Add log          
            var description = "<description>Header < </description>";
            var row = "<data>5000.1, Data < , 5.1</data>";

            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + "\" uidWell=\"" + Log.UidWell + "\" uidWellbore=\"" + Log.UidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + Well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + Wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + DevKit.Name("Test special characters") + "</name>" + Environment.NewLine +
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

            var result = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, result.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_402_MaxReturnNodes_Not_Greater_Than_Zero()
        {
            var result = DevKit.Get<LogList, Log>(DevKit.List(Log), ObjectTypes.Log, optionsIn: "maxReturnNodes=0");

            Assert.AreEqual((short)ErrorCodes.InvalidMaxReturnNodes, result.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_438_Recurring_Elements_Have_Inconsistent_Selection()
        {
            Log.LogCurveInfo = new List<LogCurveInfo>
            {
                new LogCurveInfo() {Uid = "MD", DataSource = "abc"},
                new LogCurveInfo() {Uid = "GR", CurveDescription = "efg"}
            };

            var list = DevKit.New<LogList>(x => x.Log = DevKit.List(Log));
            var queryIn = WitsmlParser.ToXml(list);
            var result = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");

            Assert.AreEqual((short)ErrorCodes.RecurringItemsInconsistentSelection, result.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_439_Recurring_Elements_Has_Empty_Selection_Value()
        {
            Log.LogCurveInfo = new List<LogCurveInfo>();
            Log.LogCurveInfo.Add(new LogCurveInfo() { Uid = "MD", Mnemonic = new ShortNameStruct("MD") });
            Log.LogCurveInfo.Add(new LogCurveInfo() { Uid = string.Empty, Mnemonic = new ShortNameStruct("ROP") });

            var result = DevKit.Get<LogList, Log>(DevKit.List(Log), ObjectTypes.Log, null,
                optionsIn: OptionsIn.ReturnElements.Requested);

            Assert.AreEqual((short)ErrorCodes.RecurringItemsEmptySelection, result.Result);
        }

        [TestMethod]
        public void ChannelDataReader_AddToStore_Error_1051_Incorrect_Row_Value_Count()
        {
            CompatibilitySettings.InvalidDataRowSetting = InvalidDataRowSetting.Error;

            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10, hasEmptyChannel: false);

            var logData = Log.LogData.FirstOrDefault();
            logData?.Data?.Add("20,20.1,20.2,20.3,20.4");

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.ErrorRowDataCount, response.Result);
        }

        [TestMethod]
        public void ChannelDataReader_UpdateInStore_Error_1051_Incorrect_Row_Value_Count()
        {
            CompatibilitySettings.InvalidDataRowSetting = InvalidDataRowSetting.Error;

            const int count = 10;
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), count, hasEmptyChannel: false);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
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

        [TestMethod, Description("Tests that you cannot perform a requestLatestValues OptionsIn with a value less than 1")]
        public void WitsmlValidator_GetFromStore_Error_1054_MaxRequestLatestValue_Not_Greater_Than_Zero()
        {
            var result = DevKit.Get<LogList, Log>(DevKit.List(Log), ObjectTypes.Log, optionsIn: "requestLatestValues=0");

            Assert.AreEqual((short)ErrorCodes.InvalidRequestLatestValue, result.Result);
        }

        [TestMethod, Description("Tests that you cannot perform a requestLatestValues OptionsIn with a value greater than MaxRequestLatestValues")]
        public void WitsmlValidator_GetFromStore_Error_1054_MaxRequestLatestValue_Not_Greater_Than_MaxReturnLatestValue()
        {
            var result = DevKit.Get<LogList, Log>(DevKit.List(Log), ObjectTypes.Log,
                optionsIn: $"requestLatestValues={WitsmlSettings.MaxRequestLatestValues + 1}");

            Assert.AreEqual((short)ErrorCodes.InvalidRequestLatestValue, result.Result);
        }

        [TestMethod]
        public void Log141Validator_GetFromStore_Error_475_No_Subset_When_Getting_Growing_Object()
        {
            AddParents();

            // Add first log
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add second log
            var log2 = DevKit.CreateLog(null, DevKit.Name("Log 02"), Log.UidWell, Log.NameWell, Log.UidWellbore,
                Log.NameWellbore);
            DevKit.InitHeader(log2, LogIndexType.measureddepth);

            response = DevKit.Add<LogList, Log>(log2);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Query
            var query = DevKit.CreateLog(null, null, log2.UidWell, null, log2.UidWellbore, null);

            var result = DevKit.Get<LogList, Log>(DevKit.List(query), ObjectTypes.Log, null,
                OptionsIn.ReturnElements.DataOnly);
            Assert.AreEqual((short)ErrorCodes.MissingSubsetOfGrowingDataObject, result.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_484_Empty_Value_For_Mandatory_Field()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var xmlIn = "<logs version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" +
                "<log uidWell=\"" + Log.UidWell + "\" uidWellbore=\"" + Log.UidWellbore + "\" uid=\"" + uidLog + "\">" +
                    "<nameWell />" +
                "</log>" +
                "</logs>";

            var updateResponse = DevKit.UpdateInStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingRequiredData, updateResponse.Result);
        }

        [TestMethod]
        public void Log141Validator_UpdateInStore_Error_445_Empty_New_Element()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log()
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore
            };

            update.LogCurveInfo = new List<LogCurveInfo>
            {
                new LogCurveInfo { Uid = "ExtraCurve" }
            };

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.EmptyNewElementsOrAttributes, updateResponse.Result);
        }

        #region Helper Methods

        private Log TestAddLogWithDelimiter(string dataDelimiter, ErrorCodes expectedReturnCode)
        {
            Well.Uid = null;
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.Uid = null;
            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01"),
                DataDelimiter = dataDelimiter
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            response = DevKit.Add<LogList, Log>(log);
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
            var update = DevKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)expectedReturnCode, update.Result);
        }

        private void AddLogBadColumnIdentifier(Log log, string badChar)
        {
            log.LogCurveInfo[1].Mnemonic.Value = log.LogCurveInfo[1].Mnemonic.Value + badChar;
            var response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.BadColumnIdentifier, response.Result);
        }

        private void AddLogBadCharInMnemonics(Log log, string[] mnemonics, string badChar)
        {
            mnemonics[1] = badChar;
            var logData = log.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);
            logData.MnemonicList = string.Join(",", mnemonics);
            var response = DevKit.Add<LogList, Log>(log);
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
