using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using System.Collections.Generic;

namespace PDS.Witsml.Server
{
    public class DevKit200Aspect : DevKitAspect
    {
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

        public void InitHeader(Log log, LoggingMethod loggingMethod, ChannelIndexType indexType)
        {
            log.ChannelSet = new List<ChannelSet>();
            log.LoggingCompanyName = "Service Co.";
            log.CurveClass = "unknown";

            if (indexType == ChannelIndexType.measureddepth)
            {
                log.TimeDepth = "depth";

                var index = new List<ChannelIndex>()
                {
                    CreateChannelIndex(indexType)
                };

                var channelSet = new ChannelSet()
                {
                    Uuid = Uid(),
                    Citation = Citation("Channel Set 01"),
                    ExistenceKind = ExistenceKind.simulated,
                    Index = index,

                    LoggingCompanyName = log.LoggingCompanyName,
                    TimeDepth = log.TimeDepth,
                    CurveClass = log.CurveClass,

                    Channel = new List<Channel>()
                    {
                        new Channel()
                        {
                            Citation = Citation("Rate of Penetration"),
                            Mnemonic = "ROP",
                            UoM = "m/h",
                            CurveClass = "Velocity",
                            LoggingMethod = loggingMethod,
                            LoggingCompanyName = log.LoggingCompanyName,
                            Source = loggingMethod.ToString(),
                            DataType = EtpDataType.@double,
                            Status = ChannelStatus.active,
                            Index = index,
                            StartIndex = new DepthIndexValue() { Depth = 0 },
                            EndIndex = new DepthIndexValue() { Depth = 1000 },
                            TimeDepth = log.TimeDepth,
                            PointMetadata = new List<PointMetadata>()
                            {
                                new PointMetadata()
                                {
                                    Name = "Value",
                                    Description = "Value",
                                    EtpDataType = EtpDataType.@double
                                },
                                new PointMetadata()
                                {
                                    Name = "Quality",
                                    Description = "Quality",
                                    EtpDataType = EtpDataType.boolean
                                }
                            }
                        },
                        new Channel()
                        {
                            Citation = Citation("Hookload"),
                            Mnemonic = "HKLD",
                            UoM = "klbf",
                            CurveClass = "Force",
                            LoggingMethod = loggingMethod,
                            LoggingCompanyName = log.LoggingCompanyName,
                            Source = loggingMethod.ToString(),
                            DataType = EtpDataType.@double,
                            Status = ChannelStatus.active,
                            Index = index,
                            StartIndex = new DepthIndexValue() { Depth = 0 },
                            EndIndex = new DepthIndexValue() { Depth = 1000 },
                            TimeDepth = log.TimeDepth,
                        }
                    },

                    DataContext = new IndexRangeContext()
                    {
                        StartIndex = new DepthIndexValue() { Depth = 0 },
                        EndIndex = new DepthIndexValue() { Depth = 1000 },
                    }
                };

                CreateMockChannelSetData(channelSet, channelSet.Index);
                log.ChannelSet.Add(channelSet);
                
            }
            else if (indexType == ChannelIndexType.datetime)
            {
                log.TimeDepth = "time";

                var index = new List<ChannelIndex>()
                {
                    CreateChannelIndex(indexType)
                };

                var channelSet = new ChannelSet()
                {
                    Uuid = Uid(),
                    Citation = Citation("Channel Set 02"),
                    ExistenceKind = ExistenceKind.simulated,
                    Index = index,

                    LoggingCompanyName = log.LoggingCompanyName,
                    TimeDepth = log.TimeDepth,
                    CurveClass = log.CurveClass,

                    Channel = new List<Channel>()
                    {
                        new Channel()
                        {
                            Citation = Citation("Rate of Penetration"),
                            Mnemonic = "ROP",
                            UoM = "m/h",
                            CurveClass = "Velocity",
                            LoggingMethod = loggingMethod,
                            LoggingCompanyName = log.LoggingCompanyName,
                            Source = loggingMethod.ToString(),
                            DataType = EtpDataType.@double,
                            Status = ChannelStatus.active,
                            Index = index,
                            StartIndex = new TimeIndexValue(),
                            EndIndex = new TimeIndexValue(),
                            TimeDepth = log.TimeDepth,
                        },
                        new Channel()
                        {
                            Citation = Citation("Hookload"),
                            Mnemonic = "HKLD",
                            UoM = "klbf",
                            CurveClass = "Force",
                            LoggingMethod = loggingMethod,
                            LoggingCompanyName = log.LoggingCompanyName,
                            Source = loggingMethod.ToString(),
                            DataType = EtpDataType.@double,
                            Status = ChannelStatus.active,
                            Index = index,
                            StartIndex = new TimeIndexValue(),
                            EndIndex = new TimeIndexValue(),
                            TimeDepth = log.TimeDepth,
                        }
                    },

                    DataContext = new IndexRangeContext()
                    {
                        StartIndex = new TimeIndexValue(),
                        EndIndex = new TimeIndexValue(),
                    }
                };
                CreateMockChannelSetData(channelSet, channelSet.Index);
                log.ChannelSet.Add(channelSet);
            }
        }

