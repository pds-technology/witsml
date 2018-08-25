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
using System.Globalization;
using System.Linq;
using System.Text;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Data.Logs
{
    /// <summary>
    /// Generates data for a 141 Log.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Data.DataGenerator" />
    public class Log141Generator : DataGenerator
    {
        /// <summary>
        /// The depth index types
        /// </summary>
        public readonly LogIndexType[] DepthIndexTypes = new LogIndexType[] { LogIndexType.length, LogIndexType.measureddepth, LogIndexType.verticaldepth };

        /// <summary>
        /// The time index types
        /// </summary>
        public readonly LogIndexType[] TimeIndexTypes = new LogIndexType[] { LogIndexType.datetime, LogIndexType.elapsedtime };

        /// <summary>
        /// The other index types
        /// </summary>
        public readonly LogIndexType[] OtherIndexTypes = new LogIndexType[] { LogIndexType.other, LogIndexType.unknown };

        private const int Seed = 123;
        private Random _random;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log141Generator"/> class.
        /// </summary>
        public Log141Generator()
        {
            _random = new Random(Seed);
        }

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
                Mnemonic = new ShortNameStruct(name),
                TypeLogData = type,
                Unit = unit
            };
        }

        /// <summary>
        /// Generates the log data.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="numOfRows">The number of rows.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="interval">The interval factor.</param>
        /// <param name="generateNulls">If set to <c>true</c>, null values will be periodically generated.</param>
        /// <returns>The end index of the generated data if the index is numeric.</returns>
        public double GenerateLogData(Log log, int numOfRows = 5, double startIndex = 0d, double interval = 1.0, bool generateNulls = true)
        {
            var indexes = GenerateLogDataIndexes(log, numOfRows, startIndex, interval);
            var logData = GenerateLogData(log.LogCurveInfo, indexes, generateNulls);

            if (log.LogData == null)
                log.LogData = new List<LogData>();

            log.LogData.Clear();
            log.LogData.Add(logData);

            return startIndex + numOfRows * (log.Direction == LogIndexDirection.decreasing ? -interval : interval);
        }

        /// <summary>
        /// Generates log data indexes for the specified log.
        /// </summary>
        /// <param name="log">The log to generate data indexes for.</param>
        /// <param name="numOfRows">The number of rows to generate indexes for.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="interval">The interval between numeric indexes.</param>
        /// <returns>The list of generated indexes.</returns>
        public List<string> GenerateLogDataIndexes(Log log, int numOfRows = 5, double startIndex = 0d, double interval = 1.0)
        {
            DateTimeOffset dateTimeIndexStart = DateTimeOffset.Now;

            if (log.Direction == LogIndexDirection.decreasing)
                interval = -interval;

            var index = startIndex;
            var indexes = new List<string>();

            for (int i = 0; i < numOfRows; i++)
            {
                // index value
                switch (log.IndexType)
                {
                    case LogIndexType.datetime:
                        {
                            dateTimeIndexStart = dateTimeIndexStart.AddSeconds(_random.Next(1, 5));
                            indexes.Add(GenerateDateTimeIndex(dateTimeIndexStart));
                            break;
                        }
                    case LogIndexType.elapsedtime:
                    case LogIndexType.length:
                    case LogIndexType.measureddepth:
                    case LogIndexType.verticaldepth:
                        {
                            indexes.Add(GenerateNumericIndex(index));
                            break;
                        }
                    default:
                        break;
                }

                index += interval;
            }

            return indexes;
        }

        /// <summary>
        /// Generates log data for the specified log curves and indexes.
        /// </summary>
        /// <param name="logCurveInfos">The log curves to generate data for.</param>
        /// <param name="indexes">The indexes to generate data for.</param>
        /// <param name="generateNulls">If set to <c>true</c>, null values will be periodically generated.</param>
        /// <returns>The generated log data.</returns>
        public LogData GenerateLogData(List<LogCurveInfo> logCurveInfos, List<string> indexes, bool generateNulls = true)
        {
            indexes.NotNull(nameof(indexes));
            logCurveInfos.NotNull(nameof(logCurveInfos));

            var logData = new LogData
            {
                MnemonicList = string.Join(",", logCurveInfos.Select(x => x.Mnemonic)),
                UnitList = string.Join(",", logCurveInfos.Select(x => x.Unit)),
                Data = new List<string>()
            };

            var data = logData.Data;

            if (indexes.Count == 0 || logCurveInfos.Count < 2)
                return logData;

            DateTimeOffset dateTimeChannelStart = DateTime.UtcNow - TimeSpan.FromSeconds(indexes.Count);

            for (int i = 0; i < indexes.Count; i++)
            {
                var row = new StringBuilder(indexes[i]);

                // channel values
                for (int k = 1; k < logCurveInfos.Count; k++)
                {
                    row.Append(",");

                    if (generateNulls && _random.Next(10) % 9 == 0)
                    {
                        continue;
                    }

                    switch (logCurveInfos[k].TypeLogData)
                    {
                        case LogDataType.@byte:
                            {
                                row.Append("Y");
                                break;
                            }
                        case LogDataType.datetime:
                            {
                                row.Append((dateTimeChannelStart + TimeSpan.FromSeconds(i)).ToString("o"));
                                break;
                            }
                        case LogDataType.@double:
                        case LogDataType.@float:
                            {
                                row.Append(_random.NextDouble().ToString(CultureInfo.InvariantCulture).Trim());
                                break;
                            }
                        case LogDataType.@int:
                        case LogDataType.@long:
                        case LogDataType.@short:
                            {
                                row.Append(_random.Next(11));
                                break;
                            }
                        case LogDataType.@string:
                            {
                                row.Append("abc");
                                break;
                            }
                        default:
                            {
                                row.Append("null");
                            }
                            break;
                    }
                }

                data.Add(row.ToString());
            }

            return logData;
        }
    }
}
