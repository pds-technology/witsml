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
using PDS.Witsml.Data.Logs;
using PDS.Witsml.Data.Trajectories;

namespace PDS.Witsml.Server
{
    public class DevKit131Aspect : DevKitAspect
    {
        public static readonly string BasicDeleteLogXmlTemplate = "<logs xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\">" + Environment.NewLine +
                          "   <log uid=\"{0}\" uidWell=\"{1}\" uidWellbore=\"{2}\">" + Environment.NewLine +
                          "{3}" +
                          "   </log>" + Environment.NewLine +
                          "</logs>";

        private const MeasuredDepthUom MdUom = MeasuredDepthUom.m;
        private const WellVerticalCoordinateUom TvdUom = WellVerticalCoordinateUom.m;
        private const PlaneAngleUom AngleUom = PlaneAngleUom.dega;

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

        public void InitData(Log log, string mnemonics, string units, params object[] values)
        {
            if (log.LogData == null)
            {
                log.LogData = List<string>();
            }

            if (values != null && values.Any())
            {
                log.LogData.Add(String.Join(",", values.Select(x => x == null ? string.Empty : x)));
            }
        }

        public void InitDataMany(Log log, string mnemonics, string units, int numRows, double factor = 1.0, bool isDepthLog = true, bool hasEmptyChannel = true, bool increasing = true)
        {
            var depthStart = log.StartIndex != null ? log.StartIndex.Value : 0;
            var timeStart = DateTimeOffset.UtcNow.AddDays(-1);
            var interval = increasing ? 1 : -1;

            for (int i = 0; i < numRows; i++)
            {
                if (isDepthLog)
                {
                    InitData(log, mnemonics, units, depthStart + i * interval, hasEmptyChannel ? (int?)null : i, depthStart + i * factor);
                }
                else
                {
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
        /// Adds wellbore object and test the return code
        /// </summary>
        /// <param name="wellbore">the wellbore</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(Wellbore wellbore, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<WellboreList, Wellbore>(wellbore, errorCode);
        }

        /// <summary>
        /// Adds log object and test the return code
        /// </summary>
        /// <param name="log">the log.</param>
        /// <param name="errorCode">the errorCode.</param>
        public WMLS_AddToStoreResponse AddAndAssert(Log log, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<LogList, Log>(log, errorCode);
        }

        /// <summary>
        /// Adds trajectory object and test the return code
        /// </summary>
        /// <param name="trajectory">the trajectory.</param>
        /// <param name="errorCode">the errorCode.</param>
        public WMLS_AddToStoreResponse AddAndAssert(Trajectory trajectory, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<TrajectoryList, Trajectory>(trajectory, errorCode);
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
        /// Adds a wellbore child object and test the return code
        /// </summary>
        /// <typeparam name="TList">The type of the container.</typeparam>
        /// <typeparam name="TObject">The type of the data object.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns>The <see cref="WMLS_AddToStoreResponse"/> from the store.</returns>
        public WMLS_AddToStoreResponse AddAndAssert<TList, TObject>(TObject dataObject, ErrorCodes errorCode = ErrorCodes.Success) where TList : IEnergisticsCollection
        {
            var response = Add<TList, TObject>(dataObject);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)errorCode, response.Result);
            return response;
        }

        /// <summary>
        /// Does get query for single well object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="well">the well</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <returns>The first well from the response</returns>
        public Well GetAndAssert(Well well, bool isNotNull = true)
        {
            Assert.IsNotNull(well.Uid);

            var query = new Well { Uid = well.Uid };
            return QueryAndAssert<WellList, Well>(query, isNotNull);
        }

        /// <summary>
        /// Does get query for single wellbore object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="wellbore">the wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <returns>The first wellbore from the response</returns>
        public Wellbore GetAndAssert(Wellbore wellbore, bool isNotNull = true)
        {
            Assert.IsNotNull(wellbore.UidWell);
            Assert.IsNotNull(wellbore.Uid);

            var query = new Wellbore { UidWell = wellbore.UidWell, Uid = wellbore.Uid };
            return QueryAndAssert<WellboreList, Wellbore>(query, isNotNull);
        }

        /// <summary>
        /// Does get query for single log object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="log">the log with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <returns>The first log from the response</returns>
        public Log GetAndAssert(Log log, bool isNotNull = true)
        {
            return GetAndAssert<LogList, Log>(log, isNotNull);
        }

        /// <summary>
        /// Does get query for single trajectory object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="trajectory">the log with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <returns>The first trajectory from the response</returns>
        public Trajectory GetAndAssert(Trajectory trajectory, bool isNotNull = true)
        {
            return GetAndAssert<TrajectoryList, Trajectory>(trajectory, isNotNull);
        }

        /// <summary>
        /// Executes GetFromStore and tests the response.
        /// </summary>
        /// <typeparam name="TList">The type of the container.</typeparam>
        /// <typeparam name="TObject">The type of the data object.</typeparam>
        /// <param name="example">The example data object.</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <returns>The data object instance if found; otherwise, null.</returns>
        public TObject GetAndAssert<TList, TObject>(TObject example, bool isNotNull = true) where TList : IEnergisticsCollection where TObject : IDataObject
        {
            var wellObject = example as IWellObject;
            var wellboreObject = example as IWellboreObject;
            var query = Activator.CreateInstance<TObject>();

            Assert.IsNotNull(example.Uid);
            query.Uid = example.Uid;

            if (wellObject != null)
            {
                Assert.IsNotNull(wellObject.UidWell);
                ((IWellObject)query).UidWell = wellObject.UidWell;
            }

            if (wellboreObject != null)
            {
                Assert.IsNotNull(wellboreObject.UidWellbore);
                ((IWellboreObject)query).UidWellbore = wellboreObject.UidWellbore;
            }

            return QueryAndAssert<TList, TObject>(query, isNotNull);
        }

        /// <summary>
        /// Executes GetFromStore and tests the response.
        /// </summary>
        /// <typeparam name="TList">The type of the container.</typeparam>
        /// <typeparam name="TObject">The type of the data object.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <returns>The data object instance if found; otherwise, null.</returns>
        public TObject QueryAndAssert<TList, TObject>(TObject query, bool isNotNull = true) where TList : IEnergisticsCollection
        {
            var results = Query<TList, TObject>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(isNotNull ? 1 : 0, results.Count);

            var result = results.FirstOrDefault();
            Assert.AreEqual(isNotNull, result != null);

            return result;
        }

        /// <summary>
        /// Does UpdateInStore on well object and test the return code
        /// </summary>
        /// <param name="well">the well</param>
        /// <param name="errorCode">The error code.</param>
        public void UpdateAndAssert(Well well, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<WellList, Well>(well);
        }

        /// <summary>
        /// Does UpdateInStore on wellbore object and test the return code
        /// </summary>
        /// <param name="wellbore">the wellbore</param>
        /// <param name="errorCode">The error code.</param>
        public void UpdateAndAssert(Wellbore wellbore, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<WellboreList, Wellbore>(wellbore);
        }

        /// <summary>
        /// Does UpdateInStore on log object and test the return code
        /// </summary>
        /// <param name="log">the log</param>
        /// <param name="errorCode">The error code.</param>
        public void UpdateAndAssert(Log log, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<LogList, Log>(log);
        }

        /// <summary>
        /// Updates the data object and test the return code
        /// </summary>
        /// <typeparam name="TList">The type of the container.</typeparam>
        /// <typeparam name="TObject">The type of the data object.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <param name="errorCode">The error code.</param>
        public void UpdateAndAssert<TList, TObject>(TObject dataObject, ErrorCodes errorCode = ErrorCodes.Success) where TList : IEnergisticsCollection
        {
            var response = Update<TList, TObject>(dataObject);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)errorCode, response.Result);
        }

