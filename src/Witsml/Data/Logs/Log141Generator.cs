using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using PDS.Framework;

namespace PDS.Witsml.Data.Logs
{
    public class Log141Generator : DataGenerator
    {
        public readonly LogIndexType[] DepthIndexTypes = new LogIndexType[] { LogIndexType.length, LogIndexType.measureddepth, LogIndexType.verticaldepth };
        public readonly LogIndexType[] TimeIndexTypes = new LogIndexType[] { LogIndexType.datetime, LogIndexType.elapsedtime };
        public readonly LogIndexType[] OtherIndexTypes = new LogIndexType[] { LogIndexType.other, LogIndexType.unknown };

        public LogCurveInfo CreateDateTimeLogCurveInfo(string name, string unit)
        {
            return CreateLogCurveInfo(name, unit, LogDataType.datetime);
        }

        public LogCurveInfo CreateDoubleLogCurveInfo(string name, string unit)
        {
            return CreateLogCurveInfo(name, unit, LogDataType.@double);
        }

        public LogCurveInfo CreateStringLogCurveInfo(string name, string unit)
        {
            return CreateLogCurveInfo(name, unit, LogDataType.@string);
        }

        public LogCurveInfo CreateLogCurveInfo(string name, string unit, LogDataType type)
        {
            return new LogCurveInfo()
            {
                Mnemonic = new ShortNameStruct(name),
                TypeLogData = type,
                Unit = unit
            };
        }

        public string Units(List<LogCurveInfo> infoList)
        {
            return infoList != null
                ? String.Join(",", infoList.Select(x => x.Unit ?? string.Empty))
                : string.Empty;
        }

        public string Mnemonics(List<LogCurveInfo> infoList)
        {
            return infoList != null
                ? String.Join(",", infoList.Select(x => x.Mnemonic))
                : string.Empty;
        }

        public List<string> GenerateLogData(LogIndexType indexType, List<LogCurveInfo> logCurveInfoList, LogIndexDirection direction, int numOfRows = 5)
        {           
            const int Seed = 123;

            Random random = new Random(Seed);
            DateTime dateTimeStart = DateTime.Now.ToUniversalTime();
            double interval = direction == LogIndexDirection.decreasing ? -1.0 : 1.0;

            List<string> data = new List<string>();

            for (int i = 0; i < numOfRows; i++)
            {
                string row = string.Empty;

                // index value
                switch (indexType)
                {
                    case LogIndexType.datetime:
                        {
                            dateTimeStart = dateTimeStart.AddSeconds(random.Next(1, 5));
                            row += "\"" + dateTimeStart.AddSeconds(1.0).ToString("o") + "\"";
                            break;
                        }
                    case LogIndexType.elapsedtime:                       
                    case LogIndexType.length:                        
                    case LogIndexType.measureddepth:                        
                    case LogIndexType.verticaldepth:
                        {
                            row += string.Format("{0:F3}", i * interval);
                            break;
                        }
                    default:
                        break;
                }

                
                // channel values
                for (int k = 1; k < logCurveInfoList.Count; k++)
                {
                    row += ", ";

                    if (random.Next(1, 10) == 2)
                    {
                        continue;
                    }

                    switch (logCurveInfoList[k].TypeLogData)
                    {
                        case LogDataType.@byte:
                            {
                                row += "Y";
                                break;
                            }
                        case LogDataType.datetime:
                            {
                                dateTimeStart = dateTimeStart.AddSeconds(random.Next(1, 5));
                                row += "\"" + dateTimeStart.AddSeconds(1.0).ToString("o") + "\"";
                                break;
                            }
                        case LogDataType.@double:
                        case LogDataType.@float:                       
                            {
                                row += random.NextDouble().ToString("N3");
                                break;
                            }
                        case LogDataType.@int:
                        case LogDataType.@long:
                        case LogDataType.@short:
                            {
                                row += random.Next(1, 10);
                                break;
                            }
                        case LogDataType.@string:
                            {
                                row += "abc";
                                break;
                            }
                        default:
                            {
                                row += "null";
                            }
                            break;
                    }
                }
                data.Add(row);
            }

            return data;
        }

        public Log CreateLog(Log log, LogIndexDirection direction, int numOfRows = 5)
        {
            log.LogCurveInfo = new List<LogCurveInfo>();
            
            LogIndexType indexType = log.IndexType.HasValue ? log.IndexType.Value : LogIndexType.measureddepth;
            if (DepthIndexTypes.Contains(indexType))
            {
                log.LogCurveInfo.Add(CreateDoubleLogCurveInfo("MD", "m"));
            }
            else
            {
                log.LogCurveInfo.Add(CreateDateTimeLogCurveInfo("DateTime", "s"));
            }
            log.LogCurveInfo.Add(CreateDoubleLogCurveInfo("GR", "api"));
            log.LogCurveInfo.Add(CreateStringLogCurveInfo("UNK", "s"));

            log.LogData = new List<LogData>() { new LogData() };
        
            log.LogData[0].Data = new List<string>();
 
            log.LogData[0].MnemonicList = Mnemonics(log.LogCurveInfo);
            log.LogData[0].UnitList = Units(log.LogCurveInfo);

            log.LogData[0].Data = GenerateLogData(indexType, log.LogCurveInfo, direction, numOfRows);

            return log;
        }
    }
}
