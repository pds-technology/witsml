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
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Etp.Common.Datatypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.Logs;
using PDS.WITSMLstudio.Data.MudLogs;
using PDS.WITSMLstudio.Data.Trajectories;

namespace PDS.WITSMLstudio.Store
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

        public DevKit141Aspect(TestContext context) : this(context, null)
        {
        }

        public DevKit141Aspect(TestContext context, string url = null) : base(url, WMLSVersion.WITSML141, context)
        {
            LogGenerator = new Log141Generator();
            TrajectoryGenerator = new Trajectory141Generator();
            MudLogGenerator = new MudLog141Generator();
        }

        public Log141Generator LogGenerator { get; }

        public Trajectory141Generator TrajectoryGenerator { get; }

        public MudLog141Generator MudLogGenerator { get; }

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
                PresBopRating = new PressureMeasure { Uom = PressureUom.Pa, Value = 1 },
                SizeBopSys = new LengthMeasure { Uom = LengthUom.cm, Value = 20 },
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
            else if (ObjectTypes.Wellbore.EqualsIgnoreCase(uri.ObjectType) ||
                ObjectTypes.DownholeComponent.EqualsIgnoreCase(uri.ObjectType) ||
                ObjectTypes.WellCompletion.Equals(uri.ObjectType))
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
        /// Does get query for single attachment object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="attachment">the attachment with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first attachment from the response</returns>
        public Attachment GetAndAssert(Attachment attachment, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<AttachmentList, Attachment>(attachment, isNotNull, optionsIn, queryByExample);
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
        /// Does UpdateInStore on attachment object and test the return code
        /// </summary>
        /// <param name="attachment">the attachment</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(Attachment attachment, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<AttachmentList, Attachment>(attachment, errorCode);
        }

        /// <summary>
        /// Deletes attachment object and test the return code
        /// </summary>
        /// <param name="attachment">the attachment</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(Attachment attachment, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<AttachmentList, Attachment>(attachment, errorCode, partialDelete);
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
        /// Adds changeLog object and test the return code
        /// </summary>
        /// <param name="changeLog">the changeLog</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(ChangeLog changeLog, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<ChangeLogList, ChangeLog>(changeLog, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on changeLog object and test the return code
        /// </summary>
        /// <param name="changeLog">the changeLog</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(ChangeLog changeLog, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<ChangeLogList, ChangeLog>(changeLog, errorCode);
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
        /// Does get query for single depthRegImage object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="depthRegImage">the depthRegImage with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first depthRegImage from the response</returns>
        public DepthRegImage GetAndAssert(DepthRegImage depthRegImage, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<DepthRegImageList, DepthRegImage>(depthRegImage, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds depthRegImage object and test the return code
        /// </summary>
        /// <param name="depthRegImage">the depthRegImage</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(DepthRegImage depthRegImage, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<DepthRegImageList, DepthRegImage>(depthRegImage, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on depthRegImage object and test the return code
        /// </summary>
        /// <param name="depthRegImage">the depthRegImage</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(DepthRegImage depthRegImage, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<DepthRegImageList, DepthRegImage>(depthRegImage, errorCode);
        }

        /// <summary>
        /// Deletes depthRegImage object and test the return code
        /// </summary>
        /// <param name="depthRegImage">the depthRegImage</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(DepthRegImage depthRegImage, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<DepthRegImageList, DepthRegImage>(depthRegImage, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single drillReport object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="drillReport">the drillReport with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first drillReport from the response</returns>
        public DrillReport GetAndAssert(DrillReport drillReport, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<DrillReportList, DrillReport>(drillReport, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds drillReport object and test the return code
        /// </summary>
        /// <param name="drillReport">the drillReport</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(DrillReport drillReport, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<DrillReportList, DrillReport>(drillReport, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on drillReport object and test the return code
        /// </summary>
        /// <param name="drillReport">the drillReport</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(DrillReport drillReport, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<DrillReportList, DrillReport>(drillReport, errorCode);
        }

        /// <summary>
        /// Deletes drillReport object and test the return code
        /// </summary>
        /// <param name="drillReport">the drillReport</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(DrillReport drillReport, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<DrillReportList, DrillReport>(drillReport, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single downholeComponent object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="downholeComponent">the downholeComponent with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first downholeComponent from the response</returns>
        public DownholeComponent GetAndAssert(DownholeComponent downholeComponent, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<DownholeComponentList, DownholeComponent>(downholeComponent, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds downholeComponent object and test the return code
        /// </summary>
        /// <param name="downholeComponent">the downholeComponent</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(DownholeComponent downholeComponent, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<DownholeComponentList, DownholeComponent>(downholeComponent, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on downholeComponent object and test the return code
        /// </summary>
        /// <param name="downholeComponent">the downholeComponent</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(DownholeComponent downholeComponent, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<DownholeComponentList, DownholeComponent>(downholeComponent, errorCode);
        }

        /// <summary>
        /// Deletes downholeComponent object and test the return code
        /// </summary>
        /// <param name="downholeComponent">the downholeComponent</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(DownholeComponent downholeComponent, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<DownholeComponentList, DownholeComponent>(downholeComponent, errorCode, partialDelete);
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
        /// <param name="errorCode">The expected error code.</param>
        /// <returns>The first log from the response</returns>
        public Log GetAndAssert(Log log, bool isNotNull = true, string optionsIn = null, bool queryByExample = false, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return GetAndAssert<LogList, Log>(log, isNotNull, optionsIn, queryByExample, errorCode);
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
        /// Does get query for single objectGroup object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="objectGroup">the objectGroup with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first objectGroup from the response</returns>
        public ObjectGroup GetAndAssert(ObjectGroup objectGroup, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<ObjectGroupList, ObjectGroup>(objectGroup, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds objectGroup object and test the return code
        /// </summary>
        /// <param name="objectGroup">the objectGroup</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(ObjectGroup objectGroup, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<ObjectGroupList, ObjectGroup>(objectGroup, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on objectGroup object and test the return code
        /// </summary>
        /// <param name="objectGroup">the objectGroup</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(ObjectGroup objectGroup, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<ObjectGroupList, ObjectGroup>(objectGroup, errorCode);
        }

        /// <summary>
        /// Deletes objectGroup object and test the return code
        /// </summary>
        /// <param name="objectGroup">the objectGroup</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(ObjectGroup objectGroup, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<ObjectGroupList, ObjectGroup>(objectGroup, errorCode, partialDelete);
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
        /// Does get query for single stimJob object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="stimJob">the stimJob with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first stimJob from the response</returns>
        public StimJob GetAndAssert(StimJob stimJob, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<StimJobList, StimJob>(stimJob, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds stimJob object and test the return code
        /// </summary>
        /// <param name="stimJob">the stimJob</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(StimJob stimJob, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<StimJobList, StimJob>(stimJob, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on stimJob object and test the return code
        /// </summary>
        /// <param name="stimJob">the stimJob</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(StimJob stimJob, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<StimJobList, StimJob>(stimJob, errorCode);
        }

        /// <summary>
        /// Deletes stimJob object and test the return code
        /// </summary>
        /// <param name="stimJob">the stimJob</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(StimJob stimJob, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<StimJobList, StimJob>(stimJob, errorCode, partialDelete);
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
        /// Does get query for single toolErrorModel object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="toolErrorModel">the toolErrorModel with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first toolErrorModel from the response</returns>
        public ToolErrorModel GetAndAssert(ToolErrorModel toolErrorModel, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<ToolErrorModelList, ToolErrorModel>(toolErrorModel, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds toolErrorModel object and test the return code
        /// </summary>
        /// <param name="toolErrorModel">the toolErrorModel</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(ToolErrorModel toolErrorModel, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<ToolErrorModelList, ToolErrorModel>(toolErrorModel, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on toolErrorModel object and test the return code
        /// </summary>
        /// <param name="toolErrorModel">the toolErrorModel</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(ToolErrorModel toolErrorModel, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<ToolErrorModelList, ToolErrorModel>(toolErrorModel, errorCode);
        }

        /// <summary>
        /// Deletes toolErrorModel object and test the return code
        /// </summary>
        /// <param name="toolErrorModel">the toolErrorModel</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(ToolErrorModel toolErrorModel, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<ToolErrorModelList, ToolErrorModel>(toolErrorModel, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single toolErrorTermSet object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="toolErrorTermSet">the toolErrorTermSet with UIDs for well and wellbore</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first toolErrorTermSet from the response</returns>
        public ToolErrorTermSet GetAndAssert(ToolErrorTermSet toolErrorTermSet, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<ToolErrorTermSetList, ToolErrorTermSet>(toolErrorTermSet, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds toolErrorTermSet object and test the return code
        /// </summary>
        /// <param name="toolErrorTermSet">the toolErrorTermSet</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(ToolErrorTermSet toolErrorTermSet, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<ToolErrorTermSetList, ToolErrorTermSet>(toolErrorTermSet, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on toolErrorTermSet object and test the return code
        /// </summary>
        /// <param name="toolErrorTermSet">the toolErrorTermSet</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(ToolErrorTermSet toolErrorTermSet, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<ToolErrorTermSetList, ToolErrorTermSet>(toolErrorTermSet, errorCode);
        }

        /// <summary>
        /// Deletes toolErrorTermSet object and test the return code
        /// </summary>
        /// <param name="toolErrorTermSet">the toolErrorTermSet</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(ToolErrorTermSet toolErrorTermSet, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<ToolErrorTermSetList, ToolErrorTermSet>(toolErrorTermSet, errorCode, partialDelete);
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
        /// Does get query for single wellboreCompletion object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="wellboreCompletion">the wellboreCompletion with UIDs for well and wellboreCompletion</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first wellboreCompletion from the response</returns>
        public WellboreCompletion GetAndAssert(WellboreCompletion wellboreCompletion, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<WellboreCompletionList, WellboreCompletion>(wellboreCompletion, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds wellboreCompletion object and test the return code
        /// </summary>
        /// <param name="wellboreCompletion">the wellboreCompletion</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(WellboreCompletion wellboreCompletion, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<WellboreCompletionList, WellboreCompletion>(wellboreCompletion, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on wellboreCompletion object and test the return code
        /// </summary>
        /// <param name="wellboreCompletion">the wellboreCompletion</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(WellboreCompletion wellboreCompletion, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<WellboreCompletionList, WellboreCompletion>(wellboreCompletion, errorCode);
        }

        /// <summary>
        /// Deletes wellboreCompletion object and test the return code
        /// </summary>
        /// <param name="wellboreCompletion">the wellboreCompletion</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(WellboreCompletion wellboreCompletion, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<WellboreCompletionList, WellboreCompletion>(wellboreCompletion, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single wellCMLedger object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="wellCMLedger">the wellCMLedger with UIDs for well and wellCMLedger</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first wellCMLedger from the response</returns>
        public WellCMLedger GetAndAssert(WellCMLedger wellCMLedger, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<WellCMLedgerList, WellCMLedger>(wellCMLedger, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds wellCMLedger object and test the return code
        /// </summary>
        /// <param name="wellCMLedger">the wellCMLedger</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(WellCMLedger wellCMLedger, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<WellCMLedgerList, WellCMLedger>(wellCMLedger, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on wellCMLedger object and test the return code
        /// </summary>
        /// <param name="wellCMLedger">the wellCMLedger</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(WellCMLedger wellCMLedger, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<WellCMLedgerList, WellCMLedger>(wellCMLedger, errorCode);
        }

        /// <summary>
        /// Deletes wellCMLedger object and test the return code
        /// </summary>
        /// <param name="wellCMLedger">the wellCMLedger</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(WellCMLedger wellCMLedger, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<WellCMLedgerList, WellCMLedger>(wellCMLedger, errorCode, partialDelete);
        }

        /// <summary>
        /// Does get query for single wellCompletion object and test for result count equal to 1 and is not null
        /// </summary>
        /// <param name="wellCompletion">the wellCompletion with UIDs for well and wellCompletion</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <returns>The first wellCompletion from the response</returns>
        public WellCompletion GetAndAssert(WellCompletion wellCompletion, bool isNotNull = true, string optionsIn = null, bool queryByExample = false)
        {
            return GetAndAssert<WellCompletionList, WellCompletion>(wellCompletion, isNotNull, optionsIn, queryByExample);
        }

        /// <summary>
        /// Adds wellCompletion object and test the return code
        /// </summary>
        /// <param name="wellCompletion">the wellCompletion</param>
        /// <param name="errorCode">the errorCode</param>
        public WMLS_AddToStoreResponse AddAndAssert(WellCompletion wellCompletion, ErrorCodes errorCode = ErrorCodes.Success)
        {
            return AddAndAssert<WellCompletionList, WellCompletion>(wellCompletion, errorCode);
        }

        /// <summary>
        /// Does UpdateInStore on wellCompletion object and test the return code
        /// </summary>
        /// <param name="wellCompletion">the wellCompletion</param>
        /// <param name="errorCode">the errorCode</param>
        public void UpdateAndAssert(WellCompletion wellCompletion, ErrorCodes errorCode = ErrorCodes.Success)
        {
            UpdateAndAssert<WellCompletionList, WellCompletion>(wellCompletion, errorCode);
        }

        /// <summary>
        /// Deletes wellCompletion object and test the return code
        /// </summary>
        /// <param name="wellCompletion">the wellCompletion</param>
        /// <param name="errorCode">the errorCode</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert(WellCompletion wellCompletion, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false)
        {
            DeleteAndAssert<WellCompletionList, WellCompletion>(wellCompletion, errorCode, partialDelete);
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
        /// Asserts the change history times unique.
        /// </summary>
        /// <param name="entity">The entity.</param>
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

        /// <summary>
        /// Asserts the change log.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="expectedHistoryCount">The expected history count.</param>
        /// <param name="expectedChangeType">Expected type of the change.</param>
        /// <returns></returns>
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

        public WMLS_AddToStoreResponse Add_MudLog_from_file(string xmlfile)
        {
            var xmlin = File.ReadAllText(xmlfile);

            var mudLogList = EnergisticsConverter.XmlToObject<MudLogList>(xmlin);
            Assert.IsNotNull(mudLogList);
            Assert.IsTrue(mudLogList.MudLog.Count > 0);

            var mudLog = new MudLog() { Uid = mudLogList.MudLog[0].Uid, UidWell = mudLogList.MudLog[0].UidWell, UidWellbore = mudLogList.MudLog[0].UidWellbore };
            var result = Query<MudLogList, MudLog>(mudLog);
            Assert.IsNotNull(result);
            if (result.Count > 0)
            {
                // Do not add if the mudLog already exists.
                return null;
            }

            var response = AddToStore(ObjectTypes.MudLog, xmlin, null, null);
            Assert.IsNotNull(response);
            return response;
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
    }
}
