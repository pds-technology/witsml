//----------------------------------------------------------------------- 
// PDS.Witsml, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PDS.Framework;

namespace PDS.Witsml.Data.Channels
{
    /// <summary>
    /// Data reader used to parse and read Channel Data for processing.
    /// </summary>
    /// <seealso cref="System.Data.IDataReader" />
    /// <seealso cref="PDS.Witsml.Data.Channels.IChannelDataRecord" />
    public class ChannelDataReader : IDataReader, IChannelDataRecord
    {
        private const string Null = "null";
        private const string NaN = "NaN";

        /// <summary>
        /// The default data delimiter
        /// </summary>
        public const string DefaultDataDelimiter = ",";

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings()
        {
            DateParseHandling = DateParseHandling.DateTimeOffset
        };

        private static readonly ILog _log = LogManager.GetLogger(typeof(ChannelDataReader));
        private static readonly string[] _empty = new string[0];

        private List<List<List<object>>> _records;
        private IList<Range<double?>> _ranges;
        private readonly string[] _originalMnemonics;
        private readonly string[] _originalUnits;
        private readonly string[] _originalDataTypes;
        private readonly string[] _originalNullValues;
        private string[] _allMnemonics;
        private string[] _allUnits;
        private string[] _allDataTypes;
        private string[] _allNullValues;
        private readonly int _indexCount;
        private readonly int _count;
        private int _current = -1;
        private bool _settingMerged;
        private Dictionary<string, int> _indexMap;
        private Range<double?> _chunkRange;
        private readonly List<string[]> _recordMnemonics;

