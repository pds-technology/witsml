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

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PDS.Framework;
using PDS.Witsml;

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

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
        {
            DateParseHandling = DateParseHandling.DateTimeOffset
        };

        private static ILog _log = LogManager.GetLogger(typeof(ChannelDataReader));
        private static readonly string[] Empty = new string[0];
        private List<List<List<object>>> _records;
        private IList<Range<double?>> _ranges;
        private string[] _originalMnemonics = Empty;
        private string[] _originalUnits = Empty;
        private readonly int _indexCount;
        private readonly int _count;
        private int _current = -1;

        /// <summary>
        /// Ordinal position of mnemonics that are included in slicing.  Null if reader is not sliced.
        /// </summary>
        private int[] _sliceOrdinals;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataReader"/> class.
        /// </summary>
        /// <param name="data">The channel data.</param>
        /// <param name="mnemonics">The channel mnemonics.</param>
        /// <param name="units">The channel units.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="id">The identifier.</param>
        public ChannelDataReader(IList<string> data, string[] mnemonics = null, string[] units = null, string uri = null, string id = null) 
            : this(Combine(data), mnemonics, units, uri, id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataReader"/> class.
        /// </summary>
        /// <param name="data">The channel data.</param>
        /// <param name="mnemonics">The channel mnemonics.</param>
        /// <param name="units">The channel units.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="id">The identifier.</param>
        public ChannelDataReader(string data, string[] mnemonics = null, string[] units = null, string uri = null, string id = null)
            : this(Deserialize(data), mnemonics, units, uri, id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataReader"/> class.
        /// </summary>
        /// <param name="records">The collection of data records.</param>
        public ChannelDataReader(IEnumerable<IChannelDataRecord> records)
        {
            _log.Debug("ChannelDataReader instance created");

            var items = records
                .Cast<ChannelDataReader>()
                .Select(x => new { Row = x._current, Record = x })
                .ToList();

            var record = items.Select(x => x.Record).FirstOrDefault();
            _records = items.Select(x => x.Record._records[x.Row]).ToList();

            _count = GetRowValues(0).Count();
            _indexCount = GetIndexValues(0).Count();
            _originalMnemonics = record?.Mnemonics ?? Empty;
            _originalUnits = record?.Units ?? Empty;

            Indices = record?.Indices ?? new List<ChannelIndexInfo>();
            Mnemonics = _originalMnemonics;
            Units = _originalUnits;
            Uri = record?.Uri;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataReader"/> class.
        /// </summary>
        /// <param name="records">The channel records.</param>
        /// <param name="mnemonics">The channel mnemonics.</param>
        /// <param name="units">The channel units.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="id">The identifier.</param>
        internal ChannelDataReader(List<List<List<object>>> records, string[] mnemonics = null, string[] units = null, string uri = null, string id = null)
        {
            _log.Debug("ChannelDataReader instance created");

            _records = records;
            _count = GetRowValues(0).Count();
            _indexCount = GetIndexValues(0).Count();
            _originalMnemonics = mnemonics ?? Empty;
            _originalUnits = units ?? Empty;

            Indices = new List<ChannelIndexInfo>();
            Mnemonics = _originalMnemonics;
            Units = _originalUnits;
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

        public string[] AllMnemonics
        {
            get { return Indices.Select(i => i.Mnemonic).Union(Mnemonics).ToArray(); }
        }

        /// <summary>
        /// Gets the mnemonics included in slicing or all mnemonics if not sliced.
        /// </summary>
        /// <value>The list of channel mnemonics.</value>
        public string[] Mnemonics { get; private set; }

        public string[] AllUnits
        {
            get { return Indices.Select(i => i.Unit).Union(Units).ToArray(); }
        }

        /// <summary>
        /// Gets the units included in slicing or all units if not sliced.
        /// </summary>
        /// <value>The list of channel units.</value>
        public string[] Units { get; private set; }

        /// <summary>
        /// Gets the indices.
        /// </summary>
        /// <value>A list of indices.</value>
        public List<ChannelIndexInfo> Indices { get; private set; }

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
            return string.IsNullOrWhiteSpace(value) ? Empty : value.Split(',');
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
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Close();
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
                ? GetUnixTimeSeconds(index)
                : GetDouble(index) * Math.Pow(10, scale);
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
                var allSlices = Indices.Select(i => i.Mnemonic).Union(Mnemonics).ToArray();

                allSlices.ForEach(m =>
                {
                    var range = _ranges[GetOrdinal(m)];
                    if (range.Start.HasValue)
                    {
                        channelRanges.Add(m, range);
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
        /// <exception cref="System.NotImplementedException"></exception>
        public string GetDataTypeName(int i)
        {
            throw new NotImplementedException();
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
            return DateTime.Parse(GetString(i));
        }

        /// <summary>
        /// Gets the date time offset.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns></returns>
        public DateTimeOffset GetDateTimeOffset(int i)
        {
            return DateTimeOffset.Parse(GetString(i));
        }

        /// <summary>
        /// Gets the unix time seconds.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns></returns>
        public long GetUnixTimeSeconds(int i)
        {
            return GetDateTimeOffset(i).ToUnixTimeSeconds();
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
        /// Gets the <see cref="T:System.Type" /> information corresponding to the type of <see cref="T:System.Object" /> 
        /// that would be returned from <see cref="M:System.Data.IDataRecord.GetValue(System.Int32)" />.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The <see cref="T:System.Type" /> information corresponding to the type of <see cref="T:System.Object" /> 
        /// that would be returned from <see cref="M:System.Data.IDataRecord.GetValue(System.Int32)" />.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Type GetFieldType(int i)
        {
            throw new NotImplementedException();
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
        /// <exception cref="System.NotImplementedException"></exception>
        public Guid GetGuid(int i)
        {
            throw new NotImplementedException();
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
            return GetAllMnemonics().Skip(i).FirstOrDefault();
        }

        private string[] _allMnemonics = null;

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
        /// <returns>
        /// A <see cref="T:System.Data.DataTable" /> that describes the column metadata.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
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
            return string.Format("{0}", GetValue(i));
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
            if (!SliceExists(i))
            {
                return null;
            }

            var value = GetRowValues(_current).Skip(i).FirstOrDefault();
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
            // Slice the results of GetRowValues
            var rowValues = GetRowValues(_current).Where((r, i) => SliceExists(i));
            
            var count = Math.Min(values.Length, rowValues.Count());

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
            return IsNull(GetString(i));
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
        /// Determines whether this instance has values.
        /// </summary>
        /// <returns>true if the current row has values, false otherwise.</returns>
        public bool HasValues()
        {
            return GetChannelValues(_current)
                .Any(x => x != null && !IsNull(x.ToString()));
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
        public void Slice(IDictionary<int, string> mnemonicSlices, IDictionary<int, string> units)
        {
            // Remove the index mnemonic from the mnemonicSlices
            var indices = Indices.Select(i => i.Mnemonic);
            string[] slices = mnemonicSlices.Values.ToArray()
                .Where(m => !indices.Contains(m))
                .ToArray();

            _allMnemonics = null;
            Mnemonics = slices;
            Units = null;

            // Slice by requestedMnemonics first
            _sliceOrdinals = slices
                .Select(m => GetOrdinal(m)).ToArray(); // Get the ordinal position of each slice.

            // Call GetChannelRanges so we can see which ranges have data or not.  
            //... Ranges will only be calculated for the current slices.
            var ranges = GetChannelRanges();

            // Apply slicing to mnemonics and units without and data (range)
            Mnemonics = Mnemonics.Where(m => ranges.Keys.Contains(m)).ToArray();
            _sliceOrdinals = Mnemonics
                .Select(m => GetOrdinal(m)).ToArray();
            Units = Mnemonics.Select(m => _originalUnits[GetOrdinal(m) - Depth]).ToArray();

            // If there is data then update the mnemonics and units from the caller.
            if (RecordsAffected > 0)
            {
                // Get mnemonic ids for mnemonics that are not in the reader's mnemonics
                var removeKeys = mnemonicSlices.Where(m => !AllMnemonics.Contains(m.Value)).Select(m => m.Key).ToArray();

                // Remove mnemonics and corresponding units that are not in the reader
                removeKeys.ForEach(k =>
                {
                    mnemonicSlices.Remove(k);
                    units.Remove(k);
                });
            }
        }

        public List<List<object>> GetData(int? requestLatestValues, out Dictionary<string, Range<double?>> ranges)
        {
            var logData = new List<List<object>>();
            var isTimeIndex = Indices.Select(x => x.IsTimeIndex).FirstOrDefault();

            // Ranges will only be returned for channels that are included in slicing
            //... and contain data.
            ranges = GetChannelRanges();

            // TODO: Add support for requestLatestValues

            // Read through all of the data
            while (Read())
            {
                var values = new List<object>();
                var index = GetIndexValue();

                // Add the index value to the values list and 
                //... use timestamp format for time index values
                values.Add(isTimeIndex
                    ? GetDateTimeOffset(0).ToString("o")
                    : (object)index);

                // Only add channel values to the list of values
                //... if the are included in current slices
                for (int i = 1; i < FieldCount; i++)
                {
                    var channelValue = GetValue(i);

                    // Limit data to mnemonics slices                    
                    if (SliceExists(i))
                        values.Add(channelValue);
                }

                // Filter rows with no channel values
                if (values.Count > 1)
                {
                    //if (!requestLatestValues.HasValue || IsRequestedValueNeeded(values, requestedValueCount, requestLatestValues.Value))
                    //{
                    logData.Add(values);
                    //start = start ?? index;
                    //end = index;

                    // Update the latest value count for each channel.
                    //if (requestLatestValues.HasValue)
                    //{
                    //    UpdateRequestedValueCount(requestedValueCount, values, mnemonics, ranges, index);
                    //}
                    //}

                    // if latest values requested and we have all of the requested values we need, break out;
                    //if (requestLatestValues.HasValue && HasRequestedValuesForAllChannels(requestedValueCount, requestLatestValues.Value))
                    //{
                    //    break;
                    //}
                }
            }

            return logData;
        }

        /// <summary>
        /// Sets an array of all of the original mnemonics including the index mnemonics.
        /// </summary>
        private string[] GetAllMnemonics()
        {
            if (_allMnemonics == null)
            {
                _allMnemonics = Indices.Select(i => i.Mnemonic).Union(_originalMnemonics).ToArray();
            }

            return _allMnemonics;
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

            return _records
                .Skip(row)
                .Take(1)
                .SelectMany(x => x.First());
        }

        /// <summary>
        /// Gets the channel values only without the metadata.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns>An <see cref="IEnumerable{Object}"/></returns>
        private IEnumerable<object> GetChannelValues(int row)
        {
            if (IsClosed)
                return Enumerable.Empty<object>();

            // Slice row
            return _records
                .Skip(row)
                .Take(1)
                .SelectMany(x => x.Last())
                .Where((r, i) => SliceExists(i));
        }

        /// <summary>
        /// Calculates the index ranges for all channels.
        /// </summary>
        /// <returns>A collection of channel index ranges.</returns>
        private IList<Range<double?>> CalculateChannelIndexRanges()
        {
            var ranges = new List<Range<double?>>();
            var channelIndex = GetIndex();

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
            var valueIndex = i - Depth;
            object start = null;
            object end = null;


            if (SliceExists(i))
            {
                _records.ForEach(x =>
                {
                    var value = string.Format("{0}", x.Last().Skip(valueIndex).FirstOrDefault());

                    if (!IsNull(value))
                    {
                        end = x[0][0];

                        if (start == null)
                            start = end;
                    }
                });
            }

            return Range.Parse(start, end, isTimeIndex);
        }

        /// <summary>
        /// Deserializes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        private static List<List<List<object>>> Deserialize(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return new List<List<List<object>>>();
            
            return JsonConvert.DeserializeObject<List<List<List<object>>>>(data, JsonSettings);       
        }

        /// <summary>
        /// Combines the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>A JSON arrary of string values from the data list.</returns>
        private static string Combine(IList<string> data)
        {
            var json = new StringBuilder("[");
            var rows = new List<string>();

            if (data != null)
            {
                foreach (var row in data)
                {
                    var values = row.Split(new[] { ',' })
                        .Select(Format)
                        .ToArray();

                    rows.Add(string.Format(
                        "[[{0}],[{1}]]", 
                        values.First(),
                        string.Join(",", values.Skip(1))));
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

            return string.Format("\"{0}\"", value.Trim());
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
        /// Tests if a slice exists for the given ordinal position.
        /// </summary>
        /// <param name="ordinal">The ordinal position being tested.</param>
        /// <returns>true if the ordinal position exists in the list of slice ordinal positions, false otherwise.</returns>
        private bool SliceExists(int ordinal)
        {
            return ordinal < Depth || _sliceOrdinals == null || _sliceOrdinals.Contains(ordinal);
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
    }
}
