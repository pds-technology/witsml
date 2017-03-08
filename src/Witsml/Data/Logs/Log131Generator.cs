//----------------------------------------------------------------------- 
// PDS.Witsml, 2017.1
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
using System.Linq;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;

namespace PDS.Witsml.Data.Logs
{
    /// <summary>
    /// Generates data for a 131 Log.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Data.DataGenerator" />
    public class Log131Generator : DataGenerator
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
        /// Initializes a new instance of the <see cref="Log131Generator"/> class.
        /// </summary>
        public Log131Generator()
        {
            _random = new Random(Seed);
        }

        /// <summary>
        /// Creates the datetime type <see cref="LogCurveInfo" />.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="columnIndex">Index of the column.</param>
        /// <returns></returns>
        public LogCurveInfo CreateDateTimeLogCurveInfo(string name, string unit, short columnIndex)
        {
            return CreateLogCurveInfo(name, unit, LogDataType.datetime, columnIndex);
        }

        /// <summary>
        /// Creates the double type <see cref="LogCurveInfo" />.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="columnIndex">Index of the column.</param>
        /// <returns></returns>
        public LogCurveInfo CreateDoubleLogCurveInfo(string name, string unit, short columnIndex)
        {
            return CreateLogCurveInfo(name, unit, LogDataType.@double, columnIndex);
        }

        /// <summary>
        /// Creates the string type <see cref="LogCurveInfo" />.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="columnIndex">Index of the column.</param>
        /// <returns></returns>
        public LogCurveInfo CreateStringLogCurveInfo(string name, string unit, short columnIndex)
        {
            return CreateLogCurveInfo(name, unit, LogDataType.@string, columnIndex);
        }

        /// <summary>
        /// Creates the <see cref="LogCurveInfo" />
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="type">The type.</param>
        /// <param name="columnIndex">Index of the column.</param>
        /// <returns></returns>
        public LogCurveInfo CreateLogCurveInfo(string name, string unit, LogDataType type, short columnIndex)
        {
            return new LogCurveInfo()
            {
                Uid = name,
                Mnemonic = name,
                TypeLogData = type,
                Unit = unit,
                ColumnIndex = columnIndex
            };
        }
        
        /// <summary>
        /// Generates the log data.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="numOfRows">The number of rows.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="interval">The interval factor.</param>
        /// <returns></returns>
        public double GenerateLogData(Log log, int numOfRows = 5, double startIndex = 0d, double interval = 1.0)
        {           
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
                        dateTimeIndexStart = dateTimeIndexStart.AddSeconds(_random.Next(1, 5));
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
                    row += ",";

                    if (_random.Next(10) % 9 == 0)
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
                                dateTimeChannelStart = dateTimeChannelStart.AddSeconds(_random.Next(1, 5));
                            }
                            row += dateTimeChannelStart.ToString("o");
                            break;
                        }
                        case LogDataType.@double:
                        //case LogDataType.@float:                       
                        {
                            row += _random.NextDouble().ToString().Trim();
                            break;
                        }
                        //case LogDataType.@int:
                        case LogDataType.@long:
                        //case LogDataType.@short:
                        {
                            row += _random.Next(11);
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
