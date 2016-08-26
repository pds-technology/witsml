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

            _devKit.AddAndAssert(_log);

            // Query log
            _devKit.GetOneAndAssert(_log);

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
            _devKit.AddAndAssert(_log);

            // Query log
            _devKit.GetOneAndAssert(_log);

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
            _devKit.AddAndAssert(_log);

            // Assert all testing elements are added
            var result = _devKit.GetOneAndAssert(_log);
            Assert.AreEqual(_log.ServiceCompany, result.ServiceCompany);
            Assert.AreEqual(_log.Direction, result.Direction);

            // Partial delete well
            const string delete = "<serviceCompany /><stepIncrement />";
            DeleteLog(_log, delete);

            // Assert the well elements has been deleted
            result = _devKit.GetOneAndAssert(_log);
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
            _devKit.AddAndAssert(_log);

            // Assert all testing elements are added
            var result = _devKit.GetOneAndAssert(_log);
            var commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            Assert.AreEqual(testCommonData.Comments, commonData.Comments);
            Assert.AreEqual(testCommonData.ItemState, commonData.ItemState);

            // Partial delete well
            const string delete = "<commonData><comments /><itemState /></commonData>";
            DeleteLog(_log, delete);

            // Assert the well elements has been deleted
            result = _devKit.GetOneAndAssert(_log);
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
            _devKit.AddAndAssert(_log);

            // Assert all testing elements are added
            var result = _devKit.GetOneAndAssert(_log);
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
            result = _devKit.GetOneAndAssert(_log);
            curves = result.LogCurveInfo;
            resultCurve1 = curves.FirstOrDefault(c => c.Uid == curve1.Uid);
            Assert.IsNull(resultCurve1);
            resultCurve2 = curves.FirstOrDefault(c => c.Uid == curve2.Uid);
            Assert.IsNotNull(resultCurve2);
            Assert.IsNull(resultCurve2.CurveDescription);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Partial_Delete_Recurring_Simple_Contents()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var param = new IndexedObject {Uid = "1", Uom = "m", Value = "10"};
            _log.LogParam = new List<IndexedObject> {param};

            // Add log
            _devKit.AddAndAssert(_log);

            // Assert all testing elements are added
            var result = _devKit.GetOneAndAssert(_log);
            Assert.AreEqual(1, result.LogParam.Count);

            // Partial delete well
            var delete = "<logParam uid=\"" + param.Uid + "\" />";
            DeleteLog(_log, delete);

            // Assert the well elements has been deleted
            result = _devKit.GetOneAndAssert(_log);
            Assert.AreEqual(0, result.LogParam.Count);
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
            _devKit.AddAndAssert(_log);

            // Assert all testing elements are added
            var result = _devKit.GetOneAndAssert(_log);

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

            // Partial delete log
            var delete = "<logCurveInfo uid=\"" + curve1.Uid + "\" />" + Environment.NewLine +
                "<logCurveInfo uid=\"" + curve2.Uid + "\">" + Environment.NewLine +
                    "<curveDescription />" + Environment.NewLine +
                    "<axisDefinition uid=\"" + axis1.Uid + "\" />" + Environment.NewLine +
                    "<axisDefinition uid=\"" + axis2.Uid + "\">" + Environment.NewLine +
                        "<name />" + Environment.NewLine +
                    "</axisDefinition>" + Environment.NewLine +
                "</logCurveInfo>";
            DeleteLog(_log, delete);

            // Assert the log elements has been deleted
            result = _devKit.GetOneAndAssert(_log);

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

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_All_Log_Channels_And_Data()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            var logData = _log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");

            _devKit.AddAndAssert(_log);

            var indexCurve = _log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == _log.IndexCurve);
            Assert.IsNotNull(indexCurve);

            var result = _devKit.GetOneAndAssert(_log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            Assert.AreEqual(_log.LogCurveInfo.Count, result.LogCurveInfo.Count);

            var delete = string.Empty;
            foreach (var curve in _log.LogCurveInfo)
            {
                delete += "<logCurveInfo uid=\"" + curve.Uid + "\" />";
            }
            DeleteLog(_log, delete);

            result = _devKit.GetOneAndAssert(_log);
            Assert.AreEqual(0, result.LogCurveInfo.Count);
            Assert.AreEqual(0, result.LogData.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_All_Log_Data_By_Mnemonics_Only()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            var logData = _log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");

            _devKit.AddAndAssert(_log);

            var indexCurve = _log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == _log.IndexCurve);
            Assert.IsNotNull(indexCurve);

            var result = _devKit.GetOneAndAssert(_log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            Assert.AreEqual(_log.LogCurveInfo.Count, result.LogCurveInfo.Count);

            var delete = string.Empty;
            foreach (var curve in _log.LogCurveInfo)
            {
                delete += "<logCurveInfo><mnemonic>" + curve.Mnemonic.Value + "</mnemonic></logCurveInfo>";
            }
            DeleteLog(_log, delete);

            result = _devKit.GetOneAndAssert(_log);          
            Assert.AreEqual(0, result.LogData.Count);

            Assert.AreEqual(_log.LogCurveInfo.Count, result.LogCurveInfo.Count);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.IsNull(curve.MinIndex);
                Assert.IsNull(curve.MaxIndex);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_All_Log_Data_By_Index_Curve_With_Range()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            var logData = _log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");

            _devKit.AddAndAssert(_log);

            var indexCurve = _log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == _log.IndexCurve);
            Assert.IsNotNull(indexCurve);

            var result = _devKit.GetOneAndAssert(_log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            Assert.AreEqual(_log.LogCurveInfo.Count, result.LogCurveInfo.Count);

            var delete = "<logCurveInfo>" + Environment.NewLine +
                "<mnemonic>" + indexCurve.Mnemonic.Value + "</mnemonic>" + Environment.NewLine +
                "<minIndex uom=\"" + indexCurve.Unit + "\">10</minIndex>" + Environment.NewLine +
                "</logCurveInfo>";
            
            DeleteLog(_log, delete);

            result = _devKit.GetOneAndAssert(_log);
            Assert.AreEqual(0, result.LogData.Count);

            Assert.AreEqual(_log.LogCurveInfo.Count, result.LogCurveInfo.Count);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.IsNull(curve.MinIndex);
                Assert.IsNull(curve.MaxIndex);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_Full_Increasing_Log_Data_By_Index()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            var logData = _log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");

            _devKit.AddAndAssert(_log);

            var indexCurve = _log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == _log.IndexCurve);
            Assert.IsNotNull(indexCurve);

            var result = _devKit.GetOneAndAssert(_log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(13, curve.MinIndex.Value);
                Assert.AreEqual(15, curve.MaxIndex.Value);
            }

            var delete = "<endIndex uom=\"" + indexCurve.Unit + "\">20</endIndex>";
            DeleteLog(_log, delete);

            result = _devKit.GetOneAndAssert(_log);
            Assert.AreEqual(0, result.LogData.Count);

            foreach (var curve in result.LogCurveInfo)
            {
                Assert.IsNull(curve.MinIndex);
                Assert.IsNull(curve.MaxIndex);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_Full_Decreasing_Log_Data_By_Index()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth, false);
            var logData = _log.LogData.First();         
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("13,13.1,13.2");

            _devKit.AddAndAssert(_log);

            var indexCurve = _log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == _log.IndexCurve);
            Assert.IsNotNull(indexCurve);

            var result = _devKit.GetOneAndAssert(_log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(13, curve.MinIndex.Value);
                Assert.AreEqual(15, curve.MaxIndex.Value);
            }
            Assert.AreEqual(15, result.StartIndex.Value);
            Assert.AreEqual(13, result.EndIndex.Value);

            var delete = "<endIndex uom=\"" + indexCurve.Unit + "\">10</endIndex>";
            DeleteLog(_log, delete);

            result = _devKit.GetOneAndAssert(_log);
            Assert.AreEqual(0, result.LogData.Count);

            foreach (var curve in result.LogCurveInfo)
            {
                Assert.IsNull(curve.MinIndex);
                Assert.IsNull(curve.MaxIndex);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_Full_Increasing_Channel_Data_By_Index()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            var logData = _log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");

            _devKit.AddAndAssert(_log);

            var indexCurve = _log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == _log.IndexCurve);
            Assert.IsNotNull(indexCurve);
            var lastCurve = _log.LogCurveInfo.Last();
            Assert.IsNotNull(lastCurve);

            var result = _devKit.GetOneAndAssert(_log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(13, curve.MinIndex.Value);
                Assert.AreEqual(15, curve.MaxIndex.Value);
            }

            var delete = "<logCurveInfo uid=\"" + lastCurve.Uid + "\">" + Environment.NewLine +
                    "<minIndex uom=\"" + indexCurve.Unit + "\">10</minIndex>" + Environment.NewLine +
                "</logCurveInfo>";
            DeleteLog(_log, delete);

            result = _devKit.GetOneAndAssert(_log);
            resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);

            var mnemonics = resultLogData.MnemonicList.Split(',');
            Assert.IsFalse(mnemonics.Contains(lastCurve.Mnemonic.Value));

            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(13, curve.MinIndex.Value);
                Assert.AreEqual(15, curve.MaxIndex.Value);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_Full_Decreasing_Channel_Data_By_Index()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth, false);
            var logData = _log.LogData.First();
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("13,13.1,13.2");

            _devKit.AddAndAssert(_log);

            var indexCurve = _log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == _log.IndexCurve);
            Assert.IsNotNull(indexCurve);
            var lastCurve = _log.LogCurveInfo.Last();
            Assert.IsNotNull(lastCurve);

            var result = _devKit.GetOneAndAssert(_log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(13, curve.MinIndex.Value);
                Assert.AreEqual(15, curve.MaxIndex.Value);
            }
            Assert.AreEqual(15, result.StartIndex.Value);
            Assert.AreEqual(13, result.EndIndex.Value);

            var delete = "<logCurveInfo uid=\"" + lastCurve.Uid + "\">" + Environment.NewLine +
                    "<minIndex uom=\"" + indexCurve.Unit + "\">10</minIndex>" + Environment.NewLine +
                "</logCurveInfo>";
            DeleteLog(_log, delete);

            result = _devKit.GetOneAndAssert(_log);
            resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);

            var mnemonics = resultLogData.MnemonicList.Split(',');
            Assert.IsFalse(mnemonics.Contains(lastCurve.Mnemonic.Value));

            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(13, curve.MinIndex.Value);
                Assert.AreEqual(15, curve.MaxIndex.Value);
            }
            Assert.AreEqual(15, result.StartIndex.Value);
            Assert.AreEqual(13, result.EndIndex.Value);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_Increasing_Channels_Data_With_Different_Index_Range()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            var logData = _log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("17,17.1,17.2");
            logData.Data.Add("18,18.1,18.2");

            _devKit.AddAndAssert(_log);

            var indexCurve = _log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == _log.IndexCurve);
            Assert.IsNotNull(indexCurve);
            var curve1 = _log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = _log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);

            var result = _devKit.GetOneAndAssert(_log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(13, curve.MinIndex.Value);
                Assert.AreEqual(18, curve.MaxIndex.Value);
            }

            var delete = "<logCurveInfo uid=\"" + curve1.Uid + "\">" + Environment.NewLine +
                    "<minIndex uom=\"" + indexCurve.Unit + "\">15</minIndex>" + Environment.NewLine +
                "</logCurveInfo>" + Environment.NewLine +
                "<logCurveInfo uid=\"" + curve2.Uid + "\">" + Environment.NewLine +
                    "<maxIndex uom=\"" + indexCurve.Unit + "\">15</maxIndex>" + Environment.NewLine +
                "</logCurveInfo>";
            DeleteLog(_log, delete);

            result = _devKit.GetOneAndAssert(_log);

            // Assert log data
            resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            var data = resultLogData.Data;
            Assert.AreEqual("13,13.1,", data[0]);
            Assert.AreEqual("14,14.1,", data[1]);
            Assert.AreEqual("16,,16.2", data[2]);
            Assert.AreEqual("17,,17.2", data[3]);
            Assert.AreEqual("18,,18.2", data[4]);

            // Assert Index
            curve1 = result.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            Assert.AreEqual(14, curve1.MaxIndex.Value);
            curve2 = result.LogCurveInfo[2];
            Assert.IsNotNull(curve2);
            Assert.AreEqual(16, curve2.MinIndex.Value);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_Decreasing_Channels_Data_With_Different_Index_Range()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth, false);
            var logData = _log.LogData.First();
            logData.Data.Add("18,18.1,18.2");
            logData.Data.Add("17,17.1,17.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("13,13.1,13.2");

            _devKit.AddAndAssert(_log);

            var indexCurve = _log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == _log.IndexCurve);
            Assert.IsNotNull(indexCurve);
            var curve1 = _log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = _log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);

            var result = _devKit.GetOneAndAssert(_log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(13, curve.MinIndex.Value);
                Assert.AreEqual(18, curve.MaxIndex.Value);
            }
            Assert.AreEqual(18, result.StartIndex.Value);
            Assert.AreEqual(13, result.EndIndex.Value);

            var delete = "<logCurveInfo uid=\"" + curve1.Uid + "\">" + Environment.NewLine +
                    "<minIndex uom=\"" + indexCurve.Unit + "\">15</minIndex>" + Environment.NewLine +
                "</logCurveInfo>" + Environment.NewLine +
                "<logCurveInfo uid=\"" + curve2.Uid + "\">" + Environment.NewLine +
                    "<maxIndex uom=\"" + indexCurve.Unit + "\">15</maxIndex>" + Environment.NewLine +
                "</logCurveInfo>";
            DeleteLog(_log, delete);

            result = _devKit.GetOneAndAssert(_log);

            // Assert log data
            resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            var data = resultLogData.Data;
            Assert.AreEqual("18,,18.2", data[0]);
            Assert.AreEqual("17,,17.2", data[1]);
            Assert.AreEqual("16,,16.2", data[2]);
            Assert.AreEqual("14,14.1,", data[3]);
            Assert.AreEqual("13,13.1,", data[4]);

            // Assert Index
            curve1 = result.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            Assert.AreEqual(14, curve1.MaxIndex.Value);
            curve2 = result.LogCurveInfo[2];
            Assert.IsNotNull(curve2);
            Assert.AreEqual(16, curve2.MinIndex.Value);

            Assert.AreEqual(18, result.StartIndex.Value);
            Assert.AreEqual(13, result.EndIndex.Value);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_Increasing_Channels_Data_With_Default_And_Specific_Index_Range()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            var logData = _log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("17,17.1,17.2");
            logData.Data.Add("18,18.1,18.2");

            _devKit.AddAndAssert(_log);

            var indexCurve = _log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == _log.IndexCurve);
            Assert.IsNotNull(indexCurve);
            var curve1 = _log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = _log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);

            var result = _devKit.GetOneAndAssert(_log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(13, curve.MinIndex.Value);
                Assert.AreEqual(18, curve.MaxIndex.Value);
            }

            var delete = "<startIndex uom=\"" + indexCurve.Unit + "\">15</startIndex>" + Environment.NewLine +
                "<logCurveInfo><mnemonic>" + curve1.Mnemonic.Value + "</mnemonic></logCurveInfo>" + Environment.NewLine +
                "<logCurveInfo uid=\"" + curve2.Uid + "\">" + Environment.NewLine +
                "<minIndex uom=\"" + indexCurve.Unit + "\">16</minIndex>" + Environment.NewLine +
                "</logCurveInfo>";
            DeleteLog(_log, delete);

            result = _devKit.GetOneAndAssert(_log);

            // Assert log data
            resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            var data = resultLogData.Data;
            Assert.AreEqual("13,13.1,13.2", data[0]);
            Assert.AreEqual("14,14.1,14.2", data[1]);
            Assert.AreEqual("15,,15.2", data[2]);

            // Assert Index
            curve1 = result.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            Assert.AreEqual(14, curve1.MaxIndex.Value);
            curve2 = result.LogCurveInfo[2];
            Assert.IsNotNull(curve2);
            Assert.AreEqual(15, curve2.MaxIndex.Value);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_Multiple_Curves_With_StartIndex_And_EndIndex()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            // Add another curve
            var newCurve = _devKit.LogGenerator.CreateDoubleLogCurveInfo("RPM", "c/s");
            _log.LogCurveInfo.Add(newCurve);

            var logData = _log.LogData.First();
            logData.MnemonicList += $",{newCurve.Mnemonic}";
            logData.UnitList += $",{newCurve.Unit}";
            logData.Data.Add("13,13.1,13.2,13.3");
            logData.Data.Add("14,14.1,14.2,14.3");
            logData.Data.Add("15,15.1,15.2,15.3");
            logData.Data.Add("16,16.1,16.2,16.3");
            logData.Data.Add("17,17.1,17.2,17.3");
            logData.Data.Add("18,18.1,18.2,18.3");

            _devKit.AddAndAssert(_log);

            var indexCurve = _log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == _log.IndexCurve);
            Assert.IsNotNull(indexCurve);
            var curve1 = _log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = _log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);
            var curve3 = _log.LogCurveInfo[3];
            Assert.IsNotNull(curve3);

            var result = _devKit.GetOneAndAssert(_log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(13, curve.MinIndex.Value);
                Assert.AreEqual(18, curve.MaxIndex.Value);
            }

            var delete = "<startIndex uom=\"" + indexCurve.Unit + "\">0</startIndex>" + Environment.NewLine +
                         "<endIndex uom=\"" + indexCurve.Unit + "\">20</endIndex>" + Environment.NewLine +
                         "<logCurveInfo><mnemonic>" + curve1.Mnemonic.Value + "</mnemonic></logCurveInfo>" + Environment.NewLine +
                         "<logCurveInfo><mnemonic>" + curve2.Mnemonic.Value + "</mnemonic></logCurveInfo>";
            DeleteLog(_log, delete);

            result = _devKit.GetOneAndAssert(_log);

            // Assert log data
            Assert.IsNotNull(result.LogData);
            resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            var data = resultLogData.Data;
            Assert.AreEqual("13,13.3", data[0]);
            Assert.AreEqual("14,14.3", data[1]);
            Assert.AreEqual("15,15.3", data[2]);
            Assert.AreEqual("16,16.3", data[3]);
            Assert.AreEqual("17,17.3", data[4]);
            Assert.AreEqual("18,18.3", data[5]);

            // Assert Index
            curve1 = result.LogCurveInfo[0];
            Assert.IsNotNull(curve1);
            Assert.AreEqual(13, curve1.MinIndex.Value);
            Assert.AreEqual(18, curve1.MaxIndex.Value);
            curve2 = result.LogCurveInfo[1];
            Assert.IsNotNull(curve2);
            Assert.AreEqual(13, curve2.MinIndex.Value);
            Assert.AreEqual(18, curve2.MaxIndex.Value);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without plural container")]
        public void Log141DataAdapter_DeleteFromStore_Error_401_No_Plural_Root_Element()
        {
            var nonPluralLog = "<log xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "<log uid=\"{0}\" uidWell=\"{1}\" uidWellbore=\"{2}\" />" + Environment.NewLine +
                           "</log>";

            var queryIn = string.Format(nonPluralLog, _log.Uid, _log.UidWell, _log.UidWellbore);
            var response = _devKit.DeleteFromStore(ObjectTypes.Log, queryIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore while missing the object type")]
        public void Log141DataAdapter_DeleteFromStore_Error_407_Missing_Witsml_Object_Type()
        {
            var response = _devKit.Delete<LogList, Log>(_log, string.Empty);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with empty queryIn")]
        public void Log141DataAdapter_DeleteFromStore_Error_408_Empty_QueryIn()
        {
            var response = _devKit.DeleteFromStore(ObjectTypes.Log, string.Empty, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with invalid xml")]
        public void Log141DataAdapter_DeleteFromStore_Error_409_QueryIn_Must_Conform_To_Schema()
        {
            AddParents();

            // Delete log with invalid element
            const string delete = "<dataDelimiter /><dataDelimiter />";
            DeleteLog(_log, delete, ErrorCodes.InputTemplateNonConforming);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without specifying the log uid")]
        public void Log141DataAdapter_DeleteFromStore_Error_415_Delete_Without_Specifing_UID()
        {
            AddParents();

            _log.Uid = string.Empty;
            DeleteLog(_log, string.Empty, ErrorCodes.DataObjectUidMissing);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore on a recurring element without specifying uid")]
        public void Log141DataAdapter_DeleteFromStore_Error_416_Empty_UID()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            var ext1 = _devKit.ExtensionNameValue("Ext-1", "1.0", "m");
            _log.CommonData = new CommonData
            {
                ExtensionNameValue = new List<ExtensionNameValue>
                {
                    ext1
                }
            };

            _devKit.AddAndAssert(_log);

            // Delete Log
            const string delete = "<commonData><extensionNameValue uid=\"\" /></commonData>";
            DeleteLog(_log, delete, ErrorCodes.EmptyUidSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with a missing uom")]
        public void Log141DataAdapter_DeleteFromStore_Error_417_Deleting_With_Empty_UOM_Attribute()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _log.StepIncrement = new RatioGenericMeasure {Uom = "m", Value = 1.0};

            _devKit.AddAndAssert(_log);

            // Delete log
            const string delete = "<stepIncrement uom=\"\" />";
            DeleteLog(_log, delete, ErrorCodes.EmptyUomSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore on a recurring element without uid attribute")]
        public void Log141DataAdapter_DeleteFromStore_Error_418_Missing_Uid()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            var ext1 = _devKit.ExtensionNameValue("Ext-1", "1.0", "m");
            _log.CommonData = new CommonData
            {
                ExtensionNameValue = new List<ExtensionNameValue>
                {
                    ext1
                }
            };

            _devKit.AddAndAssert(_log);

            // Delete log
            const string delete = "<commonData><extensionNameValue /></commonData>";
            DeleteLog(_log, delete, ErrorCodes.MissingElementUidForDelete);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore on a non recurring empty container element")]
        public void Log141DataAdapter_DeleteFromStore_Error_419_Deleting_Empty_NonRecurring_Container_Element()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.AddAndAssert(_log);

            // Delete log
            const string delete = "<commonData />";
            DeleteLog(_log, delete, ErrorCodes.EmptyNonRecurringElementSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with empty logData element")]
        public void Log141DataAdapter_DeleteFromStore_Error_419_Deleting_With_Empty_LogData_Element()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);
            _devKit.AddAndAssert(_log);

            // Delete log
            const string delete = "<logData />";
            DeleteLog(_log, delete, ErrorCodes.EmptyNonRecurringElementSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with an empty node for a non-recurring element or attribute that is mandatory in the write schema.")]
        public void Log141DataAdapter_DeleteFromStore_Error_420_Delete_Required_Element()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.AddAndAssert(_log);

            // Delete log
            const string delete = "<name />";
            DeleteLog(_log, delete, ErrorCodes.EmptyMandatoryNodeSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore on a log that does not exist")]
        public void Log141DataAdapter_DeleteFromStore_Error_433_Log_Does_Not_Exist()
        {
            AddParents();

            // Delete log
            DeleteLog(_log, string.Empty, ErrorCodes.DataObjectNotExist);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore when specifying a mnemonicList in logData element")]
        public void Log141DataAdapter_DeleteFromStore_Error_437_Specifying_MnemonicList_In_LogData_Element()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);
            _devKit.AddAndAssert(_log);

            // Delete log
            const string delete = "<logData><mnemonicList /></logData>";
            DeleteLog(_log, delete, ErrorCodes.ColumnIdentifierSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore more than one object")]
        public void Log141DataAdapter_DeleteFromStore_Error_444_Deleting_More_Than_One_Data_Object()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.AddAndAssert(_log);

            var log2 = _devKit.CreateLog(_devKit.Uid(), "log 2", _well.Uid, _well.Name, _wellbore.Uid, _wellbore.Name);
            _devKit.InitHeader(log2, LogIndexType.measureddepth);
            _devKit.AddAndAssert(log2);

            var delete = "<logs xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                          "   <log uid=\"{0}\" uidWell=\"{1}\" uidWellbore=\"{2}\" />" + Environment.NewLine +
                          "   <log uid=\"{3}\" uidWell=\"{4}\" uidWellbore=\"{5}\" />" + Environment.NewLine +
                          "</logs>";
            var queryIn = string.Format(delete, _log.Uid, _log.UidWell, _log.UidWellbore, log2.Uid, log2.UidWell,
                log2.UidWellbore);

            var results = _devKit.DeleteFromStore(ObjectTypes.Log, queryIn, null, null);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.InputTemplateMultipleDataObjects, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore of index curve unless all curve are being deleted")]
        public void Log141DataAdapter_DeleteFromStore_Error_1052_Deleting_Index_Curve()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);
            _devKit.AddAndAssert(_log);

            // Delete log
            var indexCurve = _log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == _log.IndexCurve);
            var delete = "<logCurveInfo uid=\"" + indexCurve.Uid + "\" />";
            DeleteLog(_log, delete, ErrorCodes.ErrorDeletingIndexCurve);
        }

        #region Helper Methods

        private void AddParents()
        {
            _devKit.AddAndAssert(_well);
            _devKit.AddAndAssert(_wellbore);
        }

        private void DeleteLog(Log log, string delete, ErrorCodes error = ErrorCodes.Success)
        {
            var queryIn = string.Format(DevKit141Aspect.BasicDeleteLogXmlTemplate, log.Uid, log.UidWell, log.UidWellbore, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Log, queryIn, null, null);
            Assert.AreEqual((short)error, response.Result);
        }

        #endregion
    }
}
