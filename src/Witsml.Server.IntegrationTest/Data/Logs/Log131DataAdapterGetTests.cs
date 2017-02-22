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

using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Server.Configuration;
using System.Xml.Linq;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Log131DataAdapter Get tests.
    /// </summary>
    [TestClass]
    public partial class Log131DataAdapterGetTests : Log131TestBase
    {
        protected override void OnTestSetUp()
        {
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
            var objectTemplate = CreateLogTemplateQuery(Log);

            // Set the count element value to 16
            DevKit.Template.Set(objectTemplate, "//count", 16);

            var result = DevKit.GetFromStore(ObjectTypes.Log, objectTemplate.ToString(), null, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            AssertAxisDefintion(Log, arrayCurve1, result.XMLout);

            // Create a new log template
            objectTemplate = CreateLogTemplateQuery();

            // Set the count element value to 64
            DevKit.Template.Set(objectTemplate, "//count", 64);

            result = DevKit.GetFromStore(ObjectTypes.Log, objectTemplate.ToString(), null, OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            AssertAxisDefintion(log2, arrayCurve2, result.XMLout);
            AssertAxisDefintion(log3, arrayCurve3, result.XMLout);

            // Create a new log template
            objectTemplate = CreateLogTemplateQuery();

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

        #region Helper Methods

        private XDocument CreateLogTemplateQuery(Log log = null, bool includeData = false)
        {
            var document = DevKit.Template.Create<LogList>();

            Assert.IsNotNull(document);
            Assert.IsNotNull(document.Root);

            // If log is not null set the UIDs
            if (log != null)
                DevKit.SetDocumentUids(log, document);

            // Remove log data
            if (!includeData)
                DevKit.Template.Remove(document, "//logData");

            return document;
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
        #endregion Helper Methods
    }
}
