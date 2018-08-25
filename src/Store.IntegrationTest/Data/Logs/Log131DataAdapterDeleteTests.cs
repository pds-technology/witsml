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
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    /// <summary>
    /// Log131DataAdapter Delete tests.
    /// </summary>
    [TestClass]
    public partial class Log131DataAdapterDeleteTests : Log131TestBase
    {
        [TestMethod]
        public void Log131DataAdapter_DeleteFromStore_Can_Partial_Delete_Elements()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            Log.ServiceCompany = "company 1";
            Log.StepIncrement = new RatioGenericMeasure { Uom = "m", Value = 1.0 };

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
        public void Log131DataAdapter_DeleteFromStore_Can_Delete_AllLog_Channels_And_Data()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogData = new List<string> { "13,13.1,13.2", "14,14.1,14.2", "15,15.1,15.2" };

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic == Log.IndexCurve.Value);
            Assert.IsNotNull(indexCurve);

            var result = DevKit.GetAndAssert(Log);
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
        public void Log131DataAdapter_DeleteFromStore_Can_Delete_AllLog_Data_By_Mnemonics_Only()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogData = new List<string> { "13,13.1,13.2", "14,14.1,14.2", "15,15.1,15.2" };

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic == Log.IndexCurve.Value);
            Assert.IsNotNull(indexCurve);

            var result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(Log.LogCurveInfo.Count, result.LogCurveInfo.Count);

            var delete = string.Empty;
            foreach (var curve in Log.LogCurveInfo)
            {
                delete += "<logCurveInfo><mnemonic>" + curve.Mnemonic + "</mnemonic></logCurveInfo>";
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
        public void Log131DataAdapter_DeleteFromStore_Can_Delete_Full_IncreasingLog_Data_By_Index()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogData = new List<string> { "13,13.1,13.2", "14,14.1,14.2", "15,15.1,15.2" };

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic == Log.IndexCurve.Value);
            Assert.IsNotNull(indexCurve);

            var result = DevKit.GetAndAssert(Log);
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
        public void Log131DataAdapter_DeleteFromStore_Can_Delete_AllLog_Data_By_Index_Curve_With_Range()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogData = new List<string> {"13,13.1,13.2", "14,14.1,14.2", "15,15.1,15.2"};

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic == Log.IndexCurve.Value);
            Assert.IsNotNull(indexCurve);

            var result = DevKit.GetAndAssert(Log);
            Assert.AreEqual(Log.LogCurveInfo.Count, result.LogCurveInfo.Count);

            var delete = "<logCurveInfo>" + Environment.NewLine +
                "<mnemonic>" + indexCurve.Mnemonic + "</mnemonic>" + Environment.NewLine +
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
        public void Log131DataAdapter_DeleteFromStore_Can_Delete_Full_Increasing_Channel_Data_By_Index()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogData = new List<string> { "13,13.1,13.2", "14,14.1,14.2", "15,15.1,15.2" };

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic == Log.IndexCurve.Value);
            Assert.IsNotNull(indexCurve);
            var lastCurve = Log.LogCurveInfo.Last();
            Assert.IsNotNull(lastCurve);

            var result = DevKit.GetAndAssert(Log);
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
            Assert.IsNotNull(result.LogData);

            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(13, curve.MinIndex.Value);
                Assert.AreEqual(15, curve.MaxIndex.Value);
            }
        }

        [TestMethod]
        public void Log131DataAdapter_DeleteFromStore_Can_Delete_Increasing_Channels_Data_With_Different_Index_Range()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);           
            Log.LogData = new List<string>
            {
                "13,13.1,13.2",
                "14,14.1,14.2",
                "15,15.1,15.2",
                "16,16.1,16.2",
                "17,17.1,17.2",
                "18,18.1,18.2"
            }; 

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic == Log.IndexCurve.Value);
            Assert.IsNotNull(indexCurve);
            var curve1 = Log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = Log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);

            var result = DevKit.GetAndAssert(Log);
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
            var logData = result.LogData;
            Assert.IsNotNull(logData);
            Assert.AreEqual("13,13.1,", logData[0]);
            Assert.AreEqual("14,14.1,", logData[1]);
            Assert.AreEqual("16,,16.2", logData[2]);
            Assert.AreEqual("17,,17.2", logData[3]);
            Assert.AreEqual("18,,18.2", logData[4]);

            // Assert Index
            curve1 = result.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            Assert.AreEqual(14, curve1.MaxIndex.Value);
            curve2 = result.LogCurveInfo[2];
            Assert.IsNotNull(curve2);
            Assert.AreEqual(16, curve2.MinIndex.Value);
        }

        [TestMethod]
        public void Log131DataAdapter_DeleteFromStore_Can_Delete_Decreasing_Channels_Data_With_Different_Index_Range()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth, false);           
            Log.LogData = new List<string>
            {
                "18,18.1,18.2",
                "17,17.1,17.2",
                "16,16.1,16.2",
                "15,15.1,15.2",
                "14,14.1,14.2",
                "13,13.1,13.2"
            };

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic == Log.IndexCurve.Value);
            Assert.IsNotNull(indexCurve);
            var curve1 = Log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = Log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);

            var result = DevKit.GetAndAssert(Log);
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
            var logData = result.LogData;
            Assert.IsNotNull(logData);
            Assert.AreEqual("18,,18.2", logData[0]);
            Assert.AreEqual("17,,17.2", logData[1]);
            Assert.AreEqual("16,,16.2", logData[2]);
            Assert.AreEqual("14,14.1,", logData[3]);
            Assert.AreEqual("13,13.1,", logData[4]);

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
        public void Log131DataAdapter_DeleteFromStore_Can_Delete_Increasing_Channels_Data_With_Default_And_Specific_Index_Range()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogData = new List<string>
            {
                "13,13.1,13.2",
                "14,14.1,14.2",
                "15,15.1,15.2",
                "16,16.1,16.2",
                "17,17.1,17.2",
                "18,18.1,18.2"
            };

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic == Log.IndexCurve.Value);
            Assert.IsNotNull(indexCurve);
            var curve1 = Log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = Log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);

            var result = DevKit.GetAndAssert(Log);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(13, curve.MinIndex.Value);
                Assert.AreEqual(18, curve.MaxIndex.Value);
            }

            var delete = "<startIndex uom=\"" + indexCurve.Unit + "\">15</startIndex>" + Environment.NewLine +
                "<logCurveInfo><mnemonic>" + curve1.Mnemonic + "</mnemonic></logCurveInfo>" + Environment.NewLine +
                "<logCurveInfo uid=\"" + curve2.Uid + "\">" + Environment.NewLine +
                "<minIndex uom=\"" + indexCurve.Unit + "\">16</minIndex>" + Environment.NewLine +
                "</logCurveInfo>";
            DeleteLog(Log, delete);

            result = DevKit.GetAndAssert(Log);

            // Assert log data
            var logData = result.LogData;
            Assert.IsNotNull(logData);
            Assert.AreEqual("13,13.1,13.2", logData[0]);
            Assert.AreEqual("14,14.1,14.2", logData[1]);
            Assert.AreEqual("15,,15.2", logData[2]);

            // Assert Index
            curve1 = result.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            Assert.AreEqual(14, curve1.MaxIndex.Value);
            curve2 = result.LogCurveInfo[2];
            Assert.IsNotNull(curve2);
            Assert.AreEqual(15, curve2.MaxIndex.Value);
        }

        [TestMethod]
        public void Log131DataAdapter_DeleteFromStore_Can_Delete_Multiple_Curves_With_StartIndex_And_EndIndex()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            // Add another curve
            var newCurve = DevKit.LogGenerator.CreateDoubleLogCurveInfo("RPM", "c/s", 0);
            newCurve.ColumnIndex = 4;
            Log.LogCurveInfo.Add(newCurve);

            Log.LogData = new List<string>
            {
                "13,13.1,13.2,13.3",
                "14,14.1,14.2,14.3",
                "15,15.1,15.2,15.3",
                "16,16.1,16.2,16.3",
                "17,17.1,17.2,17.3",
                "18,18.1,18.2,18.3"
            };

            DevKit.AddAndAssert(Log);

            var indexCurve = Log.LogCurveInfo.FirstOrDefault(l => l.Mnemonic == Log.IndexCurve.Value);
            Assert.IsNotNull(indexCurve);
            var curve1 = Log.LogCurveInfo[1];
            Assert.IsNotNull(curve1);
            var curve2 = Log.LogCurveInfo[2];
            Assert.IsNotNull(curve2);
            var curve3 = Log.LogCurveInfo[3];
            Assert.IsNotNull(curve3);

            var result = DevKit.GetAndAssert(Log);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.AreEqual(13, curve.MinIndex.Value);
                Assert.AreEqual(18, curve.MaxIndex.Value);
            }

            var delete = "<startIndex uom=\"" + indexCurve.Unit + "\">0</startIndex>" + Environment.NewLine +
                         "<endIndex uom=\"" + indexCurve.Unit + "\">20</endIndex>" + Environment.NewLine +
                         "<logCurveInfo><mnemonic>" + curve1.Mnemonic + "</mnemonic></logCurveInfo>" + Environment.NewLine +
                         "<logCurveInfo><mnemonic>" + curve2.Mnemonic + "</mnemonic></logCurveInfo>";
            DeleteLog(Log, delete);

            result = DevKit.GetAndAssert(Log);

            // Assert log data
            var logData = result.LogData;
            Assert.IsNotNull(logData);
            Assert.AreEqual("13,13.3", logData[0]);
            Assert.AreEqual("14,14.3", logData[1]);
            Assert.AreEqual("15,15.3", logData[2]);
            Assert.AreEqual("16,16.3", logData[3]);
            Assert.AreEqual("17,17.3", logData[4]);
            Assert.AreEqual("18,18.3", logData[5]);

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
        public void Log131DataAdapter_DeleteFromStore_Error_401_No_Plural_Root_Element()
        {
            var nonPluralLog = "<log xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\">" + Environment.NewLine +
                           "<log uid=\"{0}\" uidWell=\"{1}\" uidWellbore=\"{2}\" />" + Environment.NewLine +
                           "</log>";

            var queryIn = string.Format(nonPluralLog, Log.Uid, Log.UidWell, Log.UidWellbore);
            var response = DevKit.DeleteFromStore(ObjectTypes.Log, queryIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore while missing the object type")]
        public void Log131DataAdapter_DeleteFromStore_Error_407_Missing_Witsml_Object_Type()
        {
            var response = DevKit.Delete<LogList, Log>(Log, string.Empty);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with empty queryIn")]
        public void Log131DataAdapter_DeleteFromStore_Error_408_Empty_QueryIn()
        {
            var response = DevKit.DeleteFromStore(ObjectTypes.Log, string.Empty, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with invalid xml")]
        public void Log131DataAdapter_DeleteFromStore_Error_409_QueryIn_Must_Conform_To_Schema()
        {
            AddParents();

            // Delete log with invalid element
            const string delete = "<serviceCompany /><serviceCompany />";
            DeleteLog(Log, delete, ErrorCodes.InputTemplateNonConforming);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without specifying the log uid")]
        public void Log131DataAdapter_DeleteFromStore_Error_415_Delete_Without_Specifing_UID()
        {
            AddParents();

            Log.Uid = string.Empty;
            DeleteLog(Log, string.Empty, ErrorCodes.DataObjectUidMissing);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with a missing uom")]
        public void Log131DataAdapter_DeleteFromStore_Error_417_Deleting_With_Empty_UOM_Attribute()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.StepIncrement = new RatioGenericMeasure { Uom = "m", Value = 1.0 };

            DevKit.AddAndAssert(Log);

            // Delete log
            const string delete = "<stepIncrement uom=\"\" />";
            DeleteLog(Log, delete, ErrorCodes.EmptyUomSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with empty logData element")]
        public void Log131DataAdapter_DeleteFromStore_Error_419_Deleting_With_EmptyLogData_Element()
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
        public void Log131DataAdapter_DeleteFromStore_Error_420_Delete_Required_Element()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.AddAndAssert(Log);

            // Delete log
            const string delete = "<name />";
            DeleteLog(Log, delete, ErrorCodes.EmptyMandatoryNodeSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore on a log that does not exist")]
        public void Log131DataAdapter_DeleteFromStore_Error_433Log_Does_Not_Exist()
        {
            AddParents();

            // Delete log
            DeleteLog(Log, string.Empty, ErrorCodes.DataObjectNotExist);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore more than one object")]
        public void Log131DataAdapter_DeleteFromStore_Error_444_Deleting_More_Than_One_Data_Object()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.AddAndAssert(Log);

            var log2 = DevKit.CreateLog(DevKit.Uid(), "log 2", Well.Uid, Well.Name, Wellbore.Uid, Wellbore.Name);
            DevKit.InitHeader(log2, LogIndexType.measureddepth);
            DevKit.AddAndAssert(log2);

            var delete = "<logs xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\">" + Environment.NewLine +
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
        public void Log131DataAdapter_DeleteFromStore_Error_1052_Deleting_Index_Curve()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);
            DevKit.AddAndAssert(Log);

            // Delete log
            var delete = $"<logCurveInfo><mnemonic>{Log.IndexCurve.Value}</mnemonic></logCurveInfo>";
            DeleteLog(Log, delete, ErrorCodes.ErrorDeletingIndexCurve);
        }

        [TestMethod]
        public void Log131DataAdapter_DeleteFromStore_Delete_Updates_DataRowCount()
        {
            DeleteAndAssertDataRowCount(10, 5, 9);
        }

        [TestMethod]
        public void Log131DataAdapter_DeleteFromStore_Delete_No_Rows_Does_Not_Change_DataRowCount()
        {
            DeleteAndAssertDataRowCount(10, 100, 10);
        }

        [TestMethod, Description("Tests that a time log can be queried after removing all data and all DateTimeIndexSpecified flags are false")]
        public void Log131DataAdapter_DeleteFromStore_Can_Query_TimeLog_After_Deleting_All_Data()
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

        private void DeleteAndAssertDataRowCount(int rowsToAdd, int deleteIndex, int expectedDataRowCount)
        {
            AddParents();

            // Add a Log with dataRowCount Rows
            DevKit.AddLogWithData(Log, LogIndexType.measureddepth, rowsToAdd, false);

            // Assert that the dataRowCount is equivalent with the AddToStore
            DevKit.GetAndAssertDataRowCount(DevKit.CreateLog(Log), rowsToAdd);

            // Create a deleteLog that deletes one row
            var deleteLog = DevKit.CreateLog(Log);
            deleteLog.StartIndex = new GenericMeasure(deleteIndex, Log.LogCurveInfo[0].Unit);
            deleteLog.EndIndex = new GenericMeasure(deleteIndex, Log.LogCurveInfo[0].Unit);

            // Update the Log with a new Row
            DevKit.DeleteAndAssert(deleteLog, partialDelete: true);

            DevKit.GetAndAssertDataRowCount(DevKit.CreateLog(Log), expectedDataRowCount);
        }

        #endregion
    }
}
