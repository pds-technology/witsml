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
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data.Channels;

namespace PDS.Witsml.Server.Providers.ChannelStreaming
{
    /// <summary>
    /// Producer class for channel streaming
    /// </summary>
    /// <seealso cref="Energistics.Protocol.ChannelStreaming.ChannelStreamingProducerHandler" />
    //[Export(typeof(IChannelStreamingProducer))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ChannelStreaming_Producer : ChannelStreamingProducerHandler
    {
        private static readonly string[] ParentTypes = new[] { ObjectTypes.Log, ObjectTypes.ChannelSet };
        private static readonly string[] ChannelTypes = new[] { ObjectTypes.LogCurveInfo, ObjectTypes.Channel };
        private static readonly string[] SupportedTypes = ParentTypes.Concat(ChannelTypes).ToArray();

        // TODO: Move to an enum (EtpErrorCodes)
        private static readonly int EINVALID_STATE_CODE = 8;

        private readonly IContainer _container;
        private CancellationTokenSource _tokenSource;
        private readonly List<IList<ChannelStreamingContext>> _channelStreamingContextLists;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelStreamingProducer"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        [ImportingConstructor]
        public ChannelStreaming_Producer(IContainer container)
        {
            _container = container;
            Channels = new Dictionary<long, Tuple<EtpUri, ChannelMetadataRecord>>();
            _channelStreamingContextLists = new List<IList<ChannelStreamingContext>>();
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
            // TODO: Add suport for higher level uris.  
            // TODO: ... Breakdown the higher level uris to return the uri of a channel parent.
            // TODO: ... then loop through the parent uris

            foreach (var uri in args.Message.Uris.Select(x => new EtpUri(x)))
            {
                if (!SupportedTypes.ContainsIgnoreCase(uri.ObjectType))
                    continue;

                var isChannel = ChannelTypes.ContainsIgnoreCase(uri.ObjectType);
                var parentUri = isChannel ? uri.Parent : uri;

                var dataProvider = GetDataProvider(parentUri);
                var metadata = dataProvider.GetChannelMetadata(parentUri)
                    .Where(x => !isChannel || x.ChannelUri == uri)
                    .ToList();

                metadata.ForEach(m =>
                {
                    // Check by uri if we have the metadata in our dictionary.
                    var channelMetadataRecord =
                        Channels.Values.Select(c => c.Item2).FirstOrDefault(c => c.ChannelUri == m.ChannelUri);

                    // if not add it and set its channelId
                    if (channelMetadataRecord == null)
                    {
                        m.ChannelId = Channels.Keys.Count;
                        Channels.Add(m.ChannelId, new Tuple<EtpUri, ChannelMetadataRecord>(parentUri, m));
                        channelMetadataRecord = m;
                    }
                    args.Context.Add(channelMetadataRecord);
                });
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

            Logger.Debug("Channel Streaming starting.");
            Task.Run(() => StartChannelStreaming(header.MessageId, channelStreamingStart.Channels, token), token);
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

            StopStreamingChannels(channelStreamingStop.Channels);

            base.HandleChannelStreamingStop(header, channelStreamingStop);

            //_tokenSource?.Cancel();
            //_tokenSource = null;
        }

        private void StopStreamingChannels(IList<long> channels)
        {
            foreach (var channel in channels)
            {
                // Find the ChannelStreamingInfo list that contains the channelId
                var streamingInfoList = _channelStreamingContextLists.FirstOrDefault(list => list.Any(l => l.ChannelId == channel));

                if (streamingInfoList != null)
                {
                    // Find the info for the channelId to remove
                    var infoToRemove = streamingInfoList.First(l => l.ChannelId == channel);

                    // Remove it from its list.
                    streamingInfoList.Remove(infoToRemove);

                    // if ChannelStreamingInfo record used to start this channel is set to True...
                    //if (infoToRemove.ReceiveChangeNotification)
                    // TODO: Send ChannelStatusChanged if the receiveChangeNotification field 
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

            // Group the ChannelStreamingContext list by ChannelStreamingType and ParentUri
            var streamingContextGrouping =
                from x in streamingContextList
                group x by new { x.ChannelStreamingType, x.ParentUri };

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
            var streamingChannelIds = GetStreamingChannelIds(channelIds);

            // Send a EINVALID_STATE message if any are already streaming.
            if (streamingChannelIds == null || streamingChannelIds.Length > 0)
                SendInvalidStateMessage(messageId, streamingChannelIds);

            // Remove the channelIds that are already streaming and continue with the rest.
            streamingChannelIds.ForEach(s => channelIds.Remove(s));

            // Remove the infos for channels that are already streaming.
            var streamingInfos = infos.Where(i => streamingChannelIds.Contains(i.ChannelId));
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

        private void SendInvalidStateMessage(long messageId, long[] streamingChannelIds)
        {
            streamingChannelIds.ForEach(c => ProtocolException(EINVALID_STATE_CODE, $"EINVALID_STATE_CODE: Channel with channelId {c} is already streaming.", messageId));

            Logger.Warn($"Channels {string.Join(",", streamingChannelIds)} are already streaming.");
        }

        private long[] GetStreamingChannelIds(IEnumerable<long> channelIds)
        {
            return _channelStreamingContextLists
                .SelectMany(list => list
                    .Where(l => channelIds.Contains(l.ChannelId))
                    .Select(c => c.ChannelId))
                .ToArray();

        }

        private async Task StreamChannelData(IList<ChannelStreamingContext> contextList, CancellationToken token)
        {
            _channelStreamingContextLists.Add(contextList);

            // Loop until there is a cancellation or all channals have been removed
            while (!token.IsCancellationRequested && contextList.Count > 0)
            {
                var channelIds = contextList.Select(i => i.ChannelId).Distinct().ToArray();
                var firstContext = contextList.First();
                var channelStreamingType = firstContext.ChannelStreamingType;
                var parentUri = firstContext.ParentUri;
                var primaryIndex = firstContext.ChannelMetadata.Indexes[0];

                Logger.Debug($"Streaming data for parentUri {parentUri.Uri} and channelIds {string.Join(",", channelIds)}");

                // We only need a start index value for IndexValue and RangeRequest
                var minStart = 
                    channelStreamingType == ChannelStreamingTypes.IndexValue ||
                    channelStreamingType == ChannelStreamingTypes.RangeRequest
                        ? contextList.Min(x => Convert.ToInt64(x.StartIndex)) 
                        : (long?)null;

                // Only need and end index value for range request
                var maxEnd = channelStreamingType == ChannelStreamingTypes.RangeRequest
                    ? contextList.Max(x => Convert.ToInt64(x.EndIndex))
                    : (long?) null;

                var increasing = primaryIndex.Direction == IndexDirections.Increasing;
                var isTimeIndex = primaryIndex.IndexType == ChannelIndexTypes.Time;
                var rangeSize = WitsmlSettings.GetRangeSize(isTimeIndex);

                // Convert indexes from scaled values
                var minStartIndex = minStart?.IndexFromScale(primaryIndex.Scale, isTimeIndex);
                var maxEndIndex = channelStreamingType == ChannelStreamingTypes.IndexValue
                    ? (increasing ? minStartIndex + rangeSize : minStartIndex - rangeSize)
                    : maxEnd?.IndexFromScale(primaryIndex.Scale, isTimeIndex);
                

                // TODO: Add get of channel data when ready
                //var dataProvider = GetDataProvider(parentUri);
                //var channelData = dataProvider.GetChannelData(parentUri, new Range<double?>(minStartIndex, increasing ? minStartIndex + rangeSize : minStartIndex - rangeSize));

                // TODO: Bring back later
                // Stream Channel Data with IndexedDataItems if StreamIndexValuePairs setting is true
                //if (WitsmlSettings.StreamIndexValuePairs)
                //{
                //    await StreamIndexedChannelData(infos, channels, channelData, increasing, token);
                //}
                //else
                {
                    await StreamChannelData(contextList, null, increasing, token);
                }

                // TODO: Add logic to test if we can remove a channel from the contextList
                // TODO:... RangeRequest
                // TODO:... A Channel's Status != Active
                // TODO:... Channel's EndIndex has been reached
            }
        }

        private async Task StreamChannelData(IList<ChannelStreamingContext> contextList, IEnumerable<IChannelDataRecord> channelData, bool increasing, CancellationToken token)
        {
            var dataItemList = new List<DataItem>();

            // TODO: Change to loop while there is channelData...
            for (int i = 0; i < 15; i++) //while (true)
            {
                //Logger.Debug($"Streaming data for channel {firstChannel.ChannelName}");

                await Task.Delay(1000);

                if (token.IsCancellationRequested)
                    break;

                if (contextList.Count == 0)
                    break;

                if (dataItemList.Any())
                {
                    await SendChannelData(dataItemList);
                }
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
    }
}
