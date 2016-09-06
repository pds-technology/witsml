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

        private Dictionary<string, IChannelDataProvider> _providers;

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
            // TODO: Add suport for higher level uris.  
            // TODO: ... Breakdown the higher level uris to return the uri of a channel parent.
            // TODO: ... then loop through the parent uris

            var uris = args.Message.Uris.Select(x => new EtpUri(x)).ToList();

            var uriDictionary = OptimizeDescribeUris(uris);

            foreach (var pair in uriDictionary)
            {
                var provider = _providers[pair.Key];
                var metadata = provider.GetChannelsMetadata(pair.Value);

                metadata.ForEach(m =>
                {
                    // Check by uri if we have the metadata in our dictionary.
                    var channelMetadataRecord =
                        Channels.Values.Select(c => c.Item2).FirstOrDefault(c => c.ChannelUri == m.ChannelUri);

                    // if not add it and set its channelId
                    if (channelMetadataRecord == null)
                    {
                        m.ChannelId = Channels.Keys.Count;
                        Channels.Add(m.ChannelId, new Tuple<EtpUri, ChannelMetadataRecord>(new EtpUri(m.ChannelUri), m));
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
            while (!IsStreamingStopped(contextList, ref token))
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
                var dataProvider = GetDataProvider(parentUri);
                var channelData = dataProvider.GetChannelData(parentUri, new Range<double?>(minStartIndex, maxEndIndex));

                // TODO: Bring back later
                // Stream Channel Data with IndexedDataItems if StreamIndexValuePairs setting is true
                //if (WitsmlSettings.StreamIndexValuePairs)
                //{
                //    await StreamIndexedChannelData(infos, channels, channelData, increasing, token);
                //}
                //else
                {
                    await StreamChannelData(contextList, channelData, increasing, token);
                }

                // Check each context to see of all the data has streamed.
                var completedContexts = contextList
                    .Where(
                        c =>
                            c.ChannelStreamingType != ChannelStreamingTypes.IndexValue ||
                            (c.ChannelMetadata.Status != ChannelStatuses.Active && c.ChannelMetadata.EndIndex.HasValue &&
                            c.StartIndex >= c.ChannelMetadata.EndIndex.Value))
                    .ToArray();


                // Remove any contexts from the list that have completed returning all data
                completedContexts
                    .ForEach(c => contextList.Remove(c));
            }
        }

        private async Task StreamChannelData(IList<ChannelStreamingContext> contextList, IEnumerable<IChannelDataRecord> channelData, bool increasing, CancellationToken token)
        {
            var dataItemList = new List<DataItem>();
            var firstContext = contextList.FirstOrDefault();

            // Streaming could have been stopped for all channels in the list.
            if (firstContext == null)
                return;

            var indexes = firstContext.ChannelMetadata.Indexes;

            using (var channelDataEnum = channelData.GetEnumerator())
            {
                var endOfChannelData = !channelDataEnum.MoveNext();

                while (!endOfChannelData)
                {
                    foreach (var dataItem in CreateDataItems(contextList, indexes, channelDataEnum.Current, increasing))
                    {
                        if (IsStreamingStopped(contextList, ref token))
                            break;

                        if (dataItemList.Count >= MaxDataItems)
                        {
                            await SendChannelData(dataItemList);
                            dataItemList.Clear();
                        }

                        if (IsStreamingStopped(contextList, ref token))
                            break;

                        dataItemList.Add(dataItem);
                    }
                    endOfChannelData = !channelDataEnum.MoveNext();

                    if (IsStreamingStopped(contextList, ref token))
                        break;
                }

                if (dataItemList.Any())
                {
                    await SendChannelData(dataItemList);
                }
            }
        }

        private static bool IsStreamingStopped(IList<ChannelStreamingContext> contextList, ref CancellationToken token)
        {
            return token.IsCancellationRequested || contextList.Count == 0;
        }

        private IEnumerable<DataItem> CreateDataItems(IList<ChannelStreamingContext> contextList, IList<IndexMetadataRecord> indexes, IChannelDataRecord record, bool increasing)
        {
            // Get primary index info and scaled value for the current record

            var primaryIndex = indexes.First();
            var isTimeIndex = primaryIndex.IndexType == ChannelIndexTypes.Time;
            var primaryIndexValue = record.GetIndexValue().IndexToScale(primaryIndex.Scale, isTimeIndex);

            var indexValues = new List<long>();
            var i = 0;
            indexes.ForEach(x =>
            {
                indexValues.Add((long)record.GetIndexValue(i++, x.Scale));
            });

            foreach (var context in contextList)
            {

                var channel = context.ChannelMetadata;
                // Verify channel is included in the channelRangeInfo
                //if (channel == null) continue;

                var start = new Range<double?>(Convert.ToDouble(context.StartIndex), null);

                // Handle decreasing data
                if (start.StartsAfter(primaryIndexValue, increasing, inclusive: true))
                    continue;

                // Update ChannelStreamingInfo index value
                context.StartIndex = primaryIndexValue;

                var value = FormatValue(record.GetValue(record.GetOrdinal(channel.ChannelName)));

                // Filter null or empty data values
                if (value == null || string.IsNullOrWhiteSpace($"{value}"))
                    continue;

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
            _providers = new Dictionary<string, IChannelDataProvider>
            {
                {OptionsIn.DataVersion.Version131.Value, _container.Resolve<IChannelDataProvider>(ObjectNames.Log131)},
                {OptionsIn.DataVersion.Version141.Value, _container.Resolve<IChannelDataProvider>(ObjectNames.Log141)},
                {OptionsIn.DataVersion.Version200.Value, _container.Resolve<IChannelDataProvider>(ObjectNames.Log200)}
            };
        }

        /// <summary>
        /// Optimizes the describe uris. Mainly remove child URI if its parent/ancestor is already included.
        /// </summary>
        /// <param name="uris">The requested URI list.</param>
        /// <returns>The optimized list of URIs.</returns>
        private Dictionary<string, List<EtpUri>> OptimizeDescribeUris(List<EtpUri> uris)
        {
            var urisDictionary = new Dictionary<string, List<EtpUri>>();
            var uris13 = new List<EtpUri>();
            var uris14 = new List<EtpUri>();
            var uris20 = new List<EtpUri>();

            if (uris.Any(u => EtpUri.IsRoot(u)))
            {
                urisDictionary.Add(OptionsIn.DataVersion.Version131.Value, uris13);
                urisDictionary.Add(OptionsIn.DataVersion.Version141.Value, uris14);
                urisDictionary.Add(OptionsIn.DataVersion.Version200.Value, uris20);
            }
            else
            {
                var versions = new List<string>();
                if (uris.Any(u => u.Equals(new EtpUri("eml://witsml13/"))))
                    urisDictionary.Add(OptionsIn.DataVersion.Version131.Value, uris13);
                else
                    versions.Add(OptionsIn.DataVersion.Version131.Value);
                if (uris.Any(u => u.Equals(new EtpUri("eml://witsml14/"))))
                    urisDictionary.Add(OptionsIn.DataVersion.Version141.Value, uris14);
                else
                    versions.Add(OptionsIn.DataVersion.Version141.Value);
                if (uris.Any(u => u.Equals(new EtpUri("eml://witsml120/"))))
                    urisDictionary.Add(OptionsIn.DataVersion.Version200.Value, uris20);
                else
                    versions.Add(OptionsIn.DataVersion.Version200.Value);

                foreach (var version in versions)
                {
                    foreach (var uri in GetObjectUris(uris, version, ObjectTypes.Well))
                        AddUriToDictionary(urisDictionary, uri, version, uri.ObjectType);

                    foreach (var uri in GetObjectUris(uris, version, ObjectTypes.Wellbore))
                        AddUriToDictionary(urisDictionary, uri, version, uri.ObjectType);

                    foreach (var uri in GetObjectUris(uris, version, ObjectTypes.Log))
                        AddUriToDictionary(urisDictionary, uri, version, uri.ObjectType);

                    foreach (var uri in GetObjectUris(uris, version, ObjectTypes.LogCurveInfo))
                        AddUriToDictionary(urisDictionary, uri, version, uri.ObjectType);

                    if (version != OptionsIn.DataVersion.Version200.Value)
                        continue;

                    foreach (var uri in GetObjectUris(uris, version, ObjectTypes.ChannelSet))
                        AddUriToDictionary(urisDictionary, uri, version, uri.ObjectType);
                }
            }

            return urisDictionary;
        }

        private List<EtpUri> GetObjectUris(List<EtpUri> uris, string version, string objectType)
        {
            return uris.Where(u => u.Version == version && u.ObjectType == objectType).ToList();
        }

        private void AddUriToDictionary(Dictionary<string, List<EtpUri>> uriDictionary, EtpUri uri, string version, string objectType)
        {
            List<EtpUri> uris;
            if (!uriDictionary.ContainsKey(version))
            {
                uris = new List<EtpUri> {uri};
                uriDictionary.Add(version, uris);
                return;
            }

            uris = uriDictionary[version];

            switch (objectType)
            {
                case ObjectTypes.Well:
                    uris.Add(uri);
                    RemoveChildUris(uris, uri, uri.ObjectType);
                    return;
                case ObjectTypes.Wellbore:
                    if (!uris.Contains(uri.Parent))
                    {
                        uris.Add(uri);
                        RemoveChildUris(uris, uri, uri.ObjectType);
                    }
                    break;
                case ObjectTypes.Log:
                    if (!uris.Contains(uri.Parent.Parent) && !uris.Contains(uri.Parent))
                    {
                        uris.Add(uri);
                        RemoveChildUris(uris, uri, uri.ObjectType);
                    }
                    break;
                case ObjectTypes.LogCurveInfo:
                    if (!uris.Contains(uri.Parent.Parent.Parent) && !uris.Contains(uri.Parent.Parent) &&
                        !uris.Contains(uri.Parent))
                    {
                        uris.Add(uri);
                        RemoveChildUris(uris, uri, uri.ObjectType);
                    }
                    break;
            }
        }

        private void RemoveChildUris(List<EtpUri> uris, EtpUri uri, string objectType)
        {
            switch (objectType)
            {
                case ObjectTypes.Well:
                    uris.RemoveAll(u => u.ObjectType == ObjectTypes.Wellbore && u.Parent == uri
                                        || u.ObjectType == ObjectTypes.Log && u.Parent.Parent == uri
                                        || u.ObjectType == ObjectTypes.LogCurveInfo && u.Parent.Parent.Parent == uri);
                    break;
                case ObjectTypes.Wellbore:
                    uris.RemoveAll(u => u.ObjectType == ObjectTypes.Log && u.Parent == uri
                                        || u.ObjectType == ObjectTypes.LogCurveInfo && u.Parent.Parent == uri);
                    break;
                case ObjectTypes.Log:
                    uris.RemoveAll(u => u.ObjectType == ObjectTypes.LogCurveInfo && u.Parent == uri);
                    break;
            }
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
