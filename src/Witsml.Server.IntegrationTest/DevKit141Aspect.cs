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
        public DevKit141Aspect(string url) : base(url, WMLSVersion.WITSML141)
        {
        }

        public override string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version141.Value; }
        }

        public void InitHeader(Log log, LogIndexType indexType)
        {
            log.IndexType = indexType;
            log.IndexCurve = indexType == LogIndexType.datetime ? "TIME" : "MD";

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

        public void InitDataMany(Log log, string mnemonics, string units, int numRows, double factor = 1.0)
        {
            for (int i = 0; i < numRows; i++)
            {
                InitData(log, mnemonics, units, i, null, i * factor);
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
    }
}
