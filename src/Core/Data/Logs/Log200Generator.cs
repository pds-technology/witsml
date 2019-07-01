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
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Newtonsoft.Json;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.Channels;

using UnitOfMeasureExt = Energistics.DataAccess.ExtensibleEnum<Energistics.DataAccess.WITSML200.ReferenceData.UnitOfMeasure>;

namespace PDS.WITSMLstudio.Data.Logs
{
    /// <summary>
    /// Generates data for a 200 Log.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Data.DataGenerator" />
    public class Log200Generator : DataGenerator
    {
        /// <summary>
        /// The depth index types
        /// </summary>
        public readonly ChannelIndexType[] DepthIndexTypes = new ChannelIndexType[] { ChannelIndexType.measureddepth, ChannelIndexType.trueverticaldepth, ChannelIndexType.passindexeddepth };

        /// <summary>
        /// The time index types
        /// </summary>
        public readonly ChannelIndexType[] TimeIndexTypes = new ChannelIndexType[] { ChannelIndexType.datetime, ChannelIndexType.elapsedtime };

        /// <summary>
        /// The other index types
        /// </summary>
        public readonly ChannelIndexType[] OtherIndexTypes = new ChannelIndexType[] { ChannelIndexType.pressure, ChannelIndexType.temperature };

        /// <summary>
        /// The WITSML 2.0 data schema version.
        /// </summary>
        public static readonly string DataSchemaVersion = OptionsIn.DataVersion.Version200.Value;

        private const int Seed = 123;
        private const string SpecialCharacters = @"~ ! @ # $ % ^ & * ( ) _ + { } | < > ? ; : ' , . / [ ] \b \f \n \r \t \ """;
        private Random _random;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log200Generator"/> class.
        /// </summary>
        public Log200Generator()
        {
            _random = new Random(Seed);
        }

        /// <summary>
        /// Creates the citation <see cref="Citation"/>
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public Citation CreateCitation(string name)
        {
            var date = DateTime.UtcNow;

            return new Citation()
            {
                Title = Name(name),
                Originator = typeof(DataGenerator).Name,
                Format = typeof(DataGenerator).Assembly.FullName,
                Creation = date,
                LastUpdate = date
            };
        }

        /// <summary>
        /// Creates the point metadata <see cref="PointMetadata"/>
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="etpDataType">Type of the ETP data.</param>
        /// <returns></returns>
        public PointMetadata CreatePointMetadata(string name, string description, EtpDataType etpDataType)
        {
            return new PointMetadata()
            {
                Name = name,
                Description = description,
                EtpDataType = etpDataType
            };
        }

        /// <summary>
        /// Creates the channel index <see cref="ChannelIndex"/>
        /// </summary>
        /// <param name="indexType">Type of the index.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <param name="uom">The uom.</param>
        /// <param name="datumReference">The datum reference.</param>
        /// <returns></returns>
        public ChannelIndex CreateChannelIndex(ChannelIndexType indexType, IndexDirection direction, string mnemonic, UnitOfMeasureExt? uom, string datumReference)
        {
            return new ChannelIndex()
            {
                Direction = direction,
                IndexType = indexType,
                Mnemonic = mnemonic,
                Uom = uom,
                DatumReference = datumReference
            };
        }

        /// <summary>
        /// Creates the index <see cref="ChannelIndex"/> of the measured depth 
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public ChannelIndex CreateMeasuredDepthIndex(IndexDirection direction)
        {
            return CreateChannelIndex( ChannelIndexType.measureddepth, direction, "MD", UnitOfMeasure.m, "MSL");
        }

        /// <summary>
        /// Creates the index <see cref="ChannelIndex"/> of the true vertical depth.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public ChannelIndex CreateTrueVerticalDepthIndex(IndexDirection direction)
        {
            return CreateChannelIndex(ChannelIndexType.trueverticaldepth, direction, "TVD", UnitOfMeasure.ft, "MSL");
        }

        /// <summary>
        /// Creates the index <see cref="ChannelIndex"/> of the pass index depth.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public ChannelIndex CreatePassIndexDepthIndex(IndexDirection direction)
        {
            return CreateChannelIndex(ChannelIndexType.passindexeddepth, direction, "PID", UnitOfMeasure.m, "MSL");
        }

        /// <summary>
        /// Creates the index <see cref="ChannelIndex"/> of the date time.
        /// </summary>
        /// <returns></returns>
        public ChannelIndex CreateDateTimeIndex()
        {
            return CreateChannelIndex(ChannelIndexType.datetime, IndexDirection.increasing, "TIME", UnitOfMeasure.us, "MSL");
        }

        /// <summary>
        /// Creates the index <see cref="ChannelIndex"/> of the elapsed time.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public ChannelIndex CreateElapsedTimeIndex(IndexDirection direction)
        {
            return CreateChannelIndex(ChannelIndexType.elapsedtime, direction, "TIME", UnitOfMeasure.ms, "MSL");
        }

