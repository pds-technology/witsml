//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
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
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;
using Energistics.Etp.v11.Datatypes;
using Energistics.Etp.v11.Datatypes.ChannelData;
using Energistics.Etp.v11.Protocol.ChannelStreaming;
using PDS.WITSMLstudio.Data;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data.Channels;

namespace PDS.WITSMLstudio.Store.Providers.ChannelStreaming
{
    /// <summary>
    /// Producer class for channel streaming
    /// </summary>
    /// <seealso cref="ChannelStreamingProducerHandler" />
    [Export(typeof(IChannelStreamingProducer))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ChannelStreaming11Producer : ChannelStreamingProducerHandler, IStreamingProducer
    {
        private CancellationTokenSource _tokenSource;
        private readonly List<IList<ChannelStreamingContext>> _channelStreamingContextLists;
        private readonly Dictionary<string, List<IChannelDataProvider>> _providers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelStreaming11Producer"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        [ImportingConstructor]
        public ChannelStreaming11Producer(IContainer container)
        {
            Container = container;
            Channels = new Dictionary<long, Tuple<EtpUri, IChannelMetadataRecord>>();
            _channelStreamingContextLists = new List<IList<ChannelStreamingContext>>();
            _providers = InitializeChannelDataProviders();
        }

        /// <summary>
        /// Gets the channels.
        /// </summary>
        public Dictionary<long, Tuple<EtpUri, IChannelMetadataRecord>> Channels { get; }

        /// <summary>
        /// Gets the request.
        /// </summary>
        /// <value>The request message header.</value>
        public IMessageHeader Request { get; private set; }

        /// <summary>
        /// Gets the composition container.
        /// </summary>
        protected IContainer Container { get; }

        /// <summary>
        /// Gets the minimum message interval.
        /// </summary>
        int IStreamingProducer.MinMessageInterval => MaxMessageRate;

        /// <summary>
        /// Gets or sets the simple streamer uris.
        /// </summary>
        public IList<string> SimpleStreamerUris { get; set; }

        /// <summary>
        /// Initializes the channel streaming producer as a simple streamer.
        /// </summary>
        /// <param name="uris">The uris to stream.</param>
        public void InitializeSimpleStreamer(IList<string> uris)
        {
            IsSimpleStreamer = true;
            SimpleStreamerUris = uris;
        }

        /// <summary>
        /// Sends a ChannelMetadata message to a consumer.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="channelMetadataRecords">The list of <see cref="IChannelMetadataRecord" /> objects.</param>
        /// <param name="messageFlag">The message flag.</param>
        /// <returns>The message identifier.</returns>
        long IStreamingProducer.ChannelMetadata(IMessageHeader request, IList<IChannelMetadataRecord> channelMetadataRecords, MessageFlags messageFlag)
        {
            return ChannelMetadata(request, channelMetadataRecords.Cast<ChannelMetadataRecord>().ToList(), messageFlag);
        }

        /// <summary>
        /// Sends a ChannelData message to a consumer.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="dataItems">The list of <see cref="IDataItem" /> objects.</param>
        /// <param name="messageFlag">The message flag.</param>
        /// <returns>The message identifier.</returns>
        long IStreamingProducer.ChannelData(IMessageHeader request, IList<IDataItem> dataItems, MessageFlags messageFlag)
        {
            return ChannelData(request, dataItems.Cast<DataItem>().ToList(), messageFlag);
        }

        /// <summary>
        /// Sends a ChannelDataChange message to a consumer.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <param name="dataItems">The data items.</param>
        /// <returns>The message identifier.</returns>
        long IStreamingProducer.ChannelDataChange(long channelId, long startIndex, long endIndex, IList<IDataItem> dataItems)
        {
            return ChannelDataChange(channelId, startIndex, endIndex, dataItems.Cast<DataItem>().ToList());
        }

        /// <summary>
        /// Sends a ChannelStatusChange message to a consumer.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="isActive">if set to <c>true</c> the channel is active.</param>
        /// <returns>The message identifier.</returns>
        long IStreamingProducer.ChannelStatusChange(long channelId, bool isActive)
        {
            return ChannelStatusChange(channelId, (ChannelStatuses) Session.Adapter.GetChannelStatus(isActive));
        }

        /// <summary>
        /// Initializes the channel data providers.
        /// </summary>
        /// <returns></returns>
        protected virtual Dictionary<string, List<IChannelDataProvider>> InitializeChannelDataProviders()
        {
            var providers = Container.ResolveAll<IChannelDataProvider>();

            if (!providers.Any())
            {
                Logger.Warn("No IChannelDataProvider instances found.");
                return new Dictionary<string, List<IChannelDataProvider>>();
            }

            return new Dictionary<string, List<IChannelDataProvider>>
            {
                {OptionsIn.DataVersion.Version131.Value, new List<IChannelDataProvider>{Container.Resolve<IChannelDataProvider>(ObjectNames.Log131)}},
                {OptionsIn.DataVersion.Version141.Value, new List<IChannelDataProvider> {Container.Resolve<IChannelDataProvider>(ObjectNames.Log141)}},
                {OptionsIn.DataVersion.Version200.Value, new List<IChannelDataProvider>
                {
                    Container.Resolve<IChannelDataProvider>(ObjectNames.Log200),
                    Container.Resolve<IChannelDataProvider>(ObjectNames.Trajectory200),
                    Container.Resolve<IChannelDataProvider>(ObjectNames.Channel200)
                }}
            };
        }

        /// <summary>
        /// Gets the data provider to handle the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        protected virtual IChannelDataProvider GetDataProvider(EtpUri uri)
        {
            return Container.Resolve<IChannelDataProvider>(new ObjectName(uri.ObjectType, uri.Family, uri.Version));
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; 
        ///   <c>false</c> to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (_tokenSource != null)
            {
                using (_tokenSource)
                {
                    _tokenSource.Cancel();
                    _tokenSource = null;
                }
            }

            base.Dispose(disposing);
        }

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
                    var metadata = provider.GetChannelMetadata(Session.Adapter, family.ToArray());

                    metadata.ForEach(m =>
                    {
                        // Check by uri if we have the metadata in our dictionary.
                        var channelMetadataRecord = Channels.Values
                            .Select(c => c.Item2)
                            .FirstOrDefault(c => c.ChannelUri.EqualsIgnoreCase(m.ChannelUri));

                        // if not add it and set its channelId
                        if (channelMetadataRecord == null)
                        {
                            var channelUri = new EtpUri(m.ChannelUri);
                            var parentUri = channelUri.Parent;

                            // e.g. Trajectory channels
                            if (string.IsNullOrWhiteSpace(parentUri.ObjectId))
                                parentUri = channelUri;

                            m.ChannelId = Channels.Keys.Count;
                            Channels.Add(m.ChannelId, new Tuple<EtpUri, IChannelMetadataRecord>(parentUri, m));
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
        protected override void HandleChannelStreamingStart(IMessageHeader header, ChannelStreamingStart channelStreamingStart)
        {
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
        protected override void HandleChannelRangeRequest(IMessageHeader header, ChannelRangeRequest channelRangeRequest)
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
        protected override void HandleChannelStreamingStop(IMessageHeader header, ChannelStreamingStop channelStreamingStop)
        {
            // no action needed if streaming not in progress
            if (_tokenSource == null)
            {
                this.InvalidState("There are currently no channels streaming.", header.MessageId);
                return;
            }

            StopStreamingChannels(header.MessageId, channelStreamingStop.Channels);

            base.HandleChannelStreamingStop(header, channelStreamingStop);
        }

        /// <summary>
        /// Stops the streaming channels.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="channels">The channels.</param>
        protected virtual void StopStreamingChannels(long messageId, IList<long> channels)
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
                    this.InvalidState($"Channel {channel}{mnemonic} is not currently streaming.", messageId);
                }
            }
        }

        /// <summary>
        /// Starts the channel streaming.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="infos">The infos.</param>
        /// <param name="token">The token.</param>
        protected virtual void StartChannelStreaming(long messageId, IList<ChannelStreamingInfo> infos, CancellationToken token)
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

            StartChannelStreamingContextGroups(streamingContextList, token);
        }

        /// <summary>
        /// Starts the channel streaming context groups.
        /// </summary>
        /// <param name="streamingContextList">The streaming context list.</param>
        /// <param name="token">The token.</param>
        protected virtual void StartChannelStreamingContextGroups(IEnumerable<ChannelStreamingContext> streamingContextList, CancellationToken token)
        {
            // Group the ChannelStreamingContext list by ChannelStreamingType, ParentUri and StartIndex
            var streamingContextGrouping =
                from x in streamingContextList
                group x by new { x.ChannelStreamingType, x.ParentUri, x.StartIndex };

            // Start a Task for each context group
            Task.WhenAll(streamingContextGrouping.Select(context => StreamChannelData(context.ToList(), token)));
        }

        /// <summary>
        /// Starts the channel range request.
        /// </summary>
        /// <param name="infos">The infos.</param>
        /// <param name="token">The token.</param>
        protected virtual void StartChannelRangeRequest(IList<ChannelRangeInfo> infos, CancellationToken token)
        {
            // Validate each channel range info
            ValidateChannelRangeInfos(infos);

            // Return if there are no valid channel range infos remaining
            if (infos.Count < 1)
                return;

            var channelIds = infos.SelectMany(c => c.ChannelId).Distinct();

            // Get the channel metadata and parent uri for the channels to start range request
            var infoChannels = Channels.Where(c => channelIds.Contains(c.Key)).Select(c => c.Value);

            var flatRangeInfos =
                infos.SelectMany(
                    i => i.ChannelId.Select(c => new { ChannelId = c, i.StartIndex, i.EndIndex }));

            // Join FlatRangeInfos and infoChannels into a list of ChannelStreamingContext
            var streamingContextGroups =
                (from info in flatRangeInfos
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
                })
                .GroupBy(x => x.ChannelId);

            var streamingContextList = streamingContextGroups
                .SelectMany(x => Session.Adapter.MergeOverlappingContexts(x))
                .ToList();

            // Group the ChannelStreamingContext list by ChannelStreamingType, ParentUri and StartIndex
            var streamingContextGrouping =
                from x in streamingContextList
                group x by new { x.ChannelStreamingType, x.ParentUri, x.StartIndex };

            // Start a Task for each context group
            Task.WhenAll(streamingContextGrouping.Select(context => StreamChannelData(context.ToList(), token)));
        }

        /// <summary>
        /// Validates the and remove streaming channels.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="infos">The infos.</param>
        /// <returns></returns>
        protected List<long> ValidateAndRemoveStreamingChannels(long messageId, IList<ChannelStreamingInfo> infos)
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
                    this.InvalidState($"Channel {c.ChannelId} ({c.ChannelName}) is already streaming.", messageId));
                Logger.Warn($"Channels {string.Join(",", streamingChannelIds)} are already streaming.");
            }

            // Remove the channelIds that are already streaming and continue with the rest.
            streamingChannelIds.ForEach(s => channelIds.Remove(s));

            // Remove the infos for channels that are already streaming.
            var streamingInfos = infos.Where(i => streamingChannelIds.Contains(i.ChannelId)).ToList();
            streamingInfos.ForEach(i => infos.Remove(i));
            return channelIds;
        }

        /// <summary>
        /// To the type of the channel streaming.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        protected ChannelStreamingTypes ToChannelStreamingType(object item)
        {
            return item is long
                ? ChannelStreamingTypes.IndexValue
                : item is int
                    ? ChannelStreamingTypes.IndexCount
                    : ChannelStreamingTypes.LatestValue;
        }

        /// <summary>
        /// Gets the streaming channels.
        /// </summary>
        /// <param name="channelIds">The channel ids.</param>
        /// <returns></returns>
        protected IChannelMetadataRecord[] GetStreamingChannels(IEnumerable<long> channelIds)
        {
            return _channelStreamingContextLists
                .SelectMany(list => list
                    .Where(l => channelIds.Contains(l.ChannelId))
                    .Select(c => c.ChannelMetadata))
                .ToArray();
        }

        /// <summary>
        /// Streams the channel data.
        /// </summary>
        /// <param name="contextList">The context list.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        protected virtual async Task StreamChannelData(IList<ChannelStreamingContext> contextList, CancellationToken token)
        {
            _channelStreamingContextLists.Add(contextList);

            // These values can be set outside of our processing loop as the won't chnage
            //... as context is processed and completed.
            var firstContext = contextList.First();
            var channelStreamingType = firstContext.ChannelStreamingType;
            var parentUri = firstContext.ParentUri;
            var indexes = firstContext.ChannelMetadata.Indexes.Cast<IIndexMetadataRecord>().ToList();
            var primaryIndex = indexes[0];
            var isTimeIndex = indexes.Select(i => i.IndexKind == (int) ChannelIndexTypes.Time).ToArray();
            var requestLatestValues =
                channelStreamingType == ChannelStreamingTypes.IndexCount
                    ? firstContext.IndexCount
                    : channelStreamingType == ChannelStreamingTypes.LatestValue
                        ? 1
                        : (int?)null;
            var increasing = primaryIndex.Direction == (int) IndexDirections.Increasing;
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

                //var isTimeIndex = primaryIndex.IndexType == ChannelIndexTypes.Time;
                var rangeSize = WitsmlSettings.GetRangeSize(isTimeIndex[0]);

                // Convert indexes from scaled values
                var minStartIndex = minStart?.IndexFromScale(primaryIndex.Scale, isTimeIndex[0]);
                var maxEndIndex = channelStreamingType == ChannelStreamingTypes.IndexValue
                    ? (increasing ? minStartIndex + rangeSize : minStartIndex - rangeSize)
                    : maxEnd?.IndexFromScale(primaryIndex.Scale, isTimeIndex[0]);

                // Get channel data
                var mnemonics = contextList.Select(c => c.ChannelMetadata.ChannelName).ToList();
                var dataProvider = GetDataProvider(parentUri);
                var optimiseStart = channelStreamingType == ChannelStreamingTypes.IndexValue;
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
                            c.ChannelMetadata.Status != (int) ChannelStatuses.Active && c.ChannelMetadata.EndIndex.HasValue &&
                            c.LastIndex >= c.ChannelMetadata.EndIndex.Value) ||

                            (c.ChannelStreamingType == ChannelStreamingTypes.RangeRequest &&
                            c.LastIndex >= c.EndIndex))
                    .ToArray();

                // Remove any contexts from the list that have completed returning all data
                completedContexts.ForEach(c =>
                {
                    // Notify consumer if the ReceiveChangeNotification field is true
                    if (c.ChannelMetadata.Status != (int) ChannelStatuses.Active && c.ReceiveChangeNotification)
                    {
                        // TODO: Decide which message shoud be sent...
                        // ChannelStatusChange(c.ChannelId, c.ChannelMetadata.Status);
                        // ChannelRemove(c.ChannelId);
                    }

                   contextList.Remove(c);
                });

                // Delay to prevent CPU overhead
                await Task.Delay(WitsmlSettings.StreamChannelDataDelayMilliseconds, token);
            }
        }

        /// <summary>
        /// Streams the channel data.
        /// </summary>
        /// <param name="contextList">The context list.</param>
        /// <param name="channelData">The channel data.</param>
        /// <param name="mnemonics">The mnemonics.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <param name="isTimeIndex">Index of the is time.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="firstStart">if set to <c>true</c> [first start].</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        protected async Task StreamChannelData(IList<ChannelStreamingContext> contextList, List<List<List<object>>> channelData, string[] mnemonics, bool increasing, bool[] isTimeIndex, int scale, bool firstStart, CancellationToken token)
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

        /// <summary>
        /// Creates the data items.
        /// </summary>
        /// <param name="contextList">The context list.</param>
        /// <param name="record">The record.</param>
        /// <param name="mnemonics">The mnemonics.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <param name="isTimeIndex">Index of the is time.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="firstStart">if set to <c>true</c> [first start].</param>
        /// <returns></returns>
        protected IEnumerable<DataItem> CreateDataItems(IList<ChannelStreamingContext> contextList, List<List<object>> record, string[] mnemonics, bool increasing, bool[] isTimeIndex, int scale, bool firstStart)
        {
            // Get the index and value components for the current record
            var indexes = record[0];
            var values = record[1];

            // Create a list of all of the records indexes, scaled
            var indexValues = new List<long>();
            for (var i = 0; i < indexes.Count; i++)
            {
                var indexValue = isTimeIndex[i]
                    ? DateTimeOffset.Parse(indexes[i].ToString()).ToUnixTimeMicroseconds()
                    : ((double)indexes[i]).IndexToScale(scale);
                indexValues.Add(indexValue);
            }
            //indexes.ForEach(x =>
            //{
            //    var indexValue = isTimeIndex
            //        ? new DateTimeOffset(DateTime.Parse(x.ToString())).ToUnixTimeMicroseconds()
            //        : ((double)x).IndexToScale(scale);
            //    indexValues.Add(indexValue);
            //});

            var primaryIndexValue = indexValues[0];

            // For each channel
            foreach (var context in contextList)
            {
                // Get the channel from the context.
                var channel = context.ChannelMetadata;

                // create range for requested range.
                var range = new Range<double?>(Convert.ToDouble(context.StartIndex), Convert.ToDouble(context.EndIndex));

                // If we have an established Start and it starts after the current primaryIndexValue then skip this value.
                if (context.StartIndex.HasValue && range.StartsAfter(primaryIndexValue, increasing))
                    continue;

                // If we have an established End and it ends before the current primaryIndexValue then skip this value.
                if (context.EndIndex.HasValue && range.EndsBefore(primaryIndexValue, increasing))
                    continue;

                // Create a range for the current start index.
                var start = new Range<double?>(Convert.ToDouble(context.LastIndex), null);

                // If we have an established Start and it starts after the current primaryIndexValue then skip this value.
                if (context.LastIndex.HasValue && start.StartsAfter(primaryIndexValue, increasing, inclusive: !firstStart))
                    continue;

                // Update the current StartIndex for our context with the current index value
                context.LastIndex = primaryIndexValue;

                // If the data does not include values for the channel we're streaming, then skip
                if (!mnemonics.Contains(channel.ChannelName))
                    continue;

                var attributes = new List<object>();
                var value = FormatValue(values[Array.IndexOf(mnemonics, channel.ChannelName)], attributes);

                // Filter null or empty data values
                if (value == null || string.IsNullOrWhiteSpace($"{value}"))
                    continue;

                // Create and return a DataItem.
                yield return new DataItem
                {
                    ChannelId = context.ChannelId,
                    Indexes = indexValues.ToArray(),
                    ValueAttributes = attributes
                        .Select((x, i) => new DataAttribute
                        {
                            AttributeId = i,
                            AttributeValue = new DataValue
                            {
                                Item = x
                            }
                        })
                        .ToArray(),
                    Value = new DataValue()
                    {
                        Item = value
                    }
                };
            }
        }

        /// <summary>
        /// Sends the channel data.
        /// </summary>
        /// <param name="dataItemList">The data item list.</param>
        /// <param name="messageFlag">The message flag.</param>
        /// <returns></returns>
        protected async Task SendChannelData(List<DataItem> dataItemList, MessageFlags messageFlag = MessageFlags.MultiPart)
        {
            ChannelData(Request, dataItemList, messageFlag);
            await Task.Delay(MaxMessageRate);
        }

        /// <summary>
        /// Formats the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns></returns>
        protected object FormatValue(object value, List<object> attributes)
        {
            value = ChannelDataReader.ReadValue(value);
            var data = value as object[];

            if (data == null)
            {
                return FormatValue(value);
            }

            // Separate PointMetadata values
            attributes.AddRange(data.Skip(1).Select(FormatValue));

            return FormatValue(data.FirstOrDefault());
        }

        /// <summary>
        /// Formats the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        protected object FormatValue(object value)
        {
            if (value is DateTime)
            {
                return ((DateTime)value).ToString("o");
            }
            if (value is DateTimeOffset)
            {
                return ((DateTimeOffset)value).ToString("o");
            }

            return value;
        }

        /// <summary>
        /// Determines whether [is streaming stopped] [the specified context list].
        /// </summary>
        /// <param name="contextList">The context list.</param>
        /// <param name="token">The token.</param>
        /// <returns>
        ///   <c>true</c> if [is streaming stopped] [the specified context list]; otherwise, <c>false</c>.
        /// </returns>
        protected static bool IsStreamingStopped(IList<ChannelStreamingContext> contextList, ref CancellationToken token)
        {
            return token.IsCancellationRequested || contextList.Count == 0;
        }


        /// <summary>
        /// Validates the channel range request.
        /// </summary>
        /// <param name="infos">The infos.</param>
        protected void ValidateChannelRangeInfos(IList<ChannelRangeInfo> infos)
        {
            // Remove invalid channel range infos from streaming context
            infos
                .Select((info, i) => new
                {
                    Channels = info.ChannelId
                        .Where(channelId => Channels.ContainsKey(channelId))
                        .Select(channelId => Channels[channelId].Item2),
                    Info = info,
                    Index = i
                })
                .Where(x =>
                    x.Channels.Any(channel =>
                        ValidateRangeRequestIndexes(x.Info.StartIndex, x.Info.EndIndex,
                            channel.Indexes.Cast<IIndexMetadataRecord>().First().Direction == (int) IndexDirections.Increasing))
                )
                .Select(x => x.Index)
                .Reverse()
                .ForEach(infos.RemoveAt);
        }

        /// <summary>
        /// Validates the range request indexes.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <returns></returns>
        protected bool ValidateRangeRequestIndexes(long startIndex, long endIndex, bool increasing)
        {
            if (increasing)
            {
                if (startIndex > endIndex)
                {
                    this.InvalidArgument("startIndex > endIndex", Request.MessageId);
                    return true;
                }
            }
            else
            {
                if (startIndex < endIndex)
                {
                    this.InvalidArgument("startIndex < endIndex", Request.MessageId);
                    return true;
                }
            }
            return false;
        }
    }
}
