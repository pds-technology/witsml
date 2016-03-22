using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using MongoDB.Driver;
using Newtonsoft.Json;
using PDS.Server.MongoDb;
using PDS.Witsml.Server.Models;

namespace PDS.Witsml.Server.Data.Channels
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="ChannelSetValues"/>
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{PDS.Witsml.Server.Models.ChannelSetValues}" />
    [Export]
    public class ChannelDataAdapter : MongoDbDataAdapter<ChannelSetValues>
    {
        private static readonly int RangeSize = Settings.Default.LogIndexRangeSize;
        private static readonly char Separator = ',';

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public ChannelDataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectTypes.ChannelSetValues, ObjectTypes.Uid)
        {
            
        }

        /// <summary>
        /// Writes the channel set values for WITSML 2.0 Log.
        /// </summary>
        /// <param name="uidLog">The uid of the log.</param>
        /// <param name="channelData">The data for channel set of the log.</param>
        /// <param name="indicesMap">The index map for the list of channel set.</param>
        public void WriteChannelSetValues(string uidLog, Dictionary<string, string> channelData, Dictionary<string, List<ChannelIndexInfo>> indicesMap)
        {
            if (indicesMap == null || indicesMap.Keys.Count == 0)
                return;

            var collection = GetCollection<ChannelSetValues>(DbCollectionName);
            var dataChunks = new List<ChannelSetValues>();

            foreach (var key in indicesMap.Keys)
            {
                var dataChunk = CreateChannelSetValuesList(channelData[key], uidLog, key, indicesMap[key]);
                if (dataChunk != null && dataChunk.Count > 0)
                    dataChunks.AddRange(dataChunk);
            }

            collection.BulkWrite(dataChunks
                .Select(dc =>
                {
                    return new InsertOneModel<ChannelSetValues>(dc);
                }));
        }

        /// <summary>
        /// Writes the log data values for WITSML 1.4 log.
        /// </summary>
        /// <param name="uidLog">The uid of the log.</param>
        /// <param name="data">The log data.</param>
        /// <param name="mnemonicList">The mnemonic list for the log data.</param>
        /// <param name="indexChannel">The index channel.</param>
        public void WriteLogDataValues(string uidLog, List<string> data, string mnemonicList, string unitList, ChannelIndexInfo indexChannel)
        {
            var collection = GetCollection<ChannelSetValues>(DbCollectionName);

            collection.BulkWrite(ToChunks(indexChannel, GetSequence(string.Empty, !indexChannel.IsTimeIndex, data))
                .Select(dc =>
                {
                    dc.UidLog = uidLog;
                    dc.Uid = NewUid();
                    dc.MnemonicList = mnemonicList;
                    dc.UnitList = unitList;
                    return new InsertOneModel<ChannelSetValues>(dc);
                }));
        }

        /// <summary>
        /// Gets the log data for WITSML 1.4 log (May refactor in future to accommodate 1.3 log)
        /// </summary>
        /// <param name="uidLog">The uid of the log.</param>
        /// <param name="mnemonics">The subset of mnemonics for the requested log curves, i.e queryIn, that exists in the log.</param>
        /// <param name="range">The requested index range; double for both depth and time (needs to convert to Unix seconds).</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <returns>The LogData object that contains the log data (only includes curve(s) that having valid data within the request range.</returns>
        public LogData GetLogData(string uidLog, List<string> mnemonics, Tuple<double?, double?> range, bool increasing)
        {         
            var indexCurve = mnemonics[0].Trim();

            // Build Log Data filter
            var filter = BuildDataFilter(uidLog, indexCurve, range, increasing);           

            // Query channelSetValues collection
            var results = GetData(filter, increasing);
            if (results == null || results.Count == 0)
                return null;

            // Convert data
            var logData = new LogData();
            TransformLogData(logData, results, mnemonics, range, increasing);
            return logData;
        }

        /// <summary>
        /// Saves the channel sets to its own collection in addition as a property for the log.
        /// </summary>
        /// <param name="entity">The log entity.</param>
        /// <param name="channelData">The collection to extract the channel set data.</param>
        /// <param name="indicesMap">The indices map for the list of channel set.</param>
        public void SaveChannelSets(Log entity, Dictionary<string, string> channelData, Dictionary<string, List<ChannelIndexInfo>> indicesMap)
        {
            var collection = GetCollection<ChannelSet>(ObjectNames.ChannelSet200);

            collection.BulkWrite(entity.ChannelSet
                .Select(cs =>
                {
                    if (cs.Data != null && !string.IsNullOrEmpty(cs.Data.Data))
                    {
                        var uuid = cs.Uuid;
                        channelData.Add(uuid, cs.Data.Data);
                        indicesMap.Add(uuid, CreateChannelSetIndexInfo(cs.Index));
                        cs.Data.Data = null;
                    }
                    return new InsertOneModel<ChannelSet>(cs);
                }));
        }

        /// <summary>
        /// Creates the list of index info to be used for channel set values.
        /// </summary>
        /// <param name="indices">The original index list of a channel set.</param>
        /// <returns>The list of index info.</returns>
        private List<ChannelIndexInfo> CreateChannelSetIndexInfo(List<ChannelIndex> indices)
        {
            var indicesInfo = new List<ChannelIndexInfo>();
            foreach (var index in indices)
            {
                var indexInfo = new ChannelIndexInfo
                {
                    Mnemonic = index.Mnemonic,
                    Increasing = index.Direction == IndexDirection.increasing,
                    IsTimeIndex = index.IndexType == ChannelIndexType.datetime || index.IndexType == ChannelIndexType.elapsedtime
                };
                indicesInfo.Add(indexInfo);
            }

            return indicesInfo;
        }


        /// <summary>
        /// Gets the log data from channelSetValues collection.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <returns>The list of log data chunks that fit the query criteria sorted by the start index.</returns>
        private List<ChannelSetValues> GetData(FilterDefinition<ChannelSetValues> filter, bool increasing)
        {          
            var collection = GetCollection<ChannelSetValues>(DbCollectionName);
            var sortBuilder = Builders<ChannelSetValues>.Sort;
            var sortField = "Indices.Start";
            var sort = increasing ? sortBuilder.Ascending(sortField) : sortBuilder.Descending(sortField);

            var filterJson = filter.Render(collection.DocumentSerializer, collection.Settings.SerializerRegistry);

            return collection.Find(filter ?? "{}").Sort(sort).ToList();
        }

        /// <summary>
        /// Builds the data filter for the database query.
        /// </summary>
        /// <param name="uidLog">The uid of the log.</param>
        /// <param name="indexCurve">The index curve mnemonic.</param>
        /// <param name="range">The request range.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <returns>The query filter.</returns>
        private FilterDefinition<ChannelSetValues> BuildDataFilter(string uidLog, string indexCurve, Tuple<double?, double?> range, bool increasing)
        {
            var filters = new List<FilterDefinition<ChannelSetValues>>();
            filters.Add(Builders<ChannelSetValues>.Filter.EqIgnoreCase("UidLog", uidLog));

            var rangeFilters = new List<FilterDefinition<ChannelSetValues>>();
            if (range != null)
            {
                if (range.Item1.HasValue)
                {
                    var start = increasing ?
                        Builders<ChannelSetValues>.Filter.Gte("Indices.End", range.Item1.Value) :
                        Builders<ChannelSetValues>.Filter.Lte("Indices.End", range.Item1.Value);
                    rangeFilters.Add(start);
                }
                if (range.Item2.HasValue)
                {
                    var start = increasing ?
                        Builders<ChannelSetValues>.Filter.Lte("Indices.Start", range.Item2.Value) :
                        Builders<ChannelSetValues>.Filter.Gte("Indices.Start", range.Item2.Value);
                    rangeFilters.Add(start);
                }
            }
            
            if (rangeFilters.Count > 0)
                rangeFilters.Add(Builders<ChannelSetValues>.Filter.EqIgnoreCase("Indices.Mnemonic", indexCurve));

            if (rangeFilters.Count > 0)
                filters.Add(Builders<ChannelSetValues>.Filter.And(rangeFilters));
            return Builders<ChannelSetValues>.Filter.And(filters);
        }

        /// <summary>
        /// Transforms the log data chunks as 1.4 log data.
        /// </summary>
        /// <param name="logData">The <see cref="LogData"/> object.</param>
        /// <param name="results">The list of log data chunks.</param>
        /// <param name="mnemonics">The subset of mnemonics for the requested log curves.</param>
        private void TransformLogData(LogData logData, List<ChannelSetValues> results, List<string> mnemonics, Tuple<double?, double?> rowRange, bool increasing)
        {
            logData.Data = new List<string>();
            var dataList = new List<List<string>>();
            var rangeList = new List<List<string>>();
            List<string> dataMnemonics = null;
            List<string> units = null;
            var requestIndex = new List<int>();
            foreach (var result in results)
            {
                var values = DeserializeLogData(result.Data);
                             
                if (dataMnemonics == null)
                {
                    dataMnemonics = result.MnemonicList.Split(Separator).ToList();
                    units = result.UnitList.Split(Separator).ToList();
                    foreach (var request in mnemonics)
                    {
                        if (dataMnemonics.Contains(request))
                        {
                            requestIndex.Add(dataMnemonics.IndexOf(request));
                            rangeList.Add(new List<string>());
                        }
                    }
                }

                foreach (var value in values)
                {
                    // Get the Index from the current row of values
                    double? rowIndex = GetRowIndex(value);

                    // If the row's index is not within the rowRange then skip this row.
                    if (NotInRange(rowIndex, rowRange, increasing))
                        continue;

                    // if the row's index is past the rowRange then stop transforming anymore rows.
                    if (OutOfRange(rowIndex, rowRange, increasing))
                        break;

                    // Transform the current row.
                    dataList.Add(TransformLogDataRow(value, requestIndex, rangeList));
                }
            }

            var validIndex = new List<int>();
            var validMnemonics = new List<string>();
            var validUnits = new List<string>();

            for (var i = 0; i < rangeList.Count; i++)
            {
                var range = rangeList[i];
                if (range.Count > 0)
                {
                    validIndex.Add(i);
                    validMnemonics.Add(mnemonics[i]);
                    validUnits.Add(units[i]);
                }
            }

            logData.MnemonicList = string.Join(Separator.ToString(), validMnemonics);
            logData.UnitList = string.Join(Separator.ToString(), validUnits);

            foreach (var row in dataList)
            {
                logData.Data.Add(ConcatLogDataRow(row, validIndex));
            }

            mnemonics = validMnemonics;
        }

        private bool NotInRange(double? rowIndex, Tuple<double?, double?> rowRange, bool increasing)
        {
            if (rowIndex.HasValue && rowRange != null && rowRange.Item1.HasValue)
            {
                return increasing 
                    ? rowIndex.Value < rowRange.Item1.Value
                    : rowIndex.Value > rowRange.Item1.Value;

            }

            return false;
        }

        private bool OutOfRange(double? rowIndex, Tuple<double?, double?> rowRange, bool increasing)
        {
            if (rowIndex.HasValue && rowRange != null && rowRange.Item2.HasValue)
            {
                return increasing
                    ? rowIndex.Value > rowRange.Item2.Value
                    : rowIndex.Value < rowRange.Item2.Value;
            }

            return false;
        }

        private double? GetRowIndex(string value)
        {
            if (value != null)
            {
                var columns = value.Split(',');
                if (columns.Length > 0)
                {
                    double indexValue = 0;
                    if (double.TryParse(columns[0], out indexValue))
                    {
                        return indexValue;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Splits the log data at an index into an list of points and update the valid index range for each log curve.
        /// </summary>
        /// <param name="data">The data string at the index.</param>
        /// <param name="requestIndex">The computed index for the points (split from the data string) that are requested.</param>
        /// <param name="rangeList">The index range list for each requested curve.</param>
        /// <returns></returns>
        private List<string> TransformLogDataRow(string data, List<int> requestIndex, List<List<string>> rangeList)
        {
            var points = data.Split(Separator);
            var result = new List<string>();
            for (var i = 0; i < requestIndex.Count; i++)
            {
                var index = points[0];
                var point = points[requestIndex[i]];
                result.Add(point);
                if (string.IsNullOrEmpty(point))
                    continue;

                var range = rangeList[i];
                if (range.Count == 0)
                {
                    range.Add(index);
                    range.Add(index);
                }
                else
                {
                    range[1] = index;
                }
            }
            return result;
        }

        private string ConcatLogDataRow(List<string> points, List<int> validIndex)
        {
            var sb = new StringBuilder();

            sb.Append(points[validIndex[0]]);
            for (var j = 1; j < validIndex.Count; j++)
                sb.AppendFormat("{0}{1}", Separator, points[validIndex[j]]);

            return sb.ToString();
        }

        /// <summary>
        /// Combines a sequence of log data values into a "chunk" of data in a single <see cref="LogDataValues"/> instnace.
        /// </summary>
        /// <param name="sequence">An <see cref="IEnumerable{Tuple{string, double, string}}"/> containing a uid, the index value of the data and the row of data.</param>
        /// <returns>An <see cref="IEnumerable{LogDataValues}"/> of "chunked" data.</returns>
        private IEnumerable<ChannelSetValues> ToChunks(ChannelIndexInfo indexChannel, IEnumerable<Tuple<string, double, string>> sequence)
        {
            Tuple<int, int> plannedRange = null;
            double startIndex = 0;
            double endIndex = 0;
            var data = new List<string>();
            string uid = string.Empty;
            var increasing = indexChannel.Increasing;

            foreach (var item in sequence)
            {
                if (plannedRange == null)
                {
                    plannedRange = ComputeRange(item.Item2, RangeSize, increasing);
                    uid = item.Item1;
                    startIndex = item.Item2;
                }

                if (WithinRange(item.Item2, plannedRange.Item2, increasing, false))
                {
                    uid = string.IsNullOrEmpty(uid) ? item.Item1 : uid;
                    data.Add(item.Item3);
                    endIndex = item.Item2;
                }
                else
                {
                    var newIndex = indexChannel.Clone();
                    newIndex.Start = startIndex;
                    newIndex.End = endIndex;

                    yield return new ChannelSetValues()
                    {
                        Data = SerializeLogData(data),
                        Indices = new List<ChannelIndexInfo> { newIndex }
                    };

                    plannedRange = ComputeRange(item.Item2, RangeSize, increasing);
                    data = new List<string>();
                    data.Add(item.Item3);
                    startIndex = item.Item2;
                    endIndex = item.Item2;
                    uid = item.Item1;
                }
            }

            if (data.Count > 0)
            {
                var newIndex = indexChannel.Clone();
                newIndex.Start = startIndex;
                newIndex.End = endIndex;

                yield return new ChannelSetValues()
                {
                    Data = SerializeLogData(data),
                    Indices = new List<ChannelIndexInfo> { newIndex }
                };
            }
        }

        private IEnumerable<Tuple<string, double, string>> GetSequence(string uid, bool isDepthLog, IEnumerable<string> dataList, int[] sliceIndexes = null)
        {
            foreach (var dataRow in dataList)
            {
                // TODO: Validate the index order (is the data sorted?)
                var allValues = dataRow.Split(Separator);
                double index;
                if (isDepthLog)
                    index = double.Parse(allValues[0]);
                else
                    index = DateTimeOffset.Parse(allValues[0]).ToUnixTimeSeconds();

                // If we're not slicing send all the data
                if (sliceIndexes == null)
                {
                    yield return new Tuple<string, double, string>(uid, index, dataRow);
                }
                else
                {
                    var slicedData = string.Join(Separator.ToString(), SliceStringList(allValues, sliceIndexes));
                    yield return new Tuple<string, double, string>(uid, index, slicedData);
                }
            }
        }

        private IEnumerable<string> SliceStringList(IEnumerable<string> stringList, int[] sliceIndexes)
        {
            if (sliceIndexes != null)
            {
                return stringList.Where((x, i) => sliceIndexes.Contains(i)).ToArray();
            }
            return stringList;
        }

        /// <summary>
        /// Deserializes the channel set data from 2.0 log into a 3 dimensional (index, channel, channel data) object list.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        private List<List<List<object>>> DeserializeChannelSetData(string data)
        {
            return JsonConvert.DeserializeObject<List<List<List<object>>>>(data);
        }

        /// <summary>
        /// Serializes the channel set data for the 2.0 log to save to the database.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The serialized string of the data.</returns>
        private string SerializeChannelSetData(List<List<List<object>>> data)
        {
            return JsonConvert.SerializeObject(data);
        }

        /// <summary>
        /// Serializes the log data for the 1.4 log.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The serialized string of the data.</returns>
        private string SerializeLogData(List<string> data)
        {
            return JsonConvert.SerializeObject(data);
        }

        private List<string> DeserializeLogData(string data)
        {
            return JsonConvert.DeserializeObject<List<string>>(data);
        }

        /// <summary>
        /// Computes the range.
        /// </summary>
        /// <param name="index">The start index.</param>
        /// <param name="rangeSize">Size of the range.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <returns>The range.</returns>
        private Tuple<int, int> ComputeRange(double index, int rangeSize, bool increasing = true)
        {
            // TODO: Create a Range struct, or a class if necessary, instead of using a Tuple
            var rangeIndex = increasing ? (int)(Math.Floor(index / rangeSize)) : (int)(Math.Ceiling(index / rangeSize));
            return new Tuple<int, int>(rangeIndex * rangeSize, rangeIndex * rangeSize + (increasing ? rangeSize : -rangeSize));
        }

        /// <summary>
        /// Transform the original channel set data string to list of data chunks (For WITSML 2.0 log).
        /// </summary>
        /// <param name="data">The original channel set data.</param>
        /// <param name="uidLog">The uid of the log.</param>
        /// <param name="uidChannelSet">The uid of the channel set.</param>
        /// <param name="indices">The list of index for the channel set.</param>
        /// <returns>The list of data chunks.</returns>
        private List<ChannelSetValues> CreateChannelSetValuesList(string data, string uidLog, string uidChannelSet, List<ChannelIndexInfo> indices)
        {
            var dataChunks = new List<ChannelSetValues>();
            var logData = DeserializeChannelSetData(data);

            double start, end;

            var isTimeIndex = indices.First().IsTimeIndex;

            // TODO: Create a helper method to get start and end (instead of First.First.First....)
            if (isTimeIndex)
            {
                start = ParseTime(logData.First().First().First().ToString());
                end = ParseTime(logData.Last().First().First().ToString());
            }
            else
            {
                start = (double)logData.First().First().First();
                end = (double)logData.Last().First().First();
            }

            var increasing = indices.First().Increasing;

            var rangeSize = ComputeRange(start, RangeSize, increasing);

            do
            {
                var chunk = 
                    increasing
                        ? (isTimeIndex 
                            ? logData.Where(d => ParseTime(d.First().First().ToString()) >= rangeSize.Item1 &&  ParseTime(d.First().First().ToString()) < rangeSize.Item2).ToList() 
                            : logData.Where(d => (double)d.First().First() >= rangeSize.Item1 && (double)d.First().First() < rangeSize.Item2).ToList())
                        // Decreasing
                        : (isTimeIndex 
                            ? logData.Where(d => ParseTime(d.First().First().ToString()) <= rangeSize.Item1 &&  ParseTime(d.First().First().ToString()) > rangeSize.Item2).ToList() 
                            : logData.Where(d => (double)d.First().First() <= rangeSize.Item1 && (double)d.First().First() > rangeSize.Item2).ToList());

                SetChunkIndices(chunk.First().First(), chunk.Last().First(), indices);

                var channelSetValues = new ChannelSetValues
                {
                    Uid = NewUid(),
                    UidLog = uidLog,
                    UidChannelSet = uidChannelSet,
                    Indices = indices,
                    Data = SerializeChannelSetData(chunk)
                };

                dataChunks.Add(channelSetValues);
                rangeSize = new Tuple<int, int>(rangeSize.Item1 + RangeSize, 
                    increasing 
                        ? rangeSize.Item2 + RangeSize 
                        : rangeSize.Item2 - RangeSize);
            }
            while (WithinRange(rangeSize.Item2, end, increasing));           

            return dataChunks;
        }

        /// <summary>
        /// Check if the iteration is within the range: includes end point for entire data; excludes end point for chunking.
        /// </summary>
        /// <param name="current">The current index.</param>
        /// <param name="end">The end index.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <param name="closeRange">if set to <c>true</c> [close range].</param>
        /// <returns>True if within range; false if not.</returns>
        private bool WithinRange(double current, double end, bool increasing, bool closeRange = true)
        {
            if (closeRange)
                return increasing ? current <= end : current >= end;
            else
                return increasing ? current < end : current > end;
        }

        /// <summary>
        /// Sets the index range for each data chunk.
        /// </summary>
        /// <param name="starts">The list of start index values.</param>
        /// <param name="ends">The list of end index values.</param>
        /// <param name="indices">The list of index info object.</param>
        private void SetChunkIndices(List<object> starts, List<object> ends, List<ChannelIndexInfo> indices)
        {
            for (var i = 0; i < indices.Count; i++)
            {
                var index = indices[i];
                if (index.IsTimeIndex)
                {
                    index.Start = ParseTime(starts[i].ToString());
                    index.End = ParseTime(ends[i].ToString());
                }
                else
                {
                    index.Start = double.Parse(starts[i].ToString());
                    index.End = double.Parse(ends[i].ToString());
                }
            }
        }

        /// <summary>
        /// Parses the time from string input and converts to Unix seconds.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The time in Unix seconds.</returns>
        private double ParseTime(string input)
        {
            return DateTimeOffset.Parse(input).ToUnixTimeSeconds();
        }
    }
}
