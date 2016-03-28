using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PDS.Framework;

namespace PDS.Witsml.Data.Channels
{
    public class ChannelDataReader : IDataReader, IChannelDataRecord
    {
        private const string Null = "null";
        private const string NaN = "NaN";

        private static readonly string[] Empty = new string[0];
        private List<List<List<object>>> _records;
        private int _indexCount;
        private int _count;
        private int _current = -1;

        public ChannelDataReader(IList<string> data, string[] mnemonics = null, string[] units = null, string uri = null, string id = null) 
            : this(Combine(data), mnemonics, units, uri, id)
        {
        }

        public ChannelDataReader(string data, string[] mnemonics = null, string[] units = null, string uri = null, string id = null)
        {
            _records = Deserialize(data);
            _count = GetRowValues(0).Count();
            _indexCount = GetIndexValues(0).Count();

            Indices = new List<ChannelIndexInfo>();
            Mnemonics = mnemonics ?? Empty;
            Units = units ?? Empty;
            Uri = uri;
            Id = id;
        }

        public string Id { get; set; }

        public string Uri { get; set; }

        public string[] Mnemonics { get; private set; }

        public string[] Units { get; private set; }

        public List<ChannelIndexInfo> Indices { get; }

        public object this[string name]
        {
            get
            {
                var index = GetOrdinal(name);
                return index > -1 ? GetValue(index) : null;
            }
        }

        public object this[int i]
        {
            get { return GetValue(i); }
        }

        public int Depth
        {
            get { return _indexCount; }
        }

        public int FieldCount
        {
            get { return _count; }
        }

        public bool IsClosed
        {
            get { return _records == null || _current >= _records.Count; }
        }

        public int RecordsAffected
        {
            get { return _records.Count; }
        }

        public static string[] Split(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? Empty : value.Split(',');
        }

        public void Close()
        {
            _records = null;
            _current = -1;
        }

        public void Dispose()
        {
            Close();
        }

        public ChannelIndexInfo GetIndex(int index = 0)
        {
            return Indices.Skip(index).FirstOrDefault();
        }

        public double GetIndexValue(int index = 0)
        {
            var channelIndex = GetIndex(index);

            return channelIndex.IsTimeIndex
                ? GetUnixTimeSeconds(index)
                : GetDouble(index);
        }

        public Range<double?> GetIndexRange(int index = 0)
        {
            var channelIndex = GetIndex(index);

            if (channelIndex == null)
                return new Range<double?>(null, null);

            var start = GetIndexValues(0).Skip(index).FirstOrDefault();
            var end = GetIndexValues(RecordsAffected - 1).Skip(index).FirstOrDefault();

            return Range.Parse(start, end, channelIndex.IsTimeIndex);
        }

        public Range<double?> GetChannelIndexRange(int index)
        {
            if (RecordsAffected < 1)
                return GetIndexRange();

            if (index < Depth)
                return GetIndexRange(index);

            var channelIndex = GetIndex();
            var valueIndex = index - Depth;
            string start = null;
            string end = null;

            _records.ForEach(x =>
            {
                var value = string.Format("{0}", x.Last().Skip(valueIndex).FirstOrDefault());
                
                if (!IsNull(value))
                {
                    if (start == null)
                        start = value;

                    end = value;
                }
            });

            return Range.Parse(start, end, channelIndex.IsTimeIndex);
        }

        public bool GetBoolean(int i)
        {
            return bool.TrueString.EqualsIgnoreCase(GetString(i));
        }

        public byte GetByte(int i)
        {
            return byte.Parse(GetString(i));
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            return char.Parse(GetString(i));
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateTime(int i)
        {
            return DateTime.Parse(GetString(i));
        }

        public DateTimeOffset GetDateTimeOffset(int i)
        {
            return DateTimeOffset.Parse(GetString(i));
        }

        public long GetUnixTimeSeconds(int i)
        {
            return GetDateTimeOffset(i).ToUnixTimeSeconds();
        }

        public decimal GetDecimal(int i)
        {
            return decimal.Parse(GetString(i));
        }

        public double GetDouble(int i)
        {
            double value;
            return double.TryParse(GetString(i), out value) ? value : double.NaN;
        }

        public Type GetFieldType(int i)
        {
            throw new NotImplementedException();
        }

        public float GetFloat(int i)
        {
            float value;
            return float.TryParse(GetString(i), out value) ? value : float.NaN;
        }

        public Guid GetGuid(int i)
        {
            throw new NotImplementedException();
        }

        public short GetInt16(int i)
        {
            return short.Parse(GetString(i));
        }

        public int GetInt32(int i)
        {
            return int.Parse(GetString(i));
        }

        public long GetInt64(int i)
        {
            return long.Parse(GetString(i));
        }

        public string GetName(int i)
        {
            return Mnemonics.Skip(i).FirstOrDefault();
        }

        public int GetOrdinal(string name)
        {
            return Array.IndexOf(Mnemonics, name);
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public string GetString(int i)
        {
            return string.Format("{0}", GetValue(i));
        }

        public object GetValue(int i)
        {
            var value = GetRowValues(_current).Skip(i).FirstOrDefault();
            var array = value as JArray;

            if (array != null && array.Count == 1)
            {
                value = array[0];
            }

            return value;
        }

        public int GetValues(object[] values)
        {
            var count = Math.Min(values.Length, _count);
            var source = GetRowValues(_current).Take(count).ToArray();

            Array.Copy(source, values, count);
            return count;
        }

        public bool IsDBNull(int i)
        {
            return IsNull(GetString(i));
        }

        public bool NextResult()
        {
            return false;
        }

        public bool Read()
        {
            _current++;
            return !IsClosed;
        }

        public void Reset()
        {
            _current = -1;
        }

        public string GetJson()
        {
            if (IsClosed)
                return null;

            return JsonConvert.SerializeObject(_records[_current]);
        }

        public IEnumerable<IChannelDataRecord> AsEnumerable()
        {
            while (Read())
            {
                yield return this;
            }
        }

        private IEnumerable<object> GetRowValues(int row)
        {
            if (IsClosed)
                return Enumerable.Empty<object>();

            return _records
                .Skip(row)
                .Take(1)
                .SelectMany(x => x.SelectMany(y => y));
        }

        private IEnumerable<object> GetIndexValues(int row)
        {
            if (IsClosed)
                return Enumerable.Empty<object>();

            return _records
                .Skip(row)
                .Take(1)
                .SelectMany(x => x.First());
        }

        private IEnumerable<object> GetChannelValues(int row)
        {
            if (IsClosed)
                return Enumerable.Empty<object>();

            return _records
                .Skip(row)
                .Take(1)
                .SelectMany(x => x.Last());
        }

        private List<List<List<object>>> Deserialize(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return new List<List<List<object>>>();

            return JsonConvert.DeserializeObject<List<List<List<object>>>>(data);
        }

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

        private static string Format(string value)
        {
            double number;

            if (IsNull(value))
                return "null";

            if (double.TryParse(value, out number))
                return value;

            return string.Format("\"{0}\"", value.Trim());
        }

        private static bool IsNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) || 
                Null.EqualsIgnoreCase(value) || 
                NaN.EqualsIgnoreCase(value);
        }
    }
}
