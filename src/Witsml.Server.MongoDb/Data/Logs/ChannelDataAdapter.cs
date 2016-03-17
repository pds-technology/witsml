using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using MongoDB.Driver;
using Newtonsoft.Json;
using PDS.Witsml.Server.Models;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="ChannelSetValues"/>
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{PDS.Witsml.Server.Models.ChannelSetValues}" />
    public class ChannelDataAdapter : MongoDbDataAdapter<ChannelSetValues>
    {
        private static readonly int RangeSize = Settings.Default.LogIndexRangeSize;

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
                    return (WriteModel<ChannelSetValues>)new InsertOneModel<ChannelSetValues>(dc);
                }));
        }

        /// <summary>
        /// Writes the log data values for WITSML 1.4 log.
        /// </summary>
        /// <param name="uidLog">The uid of the log.</param>
        /// <param name="data">The log data.</param>
        /// <param name="mnemonicList">The mnemonic list for the log data.</param>
        /// <param name="indexChannel">The index channel.</param>
        public void WriteLogDataValues(string uidLog, List<string> data, string mnemonicList, ChannelIndexInfo indexChannel)
        {
            var collection = GetCollection<ChannelSetValues>(DbCollectionName);
            collection.BulkWrite(ToChunks(indexChannel, GetSequence(string.Empty, !indexChannel.IsTimeIndex, data))
                    .Select(dc =>
                    {
                        dc.UidLog = uidLog;
                        dc.Uid = NewUid();
                        return (WriteModel<ChannelSetValues>)new InsertOneModel<ChannelSetValues>(dc);
                    }));
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

            foreach (var item in sequence)
            {
                if (plannedRange == null)
                {
                    plannedRange = ComputeRange(item.Item2, RangeSize, indexChannel.Increasing);
                    uid = item.Item1;
                    startIndex = item.Item2;
                }

                if (indexChannel.Increasing)
                {
                    // If we're within the plannedRange append to the data and update the endIndex
                    if (item.Item2 < plannedRange.Item2)
                    {
                        // While still appending data for the current chunk set the uid if it is currently blank
                        uid = string.IsNullOrEmpty(uid) ? item.Item1 : uid;

                        data.Add(item.Item3);
                        endIndex = item.Item2;
                    }
                    else
                    {
                        indexChannel.Start = startIndex;
                        indexChannel.End = endIndex;
                        yield return new ChannelSetValues() { Data = SerializeLogData(data), Indices = new List<ChannelIndexInfo> { indexChannel } };
                        plannedRange = ComputeRange(item.Item2, RangeSize);
                        data = new List<string>();
                        data.Add(item.Item3);
                        startIndex = item.Item2;
                        endIndex = item.Item2;
                        uid = item.Item1;
                    }
                }
                else
                {
                    if (item.Item2 > plannedRange.Item2)
                    {
                        // While still appending data for the current chunk set the uid if it is currently blank
                        uid = string.IsNullOrEmpty(uid) ? item.Item1 : uid;

                        data.Add(item.Item3);
                        endIndex = item.Item2;
                    }
                    else
                    {
                        indexChannel.Start = startIndex;
                        indexChannel.End = endIndex;
                        yield return new ChannelSetValues() { Data = SerializeLogData(data), Indices = new List<ChannelIndexInfo> { indexChannel } };
                        plannedRange = ComputeRange(item.Item2, RangeSize, indexChannel.Increasing);
                        data = new List<string>();
                        data.Add(item.Item3);
                        startIndex = item.Item2;
                        endIndex = item.Item2;
                        uid = item.Item1;
                    }
                }
            }

            if (data.Count > 0)
            {
                indexChannel.Start = startIndex;
                indexChannel.End = endIndex;
                yield return new ChannelSetValues() { Data = SerializeLogData(data), Indices = new List<ChannelIndexInfo> { indexChannel } };
            }
        }

        private IEnumerable<Tuple<string, double, string>> GetSequence(string uid, bool isDepthLog, IEnumerable<string> dataList, int[] sliceIndexes = null)
        {
            foreach (var dataRow in dataList)
            {
                // TODO: Validate the index order (is the data sorted?)
                var allValues = dataRow.Split(',');
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
                    var slicedData = string.Join(",", SliceStringList(allValues, sliceIndexes));
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
        /// Deserializes the channel set data from 2.0 log.
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

        /// <summary>
        /// Computes the range.
        /// </summary>
        /// <param name="index">The start index.</param>
        /// <param name="rangeSize">Size of the range.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <returns>The range.</returns>
        private Tuple<int, int> ComputeRange(double index, int rangeSize, bool increasing = true)
        {
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

            if (isTimeIndex)
            {
                start = DateTimeOffset.Parse(logData.First().First().First().ToString()).ToUnixTimeSeconds();
                end = DateTimeOffset.Parse(logData.Last().First().First().ToString()).ToUnixTimeSeconds();
            }
            else
            {
                start = (double)logData.First().First().First();
                end = (double)logData.Last().First().First();
            }

            var increasing = indices.First().Increasing;

            var rangeSize = ComputeRange(start, RangeSize, increasing);

            if (increasing)
            {
                do
                {
                    var chunk = isTimeIndex ? logData.Where(d => DateTimeOffset.Parse(d.First().First().ToString()).ToUnixTimeSeconds() >= rangeSize.Item1
                        && DateTimeOffset.Parse(d.First().First().ToString()).ToUnixTimeSeconds() < rangeSize.Item2).ToList() :
                        logData.Where(d => (double)d.First().First() >= rangeSize.Item1 && (double)d.First().First() < rangeSize.Item2).ToList();

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

                    rangeSize = new Tuple<int, int>(rangeSize.Item1 + RangeSize, rangeSize.Item2 + RangeSize);
                }
                while (rangeSize.Item2 <= end);
            }
            else
            {
                do
                {
                    var chunk = isTimeIndex ? logData.Where(d => DateTimeOffset.Parse(d.First().First().ToString()).ToUnixTimeSeconds() <= rangeSize.Item1
                         && DateTimeOffset.Parse(d.First().First().ToString()).ToUnixTimeSeconds() > rangeSize.Item2).ToList() :
                        logData.Where(d => (double)d.First().First() <= rangeSize.Item1 && (double)d.First().First() > rangeSize.Item2).ToList();

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

                    rangeSize = new Tuple<int, int>(rangeSize.Item1 - RangeSize, rangeSize.Item2 - RangeSize);
                }
                while (rangeSize.Item2 >= end);
            }

            return dataChunks;
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
                    var startTime = DateTimeOffset.Parse(starts[i].ToString());
                    var endTime = DateTimeOffset.Parse(ends[i].ToString());
                    index.Start = startTime.ToUnixTimeSeconds();
                    index.End = endTime.ToUnixTimeSeconds();
                }
                else
                {
                    index.Start = double.Parse(starts[i].ToString());
                    index.End = double.Parse(ends[i].ToString());
                }
            }
        }
    }
}
