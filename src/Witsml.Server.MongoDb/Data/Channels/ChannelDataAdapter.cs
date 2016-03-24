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
using PDS.Witsml.Server.Models;
using PDS.Witsml.Server.MongoDb;

namespace PDS.Witsml.Server.Data.Channels
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="ChannelDataValues"/>
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{PDS.Witsml.Server.Models.ChannelDataValues}" />
    [Export]
    public class ChannelDataAdapter : MongoDbDataAdapter<ChannelDataValues>
    {
        private static readonly int RangeSize = Settings.Default.LogIndexRangeSize;
        private const string Delimiter = ",";
        private const char Separator = ',';

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public ChannelDataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectTypes.ChannelDataValues, ObjectTypes.Uid)
        {
            
        }

        /// <summary>
        /// Writes the channel set values for WITSML 2.0 Log.
        /// </summary>
        /// <param name="uidLog">The uid of the log.</param>
        /// <param name="channelData">The data for channel set of the log.</param>
        /// <param name="indicesMap">The index map for the list of channel set.</param>
        public void WriteChannelDataValues(string uidLog, Dictionary<string, string> channelData, Dictionary<string, List<ChannelIndexInfo>> indicesMap)
        {
            if (indicesMap == null || indicesMap.Keys.Count == 0)
                return;

            var collection = GetCollection();
            var dataChunks = new List<ChannelDataValues>();

            foreach (var key in indicesMap.Keys)
            {
                var dataChunk = CreateChannelDataValuesList(channelData[key], uidLog, key, indicesMap[key]);
                if (dataChunk != null && dataChunk.Count > 0)
                    dataChunks.AddRange(dataChunk);
            }

            collection.BulkWrite(dataChunks
                .Select(dc =>
                {
                    return new InsertOneModel<ChannelDataValues>(dc);
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
            var chunks = ToChunks(indexChannel, GetSequence(string.Empty, !indexChannel.IsTimeIndex, data));

            if (chunks != null && chunks.Any())
            {
                var collection = GetCollection();

                collection.BulkWrite(chunks
                    .Select(dc =>
                    {
                        dc.UidLog = uidLog;
                        dc.Uid = NewUid();
                        dc.MnemonicList = mnemonicList;
                        dc.UnitList = unitList;
                        return new InsertOneModel<ChannelDataValues>(dc);
                    }));
            }
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

            // Query channelDataValues collection
            var results = GetData(filter, increasing);
            if (results == null || results.Count == 0)
                return null;

            // Convert data
            var logData = new LogData();
            TransformLogData(logData, results, mnemonics, range, increasing);
            return logData;
        }

        /// <summary>
        /// NOTE: This method is currently only used for testing 2.0 Log Data but may be of value for querying 2.0 Log Data later.
        /// 
        /// Gets the ChannelDataValues that fall within a given range.
        /// </summary>
        /// <param name="uidLog">The uid log.</param>
        /// <param name="mnemonics">The mnemonics.</param>
        /// <param name="range">The range.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <returns>A List of ChannelDataValues that fall within a given range for a specific Log uid.</returns>
        public List<ChannelDataValues> GetData(string uidLog, List<string> mnemonics, Tuple<double?, double?> range, bool increasing)
        {
            var indexCurve = mnemonics[0].Trim();

            // Build Log Data filter
            var filter = BuildDataFilter(uidLog, indexCurve, range, increasing);

            // Query channelDataValues collection
            return GetData(filter, increasing);
            
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
        /// Gets the log data from channelDataValues collection.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <returns>The list of log data chunks that fit the query criteria sorted by the start index.</returns>
        private List<ChannelDataValues> GetData(FilterDefinition<ChannelDataValues> filter, bool increasing)
        {          
            var collection = GetCollection();
            var sortBuilder = Builders<ChannelDataValues>.Sort;
            var sortField = "Indices.0.Start";

            var sort = increasing 
                ? sortBuilder.Ascending(sortField) 
                : sortBuilder.Descending(sortField);

            if (Logger.IsDebugEnabled)
            {
                var filterJson = filter.Render(collection.DocumentSerializer, collection.Settings.SerializerRegistry);
                Logger.DebugFormat("Data query filters: {0}", filterJson);
            }

            return collection
                .Find(filter ?? "{}")
                .Sort(sort)
                .ToList();
        }

        /// <summary>
        /// Builds the data filter for the database query.
        /// </summary>
        /// <param name="uidLog">The uid of the log.</param>
        /// <param name="indexCurve">The index curve mnemonic.</param>
        /// <param name="range">The request range.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <returns>The query filter.</returns>
        private FilterDefinition<ChannelDataValues> BuildDataFilter(string uidLog, string indexCurve, Tuple<double?, double?> range, bool increasing)
        {
            var filters = new List<FilterDefinition<ChannelDataValues>>();
            filters.Add(Builders<ChannelDataValues>.Filter.EqIgnoreCase("UidLog", uidLog));

            var rangeFilters = new List<FilterDefinition<ChannelDataValues>>();
            if (range != null)
            {
                if (range.Item1.HasValue)
                {
                    var start = increasing ?
                        Builders<ChannelDataValues>.Filter.Gte("Indices.End", range.Item1.Value) :
                        Builders<ChannelDataValues>.Filter.Lte("Indices.End", range.Item1.Value);
                    rangeFilters.Add(start);
                }
                if (range.Item2.HasValue)
                {
                    var start = increasing ?
                        Builders<ChannelDataValues>.Filter.Lte("Indices.Start", range.Item2.Value) :
                        Builders<ChannelDataValues>.Filter.Gte("Indices.Start", range.Item2.Value);
                    rangeFilters.Add(start);
                }
            }
            
            if (rangeFilters.Count > 0)
                rangeFilters.Add(Builders<ChannelDataValues>.Filter.EqIgnoreCase("Indices.Mnemonic", indexCurve));

            if (rangeFilters.Count > 0)
                filters.Add(Builders<ChannelDataValues>.Filter.And(rangeFilters));

            return Builders<ChannelDataValues>.Filter.And(filters);
        }

        /// <summary>
        /// Transforms the log data chunks as 1.4 log data.
        /// </summary>
        /// <param name="logData">The <see cref="LogData"/> object.</param>
        /// <param name="results">The list of log data chunks.</param>
        /// <param name="mnemonics">The subset of mnemonics for the requested log curves.</param>
        private void TransformLogData(LogData logData, List<ChannelDataValues> results, List<string> mnemonics, Tuple<double?, double?> rowRange, bool increasing)
        {
            var dataList = new List<List<string>>();
            var rangeList = new List<List<string>>();
            var requestIndex = new List<int>();
            List<string> dataMnemonics = null;
            List<string> units = null;

            logData.Data = new List<string>();

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

            logData.MnemonicList = string.Join(Delimiter, validMnemonics);
            logData.UnitList = string.Join(Delimiter, validUnits);

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
        private IEnumerable<ChannelDataValues> ToChunks(ChannelIndexInfo indexChannel, IEnumerable<Tuple<string, double, string>> sequence)
        {
            ChannelIndexRange? plannedRange = null;
            double startIndex = 0;
            double endIndex = 0;
            var data = new List<string>();
            string uid = string.Empty;
            var increasing = indexChannel.Increasing;

            foreach (var item in sequence)
            {
                if (!plannedRange.HasValue)
                {
                    plannedRange = ComputeRange(item.Item2, RangeSize, increasing);
                    uid = item.Item1;
                    startIndex = item.Item2;
                }

                if (WithinRange(item.Item2, plannedRange.Value.End, increasing, false))
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

                    yield return new ChannelDataValues()
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

                yield return new ChannelDataValues()
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
                    var slicedData = string.Join(Delimiter, SliceStringList(allValues, sliceIndexes));
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
        private ChannelIndexRange ComputeRange(double index, int rangeSize, bool increasing = true)
        {
            var rangeIndex = increasing ? (int)(Math.Floor(index / rangeSize)) : (int)(Math.Ceiling(index / rangeSize));
            return new ChannelIndexRange(rangeIndex * rangeSize, rangeIndex * rangeSize + (increasing ? rangeSize : -rangeSize));
        }

        /// <summary>
        /// Transform the original channel set data string to list of data chunks (For WITSML 2.0 log).
        /// </summary>
        /// <param name="data">The original channel set data.</param>
        /// <param name="uidLog">The uid of the log.</param>
        /// <param name="uidChannelSet">The uid of the channel set.</param>
        /// <param name="indices">The list of index for the channel set.</param>
        /// <returns>The list of data chunks.</returns>
        private List<ChannelDataValues> CreateChannelDataValuesList(string data, string uidLog, string uidChannelSet, List<ChannelIndexInfo> indices)
        {
            var dataChunks = new List<ChannelDataValues>();
            var logData = DeserializeChannelSetData(data);
            List<List<List<object>>> chunk = null;

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
                start = Convert.ToDouble(logData.First().First().First());
                end = Convert.ToDouble(logData.Last().First().First());
            }

            var increasing = indices.First().Increasing;
            var rangeSizeAdjustment = increasing ? RangeSize : -RangeSize;
            ChannelIndexRange rangeSize = ComputeRange(start, RangeSize, increasing);

            do
            {
                // Create our chunk
                chunk = CreateChunk(logData, isTimeIndex, increasing, rangeSize);

                // Save chunk rows if there are any
                if (chunk.Any())
                {
                    var clonedIndices = new List<ChannelIndexInfo>();
                    indices.ForEach( i => clonedIndices.Add(i.Clone()));

                    SetChunkIndices(chunk.First().First(), chunk.Last().First(), clonedIndices);

                var channelDataValues = new ChannelDataValues
                {
                    Uid = NewUid(),
                    UidLog = uidLog,
                    UidChannelSet = uidChannelSet,
                        Indices = clonedIndices,
                    Data = SerializeChannelSetData(chunk)
                };

                dataChunks.Add(channelDataValues);

                    // Compute the next range
                    rangeSize = new ChannelIndexRange(rangeSize.Start + rangeSizeAdjustment, rangeSize.End + rangeSizeAdjustment);
                }
            }
            // Keep looking until we are creating empty chunks
            while (chunk != null && chunk.Any());

            return dataChunks;
        }

        /// <summary>
        /// Creates the data chunk for a 2.0 Log.
        /// </summary>
        /// <param name="logData">The log data.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> [is time index].</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <param name="rangeSize">Size of the range.</param>
        /// <returns>The data rows for the chunk that are within the given rangeSize</returns>
        private List<List<List<object>>> CreateChunk(List<List<List<object>>> logData, bool isTimeIndex, bool increasing, ChannelIndexRange rangeSize)
        {
            var chunk =
                increasing
                    ? (isTimeIndex
                        ? logData.Where(d => ParseTime(d.First().First().ToString()) >= rangeSize.Start && ParseTime(d.First().First().ToString()) < rangeSize.End).ToList()
                        : logData.Where(d => Convert.ToDouble(d.First().First()) >= rangeSize.Start && Convert.ToDouble(d.First().First()) < rangeSize.End).ToList())

                    // Decreasing
                    : (isTimeIndex
                        ? logData.Where(d => ParseTime(d.First().First().ToString()) <= rangeSize.Start && ParseTime(d.First().First().ToString()) > rangeSize.End).ToList()
                        : logData.Where(d => Convert.ToDouble(d.First().First()) <= rangeSize.Start && Convert.ToDouble(d.First().First()) > rangeSize.End).ToList());
            return chunk;
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

        /// <summary>
        /// Updates the 1.4 WITSML Log data.
        /// </summary>
        /// <param name="entity">The original Log object.</param>
        /// <param name="logDatas">The log data for the updates.</param>
        /// <param name="isTimeLog">if set to <c>true</c> [the log index type is time].</param>
        /// <param name="increasing">if set to <c>true</c> [the log index is increasing].</param>
        public void UpdateLogData(Energistics.DataAccess.WITSML141.Log entity, List<LogData> logDatas, bool isTimeLog, bool increasing)
        {
            var uidLog = entity.Uid;
            var inserts = new List<ChannelDataValues>();
            var updates = new Dictionary<string, ChannelDataValues>();
            var ranges = new List<double>();
            var valueUpdates = new List<List<string>>();

            var collection = GetCollection();
            var mongoDbUpdate = new MongoDbUpdate<ChannelDataValues>(collection, null, null, null);

            // Looping through each logData elements, since multiple logData nodes are allowed during update
            foreach (var logData in logDatas)
            {
                var updateChunks = new List<List<List<string>>>();
                var mnemonics = logData.MnemonicList.Split(Separator).ToList();
                var units = logData.UnitList.Split(Separator).ToList();
                var indexCurve = mnemonics[0].Trim();
                var dataUpdate = new List<List<string>>();
                var effectiveRanges = new Dictionary<string, List<double>>();

                // Split all comma delimited data into lists and divide the update list into chunks and compute the effective range for each channel in the updates
                SetUpdateChunks(logData.Data, mnemonics, updateChunks, effectiveRanges, isTimeLog, increasing);
                var indexRanges = effectiveRanges[indexCurve];

                var rangeStart = ComputeRange(indexRanges.First(), RangeSize, increasing);
                var rangeEnd = ComputeRange(indexRanges.Last(), RangeSize, increasing);
                var updateRange = new Tuple<double?, double?>(rangeStart.Start, rangeEnd.End);

                // Find the current log data chunks enclosed by the update range
                var filter = BuildDataFilter(uidLog, indexCurve, updateRange, increasing);
                var results = GetData(filter, increasing);
                var count = 0;

                var updatedChunkIds = new List<string>();

                // Looping through update chunks
                foreach (var updateChunk in updateChunks)
                {
                    var start = GetAnIndexValue(updateChunk.First().First(), isTimeLog);

                    // Looking for original chunk for the same range
                    var matchingChunk = FindChunkByRange(results, start, increasing, ref count);
                    if (matchingChunk == null)
                    {
                        // if not found: insert new chunk
                        inserts.Add(CreateChunk(uidLog, updateChunk, mnemonics, units, increasing, isTimeLog));
                    }
                    else
                    {
                        // if found: merge existing chunk with update chunk
                        UpdateChunkValues(matchingChunk, updateChunk, mnemonics, units, effectiveRanges, isTimeLog, increasing);
                        var chunkUid = matchingChunk.Uid;
                        updates.Add(chunkUid, matchingChunk);
                        updatedChunkIds.Add(chunkUid);
                    }
                }

                // Looping through original chunks that has no overlapping chunk from the updates, yet are within the update range, e.g. in the middle,
                // and update the data for the channels are in the update, i.e. overwrite it to blank since it is covered by updates
                foreach (var unmatchedChunk in results.Where(r => !updatedChunkIds.Contains(r.Uid)))
                {
                    UpdateChunkValues(unmatchedChunk, null, mnemonics, units, effectiveRanges, isTimeLog, increasing);
                    updates.Add(unmatchedChunk.Uid, unmatchedChunk);
                }

                // insert
                if (inserts.Count > 0)
                {
                    collection.BulkWrite(inserts
                        .Select(i =>
                        {
                            return new InsertOneModel<ChannelDataValues>(i);
                        }));
                }

                // update
                if (updates.Count > 0)
                {
                    mongoDbUpdate.Update(updates);
                }
            }
        }

        /// <summary>
        /// Finds the original chunk for an update chunk by its index range.
        /// </summary>
        /// <param name="results">The original collection of chunks withing the update range.</param>
        /// <param name="start">The start index of the update chunk.</param>
        /// <param name="increasing">if set to <c>true</c> [the log is increasing].</param>
        /// <param name="count">The starting index for search the original collection since both collection are ordered.</param>
        /// <returns>The original chunk if found; null if not.</returns>
        private ChannelDataValues FindChunkByRange(List<ChannelDataValues> results, double start, bool increasing, ref int count)
        {
            if (results == null || results.Count == 0)
                return null;

            for (var i = count; i < results.Count; i++)
            {
                var result = results[i];
                var index = result.Indices.FirstOrDefault();

                if (index == null)
                    return null;

                var range = ComputeRange(index.Start, RangeSize, increasing);
                if (Before(start, range.End, increasing) && !Before(start, range.Start, increasing))
                {
                    count = i++;
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a new chunk to insert.
        /// </summary>
        /// <param name="uidLog">The uid of the log.</param>
        /// <param name="updates">The collection of update log data.</param>
        /// <param name="mnemonics">The mnemonics of the channels to update.</param>
        /// <param name="units">The units of the channels to update.</param>
        /// <param name="increasing">if set to <c>true</c> [the log is increasing].</param>
        /// <param name="isTimeLog">if set to <c>true</c> [the log index type is time].</param>
        /// <returns>The created chunk</returns>
        private ChannelDataValues CreateChunk(string uidLog, List<List<string>> updates, List<string> mnemonics, List<string> units, bool increasing, bool isTimeLog)
        {
            var start = GetAnIndexValue(updates.First().First(), isTimeLog);
            var end = GetAnIndexValue(updates.Last().First(), isTimeLog);

            var chunk = new ChannelDataValues { Uid = NewUid(), UidLog = uidLog, MnemonicList = string.Join(",", mnemonics), UnitList = string.Join(",", units) };
            var index = new ChannelIndexInfo { Mnemonic = mnemonics[0], Start = start, End = end, Increasing = increasing, IsTimeIndex = isTimeLog };

            chunk.Indices = new List<ChannelIndexInfo> { index };
            chunk.Data = SerializeLogData(updates.Select(r => string.Join(Delimiter, r)).ToList());

            return chunk;
        }

        /// <summary>
        /// Updates the chunk values; merge the original chunk with the updates.
        /// </summary>
        /// <param name="chunk">The original chunk.</param>
        /// <param name="updates">The collection of update log data.</param>
        /// <param name="mnemonics">The mnemonics of the channels to update.</param>
        /// <param name="units">The units of the channels to update.</param>
        /// <param name="effectiveRanges">The effective ranges of the channels to update.</param>
        /// <param name="isTimeLog">if set to <c>true</c> [the log index type is time].</param>
        /// <param name="increasing">if set to <c>true</c> [the log is increasing].</param>
        private void UpdateChunkValues(ChannelDataValues chunk, List<List<string>> updates, List<string> mnemonics, List<string> units, Dictionary<string, List<double>> effectiveRanges, bool isTimeLog, bool increasing)
        {
            var chunkMnemonics = chunk.MnemonicList.Split(Separator).ToList();
            var chunkUnits = chunk.UnitList.Split(Separator).ToList();
            var chunkData = DeserializeLogData(chunk.Data);
            var chunkIndex = chunk.Indices.FirstOrDefault();
            var chunkRange = ComputeRange(chunkIndex.Start, RangeSize, increasing);
            var mnemonicIndexMap = new Dictionary<string, int>();
            var merges = new List<string>();

            for (var i = 0; i < chunkMnemonics.Count; i++)
            {
                mnemonicIndexMap.Add(chunkMnemonics[i], i);
            }

            for (var i = 0; i < mnemonics.Count; i++)
            {
                var mnemonic = mnemonics[i];
                if (!chunkMnemonics.Contains(mnemonic))
                {
                    var effectiveRange = effectiveRanges[mnemonic];
                    if (Before(effectiveRange.First(), chunkRange.End, increasing) && Before(chunkRange.Start, effectiveRange.Last(), increasing))
                    {
                        chunkMnemonics.Add(mnemonic);
                        chunkUnits.Add(units[i]);
                        mnemonicIndexMap.Add(mnemonic, chunkMnemonics.Count);
                    }
                }
            }

            var mergeRange = new List<double>();
            double current, next;
            List<string> update;
            List<string> points;
            var counter = 0;

            if (updates != null)
            {
                for (var i = 0; i < updates.Count; i++)
                {
                    for (var j = counter; j < chunkData.Count; j++)
                    {                       
                        points = chunkData[j].Split(Separator).ToList();
                        next = GetAnIndexValue(points.First(), isTimeLog);
                        if (i >= updates.Count)
                        {
                            MergeOneDataRow(merges, points, null, chunkMnemonics, mnemonics, mnemonicIndexMap, effectiveRanges, next, increasing);
                            continue;
                        }

                        update = updates[i];
                        current = GetAnIndexValue(update.First(), isTimeLog);

                        while (Before(current, next, increasing))
                        {
                            MergeOneDataRow(merges, null, update, chunkMnemonics, mnemonics, mnemonicIndexMap, effectiveRanges, current, increasing);
                            i++;

                            if (i >= updates.Count)
                                break;

                            update = updates[i];
                            current = GetAnIndexValue(update.First(), isTimeLog);
                        }
                        if (current == next)
                        {
                            MergeOneDataRow(merges, points, update, chunkMnemonics, mnemonics, mnemonicIndexMap, effectiveRanges, current, increasing);
                            i++;
                            j++;
                            counter = j;
                            continue;
                        }
                        while (Before(next, current, increasing))
                        {
                            MergeOneDataRow(merges, points, null, chunkMnemonics, mnemonics, mnemonicIndexMap, effectiveRanges, next, increasing);
                            j++;
                            counter = j;

                            if (j >= chunkData.Count)
                                break;

                            points = chunkData[j].Split(Separator).ToList();
                            next = GetAnIndexValue(points.First(), isTimeLog);
                        }
                    }
                    if (i >= updates.Count)
                        break;

                    update = updates[i];
                    current = GetAnIndexValue(update.First(), isTimeLog);
                    MergeOneDataRow(merges, null, update, chunkMnemonics, mnemonics, mnemonicIndexMap, effectiveRanges, current, increasing);
                }

                var indexInfo = chunk.Indices.First();
                var firstRow = merges.First().Split(Separator);
                var lastRow = merges.Last().Split(Separator);

                indexInfo.Start = GetAnIndexValue(firstRow[0], isTimeLog);
                indexInfo.End = GetAnIndexValue(lastRow[0], isTimeLog);

                chunk.MnemonicList = string.Join(Delimiter, chunkMnemonics);
                chunk.UnitList = string.Join(Delimiter, chunkUnits);
            }
            else
            {
                for (var j = 0; j < chunkData.Count; j++)
                {
                    points = chunkData[j].Split(Separator).ToList();
                    next = GetAnIndexValue(points.First(), isTimeLog);
                    MergeOneDataRow(merges, points, null, chunkMnemonics, mnemonics, mnemonicIndexMap, effectiveRanges, next, increasing);
                }
            }

            chunk.Data = SerializeLogData(merges);          
        }

        /// <summary>
        /// Merges the one data row.
        /// </summary>
        /// <param name="merges">The collection to hold the merged row.</param>
        /// <param name="points">The original points for the row.</param>
        /// <param name="updates">The updates for the row.</param>
        /// <param name="pointMnemonics">The mnemonics for the channels in the original row.</param>
        /// <param name="updateMnemonics">The mnemonics for the channel in the update row.</param>
        /// <param name="indexMap">The mnemonic index for the merged row.</param>
        /// <param name="effectiveRanges">The effective ranges for the channels in the update.</param>
        /// <param name="indexValue">The index value for the row.</param>
        /// <param name="increasing">if set to <c>true</c> [the log is increasing].</param>
        private void MergeOneDataRow(List<string> merges, List<string> points, List<string> updates, List<string> pointMnemonics, List<string> updateMnemonics, Dictionary<string, int> indexMap, Dictionary<string, List<double>> effectiveRanges, double indexValue, bool increasing)
        {
            var mnemonicsCount = indexMap.Keys.Count;
            var merge = new List<string>();

            for (var i = 0; i < mnemonicsCount; i++)
            {
                merge.Add(string.Empty);
            }

            if (points == null)
            {
                for (var i = 0; i < updateMnemonics.Count; i++)
                {
                    var mnemonic = updateMnemonics[i];
                    merge[indexMap[mnemonic]] = updates[i];
                }
            }
            else if (updates == null)
            {
                for (var i = 0; i < pointMnemonics.Count; i++)
                {
                    var mnemonic = pointMnemonics[i];
                    if (updateMnemonics.Contains(mnemonic))
                    {
                        if (effectiveRanges.ContainsKey(mnemonic))
                        {
                            var effectiveRange = effectiveRanges[mnemonic];
                            if (Before(indexValue, effectiveRange.First(), increasing) || Before(effectiveRange.Last(), indexValue, increasing))
                            {
                                merge[i] = points[i];
                            }
                            else
                            {
                                merge[i] = string.Empty;
                            }
                        }
                    }
                    else
                    {
                        merge[i] = points[i];
                    }
                }
            }
            else
            {
                foreach (var mnemonic in indexMap.Keys)
                {
                    var mergeIndex = indexMap[mnemonic];
                    var updateIndex = updates.IndexOf(mnemonic);
                    merge[mergeIndex] = updateIndex > -1 ? updates[updateIndex] : points[mergeIndex];
                }
            }

            for (var i = 1; i < merge.Count; i++)
            {
                if (!string.IsNullOrEmpty(merge[i]))
                {
                    merges.Add(string.Join(Delimiter, merge));
                    return;
                }
            }
        }

        /// <summary>
        /// Check if the start index is before the end.
        /// </summary>
        /// <param name="start">The start index.</param>
        /// <param name="end">The end index.</param>
        /// <param name="increasing">if set to <c>true</c> [the log is increasing].</param>
        /// <returns>True if before; false if not.</returns>
        private bool Before(double start, double end, bool increasing)
        {
            return increasing ? start < end : start > end;
        }

        /// <summary>
        /// Convert a string value to a double index value.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="isTimeLog">if set to <c>true</c> [the log index type is time].</param>
        /// <returns>The index value.</returns>
        private double GetAnIndexValue(string input, bool isTimeLog)
        {
            if (isTimeLog)
                return ParseTime(input);
            else
                return double.Parse(input);
        }

        /// <summary>
        /// Sets the update chunks.
        /// </summary>
        /// <param name="logData">The update log data.</param>
        /// <param name="mnemonics">The mnemonics for the channels in the update.</param>
        /// <param name="chunks">The collection to hold the update chunks.</param>
        /// <param name="ranges">The collection to compute the effective range for each channel in the update.</param>
        /// <param name="isTimeLog">if set to <c>true</c> [the log index type is time].</param>
        /// <param name="increasing">if set to <c>true</c> [the log index is increasing].</param>
        private void SetUpdateChunks(List<string> logData, List<string> mnemonics, List<List<List<string>>> chunks, Dictionary<string, List<double>> ranges, bool isTimeLog, bool increasing)
        {
            var firstRow = logData[0];
            var points = firstRow.Split(Separator).ToList();
            var indexValue = GetAnIndexValue(points.First(), isTimeLog);
            var stop = ComputeRange(indexValue, RangeSize, increasing).End;
            var chunk = new List<List<string>>();

            foreach (var row in logData)
            {
                points = row.Split(Separator).ToList();
                indexValue = GetAnIndexValue(points.First(), isTimeLog);

                for (var i = 0; i < points.Count; i++)
                {
                    var mnemonic = mnemonics[i];
                    if (!string.IsNullOrEmpty(points[i]))
                    {
                        if (ranges.ContainsKey(mnemonic))
                            ranges[mnemonic][1] = indexValue;                      
                        else
                            ranges.Add(mnemonic, new List<double> { indexValue, indexValue });
                    }
                }

                if (!Before(indexValue, stop, increasing))
                {
                    chunks.Add(chunk);
                    chunk = new List<List<string>>();
                    stop = ComputeRange(indexValue, RangeSize, increasing).End;
                }

                chunk.Add(points);
            }

            if (chunk.Count > 0)
                chunks.Add(chunk);
        }
    }
}
