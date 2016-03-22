using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using PDS.Framework;
using PDS.Witsml.Data;

namespace PDS.Witsml
{
    public class Log200Generator : DataGenerator
    {
        public readonly ChannelIndexType[] DepthIndexTypes = new ChannelIndexType[] { ChannelIndexType.measureddepth, ChannelIndexType.trueverticaldepth, ChannelIndexType.passindexeddepth };
        public readonly ChannelIndexType[] TimeIndexTypes = new ChannelIndexType[] { ChannelIndexType.datetime, ChannelIndexType.elapsedtime };
        public readonly ChannelIndexType[] OtherIndexTypes = new ChannelIndexType[] { ChannelIndexType.pressure, ChannelIndexType.temperature };

        public Citation CreateCitation(string name)
        {
            return new Citation()
            {
                Title = Name(name),
                Originator = GetType().Name,
                Format = GetType().Assembly.FullName,
                Creation = DateTime.UtcNow,
            };
        }

        public PointMetadata CreatePointMetadata(string name, string description, EtpDataType etpDataType)
        {
            return new PointMetadata()
            {
                Name = name,
                Description = description,
                EtpDataType = etpDataType
            };
        }

        public ChannelIndex CreateChannelIndex(ChannelIndexType indexType, IndexDirection direction, string mnemonic, string uom, string datumReference)
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

        public ChannelIndex CreateMeasuredDepthIndex(IndexDirection direction)
        {
            return CreateChannelIndex( ChannelIndexType.measureddepth, direction, "MD", "m", "MSL");
        }

        public ChannelIndex CreateTrueVerticalDepthIndex(IndexDirection direction)
        {
            return CreateChannelIndex(ChannelIndexType.trueverticaldepth, direction, "TVD", "ft", "MSL");
        }

        public ChannelIndex CreatePassIndexDepthIndex(IndexDirection direction)
        {
            return CreateChannelIndex(ChannelIndexType.passindexeddepth, direction, "PID", "m", "MSL");
        }

        public ChannelIndex CreateDateTimeIndex()
        {
            return CreateChannelIndex(ChannelIndexType.datetime, IndexDirection.increasing, "DateTime", "s", "MSL");
        }

        public ChannelIndex CreateElapsedTimeIndex(IndexDirection direction)
        {
            return CreateChannelIndex(ChannelIndexType.elapsedtime, direction, "ElapsedTime", "ms", "MSL");
        }

        /// <summary>
        /// Creates the channel set.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="indexList">The index list.</param>
        /// <param name="loggingMethod">The logging method.</param>
        /// <returns></returns>
        public ChannelSet CreateChannelSet(Log log, List<ChannelIndex> indexList, LoggingMethod loggingMethod = LoggingMethod.Computed)
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
                Citation = CreateCitation(Name("Citation")),
                ExistenceKind = ExistenceKind.simulated,
                Index = indexList,

                LoggingCompanyName = log.LoggingCompanyName,
                TimeDepth = log.TimeDepth,
                CurveClass = log.CurveClass,

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
            const int Seed = 123;

            Random random = new Random(Seed);
            DateTime dateTimeStart = new DateTime(2015, 3, 17, 11, 50, 0).ToUniversalTime();

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

                    string indexValues = GenerateIndexValues(random, channelSet, indexesStart);

                    string channelValues = GenerateChannelValues(random, channelSet);

                    logData += "[ " + indexValues + ", " + channelValues + " ]";
                }
                logData += Environment.NewLine + " ]";
                channelSet.Data.Data = logData;
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
                    indexesStart[idx] = ((DateTime)indexesStart[idx]).AddSeconds(random.Next(1, 5));
                    indexValues += "\"" + ((DateTime)indexesStart[idx]).ToString("o") + "\"";
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
            foreach (Channel channel in channelSet.Channel)
            {
                channelValues = channelValues == string.Empty ? " [" : channelValues + ", ";

                var column = string.Empty;
                bool setToNull = (random.Next() % 5 == 0);
                if (setToNull)
                {
                    column += "null";
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

        private void InitStartIndexes(DateTime dateTimeStart, List<ChannelIndex> channelIndexes, object[] indexesStart)
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
                return string.Empty;
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
                        column = string.Format(" {0:0.###}", random.NextDouble());
                        break;
                    }
                case EtpDataType.@int:
                case EtpDataType.@long:
                    {
                        column = string.Format(" {0:0}", random.Next());
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
                        column = "(1.1, 1.2, 1.3)";
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
    }
    }
