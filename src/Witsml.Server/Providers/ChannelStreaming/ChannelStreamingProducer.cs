//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
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
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol;
using Energistics.Protocol.ChannelStreaming;
using Newtonsoft.Json.Linq;
using PDS.Framework;
using PDS.Witsml.Server.Data.Channels;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Providers.ChannelStreaming
{

    /// <summary>
    /// Producer class for channel streaming
    /// </summary>
    /// <seealso cref="Energistics.Protocol.ChannelStreaming.ChannelStreamingProducerHandler" />
    [Export(typeof(IChannelStreamingProducer))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ChannelStreamingProducer : ChannelStreamingProducerHandler
    {
        private static readonly string[] ParentTypes = new[] { ObjectTypes.Log, ObjectTypes.ChannelSet };
        private static readonly string[] ChannelTypes = new[] { ObjectTypes.LogCurveInfo, ObjectTypes.Channel };
        private static readonly string[] SupportedTypes = ParentTypes.Concat(ChannelTypes).ToArray();

        private readonly IContainer _container;
        private CancellationTokenSource _tokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelStreamingProducer"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        [ImportingConstructor]
        public ChannelStreamingProducer(IContainer container)
        {
            _container = container;
            Uris = new List<EtpUri>();
            Channels = new Dictionary<EtpUri, List<ChannelMetadataRecord>>();
        }

        /// <summary>
        /// Gets the request.
        /// </summary>
        /// <value>The request message header.</value>
        public MessageHeader Request { get; private set; }

        /// <summary>
        /// Gets the uris.
        /// </summary>
        public List<EtpUri> Uris { get; }

        /// <summary>
        /// Gets the channels.
        /// </summary>
        public Dictionary<EtpUri, List<ChannelMetadataRecord>> Channels { get; }

        /// <summary>
        /// Handles the channel describe.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{T}"/> instance containing the event data.</param>
        protected override void HandleChannelDescribe(ProtocolEventArgs<ChannelDescribe, IList<ChannelMetadataRecord>> args)
        {
            Channels.Clear();

            foreach (var uri in args.Message.Uris.Select(x => new EtpUri(x)))
            {
                if (!SupportedTypes.ContainsIgnoreCase(uri.ObjectType))
                    continue;

                Uris.Add(uri);
                Channels[uri] = new List<ChannelMetadataRecord>();

                var isChannel = ChannelTypes.ContainsIgnoreCase(uri.ObjectType);
                var parentUri = isChannel ? uri.Parent : uri;

                var dataProvider = GetDataProvider(parentUri);
                var metadata = dataProvider.GetChannelMetadata(parentUri)
                    .Where(x => !isChannel || x.ChannelUri == uri)
                    .ToList();

                metadata.ForEach(args.Context.Add);
                Channels[uri].AddRange(metadata);
            }
        }

        /// <summary>
        /// Handles the channel streaming start.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="channelStreamingStart">The channel streaming start.</param>
        protected override void HandleChannelStreamingStart(MessageHeader header, ChannelStreamingStart channelStreamingStart)
        {
            // no action needed if streaming already started
            if (_tokenSource != null)
                return;

            base.HandleChannelStreamingStart(header, channelStreamingStart);

            Request = null;
            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            Task.Run(async () =>
            {
                using (_tokenSource)
                {
                    try
                    {
                        Logger.Debug("Channel Streaming starting.");
                        await StartChannelStreaming(channelStreamingStart.Channels, token);
                        Logger.Debug("Channel Streaming stopped.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                    finally
                    {
                        _tokenSource = null;
                    }
                }
            },
            token);
        }

        /// <summary>
        /// Handles the channel streaming stop.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="channelStreamingStop">The channel streaming stop.</param>
        protected override void HandleChannelStreamingStop(MessageHeader header, ChannelStreamingStop channelStreamingStop)
        {
            // no action needed if streaming not in progress
            if (_tokenSource == null) return;

            base.HandleChannelStreamingStop(header, channelStreamingStop);

            _tokenSource?.Cancel();
        }

        /// <summary>
        /// Handles the channel range request.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="channelRangeRequest">The channel range request.</param>
        protected override void HandleChannelRangeRequest(MessageHeader header, ChannelRangeRequest channelRangeRequest)
        {
            // no action needed if streaming already started
            if (_tokenSource != null)
                return;

            base.HandleChannelRangeRequest(header, channelRangeRequest);

            Request = header;
            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            Task.Run(async () =>
            {
                using (_tokenSource)
                {
                    try
                    {
                        Logger.Debug("Channel Streaming starting.");
                        await StartChannelRangeRequest(channelRangeRequest.ChannelRanges, token);
                        Logger.Debug("Channel Streaming stopped.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                    finally
                    {
                        _tokenSource = null;
                    }
                }
            },
            token);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed and unmanaged resources;
        ///     <c>false</c> to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _tokenSource != null)
            {
                _tokenSource.Cancel();
                _tokenSource.Dispose();
                _tokenSource = null;
            }

            base.Dispose(disposing);
        }

        private async Task StartChannelRangeRequest(IList<ChannelRangeInfo> channelRangeInfos, CancellationToken token)
        {
            foreach (var channelRangeInfo in channelRangeInfos)
            {
                if (token.IsCancellationRequested)
                    break;

                foreach (var uri in Channels.Keys)
                {
                    if (token.IsCancellationRequested)
                        break;

                    await StreamChannelDataRange(channelRangeInfo, uri, token);
                }
            }

            await Task.Delay(1000, token);
        }

        private async Task<bool> StreamChannelDataRange(ChannelRangeInfo channelRangeInfo, EtpUri uri, CancellationToken token)
        {
            // Select the channels using the channel ids from channelRangeInfo
            var channels = Channels[uri]
                .Where(x => channelRangeInfo.ChannelId.Contains(x.ChannelId))
                .ToList();

            var primaryIndex = channels
                .Take(1)
                .Select(x => x.Indexes[0])
                .FirstOrDefault();

            // Verify channels exist for the URI
            if (!channels.Any() || primaryIndex == null)
                return false;

            var increasing = primaryIndex.Direction == IndexDirections.Increasing;
            var isTimeIndex = primaryIndex.IndexType == ChannelIndexTypes.Time;
            var rangeSize = WitsmlSettings.GetRangeSize(isTimeIndex);

            // Convert indexes from scaled values
            var minStart = channelRangeInfo.StartIndex.IndexFromScale(primaryIndex.Scale, isTimeIndex);

            // Validate startIndex and endIndex are in the expected order based on Direction
            if (!ValidateChannelRangeInfo(channelRangeInfo, increasing))
                return false;

            var isChannel = ChannelTypes.ContainsIgnoreCase(uri.ObjectType);
            var parentUri = isChannel ? uri.Parent : uri;

            var dataProvider = GetDataProvider(parentUri);
            var channelData = dataProvider.GetChannelData(parentUri, new Range<double?>(minStart, increasing ? minStart + rangeSize : minStart - rangeSize));

            // Stream Channel Data with IndexedDataItems if StreamIndexValuePairs setting is true
            if (WitsmlSettings.StreamIndexValuePairs)
            {
                await StreamIndexedChannelDataRange(channelRangeInfo, channels, channelData, increasing, token);
            }
            else
            {
                await StreamChannelDataRange(channelRangeInfo, channels, channelData, increasing, token);
            }

            return true;
        }

        private bool ValidateChannelRangeInfo(ChannelRangeInfo channelRangeInfo, bool increasing)
        {
            if (increasing)
            {
                if (channelRangeInfo.StartIndex > channelRangeInfo.EndIndex)
                {
                    this.InvalidArgument("startIndex > endIndex", Request.MessageId);
                    return false;
                }
            }
            else
            {
                if (channelRangeInfo.StartIndex < channelRangeInfo.EndIndex)
                {
                    this.InvalidArgument("startIndex < endIndex", Request.MessageId);
                    return false;
                }
            }

            return true;
        }

        private async Task StreamChannelDataRange(ChannelRangeInfo channelRangeInfo, IList<ChannelMetadataRecord> channels, IEnumerable<IChannelDataRecord> channelData, bool increasing, CancellationToken token)
        {
            var dataItemList = new List<DataItem>();

            using (var channelDataEnum = channelData.GetEnumerator())
            {
                var endOfChannelData = !channelDataEnum.MoveNext();

                while (!endOfChannelData)
                {
                    foreach (var dataItem in CreateDataItems(channels, channelRangeInfo, channelDataEnum.Current, increasing))
                    {
                        if (token.IsCancellationRequested)
                            break;

                        if (dataItemList.Count >= MaxDataItems)
                        {
                            await SendChannelData(dataItemList);
                            dataItemList.Clear();
                        }

                        if (token.IsCancellationRequested)
                            break;

                        dataItemList.Add(dataItem);
                    }
                    endOfChannelData = !channelDataEnum.MoveNext();

                    if (token.IsCancellationRequested)
                        break;
                }

                if (dataItemList.Any())
                {
                    await SendChannelData(dataItemList);
                }
            }
        }

        private IEnumerable<DataItem> CreateDataItems(IList<ChannelMetadataRecord> channels, ChannelRangeInfo channelRangeInfo, IChannelDataRecord record, bool increasing)
        {
            // Get primary index info and scaled value for the current record
            var primaryIndex = channels.Take(1).Select(x => x.Indexes[0]).First();
            var isTimeIndex = primaryIndex.IndexType == ChannelIndexTypes.Time;
            var primaryIndexValue = record.GetIndexValue().IndexToScale(primaryIndex.Scale, isTimeIndex);
            var requestedRange = new Range<double?>(channelRangeInfo.StartIndex, channelRangeInfo.EndIndex);

            // Only output if we are within the range
            if (!requestedRange.Contains(primaryIndexValue))
                yield break;

            var indexes = channels.Take(1).SelectMany(x => x.Indexes);
            var indexValues = new List<long>();
            var i = 0;

            indexes.ForEach(x =>
            {
                indexValues.Add((long)record.GetIndexValue(i++, x.Scale));
            });

            // Move the range info start index
            channelRangeInfo.StartIndex = primaryIndexValue;

            foreach (var channelId in channelRangeInfo.ChannelId)
            {
                var channel = channels.FirstOrDefault(c => c.ChannelId == channelId);
                // Verify channel is included in the channelRangeInfo
                if (channel == null) continue;

                var value = FormatValue(record.GetValue(record.GetOrdinal(channel.ChannelName)));

                // Filter null or empty data values
                if (value == null || string.IsNullOrWhiteSpace($"{value}"))
                    continue;

                yield return new DataItem()
                {
                    ChannelId = channelId,
                    Indexes = indexValues.ToArray(),
                    ValueAttributes = new DataAttribute[0],
                    Value = new DataValue()
                    {
                        Item = value
                    }
                };
            }
        }

        private async Task StreamIndexedChannelDataRange(ChannelRangeInfo channelRangeInfo, IList<ChannelMetadataRecord> channels, IEnumerable<IChannelDataRecord> channelData, bool increasing, CancellationToken token)
        {
            var dataItemList = new List<DataItem>();

            using (var channelDataEnum = channelData.GetEnumerator())
            {
                var endOfChannelData = !channelDataEnum.MoveNext();

                while (!endOfChannelData)
                {
                    foreach (var dataItem in CreateIndexedDataItems(channels, channelRangeInfo, channelDataEnum.Current, increasing))
                    {
                        if (token.IsCancellationRequested)
                            break;

                        if (dataItemList.Count + 1 >= MaxDataItems)
                        {
                            await SendChannelData(dataItemList);
                            dataItemList.Clear();
                        }

                        dataItemList.Add(dataItem.IndexDataItem);
                        dataItemList.Add(dataItem.ValueDataItem);
                    }
                    endOfChannelData = !channelDataEnum.MoveNext();

                    if (token.IsCancellationRequested)
                        break;
                }

                if (dataItemList.Any())
                {
                    await SendChannelData(dataItemList);
                }
            }
        }

        private IEnumerable<IndexedDataItem> CreateIndexedDataItems(IList<ChannelMetadataRecord> channels, ChannelRangeInfo channelRangeInfo, IChannelDataRecord record, bool increasing)
        {
            // Get the value and ChannelId of the primary index
            var primaryIndexValue = record.GetIndexValue();
            var primaryIndex = channels
                .Take(1)
                .Select(x => x.Indexes[0])
                .FirstOrDefault();

            var primaryIndexChannelId = channels
                .Where(x => x.ChannelName.EqualsIgnoreCase(x.Indexes[0].Mnemonic))
                .Select(x => x.ChannelId)
                .FirstOrDefault();
            var indexMnemonics = channels
                .Take(1)
                .SelectMany(x => x.Indexes.Select(y => y.Mnemonic))
                .ToArray();

            // Create a DataItem for the Primary Index
            var indexDataItem = new DataItem()
            {
                ChannelId = primaryIndexChannelId,
                Indexes = new long[0],
                ValueAttributes = new DataAttribute[0],
                Value = new DataValue()
                {
                    Item = primaryIndexValue
                }
            };
            var isTimeIndex = primaryIndex.IndexType == ChannelIndexTypes.Time;

            // Convert range info index from scale to compare to record index values
            var startEndRange = new Range<double?>(
                channelRangeInfo.StartIndex.IndexFromScale(primaryIndex.Scale, isTimeIndex),
                channelRangeInfo.EndIndex.IndexFromScale(primaryIndex.Scale, isTimeIndex));

            // Only output if we are within the range
            if (startEndRange.Contains(primaryIndexValue))
            {
                // Move the range info start index
                channelRangeInfo.StartIndex = primaryIndexValue.IndexToScale(primaryIndex.Scale, isTimeIndex);

                foreach (var channelId in channelRangeInfo.ChannelId)
                {
                    var channel = channels.FirstOrDefault(c => c.ChannelId == channelId);
                    // Verify channel is included in the channelRangeInfo
                    if (channel == null) continue;

                    if (indexMnemonics.Any(x => x.EqualsIgnoreCase(channel.ChannelName)))
                        continue;

                    var value = FormatValue(record.GetValue(record.GetOrdinal(channel.ChannelName)));

                    // Filter null or empty data values
                    if (value == null || string.IsNullOrWhiteSpace($"{value}"))
                        continue;

                    var valueDataItem = new DataItem()
                    {
                        ChannelId = channelId,
                        Indexes = new long[0],
                        ValueAttributes = new DataAttribute[0],
                        Value = new DataValue()
                        {
                            Item = value
                        }
                    };

                    yield return new IndexedDataItem(indexDataItem, valueDataItem);
                }
            }
        }

        private async Task StartChannelStreaming(IList<ChannelStreamingInfo> infos, CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    break;

                foreach (var uri in Channels.Keys)
                    await StreamChannelData(infos, uri, token);
            }
        }

        private async Task<bool> StreamChannelData(IList<ChannelStreamingInfo> infos, EtpUri uri, CancellationToken token)
        {
            var channels = Channels[uri];
            var channelIds = channels.Select(x => x.ChannelId).ToArray();
            var channelInfos = infos.Where(x => channelIds.Contains(x.ChannelId)).ToArray();
            var primaryIndex = channels
                .Take(1)
                .Select(x => x.Indexes[0])
                .FirstOrDefault();

            // TODO: Handle 3 different StartIndex types
            //if (x.StartIndex.Item is int)
            //    // Stream Index Count Data
            //else if (x.StartIndex.Item is long)
            //    // Stream Channel Data();
            //else
            //    // Stream Latest Value Data

            // Verify channels exist for the URI
            if (!channels.Any() || primaryIndex == null)
                return false;

            var minStart = channelInfos.Min(x => Convert.ToInt64(x.StartIndex.Item));
            var increasing = primaryIndex.Direction == IndexDirections.Increasing;
            var isTimeIndex = primaryIndex.IndexType == ChannelIndexTypes.Time;
            var rangeSize = WitsmlSettings.GetRangeSize(isTimeIndex);

            // Convert indexes from scaled values
            var minStartIndex = minStart.IndexFromScale(primaryIndex.Scale, isTimeIndex);

            channelIds = channelInfos.Select(x => x.ChannelId).ToArray();
            channels = channels.Where(x => channelIds.Contains(x.ChannelId)).ToList();

            var isChannel = ChannelTypes.ContainsIgnoreCase(uri.ObjectType);
            var parentUri = isChannel ? uri.Parent : uri;

            var dataProvider = GetDataProvider(parentUri);
            var channelData = dataProvider.GetChannelData(parentUri, new Range<double?>(minStartIndex, increasing ? minStartIndex + rangeSize : minStartIndex - rangeSize));

            // Stream Channel Data with IndexedDataItems if StreamIndexValuePairs setting is true
            if (WitsmlSettings.StreamIndexValuePairs)
            {
                await StreamIndexedChannelData(infos, channels, channelData, increasing, token);
            }
            else
            {
                await StreamChannelData(infos, channels, channelData, increasing, token);
            }

            return true;
        }

        private async Task StreamChannelData(IList<ChannelStreamingInfo> infos, List<ChannelMetadataRecord> channels, IEnumerable<IChannelDataRecord> channelData, bool increasing, CancellationToken token)
        {
            var dataItemList = new List<DataItem>();

            using (var channelDataEnum = channelData.GetEnumerator())
            {
                var endOfChannelData = !channelDataEnum.MoveNext();

                while (!endOfChannelData)
                {
                    foreach (var dataItem in CreateDataItems(channels, infos, channelDataEnum.Current, increasing))
                    {
                        if (token.IsCancellationRequested)
                            break;

                        if (dataItemList.Count >= MaxDataItems)
                        {
                            await SendChannelData(dataItemList);
                            dataItemList.Clear();
                        }

                        if (token.IsCancellationRequested)
                            break;

                        dataItemList.Add(dataItem);
                    }
                    endOfChannelData = !channelDataEnum.MoveNext();

                    if (token.IsCancellationRequested)
                        break;
                }

                if (dataItemList.Any())
                {
                    await SendChannelData(dataItemList);
                }
            }
        }

        private IEnumerable<DataItem> CreateDataItems(IList<ChannelMetadataRecord> channels, IList<ChannelStreamingInfo> infos, IChannelDataRecord record, bool increasing)
        {
            // Get primary index info and scaled value for the current record
            var primaryIndex = channels.Take(1).Select(x => x.Indexes[0]).First();
            var isTimeIndex = primaryIndex.IndexType == ChannelIndexTypes.Time;
            var primaryIndexValue = record.GetIndexValue().IndexToScale(primaryIndex.Scale, isTimeIndex);

            var indexValues = new List<long>();
            var indexes = channels.Take(1).SelectMany(x => x.Indexes);
            var i = 0;

            indexes.ForEach(x =>
            {
                indexValues.Add((long)record.GetIndexValue(i++, x.Scale));
            });

            foreach (var info in infos)
            {
                var channel = channels.FirstOrDefault(c => c.ChannelId == info.ChannelId);
                // Verify channel is included in the channelRangeInfo
                if (channel == null) continue;

                var start = new Range<double?>(Convert.ToDouble(info.StartIndex.Item), null);

                // Handle decreasing data
                if (start.StartsAfter(primaryIndexValue, increasing, inclusive: true))
                    continue;

                // Update ChannelStreamingInfo index value
                info.StartIndex.Item = primaryIndexValue;

                var value = FormatValue(record.GetValue(record.GetOrdinal(channel.ChannelName)));

                // Filter null or empty data values
                if (value == null || string.IsNullOrWhiteSpace($"{value}"))
                    continue;

                yield return new DataItem()
                {
                    ChannelId = info.ChannelId,
                    Indexes = indexValues.ToArray(),
                    ValueAttributes = new DataAttribute[0],
                    Value = new DataValue()
                    {
                        Item = value
                    }
                };
            }
        }

        private async Task StreamIndexedChannelData(IList<ChannelStreamingInfo> infos, List<ChannelMetadataRecord> channels, IEnumerable<IChannelDataRecord> channelData, bool increasing, CancellationToken token)
        {
            var dataItemList = new List<DataItem>();

            using (var channelDataEnum = channelData.GetEnumerator())
            {
                var endOfChannelData = !channelDataEnum.MoveNext();

                while (!endOfChannelData)
                {
                    foreach (var dataItem in CreateIndexedDataItems(channels, infos, channelDataEnum.Current, increasing))
                    {
                        if (token.IsCancellationRequested)
                            break;

                        if (dataItemList.Count + 1 >= MaxDataItems)
                        {
                            await SendChannelData(dataItemList);
                            dataItemList.Clear();
                        }

                        dataItemList.Add(dataItem.IndexDataItem);
                        dataItemList.Add(dataItem.ValueDataItem);
                    }
                    endOfChannelData = !channelDataEnum.MoveNext();

                    if (token.IsCancellationRequested)
                        break;
                }

                if (dataItemList.Any())
                {
                    await SendChannelData(dataItemList);
                }
            }
        }

        private IEnumerable<IndexedDataItem> CreateIndexedDataItems(IList<ChannelMetadataRecord> channels, IList<ChannelStreamingInfo> infos, IChannelDataRecord record, bool increasing)
        {
            // Get the value and ChannelId of the primary index
            var primaryIndexValue = record.GetIndexValue();
            var primaryIndexChannelId = channels
                .Where(x => x.ChannelName.EqualsIgnoreCase(x.Indexes[0].Mnemonic))
                .Select(x => x.ChannelId)
                .FirstOrDefault();
            var indexMnemonics = channels
                .Take(1)
                .SelectMany(x => x.Indexes.Select(y => y.Mnemonic))
                .ToArray();

            // Create a DataItem for the Primary Index
            var indexDataItem = new DataItem()
            {
                ChannelId = primaryIndexChannelId,
                Indexes = new long[0],
                ValueAttributes = new DataAttribute[0],
                Value = new DataValue()
                {
                    Item = primaryIndexValue
                }
            };

            foreach (var info in infos)
            {
                var channel = channels.FirstOrDefault(c => c.ChannelId == info.ChannelId);
                // Verify channel is included in the channelRangeInfo
                if (channel == null) continue;

                var start = new Range<double?>(Convert.ToDouble(info.StartIndex.Item), null);

                // Handle decreasing data
                if ((start.StartsAfter(primaryIndexValue, increasing, inclusive: true))
                    || indexMnemonics.Any(x => x.EqualsIgnoreCase(channel.ChannelName)))
                {
                    continue;
                }

                // update ChannelStreamingInfo index value
                info.StartIndex.Item = primaryIndexValue;

                var value = FormatValue(record.GetValue(record.GetOrdinal(channel.ChannelName)));

                // Filter null or empty data values
                if (value == null || string.IsNullOrWhiteSpace($"{value}"))
                    continue;

                var valueDataItem = new DataItem()
                {
                    ChannelId = info.ChannelId,
                    Indexes = new long[0],
                    ValueAttributes = new DataAttribute[0],
                    Value = new DataValue()
                    {
                        Item = value
                    }
                };

                yield return new IndexedDataItem(indexDataItem, valueDataItem);
            }
        }

        private async Task SendChannelData(List<DataItem> dataItemList, MessageFlags messageFlag = MessageFlags.MultiPart)
        {
            ChannelData(Request, dataItemList, messageFlag);
            await Task.Delay(MaxMessageRate);
        }

        private IChannelDataProvider GetDataProvider(EtpUri uri)
        {
            return _container.Resolve<IChannelDataProvider>(new ObjectName(uri.ObjectType, uri.Version));
        }

        private object FormatValue(object value)
        {
            if (value is DateTime)
            {
                return ((DateTime)value).ToString("o");
            }
            else if (value is DateTimeOffset)
            {
                return ((DateTimeOffset)value).ToString("o");
            }
            else if (value is JValue)
            {
                return ((JValue)value).Value;
            }
            else if (value is JArray)
            {
                var array = value as JArray;
                return array.Count > 0 ? array[0].ToString() : null;
            }

            return value;
        }
    }
}
