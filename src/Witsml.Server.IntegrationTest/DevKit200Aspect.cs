using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Newtonsoft.Json;
using PDS.Framework;
using PDS.Witsml.Data.Logs;

namespace PDS.Witsml.Server
{
    public class DevKit200Aspect : DevKitAspect
    {
        private Log200Generator LogGenerator;

        public DevKit200Aspect() : base(null, WMLSVersion.WITSML141)
        {
            LogGenerator = new Log200Generator();
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
                Creation = DateTime.UtcNow,
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
                Title = (title == null) ? objectType : title,
                Uuid = uuid == null ? objectType : uuid,
            };
        }


        public void InitHeader(Log log, LoggingMethod loggingMethod, ChannelIndex channelIndex, IndexDirection direction = IndexDirection.increasing)
        {
            log.ChannelSet = new List<ChannelSet>();
            log.LoggingCompanyName = "Service Co.";
            log.CurveClass = "unknown";
            log.LoggingMethod = loggingMethod;

            var index = List(channelIndex); ;
            if (channelIndex.IndexType == ChannelIndexType.measureddepth)
            {
                log.TimeDepth = "depth";

                var pointMetadataList = List(LogGenerator.CreatePointMetadata("Quality", "Quality", EtpDataType.boolean));

                ChannelSet channelSet = LogGenerator.CreateChannelSet(log);
                channelSet.Index = index;

                channelSet.Channel.Add(LogGenerator.CreateChannel(log, index, "Rate of Penetration", "ROP", "m/h", "Velocity", EtpDataType.@double, pointMetadataList: pointMetadataList));
                channelSet.Channel.Add(LogGenerator.CreateChannel(log, index, "Hookload", "HKLD", "klbf", "Force", EtpDataType.@double, null));
                channelSet.Channel.Add(LogGenerator.CreateChannel(log, index, "GR1AX", "GR", "api", "Gamma_Ray", EtpDataType.@double, null));

                CreateMockChannelSetData(channelSet, channelSet.Index);
                log.ChannelSet.Add(channelSet);

            }
            else if (channelIndex.IndexType == ChannelIndexType.datetime)
            {
                log.TimeDepth = "time";

                var pointMetadataList = List(LogGenerator.CreatePointMetadata("Confidence", "Confidence", EtpDataType.@float));

                ChannelSet channelSet = LogGenerator.CreateChannelSet(log);
                channelSet.Index = index;

                channelSet.Channel.Add(LogGenerator.CreateChannel(log, index, "Rate of Penetration", "ROP", "m/h", "Velocity", EtpDataType.@double, pointMetadataList: pointMetadataList));
                channelSet.Channel.Add(LogGenerator.CreateChannel(log, index, "GR1AX", "GR", "api", "Gamma_Ray", EtpDataType.@double, null));

                CreateMockChannelSetData(channelSet, channelSet.Index);
                log.ChannelSet.Add(channelSet);
            }
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
                            [ [0.0 ], [ [ 1.0, true  ], 2.0,  3.0 ] ],
                            [ [0.1 ], [ [ 1.1, false ], null, 3.1 ] ],
                            [ [0.2 ], [ null,           null, 3.2 ] ],
                            [ [0.3 ], [ [ 1.3, true  ], 2.3,  3.3 ] ]
                        ]";
                }
                else if (indices[0].IndexType == ChannelIndexType.datetime)
                {
                    data.Data = @"[
                            [ [ ""2016-01-01T00:00:00.0000Z"" ], [ [ 1.0, true  ], 2.0,  3.0 ] ],
                            [ [ ""2016-01-01T00:00:01.0000Z"" ], [ [ 1.1, false ], null, 3.1 ] ],
                            [ [ ""2016-01-01T00:00:02.0000Z"" ], [ null,           null, 3.2 ] ],
                            [ [ ""2016-01-01T00:00:03.0000Z"" ], [ [ 1.3, true  ], 2.3,  3.3 ] ]
                        ]";
                }
            }
            else if (indices.Count == 2)
            {
                if (indices[0].IndexType == ChannelIndexType.measureddepth)
                {
                    data.Data = @"[
                            [ [0.0, ""2016-01-01T00:00:00.0000Z"" ], [ [1.0, true  ],  2.0,  3.0 ] ],
                            [ [0.1, ""2016-01-01T00:00:01.0000Z"" ], [ [1.1, false ],  null, 3.1 ] ],
                            [ [0.2, ""2016-01-01T00:00:02.0000Z"" ], [ null,           null, 3.2 ] ],
                            [ [0.3, ""2016-01-01T00:00:03.0000Z"" ], [ [1.3, true  ],  2.3,  3.3 ] ]
                        ]";
                }
                else if (indices[0].IndexType == ChannelIndexType.datetime)
                {
                    data.Data = @"[
                            [ [ ""2016-01-01T00:00:00.0000Z"", 0.0 ], [ [ 1.0, true  ], 2.0,  3.0 ] ],
                            [ [ ""2016-01-01T00:00:01.0000Z"", 0.1 ], [ [ 1.1, false ], null, 3.1 ] ],
                            [ [ ""2016-01-01T00:00:02.0000Z"", 0.2 ], [ null,           null, 3.2 ] ],
                            [ [ ""2016-01-01T00:00:03.0000Z"", 0.3 ], [ [ 1.3, true  ], 2.3,  3.3 ] ]
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
            ChannelSet channelSet = LogGenerator.CreateChannelSet(log);
            channelSet.Index = indexList;
            bool isDepth = log.TimeDepth.EqualsIgnoreCase(ObjectFolders.Depth);
            if (isDepth)
            {
                var pointMetadataList = List(LogGenerator.CreatePointMetadata("Quality", "Quality", EtpDataType.boolean));

                channelSet.Channel.Add(LogGenerator.CreateChannel(log, indexList, "Rate of Penetration", "ROP", "m/h", "Velocity", EtpDataType.@double, pointMetadataList: pointMetadataList));
                channelSet.Channel.Add(LogGenerator.CreateChannel(log, indexList, "Hookload", "HKLD", "klbf", "Force", EtpDataType.@double, null));
            }
            else
            {
                var pointMetadataList = List(LogGenerator.CreatePointMetadata("Confidence", "Confidence", EtpDataType.@float));

                channelSet.Channel.Add(LogGenerator.CreateChannel(log, indexList, "Rate of Penetration", "ROP", "m/h", "Velocity", EtpDataType.@double, pointMetadataList: pointMetadataList));
            }
            log.ChannelSet = new List<ChannelSet>();
            log.ChannelSet.Add(channelSet);


            LogGenerator.GenerateChannelData(log.ChannelSet, numDataValue: numDataValue);
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
            log.Citation = Citation("ChannelSet");
            log.Uuid = Uid();

            log.ChannelSet = new List<ChannelSet>();
            log.CurveClass = Name("Curve class");
            log.LoggingCompanyName = Name("ABC Logging Company");
            log.Wellbore = DataObjectReference(ObjectTypes.Wellbore, Name("Wellbore"), Uid());

            List<ChannelIndex> indexList = new List<ChannelIndex>();
            IndexDirection direction = isIncreasing ? IndexDirection.increasing : IndexDirection.decreasing;
            if (LogGenerator.DepthIndexTypes.Contains(indexType))
            {
                log.TimeDepth = ObjectFolders.Depth;
                ChannelIndex channelIndex = LogGenerator.CreateMeasuredDepthIndex(direction);
                if (indexType.Equals(ChannelIndexType.trueverticaldepth))
                {
                    channelIndex = LogGenerator.CreateTrueVerticalDepthIndex(direction);
                }
                else if (indexType.Equals(ChannelIndexType.passindexeddepth))
                {
                    channelIndex = LogGenerator.CreatePassIndexDepthIndex(direction);
                }
                indexList.Add(channelIndex);
            }
            else if (LogGenerator.TimeIndexTypes.Contains(indexType))
            {
                log.TimeDepth = ObjectFolders.Time;
                ChannelIndex channelIndex = LogGenerator.CreateElapsedTimeIndex(direction);
                if (indexType.Equals(ChannelIndexType.datetime))
                {
                    // DateTime should be increasing only
                    indexList.Add(LogGenerator.CreateDateTimeIndex());
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
    }
}
