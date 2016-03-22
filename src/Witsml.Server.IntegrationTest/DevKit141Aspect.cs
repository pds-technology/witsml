using System;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;

namespace PDS.Witsml.Server
{
    public class DevKit141Aspect : DevKitAspect
    {
        public DevKit141Aspect(string url = null) : base(url, WMLSVersion.WITSML141)
        {
        }

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

            log.LogCurveInfo.Add(
                new LogCurveInfo()
                {
                    Mnemonic = new ShortNameStruct(log.IndexCurve),
                    TypeLogData = indexType == LogIndexType.datetime ? LogDataType.datetime : LogDataType.@double,
                    Unit = indexType == LogIndexType.datetime ? "s" : "m"
                });

            log.LogCurveInfo.Add(
                new LogCurveInfo()
                {
                    Mnemonic = new ShortNameStruct("ROP"),
                    TypeLogData = LogDataType.@double,
                    Unit = "m/h"
                });

            log.LogCurveInfo.Add(
                new LogCurveInfo()
                {
                    Mnemonic = new ShortNameStruct("GR"),
                    TypeLogData = LogDataType.@double,
                    Unit = "gAPI"
                });

            InitData(log, Mnemonics(log), Units(log));
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
                log.LogData[0].Data.Add(String.Join(",", values.Select(x => x == null ? string.Empty : x)));
            }
        }

        public void InitDataMany(Log log, string mnemonics, string units, int numRows, double factor = 1.0, bool isDepthLog = true, bool hasEmptyChannel = true, bool increasing = true)
        {
            var start = DateTimeOffset.UtcNow.AddDays(-1);
            var interval = increasing ? 1 : -1;

            for (int i = 0; i < numRows; i++)
            {
                if (isDepthLog)
                {
                    InitData(log, mnemonics, units, i * interval, hasEmptyChannel ? (int?)null : i, i * factor);
                }
                else
                {
                    InitData(log, mnemonics, units, start.AddSeconds(i).ToString(), hasEmptyChannel ? (int?)null : i, i * factor);
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

        public WellCRS WellCRS(string uid, string name, string description = null)
        {
            return new WellCRS
            {
                Uid = uid,
                Name = name,
                Description = description
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

        public Well CreateFullWell()
        {
            string wellXml = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
            "<well>" + Environment.NewLine +
            "<name>" + Name("Test Full Well") + " </name>" + Environment.NewLine +
            "<nameLegal>Company Legal Name</nameLegal>" + Environment.NewLine +
            "<numLicense>Company License Number</numLicense>" + Environment.NewLine +
            "<numGovt>Govt-Number</numGovt>" + Environment.NewLine +
            "<dTimLicense>2001-05-15T13:20:00Z</dTimLicense>" + Environment.NewLine +
            "<field>Big Field</field>" + Environment.NewLine +
            "<country>US</country>" + Environment.NewLine +
            "<state>TX</state>" + Environment.NewLine +
            "<county>Montgomery</county>" + Environment.NewLine +
            "<region>Region Name</region>" + Environment.NewLine +
            "<district>District Name</district>" + Environment.NewLine +
            "<block>Block Name</block>" + Environment.NewLine +
            "<timeZone>-06:00</timeZone>" + Environment.NewLine +
            "<operator>Operating Company</operator>" + Environment.NewLine +
            "<operatorDiv>Division Name</operatorDiv>" + Environment.NewLine +
            "<pcInterest uom=\"%\">65</pcInterest>" + Environment.NewLine +
            "<numAPI>123-543-987AZ</numAPI>" + Environment.NewLine +
            "<statusWell>drilling</statusWell>" + Environment.NewLine +
            "<purposeWell>exploration</purposeWell>" + Environment.NewLine +
            "<fluidWell>water</fluidWell>" + Environment.NewLine +
            "<dTimSpud>2001-05-31T08:15:00Z</dTimSpud>" + Environment.NewLine +
            "<dTimPa>2001-07-15T15:30:00Z</dTimPa>" + Environment.NewLine +
            "<wellheadElevation uom=\"ft\">500</wellheadElevation>" + Environment.NewLine +
            "<wellDatum uid=\"KB\">" + Environment.NewLine +
            "<name>Kelly Bushing</name>" + Environment.NewLine +
            "<code>KB</code>" + Environment.NewLine +
            "<elevation uom=\"ft\" datum=\"SL\">78.5</elevation>" + Environment.NewLine +
            "</wellDatum>" + Environment.NewLine +
            "<wellDatum uid=\"SL\">" + Environment.NewLine +
            "<name>Sea Level</name>" + Environment.NewLine +
            "<code>SL</code>" + Environment.NewLine +
            "<datumName namingSystem=\"EPSG\" code=\"5106\">Caspian Sea</datumName>" + Environment.NewLine +
            "</wellDatum>" + Environment.NewLine +
            "<groundElevation uom=\"ft\">250</groundElevation>" + Environment.NewLine +
            "<waterDepth uom=\"ft\">520</waterDepth>" + Environment.NewLine +
            "<wellLocation uid=\"loc-1\">" + Environment.NewLine +
            "<wellCRS uidRef=\"proj1\">ED50 / UTM Zone 31N</wellCRS>" + Environment.NewLine +
            "<easting uom=\"m\">425353.84</easting>" + Environment.NewLine +
            "<northing uom=\"m\">6623785.69</northing>" + Environment.NewLine +
            "<description>Location of well surface point in projected system.</description>" + Environment.NewLine +
            "</wellLocation>" + Environment.NewLine +
            "<referencePoint uid=\"SRP1\">" + Environment.NewLine +
            "<name>Slot Bay Centre</name>" + Environment.NewLine +
            "<type>Site Reference Point</type>" + Environment.NewLine +
            "<location uid=\"loc-1\">" + Environment.NewLine +
            "<wellCRS uidRef=\"proj1\">ED50 / UTM Zone 31N</wellCRS>" + Environment.NewLine +
            "<easting uom=\"m\">425366.47</easting>" + Environment.NewLine +
            "<northing uom=\"m\">6623781.95</northing>" + Environment.NewLine +
            "</location>" + Environment.NewLine +
            "<location uid=\"loc-2\">" + Environment.NewLine +
            "<wellCRS uidRef=\"localWell1\">WellOneWSP</wellCRS>" + Environment.NewLine +
            "<localX uom=\"m\">12.63</localX>" + Environment.NewLine +
            "<localY uom=\"m\">-3.74</localY>" + Environment.NewLine +
            "<description>Location of the Site Reference Point with respect to the well surface point</description>" + Environment.NewLine +
            "</location>" + Environment.NewLine +
            "</referencePoint>" + Environment.NewLine +
            "<referencePoint uid=\"WRP2\">" + Environment.NewLine +
            "<name>Sea Bed</name>" + Environment.NewLine +
            "<type>Well Reference Point</type>" + Environment.NewLine +
            "<elevation uom=\"ft\" datum=\"SL\">-118.4</elevation>" + Environment.NewLine +
            "<measuredDepth uom=\"ft\" datum=\"KB\">173.09</measuredDepth>" + Environment.NewLine +
            "<location uid=\"loc-1\">" + Environment.NewLine +
            "<wellCRS uidRef=\"proj1\">ED50 / UTM Zone 31N</wellCRS>" + Environment.NewLine +
            "<easting uom=\"m\">425353.84</easting>" + Environment.NewLine +
            "<northing uom=\"m\">6623785.69</northing>" + Environment.NewLine +
            "</location>" + Environment.NewLine +
            "<location uid=\"loc-2\">" + Environment.NewLine +
            "<wellCRS uidRef=\"geog1\">ED50</wellCRS>" + Environment.NewLine +
            "<latitude uom=\"dega\">59.743844</latitude>" + Environment.NewLine +
            "<longitude uom=\"dega\">1.67198083</longitude>" + Environment.NewLine +
            "</location>" + Environment.NewLine +
            "</referencePoint>" + Environment.NewLine +
            "<wellCRS uid=\"geog1\">" + Environment.NewLine +
            "<name>ED50</name>" + Environment.NewLine +
            "<geodeticCRS uidRef=\"4230\">4230</geodeticCRS>" + Environment.NewLine +
            "<description>ED50 system with EPSG code 4230.</description>" + Environment.NewLine +
            "</wellCRS>" + Environment.NewLine +
            "<wellCRS uid=\"proj1\">" + Environment.NewLine +
            "<name>ED50 / UTM Zone 31N</name>" + Environment.NewLine +
            "<mapProjectionCRS uidRef=\"23031\">ED50 / UTM Zone 31N</mapProjectionCRS>" + Environment.NewLine +
            "</wellCRS>" + Environment.NewLine +
            "<wellCRS uid=\"localWell1\">" + Environment.NewLine +
            "<name>WellOneWSP</name>" + Environment.NewLine +
            "<localCRS>" + Environment.NewLine +
            "<usesWellAsOrigin>true</usesWellAsOrigin>" + Environment.NewLine +
            "<yAxisAzimuth uom=\"dega\" northDirection=\"grid north\">0</yAxisAzimuth>" + Environment.NewLine +
            "<xRotationCounterClockwise>false</xRotationCounterClockwise>" + Environment.NewLine +
            "</localCRS>" + Environment.NewLine +
            "</wellCRS>" + Environment.NewLine +
            "<commonData>" + Environment.NewLine +
            "<dTimCreation>2016-03-07T22:53:59.249Z</dTimCreation>" + Environment.NewLine +
            "<dTimLastChange>2016-03-07T22:53:59.249Z</dTimLastChange > " + Environment.NewLine +
            "<itemState>plan</itemState>" + Environment.NewLine +
            "<comments>These are the comments associated with the Well data object.</comments>" + Environment.NewLine +
            "<defaultDatum uidRef=\"KB\">Kelly Bushing</defaultDatum>" + Environment.NewLine +
            "</commonData>" + Environment.NewLine +
            "</well>" + Environment.NewLine +
            "</wells>";

            WellList wells = EnergisticsConverter.XmlToObject<WellList>(wellXml);
            return wells.Items[0] as Well;
        }
    }
}