        /// <summary>
        /// Creates the channel.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="indexList">The index list.</param>
        /// <param name="citationName">Name of the citation.</param>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <param name="uom">The uom.</param>
        /// <param name="channelClass">The channel class.</param>
        /// <param name="etpDataType">Type of the ETP data.</param>
        /// <param name="pointMetadataList">The point metadata list.</param>
        /// <returns></returns>
        public Channel CreateChannel(Log log, List<ChannelIndex> indexList, string citationName, string mnemonic, UnitOfMeasureExt? uom, string channelClass, EtpDataType etpDataType, List<PointMetadata> pointMetadataList)
        {
            return new Channel()
            {
                Uuid = Uid(),
                Citation = CreateCitation(citationName),
                Mnemonic = mnemonic,
                Uom = uom,
                ChannelClass = ToPropertyKindReference(channelClass),
                LoggingMethod = log.LoggingMethod,
                LoggingCompanyName = log.LoggingCompanyName ?? "PDS",
                Source = log.LoggingMethod.ToString(),
                DataType = etpDataType,
                GrowingStatus = ChannelStatus.active,
                Index = indexList,
                StartIndex = (log.TimeDepth.EqualsIgnoreCase(ObjectFolders.Depth) ?
                               (AbstractIndexValue)new DepthIndexValue() : (new TimeIndexValue())),
                EndIndex = (log.TimeDepth.EqualsIgnoreCase(ObjectFolders.Depth) ?
                               (AbstractIndexValue)new DepthIndexValue() : (new TimeIndexValue())),
                TimeDepth = log.TimeDepth,
                PointMetadata = pointMetadataList,
                SchemaVersion = DataSchemaVersion
            };
        }

        /// <summary>
        /// Deserializes the channel set data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public List<List<List<object>>> DeserializeChannelSetData(string data)
        {
            return JsonConvert.DeserializeObject<List<List<List<object>>>>(data);
        }

        /// <summary>
        /// Deserializes the channel values.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public List<object> DeserializeChannelValues(string data)
        {
            return JsonConvert.DeserializeObject<List<object>>(data);
        }

        /// <summary>
        /// Creates the channel set.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns></returns>
        public ChannelSet CreateChannelSet(Log log)
        {
            bool isDepth = log.TimeDepth.EqualsIgnoreCase(ObjectFolders.Depth);
            IndexRangeContext indexRangeContext = null;
            List<Channel> channelList = new List<Channel>();

            if (isDepth)
            {
                indexRangeContext = new IndexRangeContext()
                {
                    StartIndex = new DepthIndexValue(),
                    EndIndex = new DepthIndexValue(),
                };
            }
            else
            {
                indexRangeContext = new IndexRangeContext()
                {
                    StartIndex = new TimeIndexValue(),
                    EndIndex = new TimeIndexValue(),
                };
            }

            ChannelSet channelSet = new ChannelSet()
            {
                Uuid = Uid(),
                Citation = CreateCitation("ChannelSet"),
                SchemaVersion = DataSchemaVersion,
                ExistenceKind = ExistenceKind.simulated,
                Index = new List<ChannelIndex>(),
                LoggingCompanyName = log.LoggingCompanyName,
                Channel = channelList,
                DataContext = indexRangeContext,
                Data = new ChannelData()
                {
                    Data = null
                }
            };

            return channelSet;
        }

        /// <summary>
        /// Generates the channel data.
        /// </summary>
        /// <param name="channelSetList">The channel set list.</param>
        /// <param name="numDataValue">The number of data value rows.</param>
        public void GenerateChannelData(List<ChannelSet> channelSetList, int numDataValue = 5)
        {
            var dateTimeStart = new DateTimeOffset(2015, 3, 17, 11, 50, 0, TimeSpan.FromHours(-6));

            foreach (ChannelSet channelSet in channelSetList)
            {
                object[] indexesStart = new object[channelSet.Index.Count()];
                InitStartIndexes(dateTimeStart, channelSet.Index, indexesStart);

                string logData = "[ " + Environment.NewLine;

                for (int i = 0; i < numDataValue; i++)
                {
                    if (i > 0)
                    {
                        logData += ", " + Environment.NewLine;
                    }

                    string indexValues = GenerateIndexValues(_random, channelSet, indexesStart);

                    string channelValues = GenerateChannelValues(_random, channelSet);

                    logData += "[ " + indexValues + ", " + channelValues + " ]";
                }

                logData += Environment.NewLine + " ]";
                channelSet.SetData(logData);
            }
        }

        /// <summary>
        /// Generates the channel data.
        /// </summary>
        /// <param name="dataBlock">The data block.</param>
        /// <param name="numRows">The number rows.</param>
        public void GenerateChannelData(ChannelDataBlock dataBlock, int numRows)
        {
            for (var i = 0; i < numRows; i++)
            {
                var index = (i * 0.1).IndexToScale(3);
                var indexes = new List<object> { index };

                // columns
                for (var j = 1; j < dataBlock.ChannelIds.Count + 1; j++)
                {
                    dataBlock.Append(j, indexes, GenerateDataValue(dataBlock.DataTypes[j - 1]));
                }
            }
        }

