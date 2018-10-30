//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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

using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Store.Configuration;
using System.Xml.Linq;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    /// <summary>
    /// Log131DataAdapter Get tests.
    /// </summary>
    [TestClass]
    public partial class Log131DataAdapterGetTests : Log131TestBase
    {
        protected override void OnTestSetUp()
        {
            base.OnTestSetUp();

            // Sets the depth and time chunk size
            WitsmlSettings.DepthRangeSize = 1000;
            WitsmlSettings.TimeRangeSize = 86400000000; // Number of microseconds equals to one day
        }

        [TestMethod]
        public void Log131DataAdapter_GetFromStore_Header_Only_No_Data()
        {
            int numRows = 10;

            // Add the Setup Well, Wellbore and Log to the store.
            var logResponse = AddSetupWellWellboreLog(numRows, isDepthLog: true, hasEmptyChannel: false, increasing: true);
            Assert.AreEqual((short)ErrorCodes.Success, logResponse.Result);

            var queryHeaderOnly = DevKit.CreateLog(logResponse.SuppMsgOut, null, Log.UidWell, null, Log.UidWellbore, null);

            // Perform a GetFromStore with multiple log queries
            var result = DevKit.Get<LogList, Log>(
                DevKit.List(queryHeaderOnly),
                ObjectTypes.Log,
                null,
                OptionsIn.ReturnElements.HeaderOnly);

            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            Assert.IsNotNull(logList);
            Assert.AreEqual(1, logList.Log.Count);
            Assert.AreEqual(0, logList.Log[0].LogData.Count);
        }

        [TestMethod]
        public void Log131DataAdapter_GetFromStore_Request_Multiple_Recurring_Items_With_Empty_Value()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth, false);
            var responseAddLog = DevKit.Add<LogList, Log>(Log);
            Assert.IsNotNull(responseAddLog);
            Assert.AreEqual((short)ErrorCodes.Success, responseAddLog.Result);

            var queryIn = @"
                            <logs xmlns=""http://www.witsml.org/schemas/131"" version=""1.3.1.1"">      
                                <log uid=""" + Log.Uid + @""">
                                    <logCurveInfo> 
                                        <mnemonic>ROP</mnemonic> 
                                        <unit /> 
                                    </logCurveInfo>
                                    <logCurveInfo>
                                        <mnemonic>{0}</mnemonic> 
                                        <unit /> 
                                    </logCurveInfo>
                                </log>
                            </logs>";

            var response = DevKit.GetFromStore(ObjectTypes.Log, string.Format(queryIn, "GR"), null, null);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var responseError = DevKit.GetFromStore(ObjectTypes.Log, string.Format(queryIn, ""), null, null);
            Assert.IsNotNull(responseError);
            Assert.AreEqual((short)ErrorCodes.RecurringItemsEmptySelection, responseError.Result);
        }

        [TestMethod]
        public void Log131DataAdapter_GetFromStore_Recurring_By_AxisDefinition()
        {
            AddParents();

            // Create a log with a 16 count arrayCurve1
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("arrayCurve", "unitless", 4));
            var arrayCurve1 = Log.LogCurveInfo.First(x => x.Mnemonic == "arrayCurve");
            arrayCurve1.AxisDefinition = new List<AxisDefinition>()
            {
                new AxisDefinition()
                {
                    Uid = DevKit.Uid(),
                    Order = 1,
                    Count = 16,
                }
            };

            DevKit.AddAndAssert(Log);

            // Create a log with a 64 count arrayCurve2
            var log2 = DevKit.CreateLog(Log);
            log2.Uid = DevKit.Uid();
            DevKit.InitHeader(log2, LogIndexType.measureddepth);
            log2.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("arrayCurve2", "g/cm3", 4));
            var arrayCurve2 = log2.LogCurveInfo.First(x => x.Mnemonic == "arrayCurve2");
            arrayCurve2.AxisDefinition = new List<AxisDefinition>()
            {
                new AxisDefinition()
                {
                    Uid = DevKit.Uid(),
                    Order = 1,
                    Count = 64,
                }
            };

            DevKit.AddAndAssert(log2);

            // Create another log with a 64 count arrayCurve2
            var log3 = DevKit.CreateLog(Log);
            log3.Uid = DevKit.Uid();
            DevKit.InitHeader(log3, LogIndexType.measureddepth);
            log3.LogCurveInfo.Add(DevKit.CreateDoubleLogCurveInfo("arrayCurve2", "g/cm3", 4));
            var arrayCurve3 = log3.LogCurveInfo.First(x => x.Mnemonic == "arrayCurve2");
            arrayCurve3.AxisDefinition = new List<AxisDefinition>()
            {
                new AxisDefinition()
                {
                    Uid = DevKit.Uid(),
                    Order = 1,
                    Count = 64,
                }
            };

            DevKit.AddAndAssert(log3);

            // Query for log 1
            var objectTemplate = DevKit.CreateLogTemplateQuery(Log);

            // Set the count element value to 16
            DevKit.Template.Set(objectTemplate, "//count", 16);

            var result = DevKit.GetFromStore(ObjectTypes.Log, objectTemplate.ToString(), null, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            AssertAxisDefintion(Log, arrayCurve1, result.XMLout);

            // Create a new log template
            objectTemplate = DevKit.CreateLogTemplateQuery();

            // Set the count element value to 64
            DevKit.Template.Set(objectTemplate, "//count", 64);

            result = DevKit.GetFromStore(ObjectTypes.Log, objectTemplate.ToString(), null, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            AssertAxisDefintion(log2, arrayCurve2, result.XMLout);
            AssertAxisDefintion(log3, arrayCurve3, result.XMLout);

            // Create a new log template
            objectTemplate = DevKit.CreateLogTemplateQuery();

            // Query for logs that contain curve ROP or arrayCurve2
            DevKit.Template.Set(objectTemplate, "//mnemonic", "ROP");

            var lci = DevKit.Template.Clone(objectTemplate, "//logCurveInfo");
            DevKit.Template.Set(lci, "//mnemonic", arrayCurve2.Mnemonic);
            DevKit.Template.Push(objectTemplate, "//logCurveInfo", lci.Root);

            result = DevKit.GetFromStore(ObjectTypes.Log, objectTemplate.ToString(), null, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            AssertAxisDefintion(log2, arrayCurve2, result.XMLout, 3);
            AssertAxisDefintion(log3, arrayCurve3, result.XMLout, 3);
        }

        [TestMethod]
        public void Log131DataAdapter_GetFromStore_Recurring_By_LogParam()
        {
            AddParents();

            // Create a log with a 2 logParams
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogParam = new List<IndexedObject>()
            {
                DevKit.IndexedObject(1, 1),
                DevKit.IndexedObject(2, 2)
            };

            DevKit.AddAndAssert(Log);

            // Create a log with a 2 logParams
            var log2 = DevKit.CreateLog(Log);
            log2.Uid = DevKit.Uid();
            DevKit.InitHeader(log2, LogIndexType.measureddepth);
            log2.LogParam = new List<IndexedObject>()
            {
                DevKit.IndexedObject(3, 1),
                DevKit.IndexedObject(2, 2)
            };

            DevKit.AddAndAssert(log2);

            // Create a log with a 2 logParams
            var log3 = DevKit.CreateLog(Log);
            log3.Uid = DevKit.Uid();
            DevKit.InitHeader(log3, LogIndexType.measureddepth);
            log3.LogParam = new List<IndexedObject>()
            {
                DevKit.IndexedObject(3, 1),
                DevKit.IndexedObject(4, 2)
            };

            DevKit.AddAndAssert(log3);

            var logParam1 = Log.LogParam.FirstOrDefault(x => x.Name == "Test1");
            var logParam2 = Log.LogParam.FirstOrDefault(x => x.Name == "Test2");
            var logParam3 = log3.LogParam.FirstOrDefault(x => x.Name == "Test3");
            var logParam4 = log3.LogParam.FirstOrDefault(x => x.Name == "Test4");

            Assert.IsNotNull(logParam1);
            Assert.IsNotNull(logParam2);
            Assert.IsNotNull(logParam3);
            Assert.IsNotNull(logParam4);

            // Query for log 1
            var objectTemplate = DevKit.CreateLogTemplateQuery(Log);

            // Set the param value to test1
            DevKit.Template.Set(objectTemplate, "//logParam/@name", logParam1.Name);

            var result = DevKit.GetFromStore(ObjectTypes.Log, objectTemplate.ToString(), null, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            AssertLogParam(Log, logParam1, result.XMLout);

            // Query for logs that have logParam2
            objectTemplate = DevKit.CreateLogTemplateQuery();

            // Set the param value to test1
            DevKit.Template.Set(objectTemplate, "//logParam/@name", logParam2.Name);

            result = DevKit.GetFromStore(ObjectTypes.Log, objectTemplate.ToString(), null, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            AssertLogParam(Log, logParam2, result.XMLout);
            AssertLogParam(log2, logParam2, result.XMLout);

            // Query for logs that have logParams with the value of 13.0 or 14.0
            objectTemplate = DevKit.CreateLogTemplateQuery();

            // Set the param value to test1
            var logParam = DevKit.Template.Clone(objectTemplate, "//logParam");
            DevKit.Template.Set(logParam, "//logParam", logParam4.Value);
            DevKit.Template.Push(objectTemplate, "//logParam", logParam.Root);

            DevKit.Template.Set(objectTemplate, "//logParam", logParam3.Value);

            result = DevKit.GetFromStore(ObjectTypes.Log, objectTemplate.ToString(), null, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            AssertLogParam(log2, logParam3, result.XMLout);
            AssertLogParam(log3, logParam3, result.XMLout);
            AssertLogParam(log3, logParam4, result.XMLout);
        }

        [TestMethod]
        public void Log131DataAdapter_GetFromStore_With_DataRowCount_Greater_Than_LogMaxDataNodesGet()
        {
            WitsmlSettings.LogMaxDataNodesGet = 8;

            // dataRowCount will be 9
            const int totalAddRows = 10;
            const int lessRows = 1;

            // Assert that only LogMaxDataNodesGet rows are returned which is less than dataRowCount
            GetAndAssertByDataRowCount(totalAddRows, lessRows, WitsmlSettings.LogMaxDataNodesGet);
        }

        [TestMethod]
        public void Log131DataAdapter_GetFromStore_With_DataRowCount_Less_Than_Total()
        {
            const int totalAddRows = 10;
            const int lessRows = 5;
            GetAndAssertByDataRowCount(totalAddRows, lessRows, totalAddRows - lessRows);
        }

        [TestMethod]
        public void Log131DataAdapter_GetFromStore_With_DataRowCount_Equals_Total()
        {
            const int totalAddRows = 10;
            const int lessRows = 0;
            GetAndAssertByDataRowCount(totalAddRows, lessRows, totalAddRows);
        }

        [TestMethod]
        public void Log131DataAdapter_GetFromStore_With_DataRowCount_Greater_Than_Total()
        {
            const int totalAddRows = 10;
            const int lessRows = -5;
            GetAndAssertByDataRowCount(totalAddRows, lessRows, totalAddRows);
        }

        [TestMethod]
        public void Log131DataAdapter_GetFromStore_With_MaxDataNodes()
        {
            const int totalAddRows = 10;
            const int expectedRows = 5;

            AddParents();

            // Add a Log with dataRowCount Rows
            DevKit.AddLogWithData(Log, LogIndexType.measureddepth, totalAddRows, false);

            var queryLog = DevKit.CreateLog(Log);

            // Get the log
            var optionsIn = $"{OptionsIn.ReturnElements.MaxReturnNodes.Eq(expectedRows)};{OptionsIn.ReturnElements.All}";
            var result = DevKit.GetAndAssert<LogList, Log>(queryLog, optionsIn: optionsIn);

            // Assert that the DataRowCount is the same as the number of rows added.
            Assert.AreEqual(expectedRows, result.LogData.Count);

            // First rows are equal
            Assert.AreEqual(Log.LogData[0], result.LogData[0]);
        }

        [TestMethod]
        public void Log131DataAdapter_GetFromStore_With_RequestLatestValues()
        {
            const int totalAddRows = 10;
            const int expectedRows = 5;

            AddParents();

            // Add a Log with dataRowCount Rows
            DevKit.AddLogWithData(Log, LogIndexType.measureddepth, totalAddRows, false);

            var queryLog = DevKit.CreateLog(Log);

            // Get the log
            var optionsIn = $"{OptionsIn.ReturnElements.RequestLatestValues.Eq(expectedRows)};{OptionsIn.ReturnElements.All}";
            var result = DevKit.GetAndAssert<LogList, Log>(queryLog, optionsIn: optionsIn);

            // Assert that the DataRowCount is the same as the number of rows added.
            Assert.AreEqual(expectedRows, result.LogData.Count);

            // Last rows are equal
            Assert.AreEqual(Log.LogData[Log.LogData.Count-1], result.LogData[result.LogData.Count-1]);
        }

        #region Helper Methods

        private static void AssertLogParam(Log expectedLog, IndexedObject expectedLogParam, string xmlOut)
        {
            var logList = EnergisticsConverter.XmlToObject<LogList>(xmlOut);
            Assert.IsNotNull(logList);
            Assert.IsTrue(logList.Log.Count > 0);

            var log = logList.Log.FirstOrDefault(x => x.Uid == expectedLog.Uid);
            Assert.IsNotNull(log);

            var logParam = log.LogParam.FirstOrDefault(x => x.Name == expectedLogParam.Name);
            Assert.IsNotNull(logParam);
            Assert.AreEqual(expectedLogParam.Value, logParam.Value);
            Assert.AreEqual(expectedLogParam.Uom, logParam.Uom);
            Assert.AreEqual(expectedLogParam.Description, logParam.Description);
            Assert.AreEqual(expectedLogParam.Index, logParam.Index);
        }
        
        private static void AssertAxisDefintion(Log expectedLog, LogCurveInfo curve, string xmlOut, int expectedCurveCount = 2)
        {
            var logList = EnergisticsConverter.XmlToObject<LogList>(xmlOut);
            Assert.IsNotNull(logList);
            Assert.IsTrue(logList.Log.Count > 0);

            var log = logList.Log.FirstOrDefault(x => x.Uid == expectedLog.Uid);
            Assert.IsNotNull(log);
            Assert.AreEqual(expectedCurveCount, log.LogCurveInfo.Count);

            var logCurveInfo = log.LogCurveInfo.FirstOrDefault(x => x.Mnemonic == curve.Mnemonic);
            Assert.IsNotNull(logCurveInfo);
            Assert.IsTrue(logCurveInfo.AxisDefinition.Count > 0);

            var axisDef = logCurveInfo.AxisDefinition.FirstOrDefault();
            Assert.IsNotNull(axisDef);
            Assert.AreEqual(curve.AxisDefinition[0].Uid, axisDef.Uid);
            Assert.AreEqual(curve.AxisDefinition[0].Count, axisDef.Count);
            Assert.AreEqual(curve.AxisDefinition[0].DoubleValues, axisDef.DoubleValues);
        }

        private WMLS_AddToStoreResponse AddSetupWellWellboreLog(int numRows, bool isDepthLog, bool hasEmptyChannel, bool increasing)
        {
            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);
            DevKit.InitHeader(Log, LogIndexType.measureddepth, increasing);

            var startIndex = new GenericMeasure { Uom = "m", Value = 100 };
            Log.StartIndex = startIndex;
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), numRows, 1, isDepthLog, hasEmptyChannel, increasing);

            // Add a log
            return DevKit.Add<LogList, Log>(Log);
        }

        private void GetAndAssertByDataRowCount(int totalAddRows, int lessRows, int expectedRows)
        {
            AddParents();

            // Add a Log with dataRowCount Rows
            DevKit.AddLogWithData(Log, LogIndexType.measureddepth, totalAddRows, false);

            var queryLog = DevKit.CreateLog(Log);
            DevKit.GetAndAssertDataRowCount(queryLog, totalAddRows);

            // Retrieve less than dataRowCount rows
            queryLog.DataRowCount = totalAddRows - lessRows;
            DevKit.GetAndAssertDataRowCountExpected(queryLog, expectedRows);
        }
        #endregion Helper Methods
    }
}
