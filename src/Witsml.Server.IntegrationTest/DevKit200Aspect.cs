using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Newtonsoft.Json;
using PDS.Framework;

namespace PDS.Witsml.Server
{
    public class DevKit200Aspect : DevKitAspect
    {
        private ChannelIndexType[] DepthIndex = new ChannelIndexType[] { ChannelIndexType.measureddepth, ChannelIndexType.trueverticaldepth, ChannelIndexType.passindexeddepth };
        private ChannelIndexType[] TimeIndex = new ChannelIndexType[] { ChannelIndexType.datetime, ChannelIndexType.elapsedtime };
        private ChannelIndexType[] OtherIndex = new ChannelIndexType[] { ChannelIndexType.pressure, ChannelIndexType.temperature };


        public DevKit200Aspect() : base(null, WMLSVersion.WITSML141)
        {
        }

        public override string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version200.Value; }
        }

        public Citation Citation(string name)
        {
            return new Citation()
            {
                Title = Name(name),
                Originator = GetType().Name,
                Format = GetType().Assembly.FullName,
                Creation = System.DateTime.UtcNow,
            };
        }

        public GeodeticWellLocation Location()
        {
            return new GeodeticWellLocation()
            {
                Crs = new GeodeticEpsgCrs() { EpsgCode = 26914 },
                Latitude = 28.5597,
                Longitude = -90.6671
            };
        }

        public DataObjectReference DataObjectReference(string objectType, string title = null, string uuid = null)
        {
            return new DataObjectReference
            {
                ContentType = EtpContentTypes.Witsml200.For(objectType),
                Title = (title == null) ? "Test title for " + objectType : title,
                Uuid = uuid == null ? "Test Uuid for " + objectType : uuid,
            };
        }

        public ChannelIndex CreateIndex(IndexDirection isIncreasing = IndexDirection.increasing, ChannelIndexType indexType = ChannelIndexType.measureddepth, string mnemonic = "MD", string uom = "ft", string datumReference = null)
        {
            return new ChannelIndex()
            {
                Direction = isIncreasing,
                IndexType = indexType,
                Mnemonic = mnemonic,
                Uom = uom,
                // Description: For depth indexes, this contains the uid of the datum, in the Channel's Well object, to which all of the index values are referenced.
                DatumReference = string.IsNullOrEmpty(datumReference) ? Uid() : datumReference,
            };
        }

        public PointMetadata PointMetadata(string name, string description, EtpDataType etpDataType)
        {
            return new PointMetadata()
            {
                Name = name,
                Description = description,
                EtpDataType = etpDataType
            };
        }

        public Channel Channel(Log log, List<ChannelIndex> indexList, string citationName = "Citation", string mnemonic = "MD", string uom = "ft", string curveClass = "CurveClass", EtpDataType etpDataType = EtpDataType.@double, List<PointMetadata> pointMetadataList = null)
        {
            return new Channel()
            {
                Citation = Citation(citationName),
                Mnemonic = mnemonic,
                UoM = uom,
                CurveClass = curveClass,
                LoggingMethod = log.LoggingMethod,
                LoggingCompanyName = log.LoggingCompanyName,
                Source = log.LoggingMethod.ToString(),
                DataType = etpDataType,
                Status = ChannelStatus.active,
                Index = indexList,
                StartIndex = (log.TimeDepth.EqualsIgnoreCase(ObjectFolders.Depth) ?
                               (AbstractIndexValue)new DepthIndexValue() : (new TimeIndexValue())),
                EndIndex = (log.TimeDepth.EqualsIgnoreCase(ObjectFolders.Depth) ?
                               (AbstractIndexValue)new DepthIndexValue() : (new TimeIndexValue())),
                TimeDepth = log.TimeDepth,
                PointMetadata = pointMetadataList,
            };
        }

        public void InitHeader(Log log, LoggingMethod loggingMethod, ChannelIndexType indexType, IndexDirection direction=IndexDirection.increasing)
        {
            log.ChannelSet = new List<ChannelSet>();
            log.LoggingCompanyName = "Service Co.";
            log.CurveClass = "unknown";

            var index = List(ChannelIndex(indexType, direction));
            if (indexType == ChannelIndexType.measureddepth)
            {
                log.TimeDepth = "depth";

                var pointMetadataList = List(PointMetadata("Quality", "Quality", EtpDataType.boolean));

                ChannelSet channelSet = CreateChannelSet(log, index, loggingMethod);
                
                channelSet.Channel.Add(Channel(log, index, "Rate of Penetration", "ROP", "m/h", "Velocity", EtpDataType.@double, pointMetadataList: pointMetadataList));
                channelSet.Channel.Add(Channel(log, index, "Hookload", "HKLD", "klbf", "Force", EtpDataType.@double));
                channelSet.Channel.Add(Channel(log, index, "GR1AX", "GR", "api", "Gamma_Ray", EtpDataType.@double));

                CreateMockChannelSetData(channelSet, channelSet.Index);
                log.ChannelSet.Add(channelSet);

            }
            else if (indexType == ChannelIndexType.datetime)
            {
                log.TimeDepth = "time";

                var pointMetadataList = List(PointMetadata("Confidence", "Confidence", EtpDataType.@float));

                ChannelSet channelSet = CreateChannelSet(log, index, loggingMethod);
               
                channelSet.Channel.Add(Channel(log, index, "Rate of Penetration", "ROP", "m/h", "Velocity", EtpDataType.@double, pointMetadataList: pointMetadataList));
                channelSet.Channel.Add(Channel(log, index, "GR1AX", "GR", "api", "Gamma_Ray", EtpDataType.@double));

                CreateMockChannelSetData(channelSet, channelSet.Index);
                log.ChannelSet.Add(channelSet);
            }
        }

        public ChannelIndex ChannelIndex(ChannelIndexType indexType, IndexDirection direction=IndexDirection.increasing)
        {
            switch (indexType)
            {
                case ChannelIndexType.measureddepth:
                    {
                        return CreateIndex(direction, ChannelIndexType.measureddepth, "MD", "m", "MSL");
                    }
                case ChannelIndexType.trueverticaldepth:
                    {
                        return CreateIndex(direction, ChannelIndexType.trueverticaldepth, "TVD", "ft", "MSL");
                    }
                case ChannelIndexType.passindexeddepth:
                    {
                        return CreateIndex(direction, ChannelIndexType.passindexeddepth, "PID", "m", "MSL");
                    }
                case ChannelIndexType.datetime:
                    {
                        return CreateIndex(direction, ChannelIndexType.datetime, "DateTime", "s", "MSL");
                    }
                case ChannelIndexType.elapsedtime:
                    {
                        return CreateIndex(direction, ChannelIndexType.elapsedtime, "ElapsedTime", "ms", "MSL");
                    }
                default:
                    {
                        return null;
                    }
            };
        }

        public void CreateMockChannelSetData(ChannelSet channelSet, List<ChannelIndex> indices)
        {
            var data = new ChannelData()
            {
                FileUri = "file://",
            };

            if (indices.Count == 1)
            {
                if (indices[0].IndexType == ChannelIndexType.measureddepth)
                {
                    data.Data = @"[
                            [ [0.0 ], [ [ 1.0, true ],  [ 2.0 ], [ 3.0 ] ] ],
                            [ [0.1 ], [ [ 1.1, false ], null,    [ 3.1 ] ] ],
                            [ [0.2 ], [ null,           null,    [ 3.2 ] ] ],
                            [ [0.3 ], [ [ 1.3, true ],  [ 2.3 ], [ 3.3 ] ] ]
                        ]";
                }
                else if (indices[0].IndexType == ChannelIndexType.datetime)
                {
                    data.Data = @"[
                            [ [ ""2016-01-01T00:00:00.0000Z"" ], [ [ 1.0, true ],  [ 2.0 ], [ 3.0 ] ] ],
                            [ [ ""2016-01-01T00:00:01.0000Z"" ], [ [ 1.1, false ], null,    [ 3.1 ] ] ],
                            [ [ ""2016-01-01T00:00:02.0000Z"" ], [ null,           null,    [ 3.2 ] ] ],
                            [ [ ""2016-01-01T00:00:03.0000Z"" ], [ [ 1.3, true ],  [ 2.3 ], [ 3.3 ] ] ]
                        ]";
                }
            }
            else if (indices.Count == 2)
            {
                if (indices[0].IndexType == ChannelIndexType.measureddepth)
                {
                    data.Data = @"[
                            [ [0.0, ""2016-01-01T00:00:00.0000Z"" ], [ [1.0, true],   [ 2.0 ], [ 3.0 ] ] ],
                            [ [0.1, ""2016-01-01T00:00:01.0000Z"" ], [ [1.1, false],  null,    [ 3.1 ] ] ],
                            [ [0.2, ""2016-01-01T00:00:02.0000Z"" ], [ null,          null,    [ 3.2 ] ] ],
                            [ [0.3, ""2016-01-01T00:00:03.0000Z"" ], [ [1.3, true],   [ 2.3 ], [ 3.3 ] ] ]
                        ]";
                }
                else if (indices[0].IndexType == ChannelIndexType.datetime)
                {
                    data.Data = @"[
                            [ [ ""2016-01-01T00:00:00.0000Z"", 0.0 ], [ [ 1.0, true ],  [ 2.0 ], [ 3.0 ] ] ],
                            [ [ ""2016-01-01T00:00:01.0000Z"", 0.1 ], [ [ 1.1, false ], null,    [ 3.1 ] ] ],
                            [ [ ""2016-01-01T00:00:02.0000Z"", 0.2 ], [ null,           null,    [ 3.2 ] ] ],
                            [ [ ""2016-01-01T00:00:03.0000Z"", 0.3 ], [ [ 1.3, true ],  [ 2.3 ], [ 3.3 ] ] ]
                        ]";
                }
            }
            channelSet.Data = data;
        }

        /// <summary>
        /// Initializes the channel set.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="isIncreasing">if set to <c>true</c> [is increasing].</param>
        /// <param name="loggingMethod">The logging method.</param>
        /// <param name="numDataValue">The number data value.</param>
        public void InitChannelSet(Log log, List<ChannelIndex> indexList, LoggingMethod loggingMethod = LoggingMethod.Computed, int numDataValue = 5)
        {           
            ChannelSet channelSet = CreateChannelSet(log, indexList, loggingMethod);

            bool isDepth = log.TimeDepth.EqualsIgnoreCase(ObjectFolders.Depth);
            if (isDepth)
            {
                var pointMetadataList = List(PointMetadata("Quality", "Quality", EtpDataType.boolean));
                                   
                channelSet.Channel.Add(Channel(log, indexList, "Rate of Penetration", "ROP", "m/h", "Velocity", EtpDataType.@double, pointMetadataList: pointMetadataList));
                channelSet.Channel.Add(Channel(log, indexList, "Hookload", "HKLD", "klbf", "Force", EtpDataType.@double));
            }
            else
            {
                var pointMetadataList = List(PointMetadata("Confidence", "Confidence", EtpDataType.@float));
                                   
                channelSet.Channel.Add(Channel(log, indexList, "Rate of Penetration", "ROP", "m/h", "Velocity", EtpDataType.@double, pointMetadataList: pointMetadataList));
            }
            log.ChannelSet = new List<ChannelSet>();
            log.ChannelSet.Add(channelSet);


            GenerateChannelData(log.ChannelSet, numDataValue: numDataValue);
        }

        /// <summary>
        /// Creates the log.
        /// </summary>
        /// <param name="indexType">Type of the index.</param>
        /// <param name="isIncreasing">if set to <c>true</c> [is increasing].</param>
        /// <returns></returns>
        public Log CreateLog(ChannelIndexType indexType, bool isIncreasing)
        {
            Log log = new Log();
            log.Citation = Citation(Name("Generated Citation"));
            log.Uuid = Uid();

            log.ChannelSet = new List<ChannelSet>();
            log.CurveClass = Name("Curve class");
            log.LoggingCompanyName = Name("ABC Logging Company");
            log.Wellbore = DataObjectReference(ObjectTypes.Wellbore, Name("Wellbore"), Uid());

            List<ChannelIndex> indexList = new List<ChannelIndex>();
            IndexDirection direction = isIncreasing ? IndexDirection.increasing : IndexDirection.decreasing;
            if (DepthIndex.Contains(indexType))
            {
                log.TimeDepth = ObjectFolders.Depth;
                ChannelIndex channelIndex = CreateIndex(direction, ChannelIndexType.measureddepth, "MD", "m");
                if (indexType.Equals(ChannelIndexType.trueverticaldepth))
                {
                    channelIndex = CreateIndex(direction, ChannelIndexType.trueverticaldepth, "TVD", "ft");
                }
                else if (indexType.Equals(ChannelIndexType.passindexeddepth))
                {
                    channelIndex = CreateIndex(direction, ChannelIndexType.passindexeddepth, "PID", "ft");
                }
                indexList.Add(channelIndex);
            }
            else if (TimeIndex.Contains(indexType))
            {
                log.TimeDepth = ObjectFolders.Time;
                ChannelIndex channelIndex = CreateIndex(direction, ChannelIndexType.elapsedtime, "ElapsedTime", "ms");
                if (indexType.Equals(ChannelIndexType.datetime))
                {
                    // DateTime should be increasing only
                    indexList.Add(CreateIndex(IndexDirection.increasing, ChannelIndexType.datetime, "DateTime", "s"));
                }
            }
            else
            {
                log.TimeDepth = ObjectFolders.Other;
                return null;
            }

            InitChannelSet(log, indexList);

            return log;
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
                Citation = Citation(Name("ChannelSet_Citation")),
                ExistenceKind = ExistenceKind.simulated,
                Index = indexList,

                LoggingCompanyName = log.LoggingCompanyName,
                TimeDepth = log.TimeDepth,
                CurveClass = log.CurveClass,

                Channel = channelList,

                DataContext = indexRangeContext,

                Data = new ChannelData()
                {
                    FileUri = "file://",
                    Data = null
                }
            };
            return channelSet;
        }

        public List<List<List<object>>> DeserializeChannelSetData(string data)
        {
            return JsonConvert.DeserializeObject<List<List<List<object>>>>(data);
        }

        public List<object> DeserializeChannelValues(string data)
        {
            return JsonConvert.DeserializeObject<List<object>>(data);
        }

        public string SerializeChannelSetData(List<List<List<object>>> data)
        {
            return JsonConvert.SerializeObject(data);
        }

        private string GenerateIndexValues(Random random, ChannelSet channelSet, object[] indexesStart)
        {
            var indexValues = string.Empty;

            for (int idx = 0; idx < channelSet.Index.Count; idx++)
            {
                var index = channelSet.Index[idx];
                ChannelIndexType indexValue;
                if (index.IndexType.HasValue)
                    indexValue = index.IndexType.Value;
                else
                    continue;
                indexValues = indexValues == string.Empty ? "[ " : indexValues + ", ";

                bool isIncreasing = index.Direction.HasValue ? index.Direction.Value == IndexDirection.increasing : true;

                if (indexValue.Equals(ChannelIndexType.datetime))
                {
                    indexesStart[idx] = ((DateTime)indexesStart[idx]).AddSeconds(random.Next(1, 5));
                    indexValues += "\"" + ((DateTime)indexesStart[idx]).ToString("o") + "\"";
                }
                else if (DepthIndex.Contains(indexValue))
                {
                    indexesStart[idx] = isIncreasing ? (double)indexesStart[idx] + random.Next(1, 10) / 10.0 : (double)indexesStart[idx] - random.Next(1, 10) / 10.0;
                    indexValues += string.Format(" {0:0.###}", (double)indexesStart[idx]);
                }
                else if (indexValue.Equals(ChannelIndexType.elapsedtime))
                {
                    indexesStart[idx] = isIncreasing ? (long)indexesStart[idx] + 4 : (long)indexesStart[idx] - 4;
                    indexValues += string.Format(" {0:0}", (long)indexesStart[idx]);
                }
            }

            indexValues += " ]";
            return indexValues;
        }

        private static string GenerateChannelValues(Random random, ChannelSet channelSet)
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
                        indexesStart[i] = dateTimeStart;
                        break;
                    case ChannelIndexType.measureddepth:
                    case ChannelIndexType.passindexeddepth:
                    case ChannelIndexType.trueverticaldepth:
                        indexesStart[i] = 0.0;
                        break;
                    case ChannelIndexType.elapsedtime:
                        indexesStart[i] = (long)0;
                        break;
                };
            }
        }

        private static string GenerateValuesByType(Random random, EtpDataType? etpDataType, bool isChannelValue)
        {
            string column = string.Empty;

            bool setToNull = (random.Next() % 5 == 0);
            if (setToNull && !isChannelValue)
                return string.Empty;

            switch (etpDataType)
            {
                case EtpDataType.boolean:
                    column = (random.Next() % 2 == 0) ? "true" : "false";
                    break;
                case EtpDataType.bytes:
                    column = "Y";
                    break;
                case EtpDataType.@double:
                case EtpDataType.@float:                    
                        column = string.Format(" {0:0.###}", random.NextDouble());
                    break;
                case EtpDataType.@int:
                case EtpDataType.@long:
                    column = string.Format(" {0:0}", random.Next());
                    break;
                case EtpDataType.@null:
                    column = "null";
                    break;
                case EtpDataType.@string:
                    column = "\"abc\"";
                    break;
                case EtpDataType.vector:
                    column = "(1.0, 2.0, 3.0)";
                    break;
                default:
                    column = "null";
                    break;
            }
            return column;
        }
    }
}
