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
using Energistics;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Core;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Data.Channels;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Providers.ChannelStreaming
{
    /// <summary>
    /// Consumer class for channel streaming
    /// </summary>
    /// <seealso cref="Energistics.Protocol.ChannelStreaming.ChannelStreamingConsumerHandler" />
    [Export(typeof(IChannelStreamingConsumer))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ChannelStreamingConsumer : ChannelStreamingConsumerHandler
    {
        private static readonly int _maxMessageRate = Settings.Default.MaxMessageRate;

        private readonly IContainer _container;
        private readonly IDictionary<EtpUri, ChannelDataBlock> _dataBlocks;
        private readonly IDictionary<long, EtpUri> _channelParentUris;
        private Timer _flushIntervalTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelStreamingConsumer"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        [ImportingConstructor]
        public ChannelStreamingConsumer(IContainer container)
        {
            _container = container;
            _dataBlocks = new Dictionary<EtpUri, ChannelDataBlock>();
            _channelParentUris = new Dictionary<long, EtpUri>();
        }

        /// <summary>
        /// Called when the ETP session is opened.
        /// </summary>
        /// <param name="requestedProtocols">The requested protocols.</param>
        /// <param name="supportedProtocols">The supported protocols.</param>
        public override void OnSessionOpened(IList<SupportedProtocol> requestedProtocols, IList<SupportedProtocol> supportedProtocols)
        {
            // Is the client requesting the ChannelStreaming consumer role
            if (!supportedProtocols.Contains(Protocol, Role)) return;
            Start(maxMessageRate: _maxMessageRate);

            if (!requestedProtocols.IsSimpleStreamer())
            {
                ChannelDescribe(new[] { EtpUri.RootUri });
            }
        }

        /// <summary>
        /// Handles the channel metadata.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="channelMetadata">The channel metadata.</param>
        protected override void HandleChannelMetadata(MessageHeader header, ChannelMetadata channelMetadata)
        {
            // Base implementation caches ChannelMetadataRecord items sent from the producer
            base.HandleChannelMetadata(header, channelMetadata);

            // Remove invalid channels
            EvaluateChannelMetadata(header.MessageId, channelMetadata);

            // Ensure there are still channels to stream
            if (channelMetadata.Channels.Count < 1)
                return;

            InitializeDataBlocks(header.MessageId, channelMetadata.Channels);

            var infos = channelMetadata.Channels
                .Select(ToChannelStreamingInfo)
                .ToList();

            // Send ChannelStreamingStart message
            ChannelStreamingStart(infos);
        }

        private void EvaluateChannelMetadata(long messageId, ChannelMetadata channelMetadata)
        {
            var channelsToBeStopped = new List<ChannelMetadataRecord>();
            var channelIndex = new List<int>();

            for (int i = 0; i < channelMetadata.Channels.Count; i++)
            {
                var channel = channelMetadata.Channels[i];
                var uri = new EtpUri(channel.ChannelUri);

                // Ensure that all parent UIDs are populated
                foreach (var objectId in uri.GetObjectIds())
                {
                    if (string.IsNullOrWhiteSpace(objectId.ObjectId))
                    {
                        this.InvalidUri($"EINVALID_URI:  Channel {channel.ChannelName}({channel.ChannelId}) is missing the objectId of a parent.", messageId);
                        channelsToBeStopped.Add(channel);
                        channelIndex.Add(i);
                    }
                }
            }

            // Notify producer to stop streaming the channels
            ChannelStreamingStop(channelsToBeStopped.Select(x => x.ChannelId).ToList());

            // Remove the channels from the metadata
            channelIndex.Reverse();
            channelIndex.ForEach(x => channelMetadata.Channels.RemoveAt(x));
        }

        /// <summary>
        /// Handles the channel data.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="channelData">The channel data.</param>
        protected override void HandleChannelData(MessageHeader header, ChannelData channelData)
        {
            base.HandleChannelData(header, channelData);

            if (!channelData.Data.Any())
                return;

            // Reset timer
            _flushIntervalTimer?.Dispose();
            _flushIntervalTimer = new Timer(FlushDataBlocks, null, ChannelDataBlock.BlockFlushRateInMilliseconds, 0);

            AppendChannelData(channelData.Data);
            ProcessDataBlocks();
        }

        /// <summary>
        /// Handles the channel streaming stop.
        /// </summary>
        /// <param name="channelIds">The  list of channel ids to be stopped.</param>
        protected void ChannelStreamingStop(List<long> channelIds)
        {
            base.ChannelStreamingStop(channelIds);
        }

        private void AppendChannelData(IList<DataItem> data)
        {
            foreach (var dataItem in data)
            {
                // Check to see if we are accepting data for this channel
                if (!_channelParentUris.ContainsKey(dataItem.ChannelId))
                    continue;

                var parentUri = _channelParentUris[dataItem.ChannelId];

                var dataBlock = _dataBlocks[parentUri];

                var channel = ChannelMetadataRecords.FirstOrDefault(x => x.ChannelId == dataItem.ChannelId);
                if (channel == null) continue;

                var indexes = DownscaleIndexValues(channel.Indexes, dataItem.Indexes);
                dataBlock.Append(dataItem.ChannelId, indexes, dataItem.Value.Item);
            }
        }

        private IList<object> DownscaleIndexValues(IList<IndexMetadataRecord> indexMetadata, IList<long> indexValues)
        {
            return indexValues
                .Select((x, i) =>
                {
                    var index = indexMetadata[i];
                    return index.IndexType == ChannelIndexTypes.Depth
                        ? (object)(indexValues[i] / Math.Pow(10, index.Scale))
                        : DateTimeExtensions.FromUnixTimeMicroseconds(indexValues[i]);
                })
                .ToList();
        }

        private void InitializeDataBlocks(long messageId, IList<ChannelMetadataRecord> channels)
        {
            foreach (var channel in channels)
            {
                var uri = new EtpUri(channel.ChannelUri);

                var parentUri = uri.Parent; // Log or ChannelSet

                if (!_dataBlocks.ContainsKey(parentUri))
                    _dataBlocks[parentUri] = new ChannelDataBlock(parentUri);

                var dataBlock = _dataBlocks[parentUri];
                _channelParentUris[channel.ChannelId] = parentUri;

                foreach (var index in channel.Indexes)
                {
                    dataBlock.AddIndex(
                        index.Mnemonic,
                        index.Uom,
                        index.Direction == IndexDirections.Increasing,
                        index.IndexType == ChannelIndexTypes.Time);
                }

                dataBlock.AddChannel(channel.ChannelId, channel.ChannelName, channel.Uom);
            }
        }

        private void ProcessDataBlocks(bool flush = false)
        {
            foreach (var item in _dataBlocks)
            {
                if (flush || item.Value.Count() >= ChannelDataBlock.BatchSize)
                    ProcessDataBlock(item.Key, item.Value);
            }
        }

        private void FlushDataBlocks(object state)
        {
            ProcessDataBlocks(true);
        }

        private void ProcessDataBlock(EtpUri uri, ChannelDataBlock dataBlock)
        {
            var reader = dataBlock.GetReader();
            dataBlock.Clear();

            var dataProvider = _container.Resolve<IChannelDataProvider>(new ObjectName(uri.ObjectType, uri.Version));
            dataProvider.UpdateChannelData(uri, reader);
        }

        private ChannelStreamingInfo ToChannelStreamingInfo(ChannelMetadataRecord record)
        {
            return new ChannelStreamingInfo()
            {
                ChannelId = record.ChannelId,
                ReceiveChangeNotification = false,
                StartIndex = new StreamingStartIndex()
                {
                    // "null" indicates a request for the latest value
                    Item = null
                }
            };
        }
    }
}