        /// <summary>
        /// Ordinal position of mnemonics that are included in slicing.  Null if reader is not sliced.
        /// </summary>
        private int[] _allSliceOrdinals;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataReader" /> class.
        /// </summary>
        /// <param name="data">The channel data.</param>
        /// <param name="count">The number of mnemonics in mnemonicList element.</param>
        /// <param name="mnemonics">The channel mnemonics.</param>
        /// <param name="units">The channel units.</param>
        /// <param name="dataTypes">The data types.</param>
        /// <param name="nullValues">The null values.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="dataDelimiter">The log data delimiter.</param>
        public ChannelDataReader(IList<string> data, int count, string[] mnemonics = null, string[] units = null, string[] dataTypes = null, string[] nullValues = null, string uri = null, string id = null, string dataDelimiter = null)
            : this(Combine(data, dataDelimiter, count), mnemonics, units, dataTypes, nullValues, uri, id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataReader" /> class.
        /// </summary>
        /// <param name="data">The channel data.</param>
        /// <param name="mnemonics">The channel mnemonics.</param>
        /// <param name="units">The channel units.</param>
        /// <param name="dataTypes">The data types.</param>
        /// <param name="nullValues">The null values.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="id">The identifier.</param>
        public ChannelDataReader(string data, string[] mnemonics = null, string[] units = null, string[] dataTypes = null, string[] nullValues = null, string uri = null, string id = null)
            : this(Deserialize(data), mnemonics, units, dataTypes, nullValues, uri, id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataReader"/> class.
        /// </summary>
        /// <param name="records">The collection of data records.</param>
        public ChannelDataReader(IEnumerable<IChannelDataRecord> records)
        {
            _log.Debug("ChannelDataReader instance created for IChannelDataRecords");

            var items = records
                .Cast<ChannelDataReader>()
                .Select(x => new { Row = x._current, Record = x, x.Mnemonics, x.Units, x.NullValues })
                .ToList();

            var record = items.Select(x => x.Record).FirstOrDefault();
            _records = items.Select(x => x.Record._records[x.Row]).ToList();

            _recordMnemonics = items.Select(x => x.Mnemonics).ToList();

            _count = GetRowValues(0).Count();
            _indexCount = GetIndexValues(0).Count();
            _originalMnemonics = record?.Mnemonics ?? _empty;
            _originalUnits = record?.Units ?? _empty;
            _originalDataTypes = record?.DataTypes ?? _empty;
            _originalNullValues = record?.NullValues ?? _empty;

            Indices = record?.Indices ?? new List<ChannelIndexInfo>();
            Mnemonics = _originalMnemonics;
            Units = _originalUnits;
            DataTypes = _originalDataTypes;
            NullValues = _originalNullValues;
            Uri = record?.Uri;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataReader" /> class.
        /// </summary>
        /// <param name="records">The channel records.</param>
        /// <param name="mnemonics">The channel mnemonics.</param>
        /// <param name="units">The channel units.</param>
        /// <param name="dataTypes">The data types.</param>
        /// <param name="nullValues">The null values.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="id">The identifier.</param>
        internal ChannelDataReader(List<List<List<object>>> records, string[] mnemonics = null, string[] units = null, string[] dataTypes = null, string[] nullValues = null, string uri = null, string id = null)
        {
            _log.Debug("ChannelDataReader instance created");

            _records = records;
            _count = GetRowValues(0).Count();
            _indexCount = GetIndexValues(0).Count();
            _originalMnemonics = mnemonics ?? _empty;
            _originalUnits = units ?? _empty;
            _originalDataTypes = dataTypes ?? _empty;
            _originalNullValues = nullValues ?? _empty;

            Indices = new List<ChannelIndexInfo>();
            Mnemonics = _originalMnemonics;
            Units = _originalUnits;
            DataTypes = _originalDataTypes;
            NullValues = _originalNullValues;
            Uri = uri;
            Id = id;
        }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The unique identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the URI.
        /// </summary>
        /// <value>The parent data object URI.</value>
        public string Uri { get; set; }

        /// <summary>
        /// Gets all mnemonics.
        /// </summary>
        /// <value>
        /// All mnemonics.
        /// </value>
        public string[] AllMnemonics
        {
            get { return Indices.Select(i => i.Mnemonic).Concat(Mnemonics).ToArray(); }
        }

        /// <summary>
        /// Gets the mnemonics included in slicing or all mnemonics if not sliced.
        /// </summary>
        /// <value>The list of channel mnemonics.</value>
        public string[] Mnemonics { get; private set; }

        /// <summary>
        /// Gets all units.
        /// </summary>
        /// <value>
        /// All units.
        /// </value>
        public string[] AllUnits
        {
            get { return Indices.Select(i => i.Unit).Concat(Units).ToArray(); }
        }

        /// <summary>
        /// Gets the units included in slicing or all units if not sliced.
        /// </summary>
        /// <value>The list of channel units.</value>
        public string[] Units { get; private set; }

        /// <summary>
        /// Get all data types.
        /// </summary>
        public string[] AllDataTypes
        {
            get { return Indices.Select(i => i.DataType).Concat(DataTypes).ToArray(); }
        }

        /// <summary>
        /// Gets the data types included in slicing or all units if not sliced.
        /// </summary>
        /// <value> The data types. </value>
        public string[] DataTypes { get; private set; }

        /// <summary>
        /// Gets the null values included in slicing or all null values if not sliced.
        /// </summary>
        /// <value>The list of null values.</value>
        public string[] NullValues { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include the unit with the name.
        /// </summary>
        /// <value>
        /// <c>true</c> if including the unit with the name; otherwise, <c>false</c>.
        /// </value>
        public bool IncludeUnitWithName { get; set; }

        /// <summary>
        /// Gets the indices.
        /// </summary>
        /// <value>A list of indices.</value>
        public List<ChannelIndexInfo> Indices { get; }

        /// <summary>
        /// Indexer property that gets the value with the specified mnemonic name for the current row referenced by the reader.
        /// </summary>
        /// <value>The <see cref="System.Object"/>.</value>
        /// <param name="name">The name of the mnemonic.</param>
        /// <returns>The value for the mnemonic if included in slicing, otherwise null</returns>
        public object this[string name]
        {
            get
            {
                var index = SliceExists(name) ? GetOrdinal(name) : -1;
                return index > -1 ? GetValue(index) : null;
            }
        }

        /// <summary>
        /// Indexer property that gets the value with the specified numerical index in the current row referenced by the reader.
        /// </summary>
        /// <value>The <see cref="System.Object"/>.</value>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns></returns>
        public object this[int i]
        {
            get { return GetValue(i); }
        }

        /// <summary>
        /// Gets a value indicating the number of indexes for the current row.
        /// </summary>
        public int Depth
        {
            get { return _indexCount; }
        }

        /// <summary>
        /// Gets the number of columns in the current row.
        /// </summary>
        public int FieldCount
        {
            get { return _count; }
        }

        /// <summary>
        /// Gets a value indicating whether the data reader is closed.
        /// </summary>
        public bool IsClosed
        {
            get { return _records == null || _current >= _records.Count; }
        }

        /// <summary>
        /// Gets the number of rows represented by the current channel data reader.
        /// </summary>
        public int RecordsAffected
        {
            get { return _records.Count; }
        }

        /// <summary>
        /// Splits the specified comma delimited value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string[] Split(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? _empty
                : value.Split(',').Select(i => i.Trim()).ToArray();
        }

        /// <summary>
        /// Splits the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static string[] Split(string data, string delimiter)
        {
            if (data == null)
                return new string[0];

            if (delimiter.Length < 2)
                return data.Split(delimiter[0]);

            // TextFieldParser iterates when it detects new line characters
            data = data.Replace("\n", " ");

            using (var sr = new StringReader(data))
            {
                using (var parser = new TextFieldParser(sr))
                {
                    parser.SetDelimiters(delimiter);
                    return parser.ReadFields();
                }
            }
        }

        /// <summary>
        /// Splits the row with quotes into a JSON row value.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <param name="channelCount">The count of channels.</param>
        /// <returns>The row in JSON format.</returns>
        /// <exception cref="WitsmlException"></exception>
        public static string SplitRowWithQuotes(string row, string delimiter, int channelCount)
        {
            using (var sr = new StringReader(row))
            {
                using (var parser = new TextFieldParser(sr))
                {
                    parser.SetDelimiters(delimiter);
                    var values = parser.ReadFields();

                    if (values == null)
                        return null;

                    if (values.Length != channelCount)
                    {
                        _log.ErrorFormat("Data points {0} does not match number of channels {1}", values.Length, channelCount);
                        throw new WitsmlException(ErrorCodes.ErrorRowDataCount);
                    }

                    values = values.Select(Format).ToArray();

                    return $"[[{values.First()}],[{string.Join(",", values.Skip(1))}]]";
                }
            }
        }

        /// <summary>
        /// Reads the supplied channel data value to resolve any json.net data types to primitive types.
        /// </summary>
        /// <param name="value">The channel data value.</param>
        /// <returns>A single value or a collection of values.</returns>
        public static object ReadValue(object value)
        {
            var jValue = value as JValue;
            var jArray = value as JArray;

            if (jValue == null && jArray == null)
                return value;

            return jValue == null
                ? jArray.Select(ReadValue).ToArray()
                : jValue.Value;
        }

        /// <summary>
        /// Closes the <see cref="T:System.Data.IDataReader" /> Object.
        /// </summary>
        public void Close()
        {
            _records = null;
            _current = -1;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <param name="value">The value.</param>
        public void SetValue(int i, object value)
        {
            var row = _records[_current];

            if (i < Depth)
                row[0][i] = value;
            else
                row[1][i - Depth] = value;
        }

        /// <summary>
        /// Merges the value.
        /// </summary>
        /// <param name="update">The update record.</param>
        /// <param name="rangeSize">The chunk range.</param>
        /// <param name="order">The order of index value</param>
        /// <param name="increasing">if increasting set to <c>true</c> [append].</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void MergeRecord(IChannelDataRecord update, long rangeSize, IndexOrder order, bool increasing)
        {
            MergeSettings(update, order, rangeSize);

            switch (order)
            {
                case IndexOrder.Before:
                    {
                        var row = _records[_current][1];
                        for (var i = 0; i < Mnemonics.Length; i++)
                        {
                            if (i < _originalMnemonics.Length)
                            {
                                var mnemonic = _originalMnemonics[i];
                                var index = update.GetChannelIndex(mnemonic);
                                if (index > -1)
                                {
                                    var range = update.GetChannelIndexRange(index + Depth);
                                    if (range.Contains(GetIndexValue(), increasing))
                                    {
                                        row[i] = IsNull(NullValues[i])
                                            ? null
                                            : NullValues[i];
                                    }
                                }
                            }
                            else
                            {
                                row.Add(null);
                            }
                        }
                    }
                    break;
                case IndexOrder.Same:
                    {
                        var row = _records[_current][1];
                        for (var i = 0; i < update.Mnemonics.Length; i++)
                        {
                            var mnemonic = update.Mnemonics[i];
                            var index = _indexMap[mnemonic];
                            var updateIndex = update.GetChannelIndex(mnemonic);
                            if (updateIndex <= -1)
                                continue;

                            var value = update.GetValue(updateIndex + Depth);
                            if (index < row.Count)
                            {
                                if (!IsNull(value, index))
                                    row[index] = value;
                            }
                            else
                                row.Add(value);
                        }
                    }
                    break;
                case IndexOrder.After:
                    update.UpdateValues(_indexMap);
                    break;
            }
        }

        /// <summary>
        /// Updates the values.
        /// </summary>
        /// <param name="mnemonicIndexMap">The channel mnemonics index map.</param>
        public void UpdateValues(Dictionary<string, int> mnemonicIndexMap)
        {
            var row = _records[_current];

            var values = new List<object>();
            var indexMap = mnemonicIndexMap ?? _indexMap;

            for (var i = 0; i < indexMap.Keys.Count; i++)
            {
                values.Add(null);
            }

            for (var i = 0; i < Mnemonics.Length; i++)
            {
                var mnemonic = Mnemonics[i];
                var index = Array.IndexOf(_originalMnemonics, mnemonic);
                if (index < 0)
                    continue;

                values[i] = row[1][index];
            }

            row[1] = values;
        }

        /// <summary>
        /// Gets the index of the channel.
        /// </summary>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <returns></returns>
        public int GetChannelIndex(string mnemonic)
        {
            return Array.IndexOf(_originalMnemonics, mnemonic);
        }

        /// <summary>
        /// Updates the channel settings.
        /// </summary>
        /// <param name="record">The record.</param>
        /// <param name="withinRange">if set to <c>true</c> [record within range].</param>
        /// <param name="append">if set to <c>true</c> [append].</param>
        public void UpdateChannelSettings(IChannelDataRecord record, bool withinRange, bool append = true)
        {
            var mnemonics = new List<string>();
            var units = new List<string>();
            var nullValues = new List<string>();
            if (withinRange)
            {
                if (_settingMerged)
                    return;

                record.ResetMergeSettings();
                if (append)
                {
                    for (var i = 0; i < record.Mnemonics.Length; i++)
                    {
                        var mnemonic = record.Mnemonics[i];
                        if (Mnemonics.Contains(mnemonic))
                            continue;
                        mnemonics.Add(mnemonic);
                        units.Add(record.Units[i]);
                        nullValues.Add(record.NullValues[i]);
                    }

                    if (mnemonics.Count > 0)
                    {
                        Mnemonics = Mnemonics.Concat(mnemonics.ToArray()).ToArray();
                        Units = Units.Concat(units.ToArray()).ToArray();
                        NullValues = NullValues.Concat(nullValues.ToArray()).ToArray();
                    }

                    UpdateIndexMap();
                }
                else
                {
                    for (var i = 0; i < Mnemonics.Length; i++)
                    {
                        var mnemonic = Mnemonics[i];
                        if (record.Mnemonics.Contains(mnemonic))
                            continue;
                        mnemonics.Add(mnemonic);
                        units.Add(Units[i]);
                        nullValues.Add(NullValues[i]);
                    }

                    if (mnemonics.Count > 0)
                    {
                        Mnemonics = record.Mnemonics.Concat(mnemonics.ToArray()).ToArray();
                        Units = record.Units.Concat(units.ToArray()).ToArray();
                        NullValues = record.NullValues.Concat(nullValues.ToArray()).ToArray();
                    }
                }

                _settingMerged = true;
            }
            else
            {
                var indexValue = GetIndexValue();
                var indexChannel = GetIndex();
                var increasing = indexChannel.Increasing;
                if (_chunkRange.Contains(indexValue, increasing))
                    return;

                ResetMergeSettings();
                UpdateIndexMap(false);
            }
        }

        /// <summary>
        /// Resets the merge settings.
        /// </summary>
        public void ResetMergeSettings()
        {
            _indexMap = null;
            Mnemonics = _originalMnemonics;
            Units = _originalUnits;
            NullValues = _originalNullValues;
        }

        /// <summary>
        /// Partials the delete record.
        /// </summary>
        /// <param name="deletedChannels">The deleted channels.</param>
        /// <param name="ranges">The ranges.</param>
        /// <param name="updatedRanges">The updated ranges.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <exception cref="NotImplementedException"></exception>
        public void PartialDeleteRecord(List<string> deletedChannels, Dictionary<string, Range<double?>> ranges, Dictionary<string, List<double?>> updatedRanges, bool increasing)
        {
            var row = _records[_current][1];
            var newRow = new List<object>();
            var index = GetIndexValue();

            for (var i = 0; i < _originalMnemonics.Length; i++)
            {
                var mnemonic = _originalMnemonics[i];
                if (deletedChannels.Contains(mnemonic))
                    continue;

                if (ranges.ContainsKey(mnemonic))
                {
                    var range = ranges[mnemonic];
                    var updatedRange = updatedRanges[mnemonic];

                    if (WithinDeleteRange(index, range, increasing))
                    {
                        newRow.Add(null);
                    }
                    else
                    {
                        newRow.Add(row[i]);
                        if (!updatedRange[0].HasValue)
                            updatedRange[0] = index;
                        updatedRange[1] = index;
                    }

                }
                else
                {
                    newRow.Add(row[i]);
                }
            }

            _records[_current][1] = newRow;
        }

        /// <summary>
        /// Copies the channel settings.
        /// </summary>
        /// <param name="record">The record.</param>
        /// <param name="range">The planned chunk range.</param>
        public void CopyChannelSettings(IChannelDataRecord record, Range<double?> range)
        {
            Mnemonics = record.Mnemonics;
            Units = record.Units;
            NullValues = record.NullValues;
            _chunkRange = range;

            var indexValue = GetIndexValue();
            var indexChannel = GetIndex();
            var increasing = indexChannel.Increasing;
            UpdateIndexMap(_chunkRange.Contains(indexValue, increasing));
            _settingMerged = true;
        }

        /// <summary>
        /// Gets the channel index at the index parameter position
        /// </summary>
        /// <param name="index">The index position.</param>
        /// <returns>The index at the given index position</returns>
        public ChannelIndexInfo GetIndex(int index = 0)
        {
            return Indices.Skip(index).FirstOrDefault();
        }

        /// <summary>
        /// Gets the index value.
        /// </summary>
        /// <param name="index">The index position.</param>
        /// <param name="scale">The scale factor.</param>
        /// <returns>The scaled index value at the index paramter position</returns>
        public double GetIndexValue(int index = 0, int scale = 0)
        {
            var channelIndex = GetIndex(index);

            return channelIndex.IsTimeIndex
                ? GetUnixTimeMicroseconds(index)
                : GetDouble(index) * Math.Pow(10, scale);
        }

        /// <summary>
        /// Gets the index value.
        /// </summary>
        /// <param name="rowValues">The row values.</param>
        /// <param name="index">The index position.</param>
        /// <param name="scale">The scale factor.</param>
        /// <returns>The scaled index value at the index paramter position</returns>
        public double GetIndexValue(IEnumerable<object> rowValues, int index = 0, int scale = 0)
        {
            var channelIndex = GetIndex(index);

            return channelIndex.IsTimeIndex
                ? GetDateTimeOffset(rowValues, index).ToUnixTimeMicroseconds()
                : GetDouble(rowValues, index) * Math.Pow(10, scale);
        }

        /// <summary>
        /// Gets the index range for the index position given by the index parameter.
        /// </summary>
        /// <param name="index">The index position.</param>
        /// <returns>The index range</returns>
        public Range<double?> GetIndexRange(int index = 0)
        {
            var channelIndex = GetIndex(index);

            if (channelIndex == null)
                return Range.Empty;

            var start = GetIndexValues(0).Skip(index).FirstOrDefault();
            var end = GetIndexValues(RecordsAffected - 1).Skip(index).FirstOrDefault();

            return Range.Parse(start, end, channelIndex.IsTimeIndex);
        }

        /// <summary>
        /// Gets the index ranges for each channel.
        /// </summary>
        /// <returns>Gets the index ranges for each channel in the public Mnemonics property that has a range.</returns>
        public Dictionary<string, Range<double?>> GetChannelRanges()
        {
            // Calculate the ranges if we haven't done so already.
            if (_ranges == null)
                _ranges = CalculateChannelIndexRanges();

            var channelRanges = new Dictionary<string, Range<double?>>();

            // If there is no data then no need to evaluate
            if (RecordsAffected > 0)
            {
                var allSlices = Indices.Select(i => i.Mnemonic).Concat(Mnemonics).ToArray();

                allSlices.ForEach(m =>
                {
                    var rangeIndex = GetOrdinal(m);
                    if (rangeIndex >= 0)
                    {
                        var range = _ranges[rangeIndex];
                        if (range.Start.HasValue)
                        {
                            channelRanges.Add(m, range);
                        }
                    }
                });
            }

            return channelRanges;
        }

        /// <summary>
        /// Gets the channel index range.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns></returns>
        public Range<double?> GetChannelIndexRange(int i)
        {
            if (!SliceExists(i))
            {
                return Range.Empty;
            }

            if (RecordsAffected < 1)
                return GetIndexRange();

            if (_ranges == null)
                _ranges = CalculateChannelIndexRanges();

            return _ranges[i];
        }

        /// <summary>
        /// Gets the value of the specified column as a Boolean.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The value of the column.
        /// </returns>
        public bool GetBoolean(int i)
        {
            return bool.TrueString.EqualsIgnoreCase(GetString(i));
        }

        /// <summary>
        /// Gets the 8-bit unsigned integer value of the specified column.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The 8-bit unsigned integer value of the specified column.
        /// </returns>
        public byte GetByte(int i)
        {
            return byte.Parse(GetString(i));
        }

        /// <summary>
        /// Reads a stream of bytes from the specified column offset into the buffer as an array, starting at the given buffer offset.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <param name="fieldOffset">The index within the field from which to start the read operation.</param>
        /// <param name="buffer">The buffer into which to read the stream of bytes.</param>
        /// <param name="bufferoffset">The index for <paramref name="buffer" /> to start the read operation.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>
        /// The actual number of bytes read.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the character value of the specified column.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The character value of the specified column.
        /// </returns>
        public char GetChar(int i)
        {
            return char.Parse(GetString(i));
        }

        /// <summary>
        /// Reads a stream of characters from the specified column offset into the buffer as an array, starting at the given buffer offset.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <param name="fieldoffset">The index within the row from which to start the read operation.</param>
        /// <param name="buffer">The buffer into which to read the stream of bytes.</param>
        /// <param name="bufferoffset">The index for <paramref name="buffer" /> to start the read operation.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>
        /// The actual number of characters read.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns an <see cref="T:System.Data.IDataReader" /> for the specified column ordinal.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The <see cref="T:System.Data.IDataReader" /> for the specified column ordinal.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the data type information for the specified field.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The data type information for the specified field.
        /// </returns>
        public string GetDataTypeName(int i)
        {
            return GetFieldType(i).Name;
        }

        /// <summary>
        /// Gets the date and time data value of the specified field.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The date and time data value of the specified field.
        /// </returns>
        public DateTime GetDateTime(int i)
        {
            var rawValue = GetValue(i);
            return rawValue as DateTime? ?? DateTime.Parse(rawValue.ToString());
        }

        /// <summary>
        /// Gets the date time offset.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns></returns>
        public DateTimeOffset GetDateTimeOffset(int i)
        {
            var rawValue = GetValue(i);
            return rawValue as DateTimeOffset? ?? DateTimeOffset.Parse(rawValue.ToString());
        }

        /// <summary>
        /// Gets the date time offset.
        /// </summary>
        /// <param name="rowValues">The row values.</param>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns></returns>
        public DateTimeOffset GetDateTimeOffset(IEnumerable<object> rowValues, int i)
        {
            var rawValue = GetValue(rowValues, i);
            return rawValue as DateTimeOffset? ?? DateTimeOffset.Parse(rawValue.ToString());
        }

        /// <summary>
        /// Gets the unix time seconds.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns></returns>
        public long GetUnixTimeMicroseconds(int i)
        {
            return GetDateTimeOffset(i).ToUnixTimeMicroseconds();
        }

        /// <summary>
        /// Gets the fixed-position numeric value of the specified field.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The fixed-position numeric value of the specified field.
        /// </returns>
        public decimal GetDecimal(int i)
        {
            return decimal.Parse(GetString(i));
        }

        /// <summary>
        /// Gets the double-precision floating point number of the specified field.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The double-precision floating point number of the specified field.
        /// </returns>
        public double GetDouble(int i)
        {
            double value;
            return double.TryParse(GetString(i), out value) ? value : double.NaN;
        }

        /// <summary>
        /// Gets the double-precision floating point number of the specified field.
        /// </summary>
        /// <param name="rowValues">The row values.</param>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The double-precision floating point number of the specified field.
        /// </returns>
        public double GetDouble(IEnumerable<object> rowValues, int i)
        {
            double value;
            return double.TryParse(GetString(rowValues, i), out value) ? value : double.NaN;
        }

        /// <summary>
        /// Gets the <see cref="T:System.Type" /> information corresponding to the type of <see cref="T:System.Object" /> 
        /// that would be returned from <see cref="M:System.Data.IDataRecord.GetValue(System.Int32)" />.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The <see cref="T:System.Type" /> information corresponding to the type of <see cref="T:System.Object" /> 
        /// that would be returned from <see cref="M:System.Data.IDataRecord.GetValue(System.Int32)" />.
        /// </returns>
        public Type GetFieldType(int i)
        {
            var rawValue = GetValue(i);
            var type = rawValue?.GetType() ?? typeof(object);
            return type.IsNumeric() ? typeof(double) : type;

        }

        /// <summary>
        /// Gets the single-precision floating point number of the specified field.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The single-precision floating point number of the specified field.
        /// </returns>
        public float GetFloat(int i)
        {
            float value;
            return float.TryParse(GetString(i), out value) ? value : float.NaN;
        }

        /// <summary>
        /// Returns the GUID value of the specified field.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The GUID value of the specified field.
        /// </returns>
        public Guid GetGuid(int i)
        {
            return Guid.Parse(GetString(i));
        }

        /// <summary>
        /// Gets the 16-bit signed integer value of the specified field.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The 16-bit signed integer value of the specified field.
        /// </returns>
        public short GetInt16(int i)
        {
            return short.Parse(GetString(i));
        }

        /// <summary>
        /// Gets the 32-bit signed integer value of the specified field.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The 32-bit signed integer value of the specified field.
        /// </returns>
        public int GetInt32(int i)
        {
            return int.Parse(GetString(i));
        }

        /// <summary>
        /// Gets the 64-bit signed integer value of the specified field.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The 64-bit signed integer value of the specified field.
        /// </returns>
        public long GetInt64(int i)
        {
            return long.Parse(GetString(i));
        }

        /// <summary>
        /// Gets the name for the field to find.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The name of the field or the empty string (""), if there is no value to return.
        /// </returns>
        public string GetName(int i)
        {
            var name = GetAllMnemonics().Skip(i).FirstOrDefault();

            var unit = IncludeUnitWithName
                ? GetAllUnits().Skip(i).FirstOrDefault()
                : string.Empty;

            return IncludeUnitWithName
                ? FormatColumnName(name, unit)
                : name;
        }


        /// <summary>
        /// Return the index of the named field.
        /// </summary>
        /// <param name="name">The name of the field to find.</param>
        /// <returns>
        /// The index of the named field.
        /// </returns>
        public int GetOrdinal(string name)
        {
            return Array.IndexOf(GetAllMnemonics(), name);
        }

        /// <summary>
        /// Returns a <see cref="T:System.Data.DataTable" /> that describes the column metadata of the <see cref="T:System.Data.IDataReader" />.
        /// </summary>
        /// <remarks>
        /// For more information about columns that can be included in the schema table:
        /// https://msdn.microsoft.com/en-us/library/system.data.datatablereader.getschematable(v=vs.110).aspx
        /// </remarks>
        /// <returns>
        /// A <see cref="T:System.Data.DataTable" /> that describes the column metadata.
        /// </returns>
        public DataTable GetSchemaTable()
        {
            var table = new DataTable();
            var columns = new[] { "ColumnOrdinal", "ColumnName", "ColumnSize", "DataType", "IsKey" };
            var types = new[] { typeof(int), typeof(string), typeof(int), typeof(Type), typeof(bool) };
            var count = Indices.Count;

            columns.ForEach((x, i) => table.Columns.Add(x, types[i]));
            Indices.ForEach((x, i) => table.Rows.Add(i, FormatColumnName(x.Mnemonic, x.Unit), -1, GetFieldType(i), i == 0));
            Mnemonics.ForEach((x, i) => table.Rows.Add(i + count, FormatColumnName(x, Units[i]), -1, GetFieldType(i + count), false));

            return table;
        }

        /// <summary>
        /// Gets the string value of the specified field.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The string value of the specified field.
        /// </returns>
        public string GetString(int i)
        {
            return $"{GetValue(i)}";
        }

        /// <summary>
        /// Gets the string value of the specified field.
        /// </summary>
        /// <param name="rowValues">The row values.</param>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The string value of the specified field.
        /// </returns>
        public string GetString(IEnumerable<object> rowValues, int i)
        {
            return $"{GetValue(rowValues, i)}";
        }

        /// <summary>
        /// Return the value of the specified field.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The <see cref="T:System.Object" /> which will contain the field value upon return.  
        /// If the column ordinal is not included in slicing, null is returned.
        /// </returns>
        public object GetValue(int i)
        {
            if (!SliceExists(i)) return null;

            // NOTE: logging here is too verbose!
            //_log.DebugFormat("Getting the value at row: {0}, col: {1}", _current, i);

            var rowValues = GetRowValues(_current);
            return GetValue(rowValues, i);
        }

        /// <summary>
        /// Return the value of the specified field.
        /// </summary>
        /// <param name="rowValues">The row values.</param>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The <see cref="T:System.Object" /> which will contain the field value upon return.  
        /// If the column ordinal is not included in slicing, null is returned.
        /// </returns>
        public object GetValue(IEnumerable<object> rowValues, int i)
        {
            if (!SliceExists(i)) return null;

            // NOTE: logging here is too verbose!
            //_log.DebugFormat("Getting the value at col: {0}", i);

            var value = rowValues.Skip(i).FirstOrDefault();
            var array = value as JArray;

            if (array != null && array.Count == 1)
            {
                value = array[0];
            }

            return value;
        }

        /// <summary>
        /// Populates an array of objects with the column values of the current record.
        /// </summary>
        /// <param name="values">An array of <see cref="T:System.Object" /> to copy the attribute fields into.</param>
        /// <returns>
        /// The number of instances of <see cref="T:System.Object" /> in the array.
        /// </returns>
        public int GetValues(object[] values)
        {
            // NOTE: logging here is too verbose!
            //_log.DebugFormat("Getting the values for row: {0}", _current);

            // Slice the results of GetRowValues
            var rowValues = GetRowValues(_current)
                .Where((r, i) => SliceExists(i))
                .ToList();

            var count = Math.Min(values.Length, rowValues.Count);
            var source = rowValues.Take(count).ToArray();
            Array.Copy(source, values, count);

            return count;
        }

        /// <summary>
        /// Return whether the specified field is set to null.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// true if the specified field is set to null; otherwise, false.
        /// </returns>
        public bool IsDBNull(int i)
        {
            return IsNull(GetValue(i), i);
        }

        /// <summary>
        /// Advances the data reader to the next result, when reading the results of batch SQL statements.
        /// </summary>
        /// <returns>
        /// true if there are more rows; otherwise, false.
        /// </returns>
        public bool NextResult()
        {
            return false;
        }

        /// <summary>
        /// Advances the <see cref="T:System.Data.IDataReader" /> to the next record.
        /// </summary>
        /// <returns>true if there are more rows; otherwise, false.</returns>
        public bool Read()
        {
            _current++;
            return !IsClosed;
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            _current = -1;
        }

        /// <summary>
        /// Moves the current pointer to the specified row.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns>true if there are more rows; otherwise, false.</returns>
        public bool MoveTo(int row)
        {
            _current = row;
            return !IsClosed;
        }

        /// <summary>
        /// Determines whether the current row has any non-null values.
        /// </summary>
        /// <returns>true if the current row has values, false otherwise.</returns>
        public bool HasValues()
        {
            // NOTE: logging here is too verbose!
            //_log.DebugFormat("Checking if the current row has any values: {0}", _current);

            return GetChannelValuesByOrdinal(_current)
                .Any(x => !IsNull(x.Value, x.Key));
        }

        /// <summary>
        /// Determines whether the specified row has any non-null values.
        /// </summary>
        /// <param name="rowValues">The row values.</param>
        /// <returns>true if the current row has values, false otherwise.</returns>
        public bool HasValues(IEnumerable<object> rowValues)
        {
            // NOTE: logging here is too verbose!
            //_log.DebugFormat("Checking if the row has any values.");

            return rowValues
                .Select((x, i) => new { Index = i, Value = x })
                .Skip(Depth)
                .Any(x => !IsNull(x.Value, x.Index));
        }

        /// <summary>
        /// Gets current row serialized to json.
        /// This method is used for internal chunk storage and should NOT be sliced.
        /// </summary>
        /// <returns>The current row serialized as JSON.</returns>
        public string GetJson()
        {
            if (IsClosed)
                return null;

            // NOTE: logging here is too verbose!
            //_log.Debug("Serializing the current row in json format.");

            return JsonConvert.SerializeObject(_records[_current]);
        }

        /// <summary>
        /// Returns all of the data in the reader as <see cref="IEnumerable{IChannelDataRecord}"/>
        /// </summary>
        /// <returns>An <see cref="IEnumerable{IChannelDataRecord}"/></returns>
        public IEnumerable<IChannelDataRecord> AsEnumerable()
        {
            while (Read())
            {
                yield return this;
            }
        }

        /// <summary>
        /// Sorts the data by the primary index.
        /// </summary>
        /// <returns>The channel data reader instance.</returns>
        public ChannelDataReader Sort(bool reverse = false)
        {
            if (!Indices.Any()) return this;

            _log.DebugFormat("Sorting the records in the ChannelDataReader; Reverse: {0}", reverse);

            var indexChannel = Indices.First();

            var increasing = reverse
                ? !indexChannel.Increasing
                : indexChannel.Increasing;
            var isTimeIndex = indexChannel.IsTimeIndex;

            Func<List<List<object>>, object> getIndexValue = row => isTimeIndex
                ? row.First().First()
                : Convert.ToDouble(row.First().First());

            _records = increasing
                ? _records.OrderBy(getIndexValue).ToList()
                : _records.OrderByDescending(getIndexValue).ToList();

            Reset();
            return this;
        }

        /// <summary>
        /// Sets the ordinal positions for a given set of mnemonic slices.
        /// Mnemonics for channels without any data will be excluded from the slices.
        /// </summary>
        /// <param name="mnemonicSlices">The mnemonic slices.</param>
        /// <param name="units">The units.</param>
        /// <param name="dataTypes">The data types.</param>
        /// <param name="nullValues">The null values.</param>
        public void Slice(IDictionary<int, string> mnemonicSlices, IDictionary<int, string> units, IDictionary<int, string> dataTypes, IDictionary<int, string> nullValues)
        {
            _log.Debug("Slicing the channels without any data.");

            // Remove the index mnemonic from the mnemonicSlices
            var indices = Indices.Select(i => i.Mnemonic).ToArray();
            var slices = mnemonicSlices.Values.ToArray()
                .Where(m => !indices.Contains(m))
                .ToArray();

            _allMnemonics = null;
            _allUnits = null;
            _allSliceOrdinals = null;
            _allDataTypes = null;
            _allNullValues = null;
            Mnemonics = slices;
            Units = null;
            DataTypes = null;
            NullValues = null;
            
            var allUnits = GetAllUnits();
            var allDataTypes = GetAllDataTypes();
            var allNulls = GetAllNullValues();

            // Cache all mnemonic ordinal positions
            var allOrdinals = GetAllMnemonics()
                .ToDictionary(x => x, GetOrdinal);

            // Call GetChannelRanges so we can see which ranges have data or not.  
            //... Ranges will only be calculated for the current slices.
            var ranges = GetChannelRanges();

            // Apply slicing to mnemonics and units without any data (range)
            Mnemonics = Mnemonics.Where(m => ranges.Keys.Contains(m)).ToArray();

            // Get the ordinal position of each slice.
            var sliceOrdinals = Mnemonics.Select(x => allOrdinals[x]).ToArray();

            // All Slice Ordinals including the ordinals for indexes
            _allSliceOrdinals = Enumerable.Range(0, Depth).ToArray()
                .Concat(sliceOrdinals).ToArray();

            Units = Mnemonics.Select(m => allUnits[allOrdinals[m]]).ToArray();
            DataTypes = Mnemonics.Select(m => allDataTypes[allOrdinals[m]]).ToArray();
            NullValues = Mnemonics.Select(m => allNulls[allOrdinals[m]]).ToArray();

            // If there is data then update the mnemonics and units from the caller.
            if (RecordsAffected > 0)
            {
                var allMnemonics = AllMnemonics;

                // Get mnemonic ids for mnemonics that are not in the reader's mnemonics
                var removeKeys = mnemonicSlices.Where(m => !allMnemonics.Contains(m.Value)).Select(m => m.Key).ToArray();

                // Remove mnemonics and corresponding units that are not in the reader
                removeKeys.ForEach(k =>
                {
                    mnemonicSlices.Remove(k);
                    units.Remove(k);
                    dataTypes.Remove(k);
                    nullValues.Remove(k);
                });
            }

            // After slicing set the allNullValues context for next getData
            _allNullValues = Mnemonics.Select(m => allNulls[allOrdinals[m]]).ToArray();
        }

        /// <summary>
        /// Manages the amount and format of the channel data managed by the reader based on the current query context.
        /// </summary>
        /// <param name="context">The query context.</param>
        /// <param name="mnemonicSlices">The index map for requested mnemonics in current log.</param>
        /// <param name="mnemonics">The mnemonics.</param>
        /// <param name="units">The units.</param>
        /// <param name="dataTypes">The data types.</param>
        /// <param name="nullValues">The null values.</param>
        /// <param name="ranges">The ranges map.</param>
        /// <returns>The channel data managed by the reader.</returns>
        public List<List<List<object>>> GetData(
            IQueryContext context, IDictionary<int, string> mnemonicSlices, string[] mnemonics, IDictionary<int, string> units, IDictionary<int, string> dataTypes, IDictionary<int, string> nullValues, out Dictionary<string, Range<double?>> ranges)
        {
            _log.Debug("Getting the sliced channel data.");

            context.HasAllRequestedValues = false;
            int? requestLatestValues = context.RequestLatestValues;
            int maxDataNodes = context.MaxDataNodes;
            int maxDataPoints = context.MaxDataPoints;

            var dataPointCount = 0;
            var channelCount = AllMnemonics.Length;
            var channelData = new List<List<List<object>>>();

            // Ranges will only be returned for channels that are included in slicing
            //... and contain data.
            ranges = InitializeSliceRanges(mnemonicSlices.Values.ToArray());

            // Create and initialize value count dictionary for channels
            Dictionary<int, int> requestedValueCount = null;

            // Support for requestLatestValues if supplied
            if (requestLatestValues.HasValue)
            {
                // Use _allSliceOrdinals that includes the index ordinals
                //... to initialize requestedValueCount.
                requestedValueCount = new Dictionary<int, int>();
                if (_allSliceOrdinals == null)
                {
                    _allSliceOrdinals = Enumerable.Range(0, Depth).ToArray().Concat(mnemonicSlices.Select((m, i) => m.Key)).Skip(Depth).ToArray();
                }
                _allSliceOrdinals.ForEach(s => requestedValueCount.Add(s, 0));
            }

            // Read through all of the data
            while (Read())
            {
                var rowValues = GetRowValues(_current).ToArray();
                var fullRowValues = GetFullRowValues(rowValues, mnemonicSlices.Values.ToArray());

                // If there is no channel data in the current row then don't process it
                if (!HasValues(fullRowValues)) continue;

                // Note: This was removed because it was too verbose but can be used later if needed.
                //_log.DebugFormat("Appending channel data values for row: {0}", _current);

                var channelValues = fullRowValues.Skip(Depth).ToList();

                // Skip rows with no channel values
                if (channelValues.Count < 1) continue;

                var indexValues = new List<object>();
                var primaryIndex = GetIndexValue(fullRowValues);

                // Add each index value to the values list and 
                //... use timestamp format for time index values
                for (var i = 0; i < Depth; i++)
                {
                    var isTimeIndex = Indices.Skip(i)
                        .Select(x => x.IsTimeIndex)
                        .FirstOrDefault();

                    indexValues.Add(isTimeIndex
                        ? GetDateTimeOffset(rowValues, i).ToString("o")
                        : (object)GetIndexValue(rowValues, i));
                }

                var allValues = indexValues.Concat(channelValues).ToList();

                // Update the data point count
                dataPointCount += allValues.Count;

                if (!requestLatestValues.HasValue || IsRequestedValueNeeded(allValues, requestedValueCount, requestLatestValues.Value))
                {
                    channelData.Add(new List<List<object>>() { indexValues, allValues.GetRange(Depth, allValues.Count - Depth) });

                    // Update the latest value count for each channel.
                    if (requestLatestValues.HasValue)
                    {
                        UpdateRequestedValueCount(requestedValueCount, mnemonicSlices, allValues, ranges, primaryIndex);
                    }
                    // if it's not a lastest values request then update indexes
                    else
                    {
                        // Update ranges for the current primaryIndex
                        UpdateRanges(allValues, mnemonicSlices.Values.ToArray(), ranges, primaryIndex);
                    }
                }

                // if latest values requested and we have all of the requested values we need, break out;
                if (requestLatestValues.HasValue && HasRequestedValuesForAllChannels(requestedValueCount, requestLatestValues.Value))
                {
                    _log.Debug("Finished getting latest values for all channels.");
                    context.HasAllRequestedValues = true;
                    break;
                }

                // If processing the next row will exceed our maxDataNodes or maxDataPoints limits then stop
                if (channelData.Count >= maxDataNodes || (dataPointCount + channelCount) > maxDataPoints)
                {
                    _log.DebugFormat("Truncating channel data with {0} data nodes and {1} data points.", channelData.Count, dataPointCount);
                    context.DataTruncated = true;
                    break;
                }
            }

            // For requested values reverse the order before output because the channel data
            //... was retrieved from the bottom up.
            if (requestLatestValues.HasValue)
            {
                _log.Debug("Reversing the order of channel data for request latest values.");
                channelData.Reverse();
            }

            // if any ranges are empty, then we must (re)slice
            if (ranges.Values.All(r => r.Start.HasValue) || Mnemonics.Length <= 0)
                return channelData;

            _log.Debug("Re-slicing remaining channels with no data.");

            using (
                var reader = new ChannelDataReader(channelData, mnemonics.Skip(Depth).ToArray(),
                        units.Values.Skip(Depth).ToArray(), dataTypes.Values.Skip(Depth).ToArray(),
                        nullValues.Values.Skip(Depth).ToArray())
                    .WithIndices(Indices))
            {
                reader.Slice(mnemonicSlices, units, dataTypes, nullValues);

                // Clone the context without RequestLatestValues
                var resliceContext = context.Clone();
                resliceContext.RequestLatestValues = null;

                channelData = reader.GetData(resliceContext, mnemonicSlices, mnemonics, units, dataTypes, nullValues, out ranges);
            }

            return channelData;
        }

        private object[] GetFullRowValues(object[] rowValues, string[] slicedMnemonics)
        {
            var rowMnemonics = _recordMnemonics != null ? _recordMnemonics[_current] : slicedMnemonics;
            var fullRow = new object[slicedMnemonics.Length];

            // Add all of the indexes to the full row
            for (var i = 0; i < Depth; i++)
            {
                fullRow[i] = rowValues[i];
            }

            //... now add the sliced mnemonics to the full row
            for (var i = Depth; i < fullRow.Length; i++)
            {
                if (_allSliceOrdinals == null)
                {
                    // Compute the source index for the sliced mnemonic we're looking for
                    var channel = slicedMnemonics[i];
                    var index = Array.IndexOf(rowMnemonics, channel);

                    // If the mnemonic is not in our row try another
                    if (index <= -1)
                        continue;

                    // Advance the index to account for the number of index values (Depth) in the row.
                    if (_recordMnemonics != null)
                        index = index + Depth;

                    // Add the mnemonic's value to the full row
                    fullRow[i] = rowValues[index];
                }
                else
                {
                    // If we already have a array of our sliced indexes, use that.
                    fullRow[i] = rowValues[_allSliceOrdinals[i]];
                }
            }

            return fullRow;
        }

        private Dictionary<string, Range<double?>> InitializeSliceRanges(string[] mnemonics)
        {
            _log.Debug("Initializing index ranges.");

            var emptyRanges = new Dictionary<string, Range<double?>>();

            mnemonics.ForEach(m =>
            {
                emptyRanges.Add(m, new Range<double?>(null, null));
            });

            return emptyRanges;
        }

        private void UpdateRanges(List<object> values, string[] mnemonics, Dictionary<string, Range<double?>> ranges, double primaryIndex)
        {
            // Note: This was removed because it was too verbose but can be used later if needed.
            //_log.Debug("Updating index ranges.");

            for (var i = 0; i < values.Count; i++)
            {
                //var ordinal = _allSliceOrdinals[i];
                var mnemonic = mnemonics[i];

                if (!IsNull(values[i], i))
                {
                    ranges[mnemonic] = ranges[mnemonic].Start.HasValue
                        ? new Range<double?>(ranges[mnemonic].Start, primaryIndex)
                        : new Range<double?>(primaryIndex, primaryIndex);
                }
            }
        }

        private bool IsRequestedValueNeeded(List<object> channelValues, Dictionary<int, int> requestedValueCount, int requestLatestValue)
        {
            _log.Debug("Checking if request latest values requires additional values for any channels.");

            var valueAdded = false;

            for (var i = 0; i < channelValues.Count; i++)
            {
                if (channelValues[i] == null)
                    continue;

                // For the current channel, if the requested value count has not already been reached and then
                // ... current channel value is not null or blank then a value is being added.
                if (requestedValueCount[_allSliceOrdinals[i]] < requestLatestValue && !IsNull(channelValues[i], _allSliceOrdinals[i]))
                {
                    valueAdded = true;
                }
                else if (i > 0)
                {
                    channelValues[i] = null;
                }
            }

            return valueAdded;
        }

        private void UpdateRequestedValueCount(Dictionary<int, int> requestedValueCount, IDictionary<int, string> mnemonicSlices, List<object> values, Dictionary<string, Range<double?>> ranges, double primaryIndex)
        {
            _log.Debug("Updating request latest values count for all channels.");

            var valueArray = values.ToArray();

            for (var i = 0; i < valueArray.Length; i++)
            {
                var ordinal = _allSliceOrdinals[i];
                var mnemonic = mnemonicSlices[ordinal];

                if (requestedValueCount.ContainsKey(ordinal) && !IsNull(valueArray[i], ordinal))
                {
                    // If first time update for this channel value then start and end index are the same
                    if (requestedValueCount[ordinal] == 0)
                    {
                        ranges[mnemonic] = new Range<double?>(primaryIndex, primaryIndex);
                    }
                    // Move the end index for subsequent updates to the current channel value
                    else
                    {
                        ranges[mnemonic] = new Range<double?>(ranges[mnemonic].Start, primaryIndex);
                    }

                    // Update the count
                    requestedValueCount[ordinal]++;
                }
            }
        }

        private bool HasRequestedValuesForAllChannels(Dictionary<int, int> requestedValueCount, int requestLatestValues)
        {
            _log.Debug("Checking if request latest values has been filled for all channels.");
            return requestedValueCount.Keys.All(r => requestedValueCount[r] >= requestLatestValues);
        }

        /// <summary>
        /// Sets an array of all of the original mnemonics including the index mnemonics.
        /// </summary>
        private string[] GetAllMnemonics()
        {
            if (_allMnemonics == null)
            {
                _log.Debug("Initializing _allMnemonics array.");
                _allMnemonics = Indices.Select(i => i.Mnemonic).Concat(_originalMnemonics).ToArray();
            }

            return _allMnemonics;
        }

        /// <summary>
        /// Sets an array of all of the original units including the index units.
        /// </summary>
        private string[] GetAllUnits()
        {
            if (_allUnits == null)
            {
                _log.Debug("Initializing _allUnits array.");
                _allUnits = Indices.Select(i => i.Unit).Concat(_originalUnits).ToArray();
            }

            return _allUnits;
        }

        /// <summary>
        /// Sets an array of all of the original data types including the index data types.
        /// </summary>
        private string[] GetAllDataTypes()
        {
            if (_allDataTypes == null)
            {
                _log.Debug("Initializing _allDataTypes array.");
                //todo: When data type is persisted in chunk use _originalDataTypes instead
                _allDataTypes = Indices.Select(i => i.DataType).Concat(_originalMnemonics).ToArray();
            }

            return _allDataTypes;
        }

        /// <summary>
        /// Gets all null values.
        /// </summary>
        /// <returns></returns>
        private string[] GetAllNullValues()
        {
            if (_allNullValues == null)
            {
                _log.Debug("Initializing _allNullValues array.");
                _allNullValues = Indices.Select(i => i.NullValue).Concat(_originalNullValues).ToArray();
            }

            return _allNullValues;
        }

        /// <summary>
        /// Gets the row values.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns>An <see cref="IEnumerable{Object}"/> of channel values and metadata for a given row.</returns>
        private IEnumerable<object> GetRowValues(int row)
        {
            if (IsClosed)
                return Enumerable.Empty<object>();

            // NOTE: logging here is too verbose!
            //_log.DebugFormat("Getting all values for row: {0}", row);

            return _records
                .Skip(row)
                .Take(1)
                .SelectMany(x => x.SelectMany(y => y));
        }

        /// <summary>
        /// Gets the index values.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns>An <see cref="IEnumerable{Object}"/> of index values for a given row.</returns>
        private IEnumerable<object> GetIndexValues(int row)
        {
            if (IsClosed)
                return Enumerable.Empty<object>();

            // NOTE: logging here is too verbose!
            //_log.DebugFormat("Getting index values for row: {0}", row);

            return _records
                .Skip(row)
                .Take(1)
                .SelectMany(x => x.First());
        }

        /// <summary>
        /// Gets the channel values with the index.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns> A <see cref="IDictionary{TKey, TValue}"/></returns>
        private IDictionary<int, object> GetChannelValuesByOrdinal(int row)
        {
            if (IsClosed)
                return new Dictionary<int, object>();

            // NOTE: logging here is too verbose!
            //_log.DebugFormat("Getting channel values for row: {0}", row);

            // Slice row
            return _records
                .Skip(row)
                .Take(1)
                .SelectMany(x => x.Last())
                .Select((v, i) => new { Value = v, Index = i })
                .Where((r, i) => SliceExists(i + Depth))  // We need to look at the i-th + Depth slice since we're only looking at channel values
                .ToDictionary(x => x.Index + Depth, x => x.Value);
        }

        /// <summary>
        /// Calculates the index ranges for all channels.
        /// </summary>
        /// <returns>A collection of channel index ranges.</returns>
        private IList<Range<double?>> CalculateChannelIndexRanges()
        {
            _log.DebugFormat("Calculating channel index ranges.");

            var channelIndex = GetIndex();
            var ranges = new List<Range<double?>>();

            for (var i = 0; i < FieldCount; i++)
            {
                ranges.Add(i < Depth
                    ? GetIndexRange(i)
                    : CalculateChannelIndexRanges(i, channelIndex.IsTimeIndex));
            }

            return ranges;
        }

        /// <summary>
        /// Calculates the index ranges for the specified channel.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> [is time index].</param>
        /// <returns>The channel index range.</returns>
        private Range<double?> CalculateChannelIndexRanges(int i, bool isTimeIndex)
        {
            // NOTE: logging here is too verbose!
            //_log.DebugFormat("Calculating channel index range: {0}", i);

            var valueIndex = i - Depth;
            object start = null;
            object end = null;

            if (SliceExists(i))
            {
                // Find start
                foreach (var record in _records)
                {
                    var value = record.Last().Skip(valueIndex).FirstOrDefault();

                    if (!IsNull(value, i))
                    {
                        start = record[0][0];
                        break;
                    }
                }

                // Find end
                foreach (var record in _records.AsEnumerable().Reverse())
                {
                    var value = record.Last().Skip(valueIndex).FirstOrDefault();

                    if (!IsNull(value, i))
                    {
                        end = record[0][0];
                        break;
                    }
                }
            }

            return Range.Parse(start, end, isTimeIndex);
        }

        /// <summary>
        /// Deserializes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="dataDelimiter">The data delimiter.</param>
        /// <returns></returns>
        private static List<List<List<object>>> Deserialize(string data, string dataDelimiter = null)
        {
            _log.Debug("Deserializing channel data json.");

            if (string.IsNullOrWhiteSpace(data))
                return new List<List<List<object>>>();

            if (!string.IsNullOrEmpty(dataDelimiter) && dataDelimiter != DefaultDataDelimiter)
                data = data.Replace(dataDelimiter, DefaultDataDelimiter);

            return JsonConvert.DeserializeObject<List<List<List<object>>>>(data, _jsonSettings);
        }

        /// <summary>
        /// Combines the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="dataDelimiter">The log data delimiter.</param>
        /// <param name="count">The number of mnemonics in mnemonicList element.</param>
        /// <returns>A JSON arrary of string values from the data list.</returns>
        private static string Combine(IList<string> data, string dataDelimiter, int count)
        {
            _log.Debug("Combining log data elements into channel data json structure.");

            var delimiter = ChannelDataExtensions.GetDataDelimiterOrDefault(dataDelimiter);
            var json = new StringBuilder("[");
            var rows = new List<string>();

            if (data != null)
            {
                foreach (var row in data)
                {
                    // If any of the values are quoted evaluate row for extra delimiter
                    if (row.Contains("\""))
                    {
                        rows.Add(SplitRowWithQuotes(row, delimiter, count));
                    }
                    else
                    {
                        var values = Split(row, delimiter);
                        if (values.Length != count)
                        {
                            _log.ErrorFormat("Data points {0} does not match number of channels {1}", values.Length,
                                count);
                            throw new WitsmlException(ErrorCodes.ErrorRowDataCount);
                        }

                        values = values
                            .Select(Format)
                            .ToArray();

                        rows.Add($"[[{values.First()}],[{string.Join(",", values.Skip(1))}]]");
                    }
                }
            }

            json.Append(string.Join(",", rows));
            json.Append("]");

            return json.ToString();
        }

        /// <summary>
        /// Formats the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A value formated for null, string or double.</returns>
        private static string Format(string value)
        {
            double number;

            if (IsNull(value))
                return "null";

            if (double.TryParse(value, out number))
                return value;

            return JsonConvert.ToString(value.Trim());
        }

        /// <summary>
        /// Determines whether the specified value is null.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>true of the specified value is null or white space, false otherwise.</returns>
        private static bool IsNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                Null.EqualsIgnoreCase(value) ||
                NaN.EqualsIgnoreCase(value);
        }

        /// <summary>
        /// Determines whether the specified value is null.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="channelIndex">The channel index.</param>
        /// <returns></returns>
        private bool IsNull(object value, int channelIndex)
        {
            if (value == null) return true;

            var stringValue = $"{value}";
            if (IsNull(stringValue)) return true;

            var nullValues = GetAllNullValues();
            if (channelIndex >= nullValues.Length) return false;

            var nullValue = nullValues[channelIndex];
            if (string.IsNullOrWhiteSpace(nullValue)) return false;

            // Using string compare to avoid parsing
            return stringValue == nullValue;

            //if (channelIndex < nullValues.Length && !string.IsNullOrWhiteSpace(nullValues[channelIndex]))
            //{
            //    var nullValue = nullValues[channelIndex];

            //    double dNull, dValue;
            //    if (!double.TryParse(nullValue, out dNull) || !double.TryParse(value, out dValue))
            //        return false;

            //    return dNull == dValue;
            //}

            //return false;
        }

        /// <summary>
        /// Tests if a slice exists for the given ordinal position.
        /// </summary>
        /// <param name="ordinal">The ordinal position being tested.</param>
        /// <returns>true if the ordinal position exists in the list of slice ordinal positions, false otherwise.</returns>
        private bool SliceExists(int ordinal)
        {
            return _allSliceOrdinals == null || _allSliceOrdinals.Contains(ordinal);
        }

        /// <summary>
        /// Tests if a slice exists for the given mnemonic.
        /// </summary>
        /// <param name="mnemonic">The mnemonic being tested.</param>
        /// <returns>true if the ordinal position exists in the list of slice ordinal positions for the given mnemonic, false otherwise.</returns>
        private bool SliceExists(string mnemonic)
        {
            return SliceExists(GetOrdinal(mnemonic));
        }

        /// <summary>
        /// Formats the name of the column.
        /// </summary>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <param name="units">The units.</param>
        /// <returns>The formatted column name.</returns>
        private string FormatColumnName(string mnemonic, string units)
        {
            return string.IsNullOrWhiteSpace(units)
                ? mnemonic
                : $"{ mnemonic } [{ units }]";
        }

        /// <summary>
        /// Merges the settings between existing chunk and update chunk.
        /// </summary>
        /// <param name="update">The update.</param>
        /// <param name="order">The order.</param>
        /// <param name="rangeSize">Size of the range.</param>
        private void MergeSettings(IChannelDataRecord update, IndexOrder order, long rangeSize)
        {
            bool withinRange;
            var indexChannel = GetIndex();
            var increasing = indexChannel.Increasing;

            if (order == IndexOrder.After)
            {
                var indexValue = update.GetIndexValue();
                var range = GetIndexRange();

                if (!_chunkRange.Start.HasValue || !_chunkRange.Contains(indexValue, increasing))
                {
                    _chunkRange = new Range<double?>(
                        Range.ComputeRange(range.Start.GetValueOrDefault(), rangeSize, increasing).Start,
                        Range.ComputeRange(range.End.GetValueOrDefault(), rangeSize, increasing).End
                        );
                    _settingMerged = false;
                }

                withinRange = _chunkRange.Contains(indexValue, increasing);

                if (!withinRange)
                {
                    update.UpdateChannelSettings(this, false, false);
                }
                else
                {
                    UpdateChannelSettings(update, true);
                    update.CopyChannelSettings(this, _chunkRange);
                }
            }
            else
            {
                if (_settingMerged)
                    return;

                var range = GetIndexRange();
                _chunkRange = new Range<double?>(
                    Range.ComputeRange(range.Start.GetValueOrDefault(), rangeSize, increasing).Start,
                    Range.ComputeRange(range.End.GetValueOrDefault(), rangeSize, increasing).End
                    );

                withinRange = _chunkRange.Contains(update.GetIndexValue(), increasing);
                UpdateChannelSettings(update, withinRange);
                update.CopyChannelSettings(this, _chunkRange);
            }
        }

        /// <summary>
        /// Updates the index map for the chunk.
        /// </summary>
        /// <param name="withinRange">if set to <c>true</c> [within range].</param>
        private void UpdateIndexMap(bool withinRange = true)
        {
            _indexMap = new Dictionary<string, int>();
            if (withinRange)
            {
                for (var i = 0; i < Mnemonics.Length; i++)
                {
                    _indexMap.Add(Mnemonics[i], i);
                }
            }
            else
            {
                for (var i = 0; i < _originalMnemonics.Length; i++)
                {
                    _indexMap.Add(_originalMnemonics[i], i);
                }
            }
        }

        private bool WithinDeleteRange(double index, Range<double?> range, bool increasing)
        {
            return !range.StartsAfter(index, increasing) && !range.EndsBefore(index, increasing);
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // NOTE: dispose managed state (managed objects).
                    Close();
                }

                // NOTE: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // NOTE: set large fields to null.

                _disposedValue = true;
            }
        }

        // NOTE: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ChannelDataReader() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // NOTE: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
