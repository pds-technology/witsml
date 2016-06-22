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
using log4net;
using PDS.Witsml.Data.Logs;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.Witsml.Data.Channels
{
    /// <summary>
    /// Provides extension methods to work with channel data.  Many methods will create a
    /// ChannelDataReader with channel data from different data structures and types.
    /// </summary>
    public static class ChannelDataExtensions
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ChannelDataExtensions));

        /// <summary>
        /// Gets a ChannelDataReader for an <see cref="IEnumerable{IChannelDataRecord}"/>.
        /// </summary>
        /// <param name="records">The records.</param>
        /// <returns>A <see cref="ChannelDataReader"/></returns>
        public static ChannelDataReader GetReader(this IEnumerable<IChannelDataRecord> records)
        {
            return new ChannelDataReader(records);
        }

        /// <summary>
        /// Determines whether this <see cref="Witsml200.ChannelSet"/> instance is increasing.
        /// </summary>
        /// <param name="channelSet">The channel set.</param>
        /// <returns>true if increasing, false otherwise</returns>
        public static bool IsIncreasing(this Witsml200.ChannelSet channelSet)
        {
            if (channelSet?.Index == null) return true;
            return channelSet.Index.Select(x => x.IsIncreasing()).FirstOrDefault();
        }

        /// <summary>
        /// Determines whether this <see cref="Witsml200.ComponentSchemas.ChannelIndex"/> instance is increasing.
        /// </summary>
        /// <param name="channelIndex">Index of the channel.</param>
        /// <returns>true if increasing, false otherwise.</returns>
        public static bool IsIncreasing(this Witsml200.ComponentSchemas.ChannelIndex channelIndex)
        {
            return channelIndex.Direction.GetValueOrDefault(Witsml200.ReferenceData.IndexDirection.increasing) == Witsml200.ReferenceData.IndexDirection.increasing;
        }

        /// <summary>
        /// Determines whether this <see cref="Witsml200.ChannelSet"/> instance is a time index.
        /// </summary>
        /// <param name="channelSet">The channel set.</param>
        /// <param name="includeElapsedTime">if set to <c>true</c> elapsed time is included.</param>
        /// <returns>true if index is time, false otherwise</returns>
        public static bool IsTimeIndex(this Witsml200.ChannelSet channelSet, bool includeElapsedTime = false)
        {
            if (channelSet?.Index == null) return false;
            return channelSet.Index.Select(x => x.IsTimeIndex(includeElapsedTime)).FirstOrDefault();
        }

        /// <summary>
        /// Determines whether this <see cref="Witsml200.ComponentSchemas.ChannelIndex"/> instance is a time index.
        /// </summary>
        /// <param name="channelIndex">Index of the channel.</param>
        /// <param name="includeElapsedTime">if set to <c>true</c> [include elapsed time].</param>
        /// <returns>true if index is time, false otherwise</returns>
        public static bool IsTimeIndex(this Witsml200.ComponentSchemas.ChannelIndex channelIndex, bool includeElapsedTime = false)
        {
            return channelIndex.IndexType.GetValueOrDefault() == Witsml200.ReferenceData.ChannelIndexType.datetime ||
                   (channelIndex.IndexType.GetValueOrDefault() == Witsml200.ReferenceData.ChannelIndexType.elapsedtime && includeElapsedTime);
        }

        /// <summary>
        /// Gets a <see cref="ChannelDataReader"/> for a <see cref="Witsml131.Log"/>.
        /// </summary>
        /// <param name="log">The <see cref="Witsml131.Log"/> instance.</param>
        /// <returns>A <see cref="ChannelDataReader"/>.</returns>
        public static ChannelDataReader GetReader(this Witsml131.Log log)
        {
            if (log?.LogData == null || !log.LogData.Any()) return null;

            _log.DebugFormat("Creating ChannelDataReader for {0}", log.GetType().FullName);

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

        /// <summary>
        /// Gets multiple readers for each LogData from a <see cref="Witsml141.Log"/> instance.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns>An <see cref="IEnumerable{ChannelDataReader}"/>.</returns>
        public static IEnumerable<ChannelDataReader> GetReaders(this Witsml141.Log log)
        {
            if (log?.LogData == null) yield break;

            _log.DebugFormat("Creating ChannelDataReaders for {0}", log.GetType().FullName);

            var isTimeIndex = log.IsTimeLog();
            var increasing = log.IsIncreasing();

            foreach (var logData in log.LogData)
            {
                if (logData?.Data == null || !logData.Data.Any())
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

                yield return new ChannelDataReader(logData.Data, mnemonics, units, nullValues, log.GetUri(), dataDelimiter: log.DataDelimiter)
                    // Add index curve to separate collection
                    .WithIndex(indexCurve.Mnemonic.Value, indexCurve.Unit, increasing, isTimeIndex);
            }
        }

        /// <summary>
        /// Gets multiple readers for a <see cref="Witsml200.Log"/> instance.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns>An <see cref="IEnumerable{ChannelDataReader}"/>.</returns>
        public static IEnumerable<ChannelDataReader> GetReaders(this Witsml200.Log log)
        {
            if (log?.ChannelSet == null) yield break;

            _log.DebugFormat("Creating ChannelDataReaders for {0}", log.GetType().FullName);

            foreach (var channelSet in log.ChannelSet)
            {
                var reader = channelSet.GetReader();
                if (reader == null) continue;
                yield return reader;
            }
        }

        /// <summary>
        /// Gets a <see cref="ChannelDataReader"/> for a <see cref="Witsml200.ChannelSet"/> instance.
        /// </summary>
        /// <param name="channelSet">The channel set.</param>
        /// <returns>A <see cref="ChannelDataReader"/> </returns>
        public static ChannelDataReader GetReader(this Witsml200.ChannelSet channelSet)
        {
            var data = Witsml200.Extensions.GetData(channelSet);
            if (string.IsNullOrWhiteSpace(data)) return null;

            _log.DebugFormat("Creating ChannelDataReader for {0}", channelSet.GetType().FullName);

            // Not including index channels with value channels
            var mnemonics = channelSet.Channel.Select(x => x.Mnemonic).ToArray();
            var units = channelSet.Channel.Select(x => x.UoM).ToArray();
            var nullValues = new string[units.Length];

            return new ChannelDataReader(data, mnemonics, units, nullValues, channelSet.GetUri())
                // Add index channels to separate collection
                .WithIndices(channelSet.Index.Select(ToChannelIndexInfo), true);
        }

        /// <summary>
        /// Adds index data to a <see cref="ChannelDataReader"/> instance
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="increasing">if set to <c>true</c> the data is increasing, otherwise false.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> the index is time, otherwise false.</param>
        /// <returns>The <see cref="ChannelDataReader"/> instance being updated.</returns>
        public static ChannelDataReader WithIndex(this ChannelDataReader reader, string mnemonic, string unit, bool increasing, bool isTimeIndex)
        {
            _log.DebugFormat("Adding channel index to ChannelDataReader for {0}", mnemonic);

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

        /// <summary>
        /// Updates a <see cref="ChannelDataReader"/> with multiple the indices.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="indices">The indices.</param>
        /// <param name="calculate">if set to <c>true</c> the index range is calculated.</param>
        /// <param name="reverse">if set to <c>true</c> the sort order is reversed.</param>
        /// <returns>The <see cref="ChannelDataReader"/> instance being updated.</returns>
        public static ChannelDataReader WithIndices(this ChannelDataReader reader, IEnumerable<ChannelIndexInfo> indices, bool calculate = false, bool reverse = false)
        {
            _log.Debug("Adding channel indexes to ChannelDataReader");

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

        /// <summary>
        /// Creates a <see cref="ChannelIndexInfo"/> from a 
        /// <see cref="Witsml200.ComponentSchemas.ChannelIndex"/> instance.
        /// </summary>
        /// <param name="channelIndex">Index of the channel.</param>
        /// <returns>A <see cref="ChannelIndexInfo"/> instance.</returns>
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

        /// <summary>
        /// Calculates the index range for a <see cref="ChannelDataReader"/>
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="channelIndex">Index of the channel.</param>
        /// <param name="index">The index.</param>
        private static void CalculateIndexRange(ChannelDataReader reader, ChannelIndexInfo channelIndex, int index)
        {
            _log.DebugFormat("Calculating channel index range for {0}", channelIndex.Mnemonic);

            var range = reader.GetIndexRange(index);
            channelIndex.Start = range.Start.GetValueOrDefault(double.NaN);
            channelIndex.End = range.End.GetValueOrDefault(double.NaN);
        }
    }
}
