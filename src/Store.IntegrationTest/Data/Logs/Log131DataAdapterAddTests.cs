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
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Compatibility;
using PDS.WITSMLstudio.Store.Data.Channels;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    [TestClass]
    public partial class Log131DataAdapterAddTests : Log131TestBase
    {
        private IChannelDataProvider _channelDataProvider;

        protected override void OnTestSetUp()
        {
            base.OnTestSetUp();

            _channelDataProvider = DevKit.Container.Resolve<IChannelDataProvider>(ObjectNames.Log131);
        }

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

            var result =
                DevKit.GetAndAssert<LogList, Log>(new Log()
                {
                    Uid = Log.Uid,
                    UidWell = Log.UidWell,
                    UidWellbore = Log.UidWellbore
                });
            Assert.IsNotNull(result.LogCurveInfo);
            Assert.AreEqual(2, result.LogCurveInfo.Count);
            Assert.AreEqual(1, result.LogCurveInfo[0].ColumnIndex.Value);
            Assert.AreEqual(2, result.LogCurveInfo[1].ColumnIndex.Value);
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
        [Ignore, Description("For Benchmarking")]
        public void Log131DataAdapter_AddToStore_Benchmark_Add_TimeLog_With_Data()
        {
            AddParents();
            var sw = new System.Diagnostics.Stopwatch();
            var times = new List<long>();

            DevKit.InitHeader(Log, LogIndexType.datetime);

            // Add 40 more mnemonics
            for (int i = 0; i < 40; i++)
            {
                Log.LogCurveInfo.Add(DevKit.LogGenerator.CreateDoubleLogCurveInfo($"Curve{i}", "m", (short)i));
            }

            // Set column indexes
            for (int i = 0; i < Log.LogCurveInfo.Count; i++)
            {
                Log.LogCurveInfo[i].ColumnIndex = (short?)(i + 1);
            }

            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 50, 1, false, false);

            for (int i = 0; i < Log.LogData.Count; i++)
            {
                for (int x = 0; x < Log.LogCurveInfo.Count - 3; x++)
                {
                    Log.LogData[i] += $",{i}";
                }
            }

            for (int i = 0; i < 50; i++)
            {
                Log.Uid = DevKit.Uid();
                Log.Name = DevKit.Name($"Benchmark{i}");

                // Measure time taken to add log to store
                sw.Restart();
                var response = DevKit.Add<LogList, Log>(Log);
                sw.Stop();

                // Ignore first add in case server was restarted
                if (i != 0)
                    times.Add(sw.ElapsedMilliseconds);
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }

            Assert.IsTrue(times.Max() < 5000);
            Assert.IsTrue(times.Average() < 5000);
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
                Name = DevKit.Name()
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
        public void Log131DataAdapter_AddToStore_Add_DepthLog_With_Unordered_LogCurveInfo()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            // Reverse logCurveInfo order
            Log.LogCurveInfo.Reverse();
            DevKit.AddAndAssert(Log);
            var indexCurve = Log.LogCurveInfo.FirstOrDefault(x => x.Mnemonic == Log.IndexCurve.Value);

            // Create two new curves
            var newCurve1 = new LogCurveInfo()
            {
                Mnemonic = "Test",
                Unit = "gAPI",
                ColumnIndex = 3,
                NullValue = "|"
            };
            var newCurve2 = new LogCurveInfo()
            {
                Mnemonic = "Test2",
                Unit = "gAPI",
                ColumnIndex = 2,
                NullValue = "|"
            };

            // Update 2 new curves to the log header
            var update = new Log()
            {
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                Uid = Log.Uid,
                LogCurveInfo = new List<LogCurveInfo>()
                {
                    indexCurve,
                    newCurve1,
                    newCurve2
                }
            };
            DevKit.UpdateAndAssert(update);

            // Add data
            update.LogData = new List<string>();
            for (int i = 1; i < 11; i++)
            {
                var val1 = i % 2 == 0 ? "|" : i.ToString();
                var val2 = i % 2 == 1 ? "|" : i.ToString();
                update.LogData.Add($"{i},{val1},{val2}");
            }
            DevKit.UpdateAndAssert(update);

            // Query by example to get values from the index curve and 2 new curves
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
                "       <unit />" + Environment.NewLine +
                "       <columnIndex />" + Environment.NewLine +
                "    </logCurveInfo>" + Environment.NewLine +
                "    <logCurveInfo>" + Environment.NewLine +
                $"      <mnemonic>{newCurve2.Mnemonic}</mnemonic>" + Environment.NewLine +
                "       <unit />" + Environment.NewLine +
                "       <columnIndex />" + Environment.NewLine +
                "    </logCurveInfo>" + Environment.NewLine +
                "    <logCurveInfo>" + Environment.NewLine +
                $"      <mnemonic>{newCurve1.Mnemonic}</mnemonic>" + Environment.NewLine +
                "       <unit />" + Environment.NewLine +
                "       <columnIndex />" + Environment.NewLine +
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
            Assert.AreEqual(3, log.LogCurveInfo.Count);
            Assert.AreEqual(10, log.LogData.Count);
            foreach (var lc in log.LogCurveInfo)
            {
                var curve = update.LogCurveInfo.FirstOrDefault(x => x.Mnemonic == lc.Mnemonic);
                Assert.IsNotNull(curve);
                // Ensure that the value from the update matches the response using columnIndex
                for (int i = 0; i < 10; i++)
                {
                    Assert.AreEqual(log.LogData[i].Split(',')[lc.ColumnIndex.Value - 1],
                        update.LogData[i].Split(',')[curve.ColumnIndex.Value - 1]);
                }
            }
        }

        [TestMethod]
        public void Log131DataAdapter_AddToStore_Error_Add_DepthLog_Without_IndexCurve()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);
            Log.IndexCurve = null;
            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void Log131DataAdapter_AddToStore_With_Non_Double_Data_Types()
        {
            AddParents();

            // Initialize Log Header
            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            // Add Log Curves with double, datetime, long and string data types
            Log.LogCurveInfo.Clear();
            Log.LogCurveInfo.AddRange(new List<LogCurveInfo>()
            {
                DevKit.LogGenerator.CreateLogCurveInfo("MD", "m", LogDataType.@double, 0),
                DevKit.LogGenerator.CreateLogCurveInfo("ROP", "m/h", LogDataType.@double, 1),
                DevKit.LogGenerator.CreateLogCurveInfo("TS", "s", LogDataType.datetime, 2),
                DevKit.LogGenerator.CreateLogCurveInfo("CNT", "m", LogDataType.@long, 3),
                DevKit.LogGenerator.CreateLogCurveInfo("MSG", "unitless", LogDataType.@string, 4)
            });

            // Generated the data
            var numRows = 5;
            DevKit.LogGenerator.GenerateLogData(Log, numRows);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(Log);

            // Assert that Data Types match before and after
            for (var i = 0; i < Log.LogCurveInfo.Count; i++)
            {
                Assert.IsNotNull(Log.LogCurveInfo[i].TypeLogData);
                Assert.IsNotNull(result.LogCurveInfo[i].TypeLogData);
                Assert.AreEqual(Log.LogCurveInfo[i].TypeLogData.Value, result.LogCurveInfo[i].TypeLogData.Value);
            }

            // Assert data matches before and after
            var logData = Log.LogData;
            var logDataAdded = result.LogData;
            Assert.AreEqual(logData.Count, logDataAdded.Count);

            // For each row of data
            for (int i = 0; i < logData.Count; i++)
            {
                var data = logData[i].Split(',');
                var dataAdded = logDataAdded[i].Split(',');
                Assert.AreEqual(data.Length, dataAdded.Length);

                // Check that the string data matches
                for (int j = 0; j < data.Length; j++)
                {
                    if (Log.LogCurveInfo[j].TypeLogData.Value == LogDataType.@string)
                    {
                        Assert.AreEqual(data[j], dataAdded[j]);
                    }
                }
            }
        }

        [TestMethod]
        public void Log131DataAdapter_AddToStore_Invalid_Data_Rows()
        {
            CompatibilitySettings.InvalidDataRowSetting = InvalidDataRowSetting.Error;

            AddParents();

            // Initialize Log Header
            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            // Initialize data with only 1 data point in the
            DevKit.InitData(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 5);

            DevKit.AddAndAssert(Log, ErrorCodes.ErrorRowDataCount);
        }

        [TestMethod]
        public void Log131DataAdapter_AddToStore_With_Data_Updates_DataRowCount()
        {
            AddParents();

            // Add 10 rows of data
            const int dataRowCount = 10;

            // Add a Log with dataRowCount Rows
            DevKit.AddLogWithData(Log, LogIndexType.measureddepth, dataRowCount, false);

            DevKit.GetAndAssertDataRowCount(DevKit.CreateLog(Log), dataRowCount);
        }

        [TestMethod, Description("As Unit is not required on logCurveInfo this validates adding a log with no unit")]
        public void Log131DataAdapter_AddToStore_With_Empty_Unit_On_Curve()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            //Remove the unit of the last logCurveInfo
            var lci = Log.LogCurveInfo.LastOrDefault();
            Assert.IsNotNull(lci);

            lci.Unit = string.Empty;

            DevKit.AddAndAssert(Log);

            var template = DevKit.CreateLogTemplateQuery(Log);
            var log = DevKit.GetLogWithTemplate(template);

            // Verify the logCurveInfo unit
            var resultLci = log.LogCurveInfo.LastOrDefault();
            Assert.IsNotNull(resultLci);
            Assert.IsNull(resultLci.Unit);
        }

        #region Helper Methods

        private Log GetLog(Log log)
        {
            return DevKit.GetAndAssert(log);
        }

        #endregion
    }
}
