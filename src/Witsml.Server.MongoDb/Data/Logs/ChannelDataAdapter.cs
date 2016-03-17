using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PDS.Witsml.Server.Models;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Data.Logs
{
    public class ChannelDataAdapter
    {
        private static readonly int RangeSize = Settings.Default.LogIndexRangeSize;

        public List<ChannelSetValues> CreateChannelSetValuesList(string data, string uidLog, string uidChannelSet, List<ChannelIndexInfo> indices)
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

        public List<ChannelSetValues> ParseLogData(string data, string uidLog)
        {
            var logData = DeserializeLogData(data);
            return null;
        }

        public List<List<List<object>>> DeserializeChannelSetData(string data)
        {
            return JsonConvert.DeserializeObject<List<List<List<object>>>>(data);
        }

        public string SerializeChannelSetData(List<List<List<object>>> data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public List<List<object>> DeserializeLogData(string data)
        {
            return JsonConvert.DeserializeObject<List<List<object>>>(data);
        }

        public string SerializeLogData(List<List<object>> data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public Tuple<int, int> ComputeRange(double index, int rangeSize, bool increasing = true)
        {
            var rangeIndex = increasing ? (int)(Math.Floor(index / rangeSize)) : (int)(Math.Ceiling(index / rangeSize));
            return new Tuple<int, int>(rangeIndex * rangeSize, rangeIndex * rangeSize + (increasing ? rangeSize : -rangeSize));
        }

        private string NewUid(string uid = null)
        {
            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString();
            }

            return uid;
        }

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
                    index.Start = (double)starts[i];
                    index.End = (double)ends[i];
                }
            }
        }
    }
}
