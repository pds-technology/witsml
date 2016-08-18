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

using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Log141DataAdapter Delete tests.
    /// </summary>
    [TestClass]
    public class Log141DataAdapterDeleteTests
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

            _wellbore = new Wellbore
            {
                Uid = _devKit.Uid(),
                UidWell = _well.Uid,
                NameWell = _well.Name,
                Name = _devKit.Name("Wellbore 01")
            };

            _log = _devKit.CreateLog(_devKit.Uid(), _devKit.Name("Log 01"), _well.Uid, _well.Name, _wellbore.Uid, _wellbore.Name);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_Log_With_No_Data()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            AddLog(_log);

            // Query log
            GetLog(_log);

            // Delete log
            DeleteLog(_log, string.Empty);

            // Assert log is deleted
            var query = _devKit.CreateLog(_log.Uid, null, _log.UidWell, null, _log.UidWellbore, null);
            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_Log_With_Case_Insensitive_Uid()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var uid = _devKit.Uid();
            _log.Uid = "l" + uid;
            AddLog(_log);

            // Query log
            GetLog(_log);

            // Delete log
            var delete = _devKit.CreateLog("L" + uid, null, _well.Uid, null, _wellbore.Uid, null);
            DeleteLog(delete, string.Empty);

            // Assert log is deleted
            var query = _devKit.CreateLog(_log.Uid, null, _log.UidWell, null, _log.UidWellbore, null);
            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Partial_Delete_Elements()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            _log.ServiceCompany = "company 1";
            _log.StepIncrement = new RatioGenericMeasure {Uom = "m", Value = 1.0};

            // Add log
            AddLog(_log);

            // Assert all testing elements are added
            var result = GetLog(_log);
            Assert.AreEqual(_log.ServiceCompany, result.ServiceCompany);
            Assert.AreEqual(_log.Direction, result.Direction);

            // Partial delete well
            const string delete = "<serviceCompany /><stepIncrement />";
            DeleteLog(_log, delete);

            // Assert the well elements has been deleted
            result = GetLog(_log);
            Assert.IsNull(result.ServiceCompany);
            Assert.IsNull(result.StepIncrement);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Partial_Delete_Nested_Elements()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var testCommonData = new CommonData
            {
                Comments = "Testing partial delete nested elements",
                ItemState = ItemState.plan
            };

            _log.CommonData = testCommonData;

            // Add log
            AddLog(_log);

            // Assert all testing elements are added
            var result = GetLog(_log);
            var commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            Assert.AreEqual(testCommonData.Comments, commonData.Comments);
            Assert.AreEqual(testCommonData.ItemState, commonData.ItemState);

            // Partial delete well
            const string delete = "<commonData><comments /><itemState /></commonData>";
            DeleteLog(_log, delete);

            // Assert the well elements has been deleted
            result = GetLog(_log);
            commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            Assert.IsNull(commonData.Comments);
            Assert.IsNull(commonData.ItemState);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Partial_Delete_Recurring_Elements()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var curve1 = _log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = _log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);
            curve2.CurveDescription = "Testing partial delete recurring elements";

            // Add log
            AddLog(_log);

            // Assert all testing elements are added
            var result = GetLog(_log);
            var curves = result.LogCurveInfo;
            var resultCurve1 = curves.FirstOrDefault(c => c.Uid == curve1.Uid);
            Assert.IsNotNull(resultCurve1);
            var resultCurve2 = curves.FirstOrDefault(c => c.Uid == curve2.Uid);
            Assert.IsNotNull(resultCurve2);
            Assert.AreEqual(curve2.CurveDescription, resultCurve2.CurveDescription);

            // Partial delete well
            var delete = "<logCurveInfo uid=\"" + curve1.Uid + "\" />" + Environment.NewLine +
                "<logCurveInfo uid=\"" + curve2.Uid + "\">" + Environment.NewLine +
                    "<curveDescription />" + Environment.NewLine +
                "</logCurveInfo>";
            DeleteLog(_log, delete);

            // Assert the well elements has been deleted
            result = GetLog(_log);
            curves = result.LogCurveInfo;
            resultCurve1 = curves.FirstOrDefault(c => c.Uid == curve1.Uid);
            Assert.IsNull(resultCurve1);
            resultCurve2 = curves.FirstOrDefault(c => c.Uid == curve2.Uid);
            Assert.IsNotNull(resultCurve2);
            Assert.IsNull(resultCurve2.CurveDescription);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Partial_Delete_Nested_Recurring_Elements()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var curve1 = _log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = _log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);
            curve2.CurveDescription = "Testing partial delete recurring elements";

            var axis1 = new AxisDefinition
            {
                Uid = "1",
                Order = 1,
                Count = 3,
                DoubleValues = "1 2 3"
            };

            var axis2 = new AxisDefinition
            {
                Uid = "2",
                Order = 2,
                Count = 3,
                DoubleValues = "2 3 4",
                Name = "Axis 2"
            };

            curve2.AxisDefinition = new List<AxisDefinition> {axis1, axis2};

            // Add log
            AddLog(_log);

            // Assert all testing elements are added
            var result = GetLog(_log);

            // Assert log curves
            var curves = result.LogCurveInfo;
            var resultCurve1 = curves.FirstOrDefault(c => c.Uid == curve1.Uid);
            Assert.IsNotNull(resultCurve1);
            var resultCurve2 = curves.FirstOrDefault(c => c.Uid == curve2.Uid);
            Assert.IsNotNull(resultCurve2);
            Assert.AreEqual(curve2.CurveDescription, resultCurve2.CurveDescription);

            // Assert axid definition of 2nd curve
            var resultAxis = resultCurve2.AxisDefinition;
            var resultAxis1 = resultAxis.FirstOrDefault(a => a.Uid == axis1.Uid);
            Assert.IsNotNull(resultAxis1);
            var resultAxis2 = resultAxis.FirstOrDefault(a => a.Uid == axis2.Uid);
            Assert.IsNotNull(resultAxis2);
            Assert.AreEqual(axis2.Name, resultAxis2.Name);

            // Partial delete well
            var delete = "<logCurveInfo uid=\"" + curve1.Uid + "\" />" + Environment.NewLine +
                "<logCurveInfo uid=\"" + curve2.Uid + "\">" + Environment.NewLine +
                    "<curveDescription />" + Environment.NewLine +
                    "<axisDefinition uid=\"" + axis1.Uid + "\" />" + Environment.NewLine +
                    "<axisDefinition uid=\"" + axis2.Uid + "\">" + Environment.NewLine +
                        "<name />" + Environment.NewLine +
                    "</axisDefinition>" + Environment.NewLine +
                "</logCurveInfo>";
            DeleteLog(_log, delete);

            // Assert the well elements has been deleted
            result = GetLog(_log);

            // Assert log curves
            curves = result.LogCurveInfo;
            resultCurve1 = curves.FirstOrDefault(c => c.Uid == curve1.Uid);
            Assert.IsNull(resultCurve1);
            resultCurve2 = curves.FirstOrDefault(c => c.Uid == curve2.Uid);
            Assert.IsNotNull(resultCurve2);
            Assert.IsNull(resultCurve2.CurveDescription);

            // Assert axid definition of 2nd curve
            resultAxis = resultCurve2.AxisDefinition;
            resultAxis1 = resultAxis.FirstOrDefault(a => a.Uid == axis1.Uid);
            Assert.IsNull(resultAxis1);
            resultAxis2 = resultAxis.FirstOrDefault(a => a.Uid == axis2.Uid);
            Assert.IsNotNull(resultAxis2);
            Assert.IsNull(resultAxis2.Name);
        }

        private void AddParents()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        private void AddLog(Log log)
        {
            var response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        private Log GetLog(Log log)
        {
            var query = _devKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

            return result;
        }

        private void DeleteLog(Log log, string delete)
        {
            var queryIn = string.Format(DevKit141Aspect.BasicDeleteLogXmlTemplate, log.Uid, log.UidWell, log.UidWellbore, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Log, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }
    }
}