        private string GenerateIndexValues(Random random, ChannelSet channelSet, object[] indexesStart)
        {
            var indexValues = string.Empty;

            for (int idx = 0; idx < channelSet.Index.Count; idx++)
            {
                var index = channelSet.Index[idx];
                ChannelIndexType indexValue;
                if (index.IndexType.HasValue)
                {
                    indexValue = index.IndexType.Value;
                }
                else
                {
                    continue;
                }

                indexValues = indexValues == string.Empty ? "[ " : indexValues + ", ";

                bool isIncreasing = index.Direction.HasValue ? index.Direction.Value == IndexDirection.increasing : true;

                if (indexValue.Equals(ChannelIndexType.datetime))
                {
                    indexesStart[idx] = ((DateTimeOffset)indexesStart[idx]).AddSeconds(random.Next(1, 5));
                    indexValues += "\"" + ((DateTimeOffset)indexesStart[idx]).UtcDateTime.ToString("o") + "\"";
                }
                else if (indexValue.Equals(ChannelIndexType.elapsedtime))
                {
                    indexesStart[idx] = isIncreasing ? (long)indexesStart[idx] + 4 : (long)indexesStart[idx] - 4;
                    indexValues += string.Format(" {0:0}", (long)indexesStart[idx]);
                }
                else if (DepthIndexTypes.Contains(indexValue))
                {
                    indexesStart[idx] = isIncreasing ? (double)indexesStart[idx] + random.Next(1, 10) / 10.0 : (double)indexesStart[idx] - random.Next(1, 10) / 10.0;
                    indexValues += string.Format(" {0:0.###}", (double)indexesStart[idx]);
                }
            }

            indexValues += " ]";
            return indexValues;
        }

        private string GenerateChannelValues(Random random, ChannelSet channelSet)
        {
            var channelValues = string.Empty;
            var nullCount = 0;
            foreach (Channel channel in channelSet.Channel)
            {
                channelValues = channelValues == string.Empty ? " [" : channelValues + ", ";

                var column = string.Empty;
                bool setToNull = (random.Next() % 5 == 0);

                // Don't allow all channels to have a null value
                if (setToNull && nullCount < (channelSet.Channel.Count - 1))
                {
                    column += "null";
                    nullCount++;
                }
                else
                {
                    var columnValue = GenerateValuesByType(random, channel.DataType, true);
                    if (channel.PointMetadata == null)
                    {
                        column += columnValue;
                    }
                    else
                    {
                        column = "[" + columnValue;
                        foreach (PointMetadata pointMetaData in channel.PointMetadata)
                        {
                            var etpDataType = pointMetaData.EtpDataType ?? null;
                            column += ", " + GenerateValuesByType(random, etpDataType, false);
                        }
                        column += "]";

                    }
                }
                channelValues += column;
            }
            channelValues += "]";
            return channelValues;
        }

        private void InitStartIndexes(DateTimeOffset dateTimeStart, List<ChannelIndex> channelIndexes, object[] indexesStart)
        {
            for (int i = 0; i < channelIndexes.Count(); i++)
            {
                var indexType = channelIndexes[i].IndexType;
                switch (indexType)
                {
                    case ChannelIndexType.datetime:
                        {
                            indexesStart[i] = dateTimeStart;
                            break;
                        }
                    case ChannelIndexType.measureddepth:
                    case ChannelIndexType.passindexeddepth:
                    case ChannelIndexType.trueverticaldepth:
                        {
                            indexesStart[i] = 0.0;
                            break;
                        }
                    case ChannelIndexType.elapsedtime:
                        {
                            indexesStart[i] = (long)0;
                            break;
                        }
                };
            }
        }

        private string GenerateValuesByType(Random random, EtpDataType? etpDataType, bool isChannelValue)
        {
            string column = string.Empty;

            bool setToNull = (random.Next() % 5 == 0);
            if (setToNull && !isChannelValue)
            {
                return "null";
            }

            switch (etpDataType)
            {
                case EtpDataType.boolean:
                    {
                        column = (random.Next() % 3 == 0) ? "false" : "true";
                        break;
                    }
                case EtpDataType.bytes:
                    {
                        column = "Y";
                        break;
                    }
                case EtpDataType.@double:
                case EtpDataType.@float:
                    {
                        column = random.NextDouble().ToString();
                        break;
                    }
                case EtpDataType.@int:
                case EtpDataType.@long:
                    {
                        column = random.Next().ToString();
                        break;
                    }
                case EtpDataType.@null:
                    {
                        column = "null";
                        break;
                    }
                case EtpDataType.@string:
                    {
                        column = "\"abc\"";
                        break;
                    }
                case EtpDataType.vector:
                    {
                        column = "[1.1, 1.2, 1.3]";
                        break;
                    }
                default:
                    {
                        column = "null";
                        break;
                    }
            }
            return column;
        }

        private object GenerateDataValue(string dataType)
        {
            EtpDataType enumType;
            Enum.TryParse(dataType, out enumType);

            switch (enumType)
            {
                case EtpDataType.@long:
                    return _random.Next();

                case EtpDataType.@string:
                    return SpecialCharacters;

                case EtpDataType.@null:
                    return null;
            }

            return _random.NextDouble();
        }
    }
}
