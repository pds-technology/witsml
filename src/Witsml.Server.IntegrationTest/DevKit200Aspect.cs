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
                    new ChannelIndex()
                    {
                        Direction = IndexDirection.increasing,
                        IndexType = ChannelIndexType.measureddepth,
                        Mnemonic = "MD",
                        Uom = "m",
                        DatumReference = "MSL"
                    }
                };

                log.ChannelSet.Add(new ChannelSet()
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
                    },

                    //Data = new ChannelData()
                    //{
                    //}
                });
            }
            else if (indexType == ChannelIndexType.datetime)
            {
                log.TimeDepth = "time";

                var index = new List<ChannelIndex>()
                {
                    new ChannelIndex()
                    {
                        Direction = IndexDirection.increasing,
                        IndexType = ChannelIndexType.datetime,
                        Mnemonic = "TIME",
                        Uom = "s",
                        DatumReference = "MSL"
                    }
                };

                log.ChannelSet.Add(new ChannelSet()
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
                    },

                    //Data = new ChannelData()
                    //{
                    //}
                });
            }
        }
    }
}