        public ChannelIndex CreateChannelIndex(ChannelIndexType indexType)
        {
            if (indexType == ChannelIndexType.measureddepth)
            {
                return new ChannelIndex()
                {
                    Direction = IndexDirection.increasing,
                    IndexType = ChannelIndexType.measureddepth,
                    Mnemonic = "MD",
                    Uom = "m",
                    DatumReference = "MSL"
                };
            }
            else if (indexType == ChannelIndexType.datetime)
            {
                return new ChannelIndex()
                {
                    Direction = IndexDirection.increasing,
                    IndexType = ChannelIndexType.datetime,
                    Mnemonic = "TIME",
                    Uom = "s",
                    DatumReference = "MSL"
                };
            }

            return null;
        }

        public void CreateMockChannelSetData(ChannelSet channelSet, List<ChannelIndex> indices)
        {
            var data = new ChannelData()
            {
                FileUri = "file://",

                //Data = @"[ 0.0, 1.0, 2.0, 3.0 ]"

                //Data = @"[
                //    [ 0.0, 1.0, 2.0, 3.0 ],
                //    [ 0.1, 1.1, 2.1, 3.1 ]
                //]"

                //Data = @"[
                //    [ [0.0, ""2016-01-01T00:00:00.0000Z"" ], 1.0, 2.0, 3.0 ],
                //    [ [0.1, ""2016-01-01T00:00:01.0000Z"" ], 1.1, null, 3.1 ]
                //]"
            };

            if (indices.Count == 1)
            {
                if (indices[0].IndexType == ChannelIndexType.measureddepth)
                {
                    data.Data = @"[
                            [ [0.0 ], [ 1.0, true ], [ 2.0 ], [ 3.0 ] ],
                            [ [0.1 ], [ 1.1, false ], null, [ 3.1 ] ],
                            [ [0.2 ], null, null, [ 3.2 ] ],
                            [ [0.3 ], [ 1.3, true ], [ 2.3 ], [ 3.3 ] ]
                        ]";
                }
                else if (indices[0].IndexType == ChannelIndexType.datetime)
                {
                    data.Data = @"[
                            [ [ ""2016-01-01T00:00:00.0000Z"" ], [ 1.0, true ], [ 2.0 ], [ 3.0 ] ],
                            [ [ ""2016-01-01T00:00:01.0000Z"" ], [ 1.1, false ], null, [ 3.1 ] ],
                            [ [ ""2016-01-01T00:00:02.0000Z"" ], null, null, [ 3.2 ] ],
                            [ [ ""2016-01-01T00:00:03.0000Z"" ], [ 1.3, true ], [ 2.3 ], [ 3.3 ] ]
                        ]";
                }
            }
            else if (indices.Count == 2)
            {
                if (indices[0].IndexType == ChannelIndexType.measureddepth)
                {
                    data.Data = @"[
                            [ [0.0, ""2016-01-01T00:00:00.0000Z"" ], [ 1.0, true ], [ 2.0 ], [ 3.0 ] ],
                            [ [0.1, ""2016-01-01T00:00:01.0000Z"" ], [ 1.1, false ], null, [ 3.1 ] ],
                            [ [0.2, ""2016-01-01T00:00:02.0000Z"" ], null, null, [ 3.2 ] ],
                            [ [0.3, ""2016-01-01T00:00:03.0000Z"" ], [ 1.3, true ], [ 2.3 ], [ 3.3 ] ]
                        ]";
                }
                else if (indices[0].IndexType == ChannelIndexType.datetime)
                {
                    data.Data = @"[
                            [ [ ""2016-01-01T00:00:00.0000Z"", 0.0 ], [ 1.0, true ], [ 2.0 ], [ 3.0 ] ],
                            [ [ ""2016-01-01T00:00:01.0000Z"", 0.1 ], [ 1.1, false ], null, [ 3.1 ] ],
                            [ [ ""2016-01-01T00:00:02.0000Z"", 0.2 ], null, null, [ 3.2 ] ],
                            [ [ ""2016-01-01T00:00:03.0000Z"", 0.3 ], [ 1.3, true ], [ 2.3 ], [ 3.3 ] ]
                        ]";
                }
            }
            channelSet.Data = data;
        }
    }
}
