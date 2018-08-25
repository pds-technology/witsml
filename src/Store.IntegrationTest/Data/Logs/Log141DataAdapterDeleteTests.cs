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

using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    /// <summary>
    /// Log141DataAdapter Delete tests.
    /// </summary>
    [TestClass]
    public partial class Log141DataAdapterDeleteTests : Log141TestBase
    {
        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_DeleteLog_With_No_Data()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            DevKit.AddAndAssert(Log);

            // Query log
            DevKit.GetAndAssert(Log);

            // Delete log
            DeleteLog(Log, string.Empty);

            // Assert log is deleted
            var query = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_DeleteLog_With_Case_Insensitive_Uid()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var uid = DevKit.Uid();
            Log.Uid = "l" + uid;
            DevKit.AddAndAssert(Log);

            // Query log
            DevKit.GetAndAssert(Log);

            // Delete log
            var delete = DevKit.CreateLog("L" + uid, null, Well.Uid, null, Wellbore.Uid, null);
            DeleteLog(delete, string.Empty);

            // Assert log is deleted
            var query = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Partial_Delete_Elements()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            Log.ServiceCompany = "company 1";
            Log.StepIncrement = new RatioGenericMeasure {Uom = "m", Value = 1.0};

            // Add log
            DevKit.AddAndAssert(Log);

            // Assert all testing elements are added
            var result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(Log.ServiceCompany, result.ServiceCompany);
            Assert.AreEqual(Log.Direction, result.Direction);

            // Partial delete well
            const string delete = "<serviceCompany /><stepIncrement />";
            DeleteLog(Log, delete);

            // Assert the well elements has been deleted
            result = DevKit.GetAndAssert(Log);
            Assert.IsNull(result.ServiceCompany);
            Assert.IsNull(result.StepIncrement);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Partial_Delete_Nested_Elements()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var testCommonData = new CommonData
            {
                Comments = "Testing partial delete nested elements",
                ItemState = ItemState.plan
            };

            Log.CommonData = testCommonData;

            // Add log
            DevKit.AddAndAssert(Log);

            // Assert all testing elements are added
            var result = DevKit.GetAndAssert(Log);
            var commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            Assert.AreEqual(testCommonData.Comments, commonData.Comments);
            Assert.AreEqual(testCommonData.ItemState, commonData.ItemState);

            // Partial delete well
            const string delete = "<commonData><comments /><itemState /></commonData>";
            DeleteLog(Log, delete);

            // Assert the well elements has been deleted
            result = DevKit.GetAndAssert(Log);
            commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            Assert.IsNull(commonData.Comments);
            Assert.IsNull(commonData.ItemState);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Partial_Delete_Recurring_Elements()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var curve1 = Log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = Log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);
            curve2.CurveDescription = "Testing partial delete recurring elements";

            // Add log
            DevKit.AddAndAssert(Log);

            // Assert all testing elements are added
            var result = DevKit.GetAndAssert(Log);
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
            DeleteLog(Log, delete);

            // Assert the well elements has been deleted
            result = DevKit.GetAndAssert(Log);
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

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var param = new IndexedObject {Uid = "1", Uom = "m", Value = "10"};
            Log.LogParam = new List<IndexedObject> {param};

            // Add log
            DevKit.AddAndAssert(Log);

            // Assert all testing elements are added
            var result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, result.LogParam.Count);

            // Partial delete well
            var delete = "<logParam uid=\"" + param.Uid + "\" />";
            DeleteLog(Log, delete);

            // Assert the well elements has been deleted
            result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(0, result.LogParam.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Partial_Delete_Nested_Recurring_Elements()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var curve1 = Log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = Log.LogCurveInfo[2];
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
            DevKit.AddAndAssert(Log);

            // Assert all testing elements are added
            var result = DevKit.GetAndAssert(Log);

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
            DeleteLog(Log, delete);

            // Assert the log elements has been deleted
            result = DevKit.GetAndAssert(Log);

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
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_AllLog_Channels_And_Data()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var logData = Log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);

            var result = DevKit.GetAndAssert(Log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            Assert.AreEqual(Log.LogCurveInfo.Count, result.LogCurveInfo.Count);

            var delete = string.Empty;
            foreach (var curve in Log.LogCurveInfo)
            {
                delete += "<logCurveInfo uid=\"" + curve.Uid + "\" />";
            }
            DeleteLog(Log, delete);

            result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(0, result.LogCurveInfo.Count);
            Assert.AreEqual(0, result.LogData.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_AllLog_Data_By_Mnemonics_Only()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var logData = Log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);

            var result = DevKit.GetAndAssert(Log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            Assert.AreEqual(Log.LogCurveInfo.Count, result.LogCurveInfo.Count);

            var delete = string.Empty;
            foreach (var curve in Log.LogCurveInfo)
            {
                delete += "<logCurveInfo><mnemonic>" + curve.Mnemonic.Value + "</mnemonic></logCurveInfo>";
            }
            DeleteLog(Log, delete);

            result = DevKit.GetAndAssert(Log);          
            Assert.AreEqual(0, result.LogData.Count);

            Assert.AreEqual(Log.LogCurveInfo.Count, result.LogCurveInfo.Count);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.IsNull(curve.MinIndex);
                Assert.IsNull(curve.MaxIndex);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_AllLog_Data_By_Index_Curve_With_Range()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var logData = Log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);

            var result = DevKit.GetAndAssert(Log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            Assert.AreEqual(Log.LogCurveInfo.Count, result.LogCurveInfo.Count);

            var delete = "<logCurveInfo>" + Environment.NewLine +
                "<mnemonic>" + indexCurve.Mnemonic.Value + "</mnemonic>" + Environment.NewLine +
                "<minIndex uom=\"" + indexCurve.Unit + "\">10</minIndex>" + Environment.NewLine +
                "</logCurveInfo>";
            
            DeleteLog(Log, delete);

            result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(0, result.LogData.Count);

            Assert.AreEqual(Log.LogCurveInfo.Count, result.LogCurveInfo.Count);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.IsNull(curve.MinIndex);
                Assert.IsNull(curve.MaxIndex);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_Full_IncreasingLog_Data_By_Index()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var logData = Log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);

            var result = DevKit.GetAndAssert(Log);
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(13, curve.MinIndex.Value);
                Assert.AreEqual(15, curve.MaxIndex.Value);
            }

            var delete = "<endIndex uom=\"" + indexCurve.Unit + "\">20</endIndex>";
            DeleteLog(Log, delete);

            result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(0, result.LogData.Count);

            foreach (var curve in result.LogCurveInfo)
            {
                Assert.IsNull(curve.MinIndex);
                Assert.IsNull(curve.MaxIndex);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_DeleteFromStore_Can_Delete_Full_DecreasingLog_Data_By_Index()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth, false);
            var logData = Log.LogData.First();         
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("13,13.1,13.2");

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);

            var result = DevKit.GetAndAssert(Log);
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
            DeleteLog(Log, delete);

            result = DevKit.GetAndAssert(Log);
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

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var logData = Log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);
            var lastCurve = Log.LogCurveInfo.Last();
            Assert.IsNotNull(lastCurve);

            var result = DevKit.GetAndAssert(Log);
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
            DeleteLog(Log, delete);

            result = DevKit.GetAndAssert(Log);
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

            DevKit.InitHeader(Log, LogIndexType.measureddepth, false);
            var logData = Log.LogData.First();
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("13,13.1,13.2");

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);
            var lastCurve = Log.LogCurveInfo.Last();
            Assert.IsNotNull(lastCurve);

            var result = DevKit.GetAndAssert(Log);
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
            DeleteLog(Log, delete);

            result = DevKit.GetAndAssert(Log);
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

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var logData = Log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("17,17.1,17.2");
            logData.Data.Add("18,18.1,18.2");

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);
            var curve1 = Log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = Log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);

            var result = DevKit.GetAndAssert(Log);
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
            DeleteLog(Log, delete);

            result = DevKit.GetAndAssert(Log);

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

            DevKit.InitHeader(Log, LogIndexType.measureddepth, false);
            var logData = Log.LogData.First();
            logData.Data.Add("18,18.1,18.2");
            logData.Data.Add("17,17.1,17.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("13,13.1,13.2");

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);
            var curve1 = Log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = Log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);

            var result = DevKit.GetAndAssert(Log);
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
            DeleteLog(Log, delete);

            result = DevKit.GetAndAssert(Log);

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

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var logData = Log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("17,17.1,17.2");
            logData.Data.Add("18,18.1,18.2");

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);
            var curve1 = Log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = Log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);

            var result = DevKit.GetAndAssert(Log);
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
            DeleteLog(Log, delete);

            result = DevKit.GetAndAssert(Log);

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

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            // Add another curve
            var newCurve = DevKit.LogGenerator.CreateDoubleLogCurveInfo("RPM", "c/s");
            Log.LogCurveInfo.Add(newCurve);

            var logData = Log.LogData.First();
            logData.MnemonicList += $",{newCurve.Mnemonic}";
            logData.UnitList += $",{newCurve.Unit}";
            logData.Data.Add("13,13.1,13.2,13.3");
            logData.Data.Add("14,14.1,14.2,14.3");
            logData.Data.Add("15,15.1,15.2,15.3");
            logData.Data.Add("16,16.1,16.2,16.3");
            logData.Data.Add("17,17.1,17.2,17.3");
            logData.Data.Add("18,18.1,18.2,18.3");

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);
            var curve1 = Log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = Log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);
            var curve3 = Log.LogCurveInfo[3];
            Assert.IsNotNull(curve3);

            var result = DevKit.GetAndAssert(Log);
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
            DeleteLog(Log, delete);

            result = DevKit.GetAndAssert(Log);

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

        [TestMethod]
        public void Log141DataAdapter_ChangeLog_DeleteFromStore_Can_Delete_Increasing_Channels_Data_With_Default_And_Specific_Index_Range()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var logData = Log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("17,17.1,17.2");
            logData.Data.Add("18,18.1,18.2");

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);
            var curve1 = Log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = Log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);

            var result = DevKit.GetAndAssert(Log);
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
            DeleteLog(Log, delete);

            result = DevKit.GetAndAssert(Log);

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

            // Assert ChangeLog
            var changeLog = DevKit.AssertChangeLog(result, 2, ChangeInfoType.update);
            var lastChange = changeLog.ChangeHistory.LastOrDefault();
            Assert.IsNotNull(lastChange);
            DevKit.AssertChangeHistoryFlags(lastChange, false, false);
            DevKit.AssertChangeHistoryIndexRange(lastChange, 15, 18);
            DevKit.AssertChangeLogMnemonics(DevKit.GetNonIndexMnemonics(Log), lastChange.Mnemonics);
        }

        [TestMethod]
        public void Log141DataAdapter_ChangeLog_DeleteFromStore_Partial_Delete_Open_EndIndex()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var logData = Log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("17,17.1,17.2");
            logData.Data.Add("18,18.1,18.2");

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);

            var delete = "<startIndex uom=\"" + indexCurve.Unit + "\">15</startIndex>";
            DeleteLog(Log, delete);

            var result = DevKit.GetAndAssert(Log);

            // Assert log data
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            var data = resultLogData.Data;
            Assert.AreEqual("13,13.1,13.2", data[0]);
            Assert.AreEqual("14,14.1,14.2", data[1]);

            // Assert Index
            result.LogCurveInfo.ForEach(x => {
                Assert.AreEqual(13, x.MinIndex.Value);
                Assert.AreEqual(14, x.MaxIndex.Value);
            });

            // Assert ChangeLog
            var changeLog = DevKit.AssertChangeLog(result, 2, ChangeInfoType.update);
            var lastChange = changeLog.ChangeHistory.LastOrDefault();
            Assert.IsNotNull(lastChange);
            DevKit.AssertChangeHistoryFlags(lastChange, false, false);
            DevKit.AssertChangeHistoryIndexRange(lastChange, 15, 18);
            DevKit.AssertChangeLogMnemonics(DevKit.GetNonIndexMnemonics(Log), lastChange.Mnemonics);
        }

        [TestMethod]
        public void Log141DataAdapter_ChangeLog_DeleteFromStore_Partial_Delete_Open_Start()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var logData = Log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("17,17.1,17.2");
            logData.Data.Add("18,18.1,18.2");

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);

            var delete = "<endIndex uom=\"" + indexCurve.Unit + "\">15</endIndex>";
            DeleteLog(Log, delete);

            var result = DevKit.GetAndAssert(Log);

            // Assert log data
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            var data = resultLogData.Data;
            Assert.AreEqual("16,16.1,16.2", data[0]);
            Assert.AreEqual("17,17.1,17.2", data[1]);
            Assert.AreEqual("18,18.1,18.2", data[2]);

            // Assert Index
            result.LogCurveInfo.ForEach(x => {
                Assert.AreEqual(16, x.MinIndex.Value);
                Assert.AreEqual(18, x.MaxIndex.Value);
            });

            // Assert ChangeLog
            var changeLog = DevKit.AssertChangeLog(result, 2, ChangeInfoType.update);
            var lastChange = changeLog.ChangeHistory.LastOrDefault();
            Assert.IsNotNull(lastChange);
            DevKit.AssertChangeHistoryFlags(lastChange, false, false);
            DevKit.AssertChangeHistoryIndexRange(lastChange, 13, 15);
            DevKit.AssertChangeLogMnemonics(DevKit.GetNonIndexMnemonics(Log), lastChange.Mnemonics);
        }

        [TestMethod]
        public void Log141DataAdapter_ChangeLog_DeleteFromStore_Can_Delete_Range_For_Single_Mnemonic()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var logData = Log.LogData.First();
            for (var i = 0; i < 6; i++)
            {
                logData.Data.Add($"1{i},1{i}.1,1{i}.2");
            }

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            Assert.IsNotNull(indexCurve);

            var delete = "<logCurveInfo><mnemonic>" + Log.LogCurveInfo[1].Mnemonic.Value + "</mnemonic>" + Environment.NewLine +
                "<minIndex uom=\"" + indexCurve.Unit + "\">12</minIndex>" + Environment.NewLine +
                "<maxIndex uom=\"" + indexCurve.Unit + "\">13</maxIndex>" + Environment.NewLine +
                "</logCurveInfo>";
            DeleteLog(Log, delete);

            var result = DevKit.GetAndAssert(Log);

            // Assert log data
            var resultLogData = result.LogData.First();
            Assert.IsNotNull(resultLogData);
            var data = resultLogData.Data;
            for (var i = 0; i < 6; i++)
            {
                if (i != 2 && i != 3)
                {
                    Assert.AreEqual($"1{i},1{i}.1,1{i}.2", data[i]);
                }
                else
                {
                    Assert.AreEqual($"1{i},,1{i}.2", data[i]);
                }
            }

            // Assert ChangeLog
            var changeLog = DevKit.AssertChangeLog(result, 2, ChangeInfoType.update);
            var lastChange = changeLog.ChangeHistory.LastOrDefault();
            Assert.IsNotNull(lastChange);
            DevKit.AssertChangeHistoryFlags(lastChange, false, false);
            DevKit.AssertChangeHistoryIndexRange(lastChange, 12, 13);
            DevKit.AssertChangeLogMnemonics(new[] { Log.LogCurveInfo[1].Mnemonic.Value }, lastChange.Mnemonics);
        }
        
        [TestMethod]
        public void Log141DataAdapter_ChangeLog_Track_Remove_Curve()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            DevKit.AddAndAssert(Log);
            DeleteLog(Log, "<logCurveInfo uid=\"ROP\" />");

            var logUpdated = DevKit.GetAndAssert(Log);
            Assert.AreEqual(2, logUpdated.LogCurveInfo.Count);

            // Fetch the changeLog for the entity just added
            var current = DevKit.GetAndAssert(Log);
            var changeLog = DevKit.AssertChangeLog(current, 2, ChangeInfoType.update);
            var lastChange = changeLog.ChangeHistory.LastOrDefault();
            Assert.IsNotNull(lastChange);

            DevKit.AssertChangeHistoryFlags(lastChange, true, false);
            Assert.AreEqual("Mnemonics removed: ROP", lastChange.ChangeInfo);
            DevKit.AssertChangeLogMnemonics(new[] { "ROP" }, lastChange.Mnemonics);
        }

        [TestMethod]
        public void Log141DataAdapter_ChangeLog_Track_Remove_Curve_From_Growing_Log()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitData(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 1, 1, 1);

            DevKit.AddAndAssert(Log);

            // Append data to toggle object growing
            var updateLog = DevKit.CreateLog(Log.Uid, null, Log.UidWell, null, Log.UidWellbore, null);
            updateLog.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });
            updateLog.LogData[0].MnemonicList = Log.LogData.First().MnemonicList;
            updateLog.LogData[0].UnitList = Log.LogData.First().UnitList;
            var logData = updateLog.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);
            logData = updateLog.LogData.First();
            logData.Data.Add("2,2,2");

            DevKit.UpdateAndAssert(updateLog);

            // Assert log is growing
            var log = DevKit.GetAndAssert(Log);
            Assert.IsTrue(log.ObjectGrowing.GetValueOrDefault(), "Log ObjectGrowing");

            DeleteLog(Log, "<logCurveInfo uid=\"ROP\" />");

            var logUpdated = DevKit.GetAndAssert(Log);
            Assert.AreEqual(2, logUpdated.LogCurveInfo.Count);

            // Fetch the changeLog for the entity just added
            var current = DevKit.GetAndAssert(Log);
            var changeLog = DevKit.AssertChangeLog(current, 3, ChangeInfoType.update);
            var lastChange = changeLog.ChangeHistory.LastOrDefault();
            Assert.IsNotNull(lastChange);

            DevKit.AssertChangeHistoryFlags(lastChange, false, true);
            Assert.AreEqual("Mnemonics removed: ROP", lastChange.ChangeInfo);
            DevKit.AssertChangeLogMnemonics(new[] { "ROP" }, lastChange.Mnemonics);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without plural container")]
        public void Log141DataAdapter_DeleteFromStore_Error_401_No_Plural_Root_Element()
        {
            var nonPluralLog = "<log xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "<log uid=\"{0}\" uidWell=\"{1}\" uidWellbore=\"{2}\" />" + Environment.NewLine +
                           "</log>";

            var queryIn = string.Format(nonPluralLog, Log.Uid, Log.UidWell, Log.UidWellbore);
            var response = DevKit.DeleteFromStore(ObjectTypes.Log, queryIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore while missing the object type")]
        public void Log141DataAdapter_DeleteFromStore_Error_407_Missing_Witsml_Object_Type()
        {
            var response = DevKit.Delete<LogList, Log>(Log, string.Empty);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with empty queryIn")]
        public void Log141DataAdapter_DeleteFromStore_Error_408_Empty_QueryIn()
        {
            var response = DevKit.DeleteFromStore(ObjectTypes.Log, string.Empty, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with invalid xml")]
        public void Log141DataAdapter_DeleteFromStore_Error_409_QueryIn_Must_Conform_To_Schema()
        {
            AddParents();

            // Delete log with invalid element
            const string delete = "<dataDelimiter /><dataDelimiter />";
            DeleteLog(Log, delete, ErrorCodes.InputTemplateNonConforming);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without specifying the log uid")]
        public void Log141DataAdapter_DeleteFromStore_Error_415_Delete_Without_Specifing_UID()
        {
            AddParents();

            Log.Uid = string.Empty;
            DeleteLog(Log, string.Empty, ErrorCodes.DataObjectUidMissing);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore on a recurring element without specifying uid")]
        public void Log141DataAdapter_DeleteFromStore_Error_416_Empty_UID()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var ext1 = DevKit.ExtensionNameValue("Ext-1", "1.0", "m");
            Log.CommonData = new CommonData
            {
                ExtensionNameValue = new List<ExtensionNameValue>
                {
                    ext1
                }
            };

            DevKit.AddAndAssert(Log);

            // Delete Log
            const string delete = "<commonData><extensionNameValue uid=\"\" /></commonData>";
            DeleteLog(Log, delete, ErrorCodes.EmptyUidSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with a missing uom")]
        public void Log141DataAdapter_DeleteFromStore_Error_417_Deleting_With_Empty_UOM_Attribute()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.StepIncrement = new RatioGenericMeasure {Uom = "m", Value = 1.0};

            DevKit.AddAndAssert(Log);

            // Delete log
            const string delete = "<stepIncrement uom=\"\" />";
            DeleteLog(Log, delete, ErrorCodes.EmptyUomSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore on a recurring element without uid attribute")]
        public void Log141DataAdapter_DeleteFromStore_Error_418_Missing_Uid()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            var ext1 = DevKit.ExtensionNameValue("Ext-1", "1.0", "m");
            Log.CommonData = new CommonData
            {
                ExtensionNameValue = new List<ExtensionNameValue>
                {
                    ext1
                }
            };

            DevKit.AddAndAssert(Log);

            // Delete log
            const string delete = "<commonData><extensionNameValue /></commonData>";
            DeleteLog(Log, delete, ErrorCodes.MissingElementUidForDelete);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore on a non recurring empty container element")]
        public void Log141DataAdapter_DeleteFromStore_Error_419_Deleting_Empty_NonRecurring_Container_Element()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.AddAndAssert(Log);

            // Delete log
            const string delete = "<commonData />";
            DeleteLog(Log, delete, ErrorCodes.EmptyNonRecurringElementSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with empty logData element")]
        public void Log141DataAdapter_DeleteFromStore_Error_419_Deleting_With_EmptyLogData_Element()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);
            DevKit.AddAndAssert(Log);

            // Delete log
            const string delete = "<logData />";
            DeleteLog(Log, delete, ErrorCodes.EmptyNonRecurringElementSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with an empty node for a non-recurring element or attribute that is mandatory in the write schema.")]
        public void Log141DataAdapter_DeleteFromStore_Error_420_Delete_Required_Element()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.AddAndAssert(Log);

            // Delete log
            const string delete = "<name />";
            DeleteLog(Log, delete, ErrorCodes.EmptyMandatoryNodeSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore on a log that does not exist")]
        public void Log141DataAdapter_DeleteFromStore_Error_433Log_Does_Not_Exist()
        {
            AddParents();

            // Delete log
            DeleteLog(Log, string.Empty, ErrorCodes.DataObjectNotExist);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore when specifying a mnemonicList in logData element")]
        public void Log141DataAdapter_DeleteFromStore_Error_437_Specifying_MnemonicList_InLogData_Element()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);
            DevKit.AddAndAssert(Log);

            // Delete log
            const string delete = "<logData><mnemonicList /></logData>";
            DeleteLog(Log, delete, ErrorCodes.ColumnIdentifierSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore more than one object")]
        public void Log141DataAdapter_DeleteFromStore_Error_444_Deleting_More_Than_One_Data_Object()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.AddAndAssert(Log);

            var log2 = DevKit.CreateLog(DevKit.Uid(), "log 2", Well.Uid, Well.Name, Wellbore.Uid, Wellbore.Name);
            DevKit.InitHeader(log2, LogIndexType.measureddepth);
            DevKit.AddAndAssert(log2);

            var delete = "<logs xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                          "   <log uid=\"{0}\" uidWell=\"{1}\" uidWellbore=\"{2}\" />" + Environment.NewLine +
                          "   <log uid=\"{3}\" uidWell=\"{4}\" uidWellbore=\"{5}\" />" + Environment.NewLine +
                          "</logs>";
            var queryIn = string.Format(delete, Log.Uid, Log.UidWell, Log.UidWellbore, log2.Uid, log2.UidWell,
                log2.UidWellbore);

            var results = DevKit.DeleteFromStore(ObjectTypes.Log, queryIn, null, null);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.InputTemplateMultipleDataObjects, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore of index curve unless all curve are being deleted")]
        public void Log141DataAdapter_DeleteFromStore_Error_1052_Deleting_Index_Curve()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);
            DevKit.AddAndAssert(Log);

            // Delete log
            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == Log.IndexCurve);
            var delete = "<logCurveInfo uid=\"" + indexCurve.Uid + "\" />";
            DeleteLog(Log, delete, ErrorCodes.ErrorDeletingIndexCurve);
        }

        [TestMethod, Description("Tests that a time log can be queried after removing all data and all DateTimeIndexSpecified flags are false")]
        public void Log141DataAdapter_DeleteFromStore_Can_Query_TimeLog_After_Deleting_All_Data()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.datetime);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10, 1, false);

            var response = DevKit.AddAndAssert(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert that we can retrieve the time log that was just added
            var queryLog = DevKit.CreateLog(Log);
            var logWithData = DevKit.GetAndAssert(queryLog);

            // Assert that all time indexes are specified
            DevKit.AssertTimeIndexSpecified(logWithData, true);

            var deleteLogData = DevKit.CreateLog(Log);
            deleteLogData.StartDateTimeIndex = logWithData.StartDateTimeIndex;
            deleteLogData.EndDateTimeIndex = logWithData.EndDateTimeIndex;

            // Delete the full index range of data.
            DevKit.DeleteAndAssert(deleteLogData, partialDelete: true);

            // Get and Assert that we are able to retrieve the time log after all of the data has been deleted
            var logWithoutData = DevKit.GetAndAssert(queryLog);

            // Assert that there is no data
            Assert.AreEqual(0, logWithoutData.LogData.Count);

            // Assert that all time indexes are no longer specified
            DevKit.AssertTimeIndexSpecified(logWithoutData, false);
        }

        #region Helper Methods

        private void DeleteLog(Log log, string delete, ErrorCodes error = ErrorCodes.Success)
        {
            var queryIn = string.Format(BasicXMLTemplate, log.UidWell, log.UidWellbore, log.Uid, delete);
            var response = DevKit.DeleteFromStore(ObjectTypes.Log, queryIn, null, null);
            Assert.AreEqual((short)error, response.Result);
        }

        #endregion
    }
}
