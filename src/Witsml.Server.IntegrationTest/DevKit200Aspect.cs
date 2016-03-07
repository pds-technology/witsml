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

        public void InitHeader(Log log, LoggingMethod loggingMethod, ChannelIndexType indexType)
        {
            log.ChannelSet = new List<ChannelSet>();

            if (indexType == ChannelIndexType.measureddepth)
            {
                var index = new List<ChannelIndex>()
                {
                    new ChannelIndex()
                    {
                        IndexType = ChannelIndexType.measureddepth,
                        Mnemonic = "MD",
                        Uom = "m"
                    }
                };

                log.ChannelSet.Add(new ChannelSet()
                {
                    Uuid = Uid(),
                    Citation = new Citation() { Title = Name("Channel Set 01") },
                    ExistenceKind = ExistenceKind.simulated,
                    Index = index,

                    Channel = new List<Channel>()
                    {
                        new Channel()
                        {
                            Citation = new Citation() { Description = "Rate of Penetration" },
                            Mnemonic = "ROP",
                            UoM = "m/h",
                            LoggingMethod = loggingMethod,
                            Source = loggingMethod.ToString(),
                            DataType = EtpDataType.@double,
                            Index = index,
                            StartIndex = new DepthIndexValue() { Depth = 0 },
                            EndIndex = null, // new DepthIndexValue() { Depth = 1000 },
                            TimeDepth = "depth",
                        },
                        new Channel()
                        {
                            Citation = new Citation() { Description = "Hookload" },
                            Mnemonic = "HKLD",
                            UoM = "klbf",
                            LoggingMethod = loggingMethod,
                            Source = loggingMethod.ToString(),
                            DataType = EtpDataType.@double,
                            Index = index,
                            StartIndex = new DepthIndexValue() { Depth = 0 },
                            EndIndex = null, // new DepthIndexValue() { Depth = 1000 },
                            TimeDepth = "depth",
                        }
                    },

                    DataContext = new IndexRangeContext()
                    {
                        StartIndex = new DepthIndexValue() { Depth = 0 },
                        EndIndex = new DepthIndexValue() { Depth = 1000 },
                    },

                    Data = new ChannelData()
                    {
                    }
                });
            }
            else if (indexType == ChannelIndexType.datetime)
            {
                var index = new List<ChannelIndex>()
                {
                    new ChannelIndex()
                    {
                        IndexType = ChannelIndexType.datetime,
                        Mnemonic = "TIME",
                    }
                };

                log.ChannelSet.Add(new ChannelSet()
                {
                    Uuid = Uid(),
                    Citation = new Citation() { Title = Name("Channel Set 02") },
                    ExistenceKind = ExistenceKind.simulated,
                    Index = index,

                    Channel = new List<Channel>()
                    {
                        new Channel()
                        {
                            Citation = new Citation() { Description = "Rate of Penetration" },
                            Mnemonic = "ROP",
                            UoM = "m/h",
                            LoggingMethod = loggingMethod,
                            Source = loggingMethod.ToString(),
                            DataType = EtpDataType.@double,
                            Index = index,
                            StartIndex = new TimeIndexValue(),
                            EndIndex = null, // new TimeIndexValue(),
                            TimeDepth = "time",
                        },
                        new Channel()
                        {
                            Citation = new Citation() { Description = "Hookload" },
                            Mnemonic = "HKLD",
                            UoM = "klbf",
                            LoggingMethod = loggingMethod,
                            Source = loggingMethod.ToString(),
                            DataType = EtpDataType.@double,
                            Index = index,
                            StartIndex = new TimeIndexValue(),
                            EndIndex = null, // new TimeIndexValue(),
                            TimeDepth = "time",
                        }
                    },

                    DataContext = new IndexRangeContext()
                    {
                        StartIndex = new TimeIndexValue(),
                        EndIndex = new TimeIndexValue(),
                    },

                    Data = new ChannelData()
                    {
                    }
                });
            }
        }
    }
}
