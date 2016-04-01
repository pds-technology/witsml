using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;

namespace PDS.Witsml.Data.Logs
{
    public class Log131Generator : DataGenerator
    {
        public readonly LogIndexType[] DepthIndexTypes = new LogIndexType[] { LogIndexType.length, LogIndexType.measureddepth, LogIndexType.verticaldepth };
        public readonly LogIndexType[] TimeIndexTypes = new LogIndexType[] { LogIndexType.datetime, LogIndexType.elapsedtime };
        public readonly LogIndexType[] OtherIndexTypes = new LogIndexType[] { LogIndexType.other, LogIndexType.unknown };

        /// <summary>
        /// Creates the datetime type <see cref="LogCurveInfo"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="unit">The unit.</param>
        /// <returns></returns>
        public LogCurveInfo CreateDateTimeLogCurveInfo(string name, string unit)
        {
            return CreateLogCurveInfo(name, unit, LogDataType.datetime);
        }

        /// <summary>
        /// Creates the double type <see cref="LogCurveInfo"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="unit">The unit.</param>
        /// <returns></returns>
        public LogCurveInfo CreateDoubleLogCurveInfo(string name, string unit)
        {
            return CreateLogCurveInfo(name, unit, LogDataType.@double);
        }

        /// <summary>
        /// Creates the string type <see cref="LogCurveInfo"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="unit">The unit.</param>
        /// <returns></returns>
        public LogCurveInfo CreateStringLogCurveInfo(string name, string unit)
        {
            return CreateLogCurveInfo(name, unit, LogDataType.@string);
        }

        /// <summary>
        /// Creates the <see cref="LogCurveInfo"/>
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public LogCurveInfo CreateLogCurveInfo(string name, string unit, LogDataType type)
        {
            return new LogCurveInfo()
            {
                Uid = name,
                Mnemonic = name,
                TypeLogData = type,
                Unit = unit
            };
        }

        /// <summary>
        /// Gets the units in the specified list of <see cref="LogCurveInfo"/>.
        /// </summary>
        /// <param name="infoList">The information list.</param>
        /// <returns></returns>
        public string Units(List<LogCurveInfo> infoList)
        {
            return infoList != null
                ? String.Join(",", infoList.Select(x => x.Unit ?? string.Empty))
                : string.Empty;
        }

        /// <summary>
        /// Gets the mnemonicses in the specified list of <see cref="LogCurveInfo"/>.
        /// </summary>
        /// <param name="infoList">The information list.</param>
        /// <returns></returns>
        public string Mnemonics(List<LogCurveInfo> infoList)
        {
            return infoList != null
                ? String.Join(",", infoList.Select(x => x.Mnemonic))
                : string.Empty;
        }

        /// <summary>
        /// Generates the log data.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="numOfRows">The number of rows.</param>
        /// <param name="startIndex">The start index.</param>
        /// <returns></returns>
        public double GenerateLogData(Log log, int numOfRows = 5, double startIndex = 0d, double interval = 1.0)
        {           
            const int Seed = 123;

            Random random = new Random(Seed);
            DateTimeOffset dateTimeIndexStart = DateTimeOffset.Now;
            DateTimeOffset dateTimeChannelStart = dateTimeIndexStart;

            if (log.Direction == LogIndexDirection.decreasing)
                interval = -interval;

            if (log.LogData == null)
                log.LogData = new List<string>();

            var data = log.LogData;
            var index = startIndex;

            for (int i = 0; i < numOfRows; i++)
            {
                var row = string.Empty;

                // index value
                switch (log.IndexType)
                {
                    case LogIndexType.datetime:
                    {
                        dateTimeIndexStart = dateTimeIndexStart.AddSeconds(random.Next(1, 5));
                        row += dateTimeIndexStart.ToString("o");
                        break;
                    }
                    case LogIndexType.elapsedtime:                       
                    case LogIndexType.length:                        
                    case LogIndexType.measureddepth:                        
                    case LogIndexType.verticaldepth:
                    {
                        row += string.Format("{0:F3}", index);
                        break;
                    }
                    default:
                        break;
                }

                // channel values
                for (int k = 1; k < log.LogCurveInfo.Count; k++)
                {
                    row += ", ";

                    if (random.Next(1, 10) == 2)
                    {
                        continue;
                    }

                    switch (log.LogCurveInfo[k].TypeLogData)
                    {
                        //case LogDataType.@byte:
                        //{
                        //    row += "Y";
                        //    break;
                        //}
                        case LogDataType.datetime:
                        {
                            if (log.IndexType == LogIndexType.datetime)
                            {
                                dateTimeChannelStart = dateTimeIndexStart;
                            }
                            else
                            {
                                dateTimeChannelStart = dateTimeChannelStart.AddSeconds(random.Next(1, 5));
                            }
                            row += dateTimeChannelStart.ToString("o");
                            break;
                        }
                        case LogDataType.@double:
                        //case LogDataType.@float:                       
                        {
                            row += random.NextDouble().ToString();
                            break;
                        }
                        //case LogDataType.@int:
                        case LogDataType.@long:
                        //case LogDataType.@short:
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
                index += interval;
            }

            return index;
        }
    }
}
