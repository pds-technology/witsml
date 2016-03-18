using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;

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

        /// <summary>
        /// Generates the log with channel data.
        /// </summary>
        /// <param name="isDepthLog">if set to <c>true</c> [is depth log].</param>
        /// <param name="loggingMethod">The logging method.</param>
        /// <param name="numDataValue">The number data value.</param>
        /// <returns>A log</returns>
        public Log GenerateLog(bool isDepthLog=true, LoggingMethod loggingMethod = LoggingMethod.Computed, int numDataValue=5)
        {
            Log log = CreateLog();

            ChannelSet channelSet;
            if (isDepthLog)
            {
                log.TimeDepth = "Depth";
                channelSet = CreateDepthChannelSet_Increasing_Index(log, loggingMethod);
            }
            else
            {
                log.TimeDepth = "Time";
                channelSet = CreateTimeChannelSet(log, loggingMethod);
            }
            log.ChannelSet.Add(channelSet);

            GenerateChannelData(log.ChannelSet, numDataValue: numDataValue);

            return log;
        }

        /// <summary>
        /// Creates the log with some header fields initialized and without channelset.
        /// </summary>
        /// <returns></returns>
        public Log CreateLog()
        {
            Log log = new Log();
            log.Citation = Citation("Generated Citation");
            log.Uuid = "Generated Uuid";

            log.ChannelSet = new List<ChannelSet>();
            log.CurveClass = "ABC curve class";
            log.LoggingCompanyName = "ABC Logging Company";
            log.Wellbore = DataObjectReference(ObjectTypes.Wellbore, "Wellbore for generated log", "Wellbore Uuid for generated log");
            return log;
        }

        /// <summary>
        /// Generates the channel data.
        /// </summary>
        /// <param name="channelSetList">The channel set list.</param>
        /// <param name="numDataValue">The number of data value rows.</param>
        public void GenerateChannelData(List<ChannelSet> channelSetList, int numDataValue=5)
        {         
            Random random = new Random(123);
            DateTime dateTimeStart = new DateTime(2015, 3, 17, 11, 50, 0);

            foreach (ChannelSet channelSet in channelSetList)
            {
                string logData = "[ ";
                double indexDepthValue = 0.0;
                DateTime indexTimeValue = dateTimeStart;
                bool isIncreasing = true;
                for (int i= 0; i < numDataValue; i++)
                {
                    if (i>0)
                    {
                        logData += ", ";
                    }
                    string entry = string.Empty;
                    bool isIndex = false;                   
                    foreach (Channel channel in channelSet.Channel)
                    {
                        var index = channelSet.Index.Where(x => x.Mnemonic == channel.Mnemonic).SingleOrDefault();
                        if (index!=null)
                        {
                            isIndex = true;
                            isIncreasing = index.Direction.HasValue ? index.Direction.Value == IndexDirection.increasing : false;
                            if (i == 0)
                            {
                                indexDepthValue = (isIncreasing) ? 1.0 : -1.0;
                            }
                            indexDepthValue = isIncreasing ? indexDepthValue + random.Next(1, 5) : indexDepthValue - random.Next(1, 5);
                            indexTimeValue = indexTimeValue.AddSeconds(random.Next(1, 5));
                        }
                        else
                        {
                            isIndex = false;
                        }

                        entry = entry==string.Empty ? "[ " : entry + ", ";
                        
                        var column = string.Empty;
                        if (channel.PointMetadata == null)
                        {
                            bool setToNull = (random.Next() % 5 == 0);
                            if (setToNull && !isIndex)
                            {
                                column += "null";
                            }
                            else
                            {
                                var columnValue = GenerateValuesByType(random, ref indexDepthValue, ref indexTimeValue, isIndex, isIncreasing, channel.DataType);
                                column += "[ " + columnValue + " ]";
                            }
                            entry += column;
                        }
                        else
                        {
                            foreach (PointMetadata pointMetaData in channel.PointMetadata)
                            {
                                column = (column == string.Empty) ? "[ " : column + ", ";

                                var etpDataType = pointMetaData.EtpDataType ?? null;
                                column += GenerateValuesByType(random, ref indexDepthValue, ref indexTimeValue, isIndex, isIncreasing, etpDataType);
                            }
                            column += " ] ";
                            entry += column;
                        }
                    }
                    entry += "] ";
                    logData += entry;
                }               
                logData += " ]";
                channelSet.Data.Data = logData;
            }           
        }

        /// <summary>
        /// Creates the depth channel set with increasing index.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="loggingMethod">The logging method.</param>
        /// <returns></returns>
        public ChannelSet CreateDepthChannelSet_Increasing_Index(Log log, LoggingMethod loggingMethod = LoggingMethod.Computed)
        {
            var index = new List<ChannelIndex>()
                        {
                            new ChannelIndex()
                            {
                                Direction = IndexDirection.increasing,
                                IndexType = ChannelIndexType.measureddepth,
                                Mnemonic = "MD",
                                Uom = "ft",
                                // Description: For depth indexes, this contains the uid of the datum, in the Channel's Well object, to which all of the index values are referenced.
                                DatumReference = "Uid of wellbore datum"
                            }
                        };

            ChannelSet channelSet = new ChannelSet()
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
                            Citation = Citation("Citation_01"),
                            Mnemonic = "MD",
                            UoM = "ft",
                            CurveClass = "Measured Depth",
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
                                    Name = "Depth Index",
                                    Description = "Depth Index Value",
                                    EtpDataType = EtpDataType.@double
                                },
                                new PointMetadata()
                                {
                                    Name = "Date",
                                    Description = "Date",
                                    EtpDataType = EtpDataType.@string
                                }
                            }
                        },
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
                        },
                        new Channel()
                        {
                            Citation = Citation("Citation_04"),
                            Mnemonic = "Sp",
                            UoM = "mV",
                            CurveClass = "Sp",
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
                },


                Data = new ChannelData()
                {
                    FileUri = "file://",

                    Data = null
                }
            };
            return channelSet;
        }

        /// <summary>
        /// Creates the depth channel set with decreasing index.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="loggingMethod">The logging method.</param>
        /// <returns></returns>
        public ChannelSet CreateDepthChannelSet_Decreasing_Index(Log log, LoggingMethod loggingMethod = LoggingMethod.Computed)
        {
            var index = new List<ChannelIndex>()
                        {
                            new ChannelIndex()
                            {
                                Direction = IndexDirection.decreasing,
                                IndexType = ChannelIndexType.trueverticaldepth,
                                Mnemonic = "TVDSS",
                                Uom = "ft",
                                // Description: For depth indexes, this contains the uid of the datum, in the Channel's Well object, to which all of the index values are referenced.
                                DatumReference = "Uid of wellbore datum"
                            }
                        };

            ChannelSet channelSet = new ChannelSet()
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
                            Citation = Citation("Citation_01"),
                            Mnemonic = "TVDSS",
                            UoM = "ft",
                            CurveClass = "TVDSS",
                            LoggingMethod = loggingMethod,
                            LoggingCompanyName = log.LoggingCompanyName,
                            Source = loggingMethod.ToString(),
                            DataType = EtpDataType.@double,
                            Status = ChannelStatus.active,
                            Index = index,
                            StartIndex = new DepthIndexValue() { Depth = 0 },
                            EndIndex = new DepthIndexValue() { Depth = -1000 },
                            TimeDepth = log.TimeDepth,
                            PointMetadata = new List<PointMetadata>()
                            {
                                new PointMetadata()
                                {
                                    Name = "Depth Index",
                                    Description = "Depth Index Value",
                                    EtpDataType = EtpDataType.@double
                                },
                                new PointMetadata()
                                {
                                    Name = "Date",
                                    Description = "Date",
                                    EtpDataType = EtpDataType.@string
                                }
                            }
                        },
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
                            EndIndex = new DepthIndexValue() { Depth = -1000 },
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
                            EndIndex = new DepthIndexValue() { Depth = -1000 },
                            TimeDepth = log.TimeDepth,
                        },
                        new Channel()
                        {
                            Citation = Citation("Citation_04"),
                            Mnemonic = "Sp",
                            UoM = "mV",
                            CurveClass = "Sp",
                            LoggingMethod = loggingMethod,
                            LoggingCompanyName = log.LoggingCompanyName,
                            Source = loggingMethod.ToString(),
                            DataType = EtpDataType.@double,
                            Status = ChannelStatus.active,
                            Index = index,
                            StartIndex = new DepthIndexValue() { Depth = 0 },
                            EndIndex = new DepthIndexValue() { Depth = -1000 },
                            TimeDepth = log.TimeDepth,
                        }
                    },

                DataContext = new IndexRangeContext()
                {
                    StartIndex = new DepthIndexValue() { Depth = 0 },
                    EndIndex = new DepthIndexValue() { Depth = -1000 },
                },


                Data = new ChannelData()
                {
                    FileUri = "file://",

                    Data = null
                }
            };
            return channelSet;
        }

        /// <summary>
        /// Creates the time channel set.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="loggingMethod">The logging method.</param>
        /// <returns></returns>
        public ChannelSet CreateTimeChannelSet(Log log, LoggingMethod loggingMethod = LoggingMethod.Computed)
        {
            var index = new List<ChannelIndex>()
                        {
                            new ChannelIndex()
                            {
                                Direction = IndexDirection.increasing,
                                IndexType = ChannelIndexType.datetime,
                                Mnemonic = "TIME",
                                Uom = "s",
                                // Description: For depth indexes, this contains the uid of the datum, in the Channel's Well object, to which all of the index values are referenced.
                                DatumReference = "Sea Level"
                            }
                        };

            ChannelSet channelSet = new ChannelSet()
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
                            Citation = Citation("Citation_02_a"),
                            Mnemonic = "TIME",
                            UoM = "s",
                            CurveClass = "Time",
                            LoggingMethod = loggingMethod,
                            LoggingCompanyName = log.LoggingCompanyName,
                            Source = loggingMethod.ToString(),
                            DataType = EtpDataType.@string,
                            Status = ChannelStatus.active,
                            Index = index,
                            StartIndex = new TimeIndexValue(),
                            EndIndex = new TimeIndexValue(),
                            TimeDepth = log.TimeDepth,
                        },
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
                },

                Data = new ChannelData()
                {
                    FileUri = "file://",

                    //Data = logData
                }
            };
            return channelSet;
        }

        private static string GenerateValuesByType(Random random, ref double indexDepthValue, ref DateTime indexTimeValue, bool isIndex, bool isIncreasing, EtpDataType? etpDataType)
        {
            string column = string.Empty;

            bool setToNull = (random.Next() % 5 == 0);
            if (setToNull && !isIndex)
                return "null";

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
                    if (isIndex)
                    {
                        var value = isIncreasing ? indexDepthValue + random.Next(1, 10) / 10.0 : indexDepthValue - random.Next(1, 10) / 10.0;
                        column = string.Format(" {0:0.###}", value);
                    }
                    else
                    {
                        column = string.Format(" {0:0.###}", random.NextDouble());
                    }
                    break;
                case EtpDataType.@int:
                case EtpDataType.@long:
                    if (isIndex)
                    {
                        var value = isIncreasing ? indexDepthValue + random.Next(1, 5) : indexDepthValue - random.Next(1, 5);
                        column = string.Format(" {0:0}", value);
                    }
                    else
                    {
                        column = string.Format(" {0:0}", random.Next());
                    }
                    break;
                case EtpDataType.@null:
                    column = "null";
                    break;
                case EtpDataType.@string:
                    column = "\"" + indexTimeValue + "\"";
                    break;
                case EtpDataType.vector:
                    column = "(1.0, 2.0, 3.0)";
                    break;
                default:
                    break;
            }
            return column;
        }

        private DataObjectReference DataObjectReference(string objectType, string title = null, string uuid = null)
        {
            return new DataObjectReference
            {
                ContentType = EtpContentTypes.Witsml200.For(objectType),
                Title = (title == null) ? "Test title for " + objectType : title,
                Uuid = uuid == null ? "Test Uuid for " + objectType : uuid,
            };
        }

    }
}
