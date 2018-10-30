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
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Data.Logs;
using PDS.WITSMLstudio.Data.Trajectories;

namespace PDS.WITSMLstudio.Store
{
    public class DevKit131Aspect : DevKitAspect
    {
        private const MeasuredDepthUom MdUom = MeasuredDepthUom.m;
        private const WellVerticalCoordinateUom TvdUom = WellVerticalCoordinateUom.m;
        private const PlaneAngleUom AngleUom = PlaneAngleUom.dega;

        public DevKit131Aspect(TestContext context) : this(context, null)
        {
        }

        public DevKit131Aspect(TestContext context, string url = null) : base(url, WMLSVersion.WITSML131, context)
        {
            LogGenerator = new Log131Generator();
            TrajectoryGenerator = new Trajectory131Generator();
        }

        public Log131Generator LogGenerator { get; }

        public Trajectory131Generator TrajectoryGenerator { get; }

        public override string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version131.Value; }
        }

        public void InitHeader(Log log, LogIndexType indexType, bool increasing = true)
        {
            log.IndexType = indexType;
            log.IndexCurve = new IndexCurve(indexType == LogIndexType.datetime ? "TIME" : "MD")
            {
                ColumnIndex = 1
            };

            log.Direction = increasing ? LogIndexDirection.increasing : LogIndexDirection.decreasing;
            log.LogCurveInfo = List<LogCurveInfo>();

            log.LogCurveInfo.Add(
                new LogCurveInfo()
                {
                    Uid = log.IndexCurve.Value,
                    Mnemonic = log.IndexCurve.Value,
                    TypeLogData = indexType == LogIndexType.datetime ? LogDataType.datetime : LogDataType.@double,
                    Unit = indexType == LogIndexType.datetime ? "s" : "m",
                    ColumnIndex = 1
                });

            log.LogCurveInfo.Add(
                new LogCurveInfo()
                {
                    Uid = "ROP",
                    Mnemonic = "ROP",
                    TypeLogData = LogDataType.@double,
                    Unit = "m/h",
                    ColumnIndex = 2
                });

            log.LogCurveInfo.Add(
                new LogCurveInfo()
                {
                    Uid = "GR",
                    Mnemonic = "GR",
                    TypeLogData = LogDataType.@double,
                    Unit = "gAPI",
                    ColumnIndex = 3
                });

            InitData(log, Mnemonics(log), Units(log));
        }

        /// <summary>
        /// Creates the double log curve information.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public LogCurveInfo CreateDoubleLogCurveInfo(string name, string unit, short index)
        {
            return LogGenerator.CreateDoubleLogCurveInfo(name, unit, index);
        }

        public void InitData(Log log, string mnemonics, string units, params object[] values)
        {
            if (log.LogData == null)
            {
                log.LogData = List<string>();
            }

            if (values != null && values.Any())
            {
                log.LogData.Add(string.Join(",", values.Select(x => x ?? string.Empty)));
            }
        }

        public void InitDataMany(Log log, string mnemonics, string units, int numRows, double factor = 1.0, bool isDepthLog = true, bool hasEmptyChannel = true, bool increasing = true)
        {
            var depthStart = log.StartIndex?.Value ?? 0;
            var timeStart = DateTimeOffset.UtcNow.AddDays(-1);
            var interval = increasing ? 1 : -1;

            if (isDepthLog)
            {
                log.StartIndex = log.StartIndex ?? new GenericMeasure();
                log.StartIndex.Uom = "ft";
                log.EndIndex = log.EndIndex ?? new GenericMeasure();
                log.EndIndex.Uom = "ft";
            }

            for (int i = 0; i < numRows; i++)
            {
                if (isDepthLog)
                {
                    if (i == 0)
                    {
                        log.StartIndex.Value = depthStart;
                    }
                    else if (i == numRows - 1)
                    {
                        log.EndIndex.Value = depthStart + i;
                    }
                    InitData(log, mnemonics, units, depthStart + i * interval, hasEmptyChannel ? (int?)null : i, depthStart + i * factor);
                }
                else
                {
                    if (i == 0)
                    {
                        log.StartDateTimeIndex = timeStart;
                    }
                    else if (i == numRows - 1)
                    {
                        log.EndDateTimeIndex = timeStart.AddSeconds(i);
                    }
                    InitData(log, mnemonics, units, timeStart.AddSeconds(i).ToString("o"), hasEmptyChannel ? (int?)null : i, i * factor);
                }
            }
        }

        public LogList QueryLogByRange(Log log, double? startIndex, double? endIndex)
        {
            var query = Query<LogList>();
            query.Log = One<Log>(x => { x.Uid = log.Uid; x.UidWell = log.UidWell; x.UidWellbore = log.UidWellbore; });
            var queryLog = query.Log.First();

            if (startIndex.HasValue)
            {
                queryLog.StartIndex = new GenericMeasure() { Value = startIndex.Value };
            }

            if (endIndex.HasValue)
            {
                queryLog.EndIndex = new GenericMeasure() { Value = endIndex.Value };
            }

            var result = Proxy.Read(query, OptionsIn.ReturnElements.All);
            return result;
        }

        public string Units(Log log)
        {
            return log.LogCurveInfo != null
                ? String.Join(",", log.LogCurveInfo.Select(x => x.Unit ?? string.Empty))
                : string.Empty;
        }

        public string Mnemonics(Log log)
        {
            return log.LogCurveInfo != null
                ? String.Join(",", log.LogCurveInfo.Select(x => x.Mnemonic))
                : string.Empty;
        }

        public Log CreateLog(string uid, string name, string uidWell, string nameWell, string uidWellbore, string nameWellbore)
        {
            return new Log()
            {
                Uid = uid,
                Name = name,
                UidWell = uidWell,
                NameWell = nameWell,
                UidWellbore = uidWellbore,
                NameWellbore = nameWellbore,
            };
        }

        public Log CreateLog(Log log)
        {
            return CreateLog(log.Uid, log.Name, log.UidWell, log.NameWell, log.UidWellbore, log.NameWellbore);
        }

        public Trajectory CreateTrajectory(Trajectory trajectory)
        {
            return new Trajectory
            {
                Uid = trajectory.Uid,
                Name = trajectory.Name,
                UidWell = trajectory.UidWell,
                NameWell = trajectory.NameWell,
                UidWellbore = trajectory.UidWellbore,
                NameWellbore = trajectory.NameWellbore
            };
        }

        public void AddLogWithData(Log log, LogIndexType indexType, int numRows, bool hasEmptyChannel)
        {
            InitHeader(log, indexType);
            InitDataMany(log, Mnemonics(log), Units(log), numRows, hasEmptyChannel: hasEmptyChannel);

            var response = Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        public Log GetAndAssertDataRowCount(Log queryLog, int dataRowCount)
        {
            // Get the log
            var result = GetAndAssert<LogList, Log>(queryLog);

            // Assert that the DataRowCount is the same as the number of rows added.
            Assert.AreEqual(dataRowCount, result.DataRowCount);

            return result;
        }

        public Log GetAndAssertDataRowCountExpected(Log queryLog, int expectedRows)
        {
            // Get the log
            var result = GetAndAssert<LogList, Log>(queryLog, queryByExample: true);

            // Assert that the LogData.Count is the same as the number of rows added.
            Assert.AreEqual(expectedRows, result.LogData.Count);

            return result;
        }

        /// <summary>
        /// Creates the log template query.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="includeData">if set to <c>true</c> include data.</param>
        public XDocument CreateLogTemplateQuery(Log log = null, bool includeData = false)
        {
            var document = Template.Create<LogList>();

            Assert.IsNotNull(document);
            Assert.IsNotNull(document.Root);

            // If log is not null set the UIDs
            if (log != null)
                SetDocumentUids(log, document);

            // Remove log data
            if (!includeData)
                Template.Remove(document, "//logData");

            return document;
        }

        /// <summary>
        /// Gets the log with template.
        /// </summary>
        /// <param name="template">The template.</param>
        public Log GetLogWithTemplate(XDocument template)
        {
            var logList = GetWithTemplate<LogList>(template);
            var log = logList.Log.FirstOrDefault();
            Assert.IsNotNull(log);

            return log;
        }

        /// <summary>
        /// Gets the list of objects the with template.
        /// </summary>
        /// <typeparam name="T">The data object type.</typeparam>
        /// <param name="template">The template.</param>
        public T GetWithTemplate<T>(XDocument template)
        {
            var result = GetFromStore(ObjectTypes.Log, template.ToString(), null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);
            Assert.IsNotNull(result);

            var value = EnergisticsConverter.XmlToObject<T>(result.XMLout);
            Assert.IsNotNull(value);

            return value;
        }

        /// <summary>
        /// Does get query for single bhaRun object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="bhaRun">the bhaRun with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first bhaRun from the response</returns>
        public BhaRun GetAndAssert(BhaRun bhaRun, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<BhaRunList, BhaRun>(bhaRun, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds bhaRun object and test the return code
        /// </summary>
        /// <param name="bhaRun">the bhaRun</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(BhaRun bhaRun, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<BhaRunList, BhaRun>(bhaRun, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on bhaRun object and test the return code
        /// </summary>
        /// <param name="bhaRun">the bhaRun</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(BhaRun bhaRun, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<BhaRunList, BhaRun>(bhaRun, errorCode);
        }

        /// <summary>
        /// Deletes bhaRun object and test the return code
        /// </summary>
        /// <param name="bhaRun">the bhaRun</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(BhaRun bhaRun, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<BhaRunList, BhaRun>(bhaRun, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single cementJob object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="cementJob">the cementJob with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first cementJob from the response</returns>
        public CementJob GetAndAssert(CementJob cementJob, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<CementJobList, CementJob>(cementJob, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds cementJob object and test the return code
        /// </summary>
        /// <param name="cementJob">the cementJob</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(CementJob cementJob, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<CementJobList, CementJob>(cementJob, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on cementJob object and test the return code
        /// </summary>
        /// <param name="cementJob">the cementJob</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(CementJob cementJob, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<CementJobList, CementJob>(cementJob, errorCode);
        }

        /// <summary>
        /// Deletes cementJob object and test the return code
        /// </summary>
        /// <param name="cementJob">the cementJob</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(CementJob cementJob, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<CementJobList, CementJob>(cementJob, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single convCore object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="convCore">the convCore with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first convCore from the response</returns>
        public ConvCore GetAndAssert(ConvCore convCore, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<ConvCoreList, ConvCore>(convCore, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds convCore object and test the return code
        /// </summary>
        /// <param name="convCore">the convCore</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(ConvCore convCore, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<ConvCoreList, ConvCore>(convCore, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on convCore object and test the return code
        /// </summary>
        /// <param name="convCore">the convCore</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(ConvCore convCore, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<ConvCoreList, ConvCore>(convCore, errorCode);
        }

        /// <summary>
        /// Deletes convCore object and test the return code
        /// </summary>
        /// <param name="convCore">the convCore</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(ConvCore convCore, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<ConvCoreList, ConvCore>(convCore, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single dtsInstalledSystem object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="dtsInstalledSystem">the dtsInstalledSystem with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first dtsInstalledSystem from the response</returns>
        public DtsInstalledSystem GetAndAssert(DtsInstalledSystem dtsInstalledSystem, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<DtsInstalledSystemList, DtsInstalledSystem>(dtsInstalledSystem, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds dtsInstalledSystem object and test the return code
        /// </summary>
        /// <param name="dtsInstalledSystem">the dtsInstalledSystem</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(DtsInstalledSystem dtsInstalledSystem, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<DtsInstalledSystemList, DtsInstalledSystem>(dtsInstalledSystem, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on dtsInstalledSystem object and test the return code
        /// </summary>
        /// <param name="dtsInstalledSystem">the dtsInstalledSystem</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(DtsInstalledSystem dtsInstalledSystem, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<DtsInstalledSystemList, DtsInstalledSystem>(dtsInstalledSystem, errorCode);
        }

        /// <summary>
        /// Deletes dtsInstalledSystem object and test the return code
        /// </summary>
        /// <param name="dtsInstalledSystem">the dtsInstalledSystem</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(DtsInstalledSystem dtsInstalledSystem, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<DtsInstalledSystemList, DtsInstalledSystem>(dtsInstalledSystem, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single dtsMeasurement object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="dtsMeasurement">the dtsMeasurement with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first dtsMeasurement from the response</returns>
        public DtsMeasurement GetAndAssert(DtsMeasurement dtsMeasurement, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<DtsMeasurementList, DtsMeasurement>(dtsMeasurement, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds dtsMeasurement object and test the return code
        /// </summary>
        /// <param name="dtsMeasurement">the dtsMeasurement</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(DtsMeasurement dtsMeasurement, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<DtsMeasurementList, DtsMeasurement>(dtsMeasurement, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on dtsMeasurement object and test the return code
        /// </summary>
        /// <param name="dtsMeasurement">the dtsMeasurement</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(DtsMeasurement dtsMeasurement, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<DtsMeasurementList, DtsMeasurement>(dtsMeasurement, errorCode);
        }

        /// <summary>
        /// Deletes dtsMeasurement object and test the return code
        /// </summary>
        /// <param name="dtsMeasurement">the dtsMeasurement</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(DtsMeasurement dtsMeasurement, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<DtsMeasurementList, DtsMeasurement>(dtsMeasurement, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single fluidsReport object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="fluidsReport">the fluidsReport with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first fluidsReport from the response</returns>
        public FluidsReport GetAndAssert(FluidsReport fluidsReport, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<FluidsReportList, FluidsReport>(fluidsReport, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds fluidsReport object and test the return code
        /// </summary>
        /// <param name="fluidsReport">the fluidsReport</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(FluidsReport fluidsReport, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<FluidsReportList, FluidsReport>(fluidsReport, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on fluidsReport object and test the return code
        /// </summary>
        /// <param name="fluidsReport">the fluidsReport</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(FluidsReport fluidsReport, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<FluidsReportList, FluidsReport>(fluidsReport, errorCode);
        }

        /// <summary>
        /// Deletes fluidsReport object and test the return code
        /// </summary>
        /// <param name="fluidsReport">the fluidsReport</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(FluidsReport fluidsReport, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<FluidsReportList, FluidsReport>(fluidsReport, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single formationMarker object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="formationMarker">the formationMarker with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first formationMarker from the response</returns>
        public FormationMarker GetAndAssert(FormationMarker formationMarker, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<FormationMarkerList, FormationMarker>(formationMarker, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds formationMarker object and test the return code
        /// </summary>
        /// <param name="formationMarker">the formationMarker</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(FormationMarker formationMarker, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<FormationMarkerList, FormationMarker>(formationMarker, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on formationMarker object and test the return code
        /// </summary>
        /// <param name="formationMarker">the formationMarker</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(FormationMarker formationMarker, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<FormationMarkerList, FormationMarker>(formationMarker, errorCode);
        }

        /// <summary>
        /// Deletes formationMarker object and test the return code
        /// </summary>
        /// <param name="formationMarker">the formationMarker</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(FormationMarker formationMarker, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<FormationMarkerList, FormationMarker>(formationMarker, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single log object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="log">the log with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first log from the response</returns>
        public Log GetAndAssert(Log log, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<LogList, Log>(log, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds log object and test the return code
        /// </summary>
        /// <param name="log">the log</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(Log log, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<LogList, Log>(log, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on log object and test the return code
        /// </summary>
        /// <param name="log">the log</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(Log log, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<LogList, Log>(log, errorCode);
        }

        /// <summary>
        /// Deletes log object and test the return code
        /// </summary>
        /// <param name="log">the log</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Log log, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<LogList, Log>(log, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single message object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="message">the message with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first message from the response</returns>
        public Message GetAndAssert(Message message, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<MessageList, Message>(message, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds message object and test the return code
        /// </summary>
        /// <param name="message">the message</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(Message message, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<MessageList, Message>(message, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on message object and test the return code
        /// </summary>
        /// <param name="message">the message</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(Message message, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<MessageList, Message>(message, errorCode);
        }

        /// <summary>
        /// Deletes message object and test the return code
        /// </summary>
        /// <param name="message">the message</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Message message, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<MessageList, Message>(message, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single mudLog object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="mudLog">the mudLog with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first mudLog from the response</returns>
        public MudLog GetAndAssert(MudLog mudLog, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<MudLogList, MudLog>(mudLog, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds mudLog object and test the return code
        /// </summary>
        /// <param name="mudLog">the mudLog</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(MudLog mudLog, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<MudLogList, MudLog>(mudLog, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on mudLog object and test the return code
        /// </summary>
        /// <param name="mudLog">the mudLog</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(MudLog mudLog, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<MudLogList, MudLog>(mudLog, errorCode);
        }

        /// <summary>
        /// Deletes mudLog object and test the return code
        /// </summary>
        /// <param name="mudLog">the mudLog</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(MudLog mudLog, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<MudLogList, MudLog>(mudLog, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single opsReport object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="opsReport">the opsReport with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first opsReport from the response</returns>
        public OpsReport GetAndAssert(OpsReport opsReport, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<OpsReportList, OpsReport>(opsReport, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds opsReport object and test the return code
        /// </summary>
        /// <param name="opsReport">the opsReport</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(OpsReport opsReport, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<OpsReportList, OpsReport>(opsReport, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on opsReport object and test the return code
        /// </summary>
        /// <param name="opsReport">the opsReport</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(OpsReport opsReport, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<OpsReportList, OpsReport>(opsReport, errorCode);
        }

        /// <summary>
        /// Deletes opsReport object and test the return code
        /// </summary>
        /// <param name="opsReport">the opsReport</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(OpsReport opsReport, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<OpsReportList, OpsReport>(opsReport, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single rig object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="rig">the rig with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first rig from the response</returns>
        public Rig GetAndAssert(Rig rig, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<RigList, Rig>(rig, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds rig object and test the return code
        /// </summary>
        /// <param name="rig">the rig</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(Rig rig, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<RigList, Rig>(rig, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on rig object and test the return code
        /// </summary>
        /// <param name="rig">the rig</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(Rig rig, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<RigList, Rig>(rig, errorCode);
        }

        /// <summary>
        /// Deletes rig object and test the return code
        /// </summary>
        /// <param name="rig">the rig</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Rig rig, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<RigList, Rig>(rig, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single risk object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="risk">the risk with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first risk from the response</returns>
        public Risk GetAndAssert(Risk risk, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<RiskList, Risk>(risk, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds risk object and test the return code
        /// </summary>
        /// <param name="risk">the risk</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(Risk risk, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<RiskList, Risk>(risk, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on risk object and test the return code
        /// </summary>
        /// <param name="risk">the risk</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(Risk risk, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<RiskList, Risk>(risk, errorCode);
        }

        /// <summary>
        /// Deletes risk object and test the return code
        /// </summary>
        /// <param name="risk">the risk</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Risk risk, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<RiskList, Risk>(risk, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single sidewallCore object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="sidewallCore">the sidewallCore with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first sidewallCore from the response</returns>
        public SidewallCore GetAndAssert(SidewallCore sidewallCore, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<SidewallCoreList, SidewallCore>(sidewallCore, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds sidewallCore object and test the return code
        /// </summary>
        /// <param name="sidewallCore">the sidewallCore</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(SidewallCore sidewallCore, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<SidewallCoreList, SidewallCore>(sidewallCore, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on sidewallCore object and test the return code
        /// </summary>
        /// <param name="sidewallCore">the sidewallCore</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(SidewallCore sidewallCore, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<SidewallCoreList, SidewallCore>(sidewallCore, errorCode);
        }

        /// <summary>
        /// Deletes sidewallCore object and test the return code
        /// </summary>
        /// <param name="sidewallCore">the sidewallCore</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(SidewallCore sidewallCore, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<SidewallCoreList, SidewallCore>(sidewallCore, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single surveyProgram object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="surveyProgram">the surveyProgram with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first surveyProgram from the response</returns>
        public SurveyProgram GetAndAssert(SurveyProgram surveyProgram, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<SurveyProgramList, SurveyProgram>(surveyProgram, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds surveyProgram object and test the return code
        /// </summary>
        /// <param name="surveyProgram">the surveyProgram</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(SurveyProgram surveyProgram, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<SurveyProgramList, SurveyProgram>(surveyProgram, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on surveyProgram object and test the return code
        /// </summary>
        /// <param name="surveyProgram">the surveyProgram</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(SurveyProgram surveyProgram, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<SurveyProgramList, SurveyProgram>(surveyProgram, errorCode);
        }

        /// <summary>
        /// Deletes surveyProgram object and test the return code
        /// </summary>
        /// <param name="surveyProgram">the surveyProgram</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(SurveyProgram surveyProgram, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<SurveyProgramList, SurveyProgram>(surveyProgram, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single target object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="target">the target with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first target from the response</returns>
        public Target GetAndAssert(Target target, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<TargetList, Target>(target, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds target object and test the return code
        /// </summary>
        /// <param name="target">the target</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(Target target, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<TargetList, Target>(target, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on target object and test the return code
        /// </summary>
        /// <param name="target">the target</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(Target target, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<TargetList, Target>(target, errorCode);
        }

        /// <summary>
        /// Deletes target object and test the return code
        /// </summary>
        /// <param name="target">the target</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Target target, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<TargetList, Target>(target, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single trajectory object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="trajectory">the trajectory with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first trajectory from the response</returns>
        public Trajectory GetAndAssert(Trajectory trajectory, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<TrajectoryList, Trajectory>(trajectory, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds trajectory object and test the return code
        /// </summary>
        /// <param name="trajectory">the trajectory</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(Trajectory trajectory, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<TrajectoryList, Trajectory>(trajectory, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on trajectory object and test the return code
        /// </summary>
        /// <param name="trajectory">the trajectory</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(Trajectory trajectory, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<TrajectoryList, Trajectory>(trajectory, errorCode);
        }

        /// <summary>
        /// Deletes trajectory object and test the return code
        /// </summary>
        /// <param name="trajectory">the trajectory</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Trajectory trajectory, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<TrajectoryList, Trajectory>(trajectory, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single tubular object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="tubular">the tubular with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first tubular from the response</returns>
        public Tubular GetAndAssert(Tubular tubular, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<TubularList, Tubular>(tubular, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds tubular object and test the return code
        /// </summary>
        /// <param name="tubular">the tubular</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(Tubular tubular, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<TubularList, Tubular>(tubular, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on tubular object and test the return code
        /// </summary>
        /// <param name="tubular">the tubular</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(Tubular tubular, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<TubularList, Tubular>(tubular, errorCode);
        }

        /// <summary>
        /// Deletes tubular object and test the return code
        /// </summary>
        /// <param name="tubular">the tubular</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Tubular tubular, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<TubularList, Tubular>(tubular, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single wbGeometry object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="wbGeometry">the wbGeometry with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first wbGeometry from the response</returns>
        public StandAloneWellboreGeometry GetAndAssert(StandAloneWellboreGeometry wbGeometry, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<WellboreGeometryList, StandAloneWellboreGeometry>(wbGeometry, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds wbGeometry object and test the return code
        /// </summary>
        /// <param name="wbGeometry">the wbGeometry</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(StandAloneWellboreGeometry wbGeometry, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<WellboreGeometryList, StandAloneWellboreGeometry>(wbGeometry, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on wbGeometry object and test the return code
        /// </summary>
        /// <param name="wbGeometry">the wbGeometry</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(StandAloneWellboreGeometry wbGeometry, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<WellboreGeometryList, StandAloneWellboreGeometry>(wbGeometry, errorCode);
        }

        /// <summary>
        /// Deletes wbGeometry object and test the return code
        /// </summary>
        /// <param name="wbGeometry">the wbGeometry</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(StandAloneWellboreGeometry wbGeometry, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<WellboreGeometryList, StandAloneWellboreGeometry>(wbGeometry, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single well object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="well">the well with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first well from the response</returns>
        public Well GetAndAssert(Well well, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<WellList, Well>(well, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds well object and test the return code
        /// </summary>
        /// <param name="well">the well</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(Well well, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<WellList, Well>(well, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on well object and test the return code
        /// </summary>
        /// <param name="well">the well</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(Well well, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<WellList, Well>(well, errorCode);
        }

        /// <summary>
        /// Deletes well object and test the return code
        /// </summary>
        /// <param name="well">the well</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Well well, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<WellList, Well>(well, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single wellbore object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="wellbore">the wellbore with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first wellbore from the response</returns>
        public Wellbore GetAndAssert(Wellbore wellbore, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<WellboreList, Wellbore>(wellbore, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds wellbore object and test the return code
        /// </summary>
        /// <param name="wellbore">the wellbore</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(Wellbore wellbore, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<WellboreList, Wellbore>(wellbore, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on wellbore object and test the return code
        /// </summary>
        /// <param name="wellbore">the wellbore</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(Wellbore wellbore, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<WellboreList, Wellbore>(wellbore, errorCode);
        }

        /// <summary>
        /// Deletes wellbore object and test the return code
        /// </summary>
        /// <param name="wellbore">the wellbore</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Wellbore wellbore, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<WellboreList, Wellbore>(wellbore, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single wellLog object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="wellLog">the wellLog with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first wellLog from the response</returns>
        public WellLog GetAndAssert(WellLog wellLog, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<WellLogList, WellLog>(wellLog, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds wellLog object and test the return code
        /// </summary>
        /// <param name="wellLog">the wellLog</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(WellLog wellLog, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<WellLogList, WellLog>(wellLog, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on wellLog object and test the return code
        /// </summary>
        /// <param name="wellLog">the wellLog</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(WellLog wellLog, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<WellLogList, WellLog>(wellLog, errorCode);
        }

        /// <summary>
        /// Deletes wellLog object and test the return code
        /// </summary>
        /// <param name="wellLog">the wellLog</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(WellLog wellLog, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<WellLogList, WellLog>(wellLog, errorCode, partialDelete);
        }

        /// <summary>
        /// Creates the update log with rows with number of totalUpdateRows specified.
        /// The index starts after the last index in the specified log.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="totalUpdateRows">The total update rows.</param>
        /// <returns>An update log with totalUpdateRows rows</returns>
        public Log CreateUpdateLogWithRows(Log log, int totalUpdateRows)
        {
            // Create Update Log with one new row
            var updateLog = CreateLog(log);
            updateLog.LogCurveInfo = log.LogCurveInfo;

            var startIndex = int.Parse(log.LogData[log.LogData.Count - 1].Split(',')[0]);
            updateLog.LogData = new List<string>();

            for (var i = startIndex; i < startIndex + totalUpdateRows; i++)
            {
                // Compute the next index value
                var index = startIndex + 1;

                // Create a row of Data
                var data = Enumerable.Repeat(index, log.LogCurveInfo.Count);
                updateLog.LogData.Add(string.Join(",", data));
            }

            return updateLog;
        }

        /// <summary>
        /// Does get query for single trajectory object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="basicXmlTemplate">A XML string with reference parameters for UIDs and body elements</param>
        /// <param name="trajectory">The trajectory.</param>
        /// <param name="queryContent">The query xml descendants of the trajectory element.</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <returns>The first trajectory from the response.</returns>
        public Trajectory GetAndAssertWithXml(string basicXmlTemplate, Trajectory trajectory, string queryContent = null, bool isNotNull = true, string optionsIn = null)
        {
            var queryIn = string.Format(basicXmlTemplate, trajectory.UidWell, trajectory.UidWellbore, trajectory.Uid, queryContent);

            var results = Query<TrajectoryList, Trajectory>(ObjectTypes.Trajectory, queryIn, null, optionsIn ?? OptionsIn.ReturnElements.All);
            Assert.AreEqual(isNotNull ? 1 : 0, results.Count);

            var result = results.FirstOrDefault();
            Assert.AreEqual(isNotNull, result != null);
            return result;
        }

        /// <summary>
        /// Generations trajectory station data.
        /// </summary>
        /// <param name="numOfStations">The number of stations.</param>
        /// <param name="startMd">The start md.</param>
        /// <param name="mdUom">The MD index uom.</param>
        /// <param name="tvdUom">The Tvd uom.</param>
        /// <param name="angleUom">The angle uom.</param>
        /// <param name="inCludeExtra">True if to generate extra information for trajectory station.</param>
        /// <returns>The trajectoryStation collection.</returns>
        public List<TrajectoryStation> TrajectoryStations(int numOfStations, double startMd, MeasuredDepthUom mdUom = MdUom, WellVerticalCoordinateUom tvdUom = TvdUom, PlaneAngleUom angleUom = AngleUom, bool inCludeExtra = false)
        {
            return TrajectoryGenerator.GenerationStations(numOfStations, startMd, mdUom, tvdUom, angleUom, inCludeExtra);
        }

        public Well CreateTestWell()
        {
            var dateTimeSpud = DateTimeOffset.UtcNow;
            var groundElevation = new WellElevationCoord
            {
                Uom = WellVerticalCoordinateUom.m,
                Value = 40.0
            };

            var datum1 = WellDatum("Kelly Bushing", code: ElevCodeEnum.KB, uid: ElevCodeEnum.KB.ToString());
            var datum2 = WellDatum("Sea Level", code: ElevCodeEnum.SL, uid: ElevCodeEnum.SL.ToString());

            var commonData = new CommonData
            {
                ItemState = ItemState.plan,
                Comments = "well in plan"
            };

            var well = new Well
            {
                Name = Name("Test Well"),
                Country = "US",
                DateTimeSpud = dateTimeSpud,
                DirectionWell = WellDirection.unknown,
                GroundElevation = groundElevation,
                TimeZone = TimeZone,
                WellDatum = List(datum1, datum2),
                CommonData = commonData
            };

            return well;
        }

        public Well GetFullWell()
        {
            var dataDir = new DirectoryInfo(@".\TestData").FullName;
            var filePath = Path.Combine(dataDir, "Full131Well.xml");

            var xmlin = File.ReadAllText(filePath);
            var wells = EnergisticsConverter.XmlToObject<WellList>(xmlin);
            return wells.Items[0] as Well;
        }

        public WellDatum WellDatum(string name, ElevCodeEnum? code = null, string uid = null)
        {
            return new WellDatum()
            {
                Uid = uid,
                Name = name,
                Code = code,
            };
        }

        public IndexedObject IndexedObject(int id, short index) => new IndexedObject()
        {
            Description = $"Test param{id}",
            Index = index,
            Name = $"Test{id}",
            Uom = "m",
            Value = $"1{id}.0"
        };

        public void AssertTimeIndexSpecified(Log log, bool isIndexSpecified)
        {
            Assert.AreEqual(isIndexSpecified, log.StartDateTimeIndexSpecified);
            Assert.AreEqual(isIndexSpecified, log.EndDateTimeIndexSpecified);

            foreach (var logCurveInfo in log.LogCurveInfo)
            {
                Assert.AreEqual(isIndexSpecified, logCurveInfo.MinDateTimeIndexSpecified);
                Assert.AreEqual(isIndexSpecified, logCurveInfo.MaxDateTimeIndexSpecified);
            }
        }
        public WMLS_AddToStoreResponse Add_Log_from_file(string xmlfile)
        {
            var xmlin = File.ReadAllText(xmlfile);

            var logList = EnergisticsConverter.XmlToObject<LogList>(xmlin);
            Assert.IsNotNull(logList);
            Assert.IsTrue(logList.Log.Count > 0);

            var log = new Log() { Uid = logList.Log[0].Uid, UidWell = logList.Log[0].UidWell, UidWellbore = logList.Log[0].UidWellbore };
            var result = Query<LogList, Log>(log);
            Assert.IsNotNull(result);
            if (result.Count > 0)
            {
                // Do not add if the log already exists.
                return null;
            }

            var response = AddToStore(ObjectTypes.Log, xmlin, null, null);
            Assert.IsNotNull(response);
            return response;
        }

        public WMLS_UpdateInStoreResponse Update_Log_from_file(string xmlfile)
        {
            var xmlin = File.ReadAllText(xmlfile);

            var logList = EnergisticsConverter.XmlToObject<LogList>(xmlin);
            Assert.IsNotNull(logList);
            Assert.IsTrue(logList.Log.Count > 0);

            var log = new Log() { Uid = logList.Log[0].Uid, UidWell = logList.Log[0].UidWell, UidWellbore = logList.Log[0].UidWellbore };
            var result = Query<LogList, Log>(log);
            Assert.IsNotNull(result);
            if (result.Count > 0)
            {
                var response = UpdateInStore(ObjectTypes.Log, xmlin, null, null);
                Assert.IsNotNull(response);
            }

            return null;
        }

        public WMLS_AddToStoreResponse Add_Well_from_file(string xmlfile)
        {
            var xmlin = File.ReadAllText(xmlfile);

            var wellList = EnergisticsConverter.XmlToObject<WellList>(xmlin);
            Assert.IsNotNull(wellList);
            Assert.IsTrue(wellList.Well.Count > 0);

            var well = new Well() { Uid = wellList.Well[0].Uid };
            var result = Query<WellList, Well>(well);
            Assert.IsNotNull(result);

            if (result.Count > 0)
            {
                // Do not add if the well already exists.
                return null;
            }

            var response = AddToStore(ObjectTypes.Well, xmlin, null, null);
            Assert.IsNotNull(response);
            return response;
        }

        public WMLS_AddToStoreResponse Add_Wellbore_from_file(string xmlfile)
        {
            var xmlin = File.ReadAllText(xmlfile);

            var wellboreList = EnergisticsConverter.XmlToObject<WellboreList>(xmlin);
            Assert.IsNotNull(wellboreList);
            Assert.IsTrue(wellboreList.Wellbore.Count > 0);

            var wellbore = new Wellbore() { Uid = wellboreList.Wellbore[0].Uid, UidWell = wellboreList.Wellbore[0].UidWell };
            var result = Query<WellboreList, Wellbore>(wellbore);
            Assert.IsNotNull(result);

            if (result.Count > 0)
            {
                // Do not add if the wellbore already exists.
                return null;
            }

            var response = AddToStore(ObjectTypes.Wellbore, xmlin, null, null);
            Assert.IsNotNull(response);
            return response;
        }
    }
}
