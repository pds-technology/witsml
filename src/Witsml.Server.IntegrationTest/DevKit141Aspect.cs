//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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
using System.IO;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Datatypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;
using PDS.Witsml.Data.Logs;
using PDS.Witsml.Data.Trajectories;

namespace PDS.Witsml.Server
{
    public class DevKit141Aspect : DevKitAspect
    {
        public static readonly string BasicAddWellXmlTemplate = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                          "   <well uid=\"{0}\">" + Environment.NewLine +
                          "     <name>{1}</name>" + Environment.NewLine +
                          "     <timeZone>-06:00</timeZone>" + Environment.NewLine +
                          "{2}" +
                          "   </well>" + Environment.NewLine +
                          "</wells>";

        public static readonly string BasicDeleteLogXmlTemplate = "<logs xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                          "   <log uid=\"{0}\" uidWell=\"{1}\" uidWellbore=\"{2}\">" + Environment.NewLine +
                          "{3}" +
                          "   </log>" + Environment.NewLine +
                          "</logs>";

        private const MeasuredDepthUom MdUom = MeasuredDepthUom.m;
        private const WellVerticalCoordinateUom TvdUom = WellVerticalCoordinateUom.m;
        private const PlaneAngleUom AngleUom = PlaneAngleUom.dega;

        public DevKit141Aspect(TestContext context, string url = null) : base(url, WMLSVersion.WITSML141, context)
        {
            LogGenerator = new Log141Generator();
            TrajectoryGenerator = new Trajectory141Generator();
        }

        public Log141Generator LogGenerator { get; }

        public Trajectory141Generator TrajectoryGenerator { get; }

