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
            log.Index = new List<ChannelIndex>();
            log.LoggingMethod = loggingMethod;

            if (indexType == ChannelIndexType.measureddepth)
            {
                log.StartIndex = new DepthIndexValue();
                log.EndIndex = new DepthIndexValue();
                log.TimeDepth = "depth";

                log.Index.Add(new ChannelIndex()
                {
                    IndexType = ChannelIndexType.measureddepth,
                    Mnemonic = "MD",
                    Uom = "m"
                });

                log.ChannelSet.Add(new ChannelSet()
                {
                    Uuid = Uid(),
                    Citation = new Citation() { Title = Name("Channel Set 01") },
                    ExistenceKind = ExistenceKind.simulated,
                    Channel = new List<Channel>()
                    {
                        new Channel()
                        {
                            Citation = new Citation() { Description = "Rate of Penetration" },
                            Mnemonic = "ROP",
                            UoM = "m/h",
                            PwlsClass = "Velocity",
                            Source = loggingMethod.ToString(),
                            DataType = "double",
                            Index = log.Index,
                        },
                        new Channel()
                        {
                            Citation = new Citation() { Description = "Hookload" },
                            Mnemonic = "HKLD",
                            UoM = "klbf",
                            PwlsClass = "Weight",
                            Source = loggingMethod.ToString(),
                            DataType = "double",
                            Index = log.Index,
                        }
                    }
                });
            }
            else if (indexType == ChannelIndexType.datetime)
            {
                log.StartIndex = new TimeIndexValue();
                log.EndIndex = new TimeIndexValue();
                log.TimeDepth = "time";

                log.Index.Add(new ChannelIndex()
                {
                    IndexType = ChannelIndexType.datetime,
                    Mnemonic = "TIME"
                });

                log.ChannelSet.Add(new ChannelSet()
                {
                    Uuid = Uid(),
                    Citation = new Citation() { Title = Name("Channel Set 02") },
                    ExistenceKind = ExistenceKind.simulated,
                    Channel = new List<Channel>()
                    {
                        new Channel()
                        {
                            Citation = new Citation() { Description = "Rate of Penetration" },
                            Mnemonic = "ROP",
                            UoM = "m/h",
                            PwlsClass = "Velocity",
                            Source = loggingMethod.ToString(),
                            DataType = "double",
                            Index = log.Index,
                        },
                        new Channel()
                        {
                            Citation = new Citation() { Description = "Hookload" },
                            Mnemonic = "HKLD",
                            UoM = "klbf",
                            PwlsClass = "Weight",
                            Source = loggingMethod.ToString(),
                            DataType = "double",
                            Index = log.Index,
                        }
                    }
                });
            }
        }
    }
}
