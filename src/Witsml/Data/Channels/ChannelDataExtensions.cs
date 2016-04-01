using System.Collections.Generic;
using System.Linq;
using PDS.Framework;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.Witsml.Data.Channels
{
    public static class ChannelDataExtensions
    {
        public static ChannelDataReader GetReader(this Witsml131.Log log)
        {
            var isTimeIndex = log.IndexType.GetValueOrDefault() == Witsml131.ReferenceData.LogIndexType.datetime;
            var increasing = log.Direction.GetValueOrDefault() == Witsml131.ReferenceData.LogIndexDirection.increasing;

            // Split index curve from other value curves
            var indexCurve = log.LogCurveInfo.FirstOrDefault(x => x.Mnemonic.EqualsIgnoreCase(log.IndexCurve.Value));
            var mnemonics = log.LogCurveInfo.Where(x => x.Mnemonic != indexCurve.Mnemonic).Select(x => x.Mnemonic).ToArray();
            var units = log.LogCurveInfo.Where(x => x.Mnemonic != indexCurve.Mnemonic).Select(x => x.Unit).ToArray();

            return new ChannelDataReader(log.LogData, mnemonics, units, log.GetUri())
                // Add index curve to separate collection
                .WithIndex(indexCurve.Mnemonic, indexCurve.Unit, increasing, isTimeIndex);
        }

        public static IEnumerable<ChannelDataReader> GetReaders(this Witsml141.Log log)
        {
            if (log.LogData == null)
                yield break;

            var isTimeIndex = log.IndexType.GetValueOrDefault() == Witsml141.ReferenceData.LogIndexType.datetime;
            var increasing = log.Direction.GetValueOrDefault() == Witsml141.ReferenceData.LogIndexDirection.increasing;

            foreach (var logData in log.LogData)
            {
                // Split index curve from other value curves
                var indexCurve = log.LogCurveInfo.FirstOrDefault(x => x.Mnemonic.Value.EqualsIgnoreCase(log.IndexCurve));
                var mnemonics = ChannelDataReader.Split(logData.MnemonicList).Skip(1).ToArray();
                var units = ChannelDataReader.Split(logData.UnitList).Skip(1).ToArray();

                yield return new ChannelDataReader(logData.Data, mnemonics, units, log.GetUri())
                    // Add index curve to separate collection
                    .WithIndex(indexCurve.Mnemonic.Value, indexCurve.Unit, increasing, isTimeIndex);
            }
        }

        public static IEnumerable<ChannelDataReader> GetReaders(this Witsml200.Log log)
        {
            if (log.ChannelSet == null)
                yield break;

            foreach (var channelSet in log.ChannelSet)
            {
                yield return channelSet.GetReader();
            }
        }

        public static ChannelDataReader GetReader(this Witsml200.ChannelSet channelSet)
        {
            // Not including index channels with value channels
            var mnemonics = channelSet.Channel.Select(x => x.Mnemonic).ToArray();
            var units = channelSet.Channel.Select(x => x.UoM).ToArray();

            return new ChannelDataReader(channelSet.Data.Data, mnemonics, units, channelSet.GetUri())
                // Add index channels to separate collection
                .WithIndices(channelSet.Index.Select(ToChannelIndexInfo), true);
        }

        public static ChannelDataReader WithIndex(this ChannelDataReader reader, string mnemonic, string unit, bool increasing, bool isTimeIndex)
        {
            var index = new ChannelIndexInfo()
            {
                Mnemonic = mnemonic,
                Increasing = increasing,
                IsTimeIndex = isTimeIndex,
                Unit = unit
            };

            reader.Indices.Add(index);
            CalculateIndexRange(reader, index, reader.Indices.Count - 1);

            return reader;
        }

        public static ChannelDataReader WithIndices(this ChannelDataReader reader, IEnumerable<ChannelIndexInfo> indices, bool calculate = false)
        {
            foreach (var index in indices)
            {
                reader.Indices.Add(index);

                if (calculate)
                {
                    CalculateIndexRange(reader, index, reader.Indices.Count - 1);
                }
            }

            return reader;
        }

        public static ChannelIndexInfo ToChannelIndexInfo(this Witsml200.ComponentSchemas.ChannelIndex channelIndex)
        {
            return new ChannelIndexInfo()
            {
                Mnemonic = channelIndex.Mnemonic,
                Unit = channelIndex.Uom,
                Increasing = channelIndex.Direction.GetValueOrDefault() == Witsml200.ReferenceData.IndexDirection.increasing,
                IsTimeIndex = channelIndex.IndexType.GetValueOrDefault() == Witsml200.ReferenceData.ChannelIndexType.datetime
            };
        }

        private static void CalculateIndexRange(ChannelDataReader reader, ChannelIndexInfo channelIndex, int index)
        {
            var range = reader.GetIndexRange(index);
            channelIndex.Start = range.Start.GetValueOrDefault(double.NaN);
            channelIndex.End = range.End.GetValueOrDefault(double.NaN);
        }
    }
}