        public override string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version141.Value; }
        }

        public void InitHeader(Log log, LogIndexType indexType, bool increasing = true)
        {
            log.IndexType = indexType;
            log.IndexCurve = indexType == LogIndexType.datetime ? "TIME" : "MD";
            log.Direction = increasing ? LogIndexDirection.increasing : LogIndexDirection.decreasing;

            log.LogCurveInfo = List<LogCurveInfo>();

            if (indexType == LogIndexType.datetime)
            {
                log.LogCurveInfo.Add(LogGenerator.CreateDateTimeLogCurveInfo(log.IndexCurve, "s"));
            }
            else
            {
                log.LogCurveInfo.Add(LogGenerator.CreateDoubleLogCurveInfo(log.IndexCurve, "m"));
            }

            log.LogCurveInfo.Add(LogGenerator.CreateDoubleLogCurveInfo("ROP", "m/h"));
            log.LogCurveInfo.Add(LogGenerator.CreateDoubleLogCurveInfo("GR", "gAPI"));

            InitData(log, Mnemonics(log), Units(log));
        }

        /// <summary>
        /// Creates the double log curve information.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="unit">The unit.</param>
        /// <returns></returns>
        public LogCurveInfo CreateDoubleLogCurveInfo(string name, string unit)
        {
            return LogGenerator.CreateDoubleLogCurveInfo(name, unit);
        }

        /// <summary>
        /// Creates the string log curve information.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="unit">The unit.</param>
        /// <returns></returns>
        public LogCurveInfo CreateStringLogCurveInfo(string name, string unit)
        {
            return LogGenerator.CreateStringLogCurveInfo(name, unit);
        }

        public void InitData(Log log, string mnemonics, string units, params object[] values)
        {
            if (log.LogData == null)
            {
                log.LogData = List(new LogData());
            }

            if (log.LogData[0].Data == null)
            {
                log.LogData[0].Data = List<string>();
            }

            log.LogData[0].MnemonicList = mnemonics;
            log.LogData[0].UnitList = units;

            if (values != null && values.Any())
            {
                var delimiter = log.GetDataDelimiterOrDefault();
                log.LogData[0].Data.Add(string.Join(delimiter, values.Select(x => x ?? string.Empty)));
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
            query.Log = One<Log>(x => x.Uid = log.Uid);
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

        public WellDatum WellDatum(string name, ElevCodeEnum? code = null, string uid = null)
        {
            return new WellDatum()
            {
                Uid = uid,
                Name = name,
                Code = code,
            };
        }

        public WellDatum WellDatum(
            ElevCodeEnum code, string namePrefix, int totalKind, int kindStart, string kindPrefix,
            WellVerticalCoordinateUom uomElevation, MeasuredDepthUom uomMeasuredDepth,
            int elevation, int measuredDepth, string comment)
        {
            var datum = WellDatum($"{namePrefix}-{code.ToString()}", code, code.ToString());

            datum.Elevation = new WellElevationCoord { Uom = uomElevation, Value = elevation };
            datum.MeasuredDepth = new MeasuredDepthCoord { Uom = uomMeasuredDepth, Value = measuredDepth };
            datum.Comment = comment;

            // Add kind if necessary
            if (totalKind <= 0) return datum;
            datum.Kind = new List<string>();
            Enumerable.Range(kindStart, totalKind).ForEach(k => datum.Kind.Add($"{kindPrefix}-{k}"));

            return datum;
        }

        public List<WellDatum> WellDatums(
            List<ElevCodeEnum> codes, int totalKind, int kindStart, string commonString,
            WellVerticalCoordinateUom uomElevation, MeasuredDepthUom uomMeasuredDepth,
            int elevationStart, int measuredDepthStart, int commentStart)
        {
            var datums = new List<WellDatum>();
            codes.ForEach(c =>
            {
                datums.Add(
                    WellDatum(c, commonString, totalKind, kindStart, $"kind-{commonString}", uomElevation, uomMeasuredDepth,
                        elevationStart++, measuredDepthStart++, $"comment-{commonString}-{commentStart++}"));
                kindStart += totalKind;
            }
            );

            return datums;
        }

        public WellCRS WellCRS(string uid, string name, string description = null)
        {
            return new WellCRS
            {
                Uid = uid,
                Name = name,
                Description = description
            };
        }

        public Bop Bop(double pressure, double length, string manufacturer)
        {
            return new Bop
            {
                PresBopRating = new PressureMeasure {Uom = PressureUom.Pa, Value = 1},
                SizeBopSys = new LengthMeasure {Uom = LengthUom.cm, Value = 20},
                Manufacturer = manufacturer
            };
        }

        public BopComponent BopComponent(string uid, BopType bopType, string descComp)
        {
            return new BopComponent
            {
                Uid = uid,
                TypeBopComp = bopType,
                DescComp = descComp
            };
        }

        public List<BopComponent> BopComponents(int start, int total, BopType bopType, string prefix)
        {
            var bopList = new List<BopComponent>();

            for (var i = start; i < (start + total); i++)
            {
                bopList.Add(BopComponent($"{prefix}-{i}", bopType, $"{prefix}-{i}"));
            }

            return bopList;
        }

        public NameTag NameTag(string uid, string name, string comment, NameTagNumberingScheme numberingScheme)
        {
            return new NameTag
            {
                Uid = uid,
                Name = name,
                Comment = comment,
                NumberingScheme = numberingScheme
            };
        }

        public List<NameTag> NameTags(int start, int total, string prefix, NameTagNumberingScheme numberingScheme)
        {
            var nameTags = new List<NameTag>();

            for (var i = start; i < (start + total); i++)
            {
                nameTags.Add(NameTag($"{prefix}-{i}", $"{prefix}-{i}", $"{prefix}-{i}", numberingScheme));
            }

            return nameTags;
        }

        public Rig CreateRig(string rigNamePrefix, Wellbore wellbore, Bop bop = null)
        {
            return new Rig
            {
                Uid = Uid(),
                Name = Name(rigNamePrefix),
                UidWell = wellbore.UidWell,
                NameWell = wellbore.NameWell,
                UidWellbore = wellbore.Uid,
                NameWellbore = wellbore.Name,
                Bop = bop
            };
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

        public ExtensionNameValue ExtensionNameValue(string uid, string value, string uom, PrimitiveType dataType = PrimitiveType.@double, string name = null)
        {
            return new ExtensionNameValue()
            {
                Uid = uid,
                Name = new ExtensionName(name ?? uid),
                Value = new Extensionvalue()
                {
                    Value = value,
                    Uom = uom
                },
                DataType = dataType
            };
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

        public Well CreateBaseWell(string namePrefix)
        {
            return new Well
            {
                Uid = Uid(),
                Name = Name(namePrefix),
                TimeZone = TimeZone
            };
        }

        /// <summary>
        /// Creates an empty changeLog for the object of the URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>An instance of <see cref="ChangeLog"/>.</returns>
        public ChangeLog CreateChangeLog(EtpUri uri)
        {
            if (ObjectTypes.Well.EqualsIgnoreCase(uri.ObjectType))
            {
                return new ChangeLog()
                {
                    UidObject = uri.ObjectId
                };
            }
            else if (ObjectTypes.Wellbore.EqualsIgnoreCase(uri.ObjectType))
            {
                return new ChangeLog()
                {
                    UidWell = uri.Parent.ObjectId,
                    UidObject = uri.ObjectId
                };
            }
            else
            {
                return new ChangeLog()
                {
                    UidWell = uri.Parent.Parent.ObjectId,
                    UidWellbore = uri.Parent.ObjectId,
                    UidObject = uri.ObjectId
                };
            }
        }

        public Well GetFullWell()
        {
            var dataDir = new DirectoryInfo(@".\TestData").FullName;
            var filePath = Path.Combine(dataDir, "Full141Well.xml");

            var xmlin = File.ReadAllText(filePath);
            var wells = EnergisticsConverter.XmlToObject<WellList>(xmlin);
            return wells.Items[0] as Well;
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
        /// Adds attachment object and test the return code
        /// </summary>
        /// <param name="attachment">the attachment</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(Attachment attachment, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<AttachmentList, Attachment>(attachment, errorCode);
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
        /// Adds wbGeometry object and test the return code
        /// </summary>
        /// <param name="wbGeometry">the wbGeometry</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(StandAloneWellboreGeometry wbGeometry, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<WellboreGeometryList, StandAloneWellboreGeometry>(wbGeometry, errorCode);
        }

        /// <summary>
        /// Does get query for single well object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="well">the well</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first well from the response</returns>
        public Well GetAndAssert(Well well, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<WellList, Well>(well, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Does get query for single wellbore object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="wellbore">the wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first wellbore from the response</returns>
        public Wellbore GetAndAssert(Wellbore wellbore, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<WellboreList, Wellbore>(wellbore, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Does get query for single log object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="log">the log with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <param name="errorCode">The expected error code.</param>
        /// <returns>The first log from the response</returns>
        public Log GetAndAssert(Log log, bool isNotNull = true, string optionsIn = null, bool queryByExample = false, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return GetAndAssert<LogList, Log>(log, isNotNull, optionsIn, queryByExample, errorCode);
        }

        /// <summary>
        /// Does get query for single trajectory object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="trajectory">the trajectory with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <param name="errorCode">The expected error code.</param>
        /// <returns>The first trajectory from the response</returns>
        public Trajectory GetAndAssert(Trajectory trajectory, bool isNotNull = true, string optionsIn = null, bool queryByExample = false, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return GetAndAssert<TrajectoryList, Trajectory>(trajectory, isNotNull, optionsIn, queryByExample, errorCode);
        }

        /// <summary>
        /// Does get query for single trajectory object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="attachment">the attachment with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first trajectory from the response</returns>
        public Attachment GetAndAssert(Attachment attachment, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<AttachmentList, Attachment>(attachment, isNotNull, optionsIn, queryByExample);
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
        /// Does UpdateInStore on well object and test the return code
        /// </summary>
        /// <param name="well">the well</param>
        /// <param name="errorCode">The error code.</param>
        public void UpdateAndAssert(Well well, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<WellList, Well>(well, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on wellbore object and test the return code
        /// </summary>
        /// <param name="wellbore">the wellbore</param>
        /// <param name="errorCode">The error code.</param>
        public void UpdateAndAssert(Wellbore wellbore, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<WellboreList, Wellbore>(wellbore, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on log object and test the return code
        /// </summary>
        /// <param name="log">the log</param>
        /// <param name="errorCode">The error code.</param>
        public void UpdateAndAssert(Log log, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<LogList, Log>(log, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on trajectory object and test the return code
        /// </summary>
        /// <param name="trajectory">the trajectory</param>
        /// <param name="errorCode">The error code.</param>
        public void UpdateAndAssert(Trajectory trajectory, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<TrajectoryList, Trajectory>(trajectory, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on rig object and test the return code
        /// </summary>
        /// <param name="rig">the rig</param>
        /// <param name="errorCode">The error code.</param>
        public void UpdateAndAssert(Rig rig, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<RigList, Rig>(rig, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on attachment object and test the return code
        /// </summary>
        /// <param name="attachment">the attachment</param>
        /// <param name="errorCode">The error code.</param>
        public void UpdateAndAssert(Attachment attachment, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<AttachmentList, Attachment>(attachment, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on message object and test the return code
        /// </summary>
        /// <param name="message">the message</param>
        /// <param name="errorCode">The error code.</param>
        public void UpdateAndAssert(Message message, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<MessageList, Message>(message, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on wbGeometry object and test the return code
        /// </summary>
        /// <param name="wbGeometry">the wbGeometry</param>
        /// <param name="errorCode">The error code.</param>
        public void UpdateAndAssert(StandAloneWellboreGeometry wbGeometry, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<WellboreGeometryList, StandAloneWellboreGeometry>(wbGeometry, errorCode);
        }

        /// <summary>
        /// Deletes the well and test the return code
        /// </summary>
        /// <param name="well">The well.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Well well, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<WellList, Well>(well, errorCode, partialDelete);
        }

        /// <summary>
        /// Deletes the wellbore and test the return code
        /// </summary>
        /// <param name="wellbore">The wellbore.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Wellbore wellbore, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<WellboreList, Wellbore>(wellbore, errorCode, partialDelete);
        }

        /// <summary>
        /// Deletes the log and test the return code
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Log log, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<LogList, Log>(log, errorCode, partialDelete);
        }

        /// <summary>
        /// Deletes the message and test the return code
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Message message, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<MessageList, Message>(message, errorCode, partialDelete);
        }

        /// <summary>
        /// Deletes the trajectory and test the return code
        /// </summary>
        /// <param name="trajectory">The trajectory.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Trajectory trajectory, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<TrajectoryList, Trajectory>(trajectory, errorCode, partialDelete);
        }

        /// <summary>
        /// Deletes the rig and test the return code
        /// </summary>
        /// <param name="rig">The rig.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Rig rig, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<RigList, Rig>(rig, errorCode, partialDelete);
        }

        /// <summary>
        /// Deletes the wbGeometry and test the return code
        /// </summary>
        /// <param name="wbGeometry">The wbGeometry.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(StandAloneWellboreGeometry wbGeometry, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<WellboreGeometryList, StandAloneWellboreGeometry>(wbGeometry, errorCode, partialDelete);
        }

        /// <summary>
        /// Deletes the attachment and test the return code
        /// </summary>
        /// <param name="attachment">The attachment.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Attachment attachment, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<AttachmentList, Attachment>(attachment, errorCode, partialDelete);
        }

        public void AssertChangeHistoryTimesUnique(object entity)
        {
            var dataObject = entity as IDataObject;
            var changeLogQuery = CreateChangeLog(dataObject.GetUri());
            var changeLog = QueryAndAssert<ChangeLogList, ChangeLog>(changeLogQuery);

            Assert.IsNotNull(changeLog);
            Assert.IsNotNull(changeLog.ChangeHistory);

            var dupCount = changeLog.ChangeHistory.GroupBy(c => c.DateTimeChange).Count(grp => grp.Count() > 1);
            Assert.AreEqual(0, dupCount);
        }

        public ChangeLog AssertChangeLog(object entity, int expectedHistoryCount, ChangeInfoType expectedChangeType)
        {
            var dataObject = entity as IDataObject;
            var commonDataObject = entity as ICommonDataObject;
            var commonData = commonDataObject?.CommonData;

            // Assert that the entity is not null and has a UID
            Assert.IsNotNull(dataObject?.Uid);

            // Fetch the changeLog for the entity just added
            var changeLogQuery = CreateChangeLog(dataObject.GetUri());
            var changeLog = QueryAndAssert<ChangeLogList, ChangeLog>(changeLogQuery);

            var changeHistory = changeLog.ChangeHistory.LastOrDefault();

            // Verify that we found a changeHistory for the latest change.
            Assert.IsNotNull(changeHistory);

            // Assert that the entity has CommonData with a DateTimeLastChange
            // The SourceName of the change log MUST match the Source name of the entity
            if (expectedChangeType != ChangeInfoType.delete)
            {
                Assert.IsNotNull(commonData);
                Assert.IsTrue(commonData.DateTimeLastChange.HasValue);
                Assert.AreEqual(commonData.SourceName, changeLog.SourceName);
            }

            // Verify that the LastChangeType exists and matches the expected changeType
            Assert.IsTrue(changeLog.LastChangeType.HasValue);
            Assert.AreEqual(expectedChangeType, changeLog.LastChangeType.Value);

            // Verify that there is changeHistory
            Assert.AreEqual(expectedHistoryCount, changeLog.ChangeHistory.Count);

            // The LastChangeType of the changeLog MUST match the ChangeType of the last changeHistory added
            Assert.AreEqual(changeLog.LastChangeType, changeHistory.ChangeType);

            // The LastChangeInfo of the changeLog MUST match the ChangeInfo of the last changeHistory added
            Assert.AreEqual(changeLog.LastChangeInfo, changeHistory.ChangeInfo);

            // If the entity was deleted then we don't have a DateTimeLastChange to compare
            if (expectedChangeType != ChangeInfoType.delete)
            {
                // Verify that the changeHistory has a DateTimeLastChange and it matches
                //... the entity DateTimeLastChange
                Assert.IsTrue(changeHistory.DateTimeChange.HasValue);
                Assert.AreEqual(commonData.DateTimeLastChange.Value, changeHistory.DateTimeChange.Value);
            }

            return changeLog;
        }

        public void AssertChangeLogNames(object entity)
        {
            var dataObject = entity as IDataObject;
            var wellObject = entity as IWellObject;
            var wellboreObject = entity as IWellboreObject;

            var changeLogQuery = CreateChangeLog(dataObject.GetUri());
            var changeLog = QueryAndAssert<ChangeLogList, ChangeLog>(changeLogQuery);

            Assert.AreEqual(wellObject?.NameWell, changeLog.NameWell);
            Assert.AreEqual(wellboreObject?.NameWellbore, changeLog.NameWellbore);
            Assert.AreEqual(dataObject?.Name, changeLog.NameObject);
        }

        public List<ChangeHistory> GetAndAssertChangeLogHistory(EtpUri uri, bool returnLatestChangeHistoryOnly = true)
        {
            var changeLog = QueryAndAssert<ChangeLogList, ChangeLog>(CreateChangeLog(uri));
            Assert.IsNotNull(changeLog.ChangeHistory);

            if (returnLatestChangeHistoryOnly)
            {
                var changeHistory = changeLog.ChangeHistory.LastOrDefault();
                Assert.IsNotNull(changeHistory);
                return new List<ChangeHistory> { changeHistory };
            }

            return changeLog.ChangeHistory;
        }

        public void AssertChangeHistoryFlags(ChangeHistory changeHistory, bool updateHeaderTrue, bool objectGrowingTrue)
        {
            Assert.IsNotNull(changeHistory);
            Assert.AreEqual(updateHeaderTrue, changeHistory.UpdatedHeader.GetValueOrDefault(), "updatedHeader");
            Assert.AreEqual(objectGrowingTrue, changeHistory.ObjectGrowingState.GetValueOrDefault(), "objectGrowingState");
        }

        public void AssertChangeHistoryIndexRange(ChangeHistory changeHistory, double startIndexRange, double endIndexRange)
        {
            Assert.IsNotNull(changeHistory);
            Assert.IsNotNull(changeHistory.StartIndex);
            Assert.IsNotNull(changeHistory.EndIndex);
            Assert.AreEqual(startIndexRange, changeHistory.StartIndex.Value);
            Assert.AreEqual(endIndexRange, changeHistory.EndIndex.Value);
        }

        public void AssertChangeHistoryIndexRange(ChangeHistory changeHistory, Timestamp startIndexRange, Timestamp endIndexRange)
        {
            Assert.IsNotNull(changeHistory);
            Assert.IsNotNull(changeHistory.StartDateTimeIndex);
            Assert.IsNotNull(changeHistory.EndDateTimeIndex);
            Assert.AreEqual(startIndexRange.ToUnixTimeMicroseconds(), changeHistory.StartDateTimeIndex.GetValueOrDefault().ToUnixTimeMicroseconds());
            Assert.AreEqual(endIndexRange.ToUnixTimeMicroseconds(), changeHistory.EndDateTimeIndex.GetValueOrDefault().ToUnixTimeMicroseconds());
        }

        public void AssertChangeLogMnemonics(string[] logMnemonics, string changeMnemonics)
        {
            var changLogMnemonics = changeMnemonics.Split(',').ToArray();
            CollectionAssert.AreEqual(logMnemonics, changLogMnemonics);
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

        public WMLS_AddToStoreResponse AddValidAcquisition(Well well)
        {
            well.CommonData = new CommonData
            {
                AcquisitionTimeZone = new List<TimestampedTimeZone>()
                {
                    new TimestampedTimeZone() {DateTimeSpecified = false, Value = "+01:00"},
                    new TimestampedTimeZone() {DateTimeSpecified = true, DateTime = DateTime.UtcNow, Value = "+02:00"},
                    new TimestampedTimeZone() {DateTimeSpecified = true, DateTime = DateTime.UtcNow, Value = "+03:00"}
                }
            };

            return AddAndAssert(well);
        }

        public string[] GetMnemonics(Log log)
        {
            return log.LogCurveInfo
                    .Select(x => x.Mnemonic.Value)
                    .ToArray();
        }

        public string[] GetNonIndexMnemonics(Log log)
        {
            return log.LogCurveInfo.Where(x => x.Mnemonic.Value != log.IndexCurve)
                    .Select(x => x.Mnemonic.Value)
                    .ToArray();
        }

        public void AddListOfTrajectoriesToWellbore(List<Trajectory> trajectories, Wellbore wellbore)
        {
            trajectories.ForEach(x =>
            {
                x.UidWellbore = wellbore.Uid;
                x.NameWellbore = wellbore.Name;
                AddAndAssert(x);
            });
        }

        public void AddListOfLogsToWellbore(List<Log> logs, Wellbore wellbore)
        {
            logs.ForEach(x =>
            {
                x.UidWellbore = wellbore.Uid;
                x.NameWellbore = wellbore.Name;
                AddAndAssert(x);
            });
        }

        public List<Trajectory> GenerateTrajectories(string wellUid, string wellName, int numOfObjects, int numOfStations = 100)
        {
            var trajectories = new List<Trajectory>();
            for (var i = 0; i < numOfObjects; i++)
            {
                var trajectory = new Trajectory
                {
                    Uid = Uid(),
                    Name = Name(),
                    UidWell = wellUid,
                    NameWell = wellName,
                    TrajectoryStation = TrajectoryGenerator.GenerationStations(numOfStations, 0)
                };
                trajectories.Add(trajectory);
            }

            return trajectories;
        }

        public List<Log> GenerateLogs(string wellUid, string wellName, LogIndexType indexType, int numOfObjects, int numOfRows = 1000)
        {
            var logs = new List<Log>();
            for (var i = 0; i < numOfObjects; i++)
            {
                var log = CreateLog(Uid(), Name(), wellUid, wellName, string.Empty, string.Empty);
                log.IndexType = indexType;
                log.IndexCurve = "MD";
                InitHeader(log, log.IndexType.GetValueOrDefault());
                InitDataMany(log, string.Join(",", log.LogCurveInfo.Select(x => x.Mnemonic.Value)),
                    string.Join(",", log.LogCurveInfo.Select(x => x.Unit)), numOfRows);
                logs.Add(log);
            }

            return logs;
        }

        public IndexedObject IndexedObject(int id, short index) => new IndexedObject()
        {
            Uid = Uid(),
            Description = $"Test param{id}",
            Index = index,
            Name = $"Test{id}",
            Uom = "m",
            Value = $"1{id}.0"
        };
    }
}
