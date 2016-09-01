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
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Logs
{
    public partial class Log131DataAdapterAddTests
    {
        [TestMethod]
        public void Log131DataAdapter_AddToStore_Add_DepthLog_Header()
        {
            AddParents();

            // check if log already exists
            var logResults = DevKit.Query<LogList, Log>(Log);
            if (!logResults.Any())
            {
                DevKit.InitHeader(Log, LogIndexType.measureddepth);
                var response = DevKit.Add<LogList, Log>(Log);
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }
        }

        [TestMethod]
        public void Log131DataAdapter_AddToStore_Add_TimeLog_Header()
        {
            AddParents();

            // check if log already exists
            var logResults = DevKit.Query<LogList, Log>(Log);
            if (!logResults.Any())
            {
                DevKit.InitHeader(Log, LogIndexType.datetime);
                var response = DevKit.Add<LogList, Log>(Log);
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }
        }

        [TestMethod]
        public void Log131DataAdapter_AddToStore_Add_DepthLog_With_Data()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Log131DataAdapter_AddToStore_Add_TimeLog_With_Data()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.datetime);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10, 1, false, false);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Log131DataAdapter_AddToStore_Structural_Ranges_Ignored()
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

            log.StartIndex = new GenericMeasure { Uom = "m", Value = 1.0 };
            log.EndIndex = new GenericMeasure { Uom = "m", Value = 10.0 };

            foreach (var curve in log.LogCurveInfo)
            {
                curve.MinIndex = log.StartIndex;
                curve.MaxIndex = log.EndIndex;
            }

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var query = new Log
            {
                Uid = response.SuppMsgOut,
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
        }

        [TestMethod]
        public void Log131DataAdapter_AddToStore_Add_Unordered_DepthLog()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);
            Log.LogCurveInfo.Reverse();
            DevKit.AddAndAssert(Log);
            var indexCurve = Log.LogCurveInfo.FirstOrDefault(x => x.Mnemonic == Log.IndexCurve.Value);
            var newCurve = new LogCurveInfo()
            {
                Mnemonic = "Test",
                Unit = "gAPI",
                ColumnIndex = 2,
                NullValue = "|"
            };

            var update = new Log()
            {
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                Uid = Log.Uid,
                LogCurveInfo = new List<LogCurveInfo>()
                {
                    indexCurve,
                    newCurve
                }
            };
            DevKit.UpdateAndAssert(update);
            update.LogData = new List<string>();
            for (int i = 1; i < 11; i++)
            {
                var val = i%2 == 0 ? "|" : i.ToString();
                update.LogData.Add($"{i},{val}");
            }
            DevKit.UpdateAndAssert(update);
            var queryIn = "<logs version=\"1.3.1.1\" xmlns=\"http://www.witsml.org/schemas/131\" > " + Environment.NewLine +
                $"  <log uidWell=\"{Log.UidWell}\" uidWellbore=\"{Log.UidWellbore}\" uid=\"{Log.Uid}\">" + Environment.NewLine +
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
                "    <indexCurve columnIndex=\"\" />" + Environment.NewLine +
                "    <logCurveInfo>" + Environment.NewLine +
                $"      <mnemonic>{indexCurve.Mnemonic}</mnemonic>" + Environment.NewLine +
                "    </logCurveInfo>" + Environment.NewLine +
                "    <logCurveInfo>" + Environment.NewLine +
                $"      <mnemonic>{newCurve.Mnemonic}</mnemonic>" + Environment.NewLine +
                "    </logCurveInfo>" + Environment.NewLine +
                "    <logData />" + Environment.NewLine +
                "  </log>" + Environment.NewLine +
                "</logs>";

            var result = DevKit.GetFromStore(ObjectTypes.Log, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);
            Assert.IsNotNull(result);
            var logs = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            Assert.IsNotNull(logs);
            var log = logs.Log.FirstOrDefault();
            Assert.IsNotNull(log);
            Assert.IsNotNull(log.LogData);
            Assert.AreEqual(log.LogData.Count, 10);
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(log.LogData[i], update.LogData[i]);
            }
        }
    }
}
