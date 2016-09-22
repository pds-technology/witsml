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
using Energistics;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol;
using Energistics.Protocol.ChannelStreaming;
using Newtonsoft.Json.Linq;
using PDS.Framework;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data.Channels;

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
        private readonly IContainer _container;
        private CancellationTokenSource _tokenSource;
        private readonly List<IList<ChannelStreamingContext>> _channelStreamingContextLists;

        private Dictionary<string, List<IChannelDataProvider>> _providers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelStreamingProducer"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        [ImportingConstructor]
        public ChannelStreamingProducer(IContainer container)
        {
            _container = container;
            Channels = new Dictionary<long, Tuple<EtpUri, ChannelMetadataRecord>>();
            _channelStreamingContextLists = new List<IList<ChannelStreamingContext>>();
            InitializeChannelDataProviders();
        }

        /// <summary>
        /// Gets the channels.
        /// </summary>
        public Dictionary<long, Tuple<EtpUri, ChannelMetadataRecord>> Channels { get; }

        /// <summary>
        /// Gets the request.
        /// </summary>
        /// <value>The request message header.</value>
        public MessageHeader Request { get; private set; }

        /// <summary>
        /// Handles the channel describe.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{T}"/> instance containing the event data.</param>
        protected override void HandleChannelDescribe(ProtocolEventArgs<ChannelDescribe, IList<ChannelMetadataRecord>> args)
        {
            var uris = args.Message.Uris.Contains(EtpUri.RootUri)
                ? new[] { EtpUris.Witsml131, EtpUris.Witsml141, EtpUris.Witsml200 }
                : args.Message.Uris.Select(x => new EtpUri(x));

            foreach (var family in uris.ToLookup(x => x.Version))
            {
                var providers = _providers[family.Key];
                foreach (var provider in providers)
                {
                    var metadata = provider.GetChannelMetadata(family.ToArray());

                    metadata.ForEach(m =>
                    {
                        // Check by uri if we have the metadata in our dictionary.
                        var channelMetadataRecord =
                            Channels.Values.Select(c => c.Item2).FirstOrDefault(c => c.ChannelUri.EqualsIgnoreCase(m.ChannelUri));

                        // if not add it and set its channelId
                        if (channelMetadataRecord == null)
                        {
                            m.ChannelId = Channels.Keys.Count;
                            Channels.Add(m.ChannelId, new Tuple<EtpUri, ChannelMetadataRecord>(new EtpUri(m.ChannelUri).Parent, m));
                            channelMetadataRecord = m;
                        }
                        args.Context.Add(channelMetadataRecord);
                    });
                }
            }
        }

        /// <summary>
        /// Handles the channel streaming start.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="channelStreamingStart">The channel streaming start.</param>
        protected override void HandleChannelStreamingStart(MessageHeader header, ChannelStreamingStart channelStreamingStart)
        {
            // TODO: Send ChannelStatusChanged if the receiveChangeNotification field is true

            // no action needed if streaming already started
            //if (_tokenSource != null)
            //    return;

            base.HandleChannelStreamingStart(header, channelStreamingStart);

            Request = null;

            if (_tokenSource == null)
                _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            Logger.Debug("Channel Streaming starting.");
            Task.Run(() => StartChannelStreaming(header.MessageId, channelStreamingStart.Channels, token), token);
        }

        /// <summary>
        /// Handles the channel range request.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="channelRangeRequest">The channel range request.</param>
        protected override void HandleChannelRangeRequest(MessageHeader header, ChannelRangeRequest channelRangeRequest)
        {
            base.HandleChannelRangeRequest(header, channelRangeRequest);

            Request = header;
            if (_tokenSource == null)
                _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            Logger.Debug("Channel Range Request starting.");
            Task.Run(() => StartChannelRangeRequest(channelRangeRequest.ChannelRanges, token), token);

        }

        /// <summary>
        /// Handles the channel streaming stop.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="channelStreamingStop">The channel streaming stop.</param>
        protected override void HandleChannelStreamingStop(MessageHeader header, ChannelStreamingStop channelStreamingStop)
        {
            // no action needed if streaming not in progress
            if (_tokenSource == null)
            {
                SendInvalidStateMessage(header.MessageId, "EINVALID_STATE_CODE: There are currently no channels streaming.");
                return;
            }

            StopStreamingChannels(header.MessageId, channelStreamingStop.Channels);

            base.HandleChannelStreamingStop(header, channelStreamingStop);
        }

        private void StopStreamingChannels(long messageId, IList<long> channels)
        {
            foreach (var channel in channels)
            {
                // Find the ChannelStreamingInfo list that contains the channelId
                var streamingContextList = _channelStreamingContextLists.FirstOrDefault(list => list.Any(l => l.ChannelId == channel));

                if (streamingContextList != null)
                {
                    // Find the context for the channelId to remove
                    var contextToRemove = streamingContextList.First(l => l.ChannelId == channel);

                    // Remove context from its list.
                    streamingContextList.Remove(contextToRemove);

                    // If the context list is empty remove it from its list of lists.
                    if (streamingContextList.Count == 0)
                        _channelStreamingContextLists.Remove(streamingContextList);
                }
                else
                {
                    // Try to get the mnemonic from the Described channels
                    var mnemonic = Channels.ContainsKey(channel) ? $" ({Channels[channel].Item2.ChannelName})" : string.Empty;
                    SendInvalidStateMessage(messageId, $"EINVALID_STATE_CODE: Channel {channel}{mnemonic} is not currently streaming.");
                }
            }
        }

        private void StartChannelStreaming(long messageId, IList<ChannelStreamingInfo> infos, CancellationToken token)
        {
            List<long> channelIds = ValidateAndRemoveStreamingChannels(messageId, infos);

            // Get the channel metadata and parent uri for the channels to start streaming
            var infoChannels = Channels.Where(c => channelIds.Contains(c.Key)).Select(c => c.Value);

            // Join ChannelStreamingInfos and infoChannels into a list of ChannelStreamingContext
            var streamingContextList =
                from info in infos
                join infoChannel in infoChannels on info.ChannelId equals infoChannel.Item2.ChannelId
                select new ChannelStreamingContext()
                {
                    ChannelId = info.ChannelId,
                    ChannelMetadata = infoChannel.Item2,
                    ParentUri = infoChannel.Item1,
                    StartIndex = info.StartIndex.Item as long?, // indexValue
                    EndIndex = null,
                    IndexCount = info.StartIndex.Item as int? ?? 0, // indexCount
                    ChannelStreamingType = ToChannelStreamingType(info.StartIndex.Item),
                    RangeRequestHeader = null,
                    ReceiveChangeNotification = info.ReceiveChangeNotification
                };

            // Group the ChannelStreamingContext list by ChannelStreamingType, ParentUri and StartIndex
            var streamingContextGrouping =
                from x in streamingContextList
                group x by new { x.ChannelStreamingType, x.ParentUri, x.StartIndex };

            // Start a Task for each context group
            Task.WhenAll(streamingContextGrouping.Select(context => StreamChannelData(context.ToList(), token)));
        }


        private void StartChannelRangeRequest(IList<ChannelRangeInfo> infos, CancellationToken token)
        {
            var channelIds = infos.SelectMany(c => c.ChannelId).Distinct();

            // Get the channel metadata and parent uri for the channels to start range request
            var infoChannels = Channels.Where(c => channelIds.Contains(c.Key)).Select(c => c.Value);

            var flatRangeInfos =
                infos.SelectMany(
                    i => i.ChannelId.Select(c => new { ChannelId = c, i.StartIndex, i.EndIndex }));

            // Join FlatRangeInfos and infoChannels into a list of ChannelStreamingContext
            var streamingContextList =
                from info in flatRangeInfos
                join infoChannel in infoChannels on info.ChannelId equals infoChannel.Item2.ChannelId
                select new ChannelStreamingContext()
                {
                    ChannelId = info.ChannelId,
                    ChannelMetadata = infoChannel.Item2,
                    ParentUri = infoChannel.Item1,
                    StartIndex = info.StartIndex,
                    EndIndex = info.EndIndex,
                    IndexCount = 0,
                    ChannelStreamingType = ChannelStreamingTypes.RangeRequest,
                    RangeRequestHeader = Request,
                    ReceiveChangeNotification = false
                };

            // Group the ChannelStreamingContext list by ChannelStreamingType, ParentUri and StartIndex
            var streamingContextGrouping =
                from x in streamingContextList
                group x by new { x.ChannelStreamingType, x.ParentUri, x.StartIndex };

            // Start a Task for each context group
            Task.WhenAll(streamingContextGrouping.Select(context => StreamChannelData(context.ToList(), token)));
        }

        private List<long> ValidateAndRemoveStreamingChannels(long messageId, IList<ChannelStreamingInfo> infos)
        {
            var channelIds = infos
                .Select(i => i.ChannelId)
                .Distinct()
                .ToList();

            // Get an array of any channelId that are already streaming.
            var streamingChannels = GetStreamingChannels(channelIds);
            var streamingChannelIds = streamingChannels.Select(c => c.ChannelId).ToArray();

            // Send a EINVALID_STATE message if any are already streaming.
            if (streamingChannels.Length > 0)
            {
                streamingChannels.ForEach(c => 
                    SendInvalidStateMessage(messageId, $"EINVALID_STATE_CODE: Channel {c.ChannelId} ({c.ChannelName}) is already streaming."));
                Logger.Warn($"Channels {string.Join(",", streamingChannelIds)} are already streaming.");
            }

            // Remove the channelIds that are already streaming and continue with the rest.
            streamingChannelIds.ForEach(s => channelIds.Remove(s));

            // Remove the infos for channels that are already streaming.
            var streamingInfos = infos.Where(i => streamingChannelIds.Contains(i.ChannelId)).ToList();
            streamingInfos.ForEach(i => infos.Remove(i));
            return channelIds;
        }

        private ChannelStreamingTypes ToChannelStreamingType(object item)
        {
            return item is long
                ? ChannelStreamingTypes.IndexValue
                : item is int
                    ? ChannelStreamingTypes.IndexCount
                    : ChannelStreamingTypes.LatestValue;
        }

        private void SendInvalidStateMessage(long messageId, string message)
        {
            ProtocolException((int)EtpErrorCodes.InvalidState, message, messageId);
        }

        private ChannelMetadataRecord[] GetStreamingChannels(IEnumerable<long> channelIds)
        {
            return _channelStreamingContextLists
                .SelectMany(list => list
                    .Where(l => channelIds.Contains(l.ChannelId))
                    .Select(c => c.ChannelMetadata))
                .ToArray();

        }

        private async Task StreamChannelData(IList<ChannelStreamingContext> contextList, CancellationToken token)
        {
            _channelStreamingContextLists.Add(contextList);

            // These values can be set outside of our processing loop as the won't chnage
            //... as context is processed and completed.
            var firstContext = contextList.First();
            var channelStreamingType = firstContext.ChannelStreamingType;
            var parentUri = firstContext.ParentUri;
            var primaryIndex = firstContext.ChannelMetadata.Indexes[0];
            var requestLatestValues =
                channelStreamingType == ChannelStreamingTypes.IndexCount
                    ? firstContext.IndexCount
                    : channelStreamingType == ChannelStreamingTypes.LatestValue
                        ? 1
                        : (int?)null;
            var increasing = primaryIndex.Direction == IndexDirections.Increasing;
            bool? firstStart = null;

            // Loop until there is a cancellation or all channals have been removed
            while (!IsStreamingStopped(contextList, ref token))
            {
                firstStart = !firstStart.HasValue;

                var channelIds = contextList.Select(i => i.ChannelId).Distinct().ToArray();
                Logger.Debug($"Streaming data for parentUri {parentUri.Uri} and channelIds {string.Join(",", channelIds)}");

                // We only need a start index value for IndexValue and RangeRequest or if we're streaming
                //... IndexCount or LatestValue and requestLatestValues is no longer set.
                var minStart =
                    (channelStreamingType == ChannelStreamingTypes.IndexValue || channelStreamingType == ChannelStreamingTypes.RangeRequest) ||
                    ((channelStreamingType == ChannelStreamingTypes.IndexCount || channelStreamingType == ChannelStreamingTypes.LatestValue) && 
                    !requestLatestValues.HasValue)
                        ? contextList.Min(x => Convert.ToInt64(x.StartIndex))
                        : (long?)null;

                // Only need and end index value for range request
                var maxEnd = channelStreamingType == ChannelStreamingTypes.RangeRequest
                    ? contextList.Max(x => Convert.ToInt64(x.EndIndex))
                    : (long?)null;


                // Validate startIndex and endIndex are in the expected order based on Direction
                if (channelStreamingType == ChannelStreamingTypes.RangeRequest)
                {
                    var minEnd = contextList.Min(x => Convert.ToInt64(x.EndIndex));
                    ValidateRangeRequestIndexes(minStart ?? 0, minEnd, increasing);
                }

                var isTimeIndex = primaryIndex.IndexType == ChannelIndexTypes.Time;
                var rangeSize = WitsmlSettings.GetRangeSize(isTimeIndex);

                // Convert indexes from scaled values
                var minStartIndex = minStart?.IndexFromScale(primaryIndex.Scale, isTimeIndex);
                var maxEndIndex = channelStreamingType == ChannelStreamingTypes.IndexValue
                    ? (increasing ? minStartIndex + rangeSize : minStartIndex - rangeSize)
                    : maxEnd?.IndexFromScale(primaryIndex.Scale, isTimeIndex);

                // Get channel data
                var mnemonics = contextList.Select(c => c.ChannelMetadata.ChannelName).ToList();
                var dataProvider = GetDataProvider(parentUri);
                var optimiseStart = channelStreamingType == ChannelStreamingTypes.IndexValue ? true : false;
                var channelData = dataProvider.GetChannelData(parentUri, new Range<double?>(minStartIndex, maxEndIndex), mnemonics, requestLatestValues, optimiseStart);

                // Stream the channel data
                await StreamChannelData(contextList, channelData, mnemonics.ToArray(), increasing, isTimeIndex, primaryIndex.Scale, firstStart.Value, token);

                // If we have processed an IndexCount or LatestValue query clear requestLatestValues so we can 
                //... keep streaming new data as long as the channel is active.
                if (channelStreamingType == ChannelStreamingTypes.IndexCount ||
                    channelStreamingType == ChannelStreamingTypes.LatestValue)
                {
                    requestLatestValues = null;
                }

                // Check each context to see of all the data has streamed.
                var completedContexts = contextList
                    .Where(
                        c =>
                            (c.ChannelStreamingType != ChannelStreamingTypes.RangeRequest &&
                            c.ChannelMetadata.Status != ChannelStatuses.Active && c.ChannelMetadata.EndIndex.HasValue &&
                            c.StartIndex >= c.ChannelMetadata.EndIndex.Value) ||

                            (c.ChannelStreamingType == ChannelStreamingTypes.RangeRequest &&
                            c.StartIndex >= c.EndIndex))
                    .ToArray();


                // Remove any contexts from the list that have completed returning all data
                completedContexts
                    .ForEach(c => contextList.Remove(c));
            }
        }

        private async Task StreamChannelData(IList<ChannelStreamingContext> contextList, List<List<List<object>>> channelData, string[] mnemonics, bool increasing, bool isTimeIndex, int scale, bool firstStart, CancellationToken token)
        {
            var dataItemList = new List<DataItem>();
            var firstContext = contextList.FirstOrDefault();

            // Streaming could have been stopped for all channels in the list.
            if (firstContext == null)
                return;

            using (var channelDataEnum = channelData.GetEnumerator())
            {
                var endOfChannelData = !channelDataEnum.MoveNext();

                while (!endOfChannelData)
                {
                    foreach (var dataItem in CreateDataItems(contextList, channelDataEnum.Current, mnemonics, increasing, isTimeIndex, scale, firstStart))
                    {
                        if (IsStreamingStopped(contextList, ref token))
                            break;

                        // If our data list has reached the maximum size, then send it.
                        if (dataItemList.Count >= MaxDataItems)
                        {
                            await SendChannelData(dataItemList);
                            dataItemList.Clear();
                        }

                        if (IsStreamingStopped(contextList, ref token))
                            break;

                        // Add the dataItem to a list to be streamed.
                        dataItemList.Add(dataItem);
                    }
                    endOfChannelData = !channelDataEnum.MoveNext();

                    if (IsStreamingStopped(contextList, ref token))
                        break;
                }

                // Send any remaining channel data that has not been streamed.
                if (dataItemList.Any())
                {
                    await SendChannelData(dataItemList);
                }
            }
        }

        private IEnumerable<DataItem> CreateDataItems(IList<ChannelStreamingContext> contextList, List<List<object>> record, string[] mnemonics, bool increasing, bool isTimeIndex, int scale, bool firstStart)
        {
            // Get the index and value components for the current record
            var indexes = record[0];
            var values = record[1];

            // Create a list of all of the records indexes, scaled
            var indexValues = new List<long>();
            indexes.ForEach(x =>
            {
                var indexValue = isTimeIndex
                    ? new DateTimeOffset(DateTime.Parse(x.ToString())).ToUnixTimeMicroseconds()
                    : ((double)x).IndexToScale(scale);
                indexValues.Add(indexValue);
            });

            var primaryIndexValue = indexValues[0];

            // For each channel
            foreach (var context in contextList)
            {
                // Get the channel from the context.
                var channel = context.ChannelMetadata;

                // Create a range for the current start index.
                var start = new Range<double?>(Convert.ToDouble(context.StartIndex), null);

                // If we have an established Start and it starts after the current primaryIndexValue then skip this value.
                if (context.StartIndex.HasValue && start.StartsAfter(primaryIndexValue, increasing, inclusive: !firstStart))
                    continue;

                // Update the current StartIndex for our context with the current index value
                context.StartIndex = primaryIndexValue;

                // If the data does not include values for the channel we're streaming, then skip
                if (!mnemonics.Contains(channel.ChannelName))
                    continue;

                var value = FormatValue(values[Array.IndexOf(mnemonics, channel.ChannelName)]);

                // Filter null or empty data values
                if (value == null || string.IsNullOrWhiteSpace($"{value}"))
                    continue;

                // Create and return a DataItem.
                yield return new DataItem()
                {
                    ChannelId = context.ChannelId,
                    Indexes = indexValues.ToArray(),
                    ValueAttributes = new DataAttribute[0],
                    Value = new DataValue()
                    {
                        Item = value
                    }
                };
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

        private void InitializeChannelDataProviders()
        {
            _providers = new Dictionary<string, List<IChannelDataProvider>>
            {
                {OptionsIn.DataVersion.Version131.Value, new List<IChannelDataProvider>{_container.Resolve<IChannelDataProvider>(ObjectNames.Log131)}},
                {OptionsIn.DataVersion.Version141.Value, new List<IChannelDataProvider> {_container.Resolve<IChannelDataProvider>(ObjectNames.Log141)}},
                {OptionsIn.DataVersion.Version200.Value, new List<IChannelDataProvider>
                {
                    _container.Resolve<IChannelDataProvider>(ObjectNames.Log200),
                    _container.Resolve<IChannelDataProvider>(ObjectNames.Channel200)
                }}
            };
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

        private static bool IsStreamingStopped(IList<ChannelStreamingContext> contextList, ref CancellationToken token)
        {
            return token.IsCancellationRequested || contextList.Count == 0;
        }

        private void ValidateRangeRequestIndexes(long startIndex, long endIndex, bool increasing)
        {
            if (increasing)
            {
                if (startIndex > endIndex)
                {
                    this.InvalidArgument("startIndex > endIndex", Request.MessageId);
                }
            }
            else
            {
                if (startIndex < endIndex)
                {
                    this.InvalidArgument("startIndex < endIndex", Request.MessageId);
                }
            }
        }
    }
}