        /// <summary>
        /// Deletes the well and test the return code
        /// </summary>
        /// <param name="well">The well.</param>
        /// <param name="errorCode">The error code.</param>
        public void DeleteAndAssert(Well well, ErrorCodes errorCode = ErrorCodes.Success)
        {
            DeleteAndAssert<WellList, Well>(well);
        }

        /// <summary>
        /// Deletes the wellbore and test the return code
        /// </summary>
        /// <param name="wellbore">The wellbore.</param>
        /// <param name="errorCode">The error code.</param>
        public void DeleteAndAssert(Wellbore wellbore, ErrorCodes errorCode = ErrorCodes.Success)
        {
            DeleteAndAssert<WellboreList, Wellbore>(wellbore);
        }

        /// <summary>
        /// Deletes the log and test the return code
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="errorCode">The error code.</param>
        public void DeleteAndAssert(Log log, ErrorCodes errorCode = ErrorCodes.Success)
        {
            DeleteAndAssert<LogList, Log>(log);
        }

        /// <summary>
        /// Deletes the data object and test the return code
        /// </summary>
        /// <typeparam name="TList">The type of the container.</typeparam>
        /// <typeparam name="TObject">The type of the data object.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <param name="errorCode">The error code.</param>
        public void DeleteAndAssert<TList, TObject>(TObject dataObject, ErrorCodes errorCode = ErrorCodes.Success) where TList : IEnergisticsCollection
        {
            var response = Delete<TList, TObject>(dataObject);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)errorCode, response.Result);
        }

        /// <summary>
        /// Generations trajectory station data.
        /// </summary>
        /// <param name="numOfStations">The number of stations.</param>
        /// <param name="startMd">The start md.</param>
        /// <param name="mdUom">The MD index uom.</param>
        /// <param name="tvdUom">The Tvd uom.</param>
        /// <param name="angleUom">The angle uom.</param>
        /// <returns>The trajectoryStation collection.</returns>
        public List<TrajectoryStation> TrajectoryStations(int numOfStations, double startMd, MeasuredDepthUom mdUom = MdUom, WellVerticalCoordinateUom tvdUom = TvdUom, PlaneAngleUom angleUom = AngleUom)
        {
            return TrajectoryGenerator.GenerationStations(numOfStations, startMd, mdUom, tvdUom, angleUom);
        }
    }
}
