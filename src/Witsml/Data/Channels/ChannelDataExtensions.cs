//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using PDS.Witsml.Data.Logs;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.Witsml.Data.Channels
{
    public static class ChannelDataExtensions
    {
        public static ChannelDataReader GetReader(this IEnumerable<IChannelDataRecord> records)
        {
            return new ChannelDataReader(records);
        }

        public static bool IsIncreasing(this Witsml200.ChannelSet channelSet)
        {
            if (channelSet?.Index == null) return true;
            return channelSet.Index.Select(x => x.IsIncreasing()).FirstOrDefault();
        }

        public static bool IsIncreasing(this Witsml200.ComponentSchemas.ChannelIndex channelIndex)
        {
            return channelIndex.Direction.GetValueOrDefault(Witsml200.ReferenceData.IndexDirection.increasing) == Witsml200.ReferenceData.IndexDirection.increasing;
        }

        public static bool IsTimeIndex(this Witsml200.ChannelSet channelSet, bool includeElapsedTime = false)
        {
            if (channelSet?.Index == null) return false;
            return channelSet.Index.Select(x => x.IsTimeIndex(includeElapsedTime)).FirstOrDefault();
        }

        public static bool IsTimeIndex(this Witsml200.ComponentSchemas.ChannelIndex channelIndex, bool includeElapsedTime = false)
        {
            return channelIndex.IndexType.GetValueOrDefault() == Witsml200.ReferenceData.ChannelIndexType.datetime ||
                   (channelIndex.IndexType.GetValueOrDefault() == Witsml200.ReferenceData.ChannelIndexType.elapsedtime && includeElapsedTime);
        }

        public static ChannelDataReader GetReader(this Witsml131.Log log)
        {
            if (log.LogData == null || !log.LogData.Any()) return null;

            var isTimeIndex = log.IsTimeLog();
            var increasing = log.IsIncreasing();

            // Split index curve from other value curves
            var indexCurve = log.LogCurveInfo.GetByMnemonic(log.IndexCurve.Value);
            var mnemonics = log.LogCurveInfo.Where(x => x != indexCurve).Select(x => x.Mnemonic).ToArray();
            var units = log.LogCurveInfo.Where(x => x != indexCurve).Select(x => x.Unit).ToArray();
            var nullValues = log.GetNullValues(mnemonics).ToArray();

            return new ChannelDataReader(log.LogData, mnemonics, units, nullValues, log.GetUri())
                // Add index curve to separate collection
                .WithIndex(indexCurve.Mnemonic, indexCurve.Unit, increasing, isTimeIndex);
        }
      
        public static IEnumerable<ChannelDataReader> GetReaders(this Witsml141.Log log)
        {
            if (log.LogData == null) yield break;

            var isTimeIndex = log.IsTimeLog();
            var increasing = log.IsIncreasing();

            foreach (var logData in log.LogData)
            {
                if (logData.Data == null || !logData.Data.Any())
                    continue;

                var mnemonics = ChannelDataReader.Split(logData.MnemonicList);
                var units = ChannelDataReader.Split(logData.UnitList);
                var nullValues = log.GetNullValues(mnemonics).Skip(1).ToArray();

                // Split index curve from other value curves
                var indexCurve = log.LogCurveInfo.GetByMnemonic(log.IndexCurve) ?? new Witsml141.ComponentSchemas.LogCurveInfo
                {
                    Mnemonic = new Witsml141.ComponentSchemas.ShortNameStruct(mnemonics.FirstOrDefault()),
                    Unit = units.FirstOrDefault()
                };

                // Skip index curve when passing mnemonics to reader
                mnemonics = mnemonics.Skip(1).ToArray();
                units = units.Skip(1).ToArray();

                yield return new ChannelDataReader(logData.Data, mnemonics, units, nullValues, log.GetUri())
                    // Add index curve to separate collection
                    .WithIndex(indexCurve.Mnemonic.Value, indexCurve.Unit, increasing, isTimeIndex);
            }
        }

        public static IEnumerable<ChannelDataReader> GetReaders(this Witsml200.Log log)
        {
            if (log.ChannelSet == null) yield break;

            foreach (var channelSet in log.ChannelSet)
            {
                var reader = channelSet.GetReader();
                if (reader == null) continue;
                yield return reader;
            }
        }

        public static ChannelDataReader GetReader(this Witsml200.ChannelSet channelSet)
        {
            if (string.IsNullOrWhiteSpace(channelSet?.Data?.Data)) return null;

            // Not including index channels with value channels
            var mnemonics = channelSet.Channel.Select(x => x.Mnemonic).ToArray();
            var units = channelSet.Channel.Select(x => x.UoM).ToArray();
            var nullValues = new string[units.Length];

            return new ChannelDataReader(channelSet.Data.Data, mnemonics, units, nullValues, channelSet.GetUri())
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

            return reader.Sort();
        }

        public static ChannelDataReader WithIndices(this ChannelDataReader reader, IEnumerable<ChannelIndexInfo> indices, bool calculate = false, bool reverse = false)
        {
            foreach (var index in indices)
            {
                reader.Indices.Add(index);

                if (calculate)
                {
                    CalculateIndexRange(reader, index, reader.Indices.Count - 1);
                }
            }

            return calculate ? reader.Sort(reverse) : reader;
        }

        public static ChannelIndexInfo ToChannelIndexInfo(this Witsml200.ComponentSchemas.ChannelIndex channelIndex)
        {
            return new ChannelIndexInfo()
            {
                Mnemonic = channelIndex.Mnemonic,
                Unit = channelIndex.Uom,
                Increasing = channelIndex.IsIncreasing(),
                IsTimeIndex = channelIndex.IsTimeIndex()
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
