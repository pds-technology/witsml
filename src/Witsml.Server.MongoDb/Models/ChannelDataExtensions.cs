using System.Collections.Generic;
using System.Linq;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.Witsml.Server.Models
{
    public static class ChannelDataExtensions
    {
        public static ChannelDataReader GetReader(this Witsml131.Log log)
        {
            var mnemonics = log.LogCurveInfo.Select(x => x.Mnemonic).ToArray();
            var units = log.LogCurveInfo.Select(x => x.Unit).ToArray();

            return new ChannelDataReader(log.LogData, mnemonics, units, log.Uid);
        }

        public static IEnumerable<ChannelDataReader> GetReaders(this Witsml141.Log log)
        {
            foreach (var logData in log.LogData)
            {
                var mnemonics = ChannelDataReader.Split(logData.MnemonicList);
                var units = ChannelDataReader.Split(logData.UnitList);

                yield return new ChannelDataReader(logData.Data, mnemonics, units, log.Uid);
            }
        }

        public static IEnumerable<ChannelDataReader> GetReaders(this Witsml200.Log log)
        {
            foreach (var channelSet in log.ChannelSet)
            {
                yield return channelSet.GetReader();
            }
        }

        public static ChannelDataReader GetReader(this Witsml200.ChannelSet channelSet)
        {
            var mnemonics = channelSet.Index.Select(x => x.Mnemonic)
                .Union(channelSet.Channel.Select(x => x.Mnemonic))
                .ToArray();

            var units = channelSet.Index.Select(x => x.Uom)
                .Union(channelSet.Channel.Select(x => x.UoM))
                .ToArray();

            return new ChannelDataReader(channelSet.Data.Data, mnemonics, units, channelSet.Uuid);
        }

        public static ChannelDataReader GetReader(this ChannelDataValues channelDataValues)
        {
            var mnemonics = ChannelDataReader.Split(channelDataValues.MnemonicList);
            var units = ChannelDataReader.Split(channelDataValues.UnitList);

            return new ChannelDataReader(channelDataValues.Data, mnemonics, units, channelDataValues.Uid);
        }
    }
}
