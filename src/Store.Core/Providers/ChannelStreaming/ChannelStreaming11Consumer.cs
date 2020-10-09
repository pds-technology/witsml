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
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;
using Energistics.Etp.v11.Datatypes.ChannelData;
using Energistics.Etp.v11.Protocol.ChannelStreaming;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data.Channels;
using PDS.WITSMLstudio.Store.Properties;

namespace PDS.WITSMLstudio.Store.Providers.ChannelStreaming
{
    /// <summary>
    /// Consumer class for channel streaming
    /// </summary>
    /// <seealso cref="ChannelStreamingConsumerHandler" />
    [Export(typeof(IChannelStreamingConsumer))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ChannelStreaming11Consumer : ChannelStreamingConsumerHandler, IStreamingConsumer
    {
        private static readonly int _maxMessageRate = Settings.Default.MaxMessageRate;

        private readonly IContainer _container;
        private readonly IDictionary<EtpUri, ChannelDataBlock> _dataBlocks;
        private readonly IDictionary<long, EtpUri> _channelParentUris;
        private Timer _flushIntervalTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelStreaming11Consumer"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        [ImportingConstructor]
        public ChannelStreaming11Consumer(IContainer container)
        {
            _container = container;
            _dataBlocks = new Dictionary<EtpUri, ChannelDataBlock>();
            _channelParentUris = new Dictionary<long, EtpUri>();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is simple streamer.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is simple streamer; otherwise, <c>false</c>.
        /// </value>
        public bool IsSimpleStreamer { get; set; }

        /// <summary>
        /// Gets or sets the basic streamer uris.
        /// </summary>
        public IList<string> BasicStreamerUris { get; set; }

        /// <summary>
        /// Called when the ETP session is opened.
        /// </summary>
        /// <param name="requestedProtocols">The requested protocols.</param>
        /// <param name="supportedProtocols">The supported protocols.</param>
        public override void OnSessionOpened(IList<ISupportedProtocol> requestedProtocols, IList<ISupportedProtocol> supportedProtocols)
        {
            // Is the client requesting the ChannelStreaming consumer role
            if (!supportedProtocols.Contains(Protocol, Role)) return;

            // Check the protocol capabilities for the SimpleStreamer flag
            IsSimpleStreamer = requestedProtocols.IsSimpleStreamer() || supportedProtocols.IsSimpleStreamer();

            Start(maxMessageRate: _maxMessageRate);

            // Do not send ChannelDescribe to a SimpleStreamer
            if (IsSimpleStreamer) return;

            if (!string.IsNullOrEmpty(WitsmlSettings.DefaultDescribeUri))
                ChannelDescribe(new[] { WitsmlSettings.DefaultDescribeUri });

            if (BasicStreamerUris?.Any() ?? false)
                ChannelDescribe(BasicStreamerUris);
        }

        /// <summary>
        /// Initializes the channel streaming consumer as a basic streamer.
        /// </summary>
        /// <param name="uris">The uris to stream.</param>
        public void InitializeBasicStreamer(IList<string> uris)
        {
            BasicStreamerUris = uris?.ToList();
        }

        /// <summary>
        /// Gets the channel metadata record for the specified channelId
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <returns>
        /// An <see cref="T:Energistics.Etp.Common.Datatypes.ChannelData.IChannelMetadataRecord" /> for the specified channelId
        /// </returns>
        public IChannelMetadataRecord GetChannelMetadataRecord(long channelId)
        {
            return ChannelMetadataRecords.FirstOrDefault(x => x.ChannelId == channelId);
        }

        /// <summary>
        /// Updates the channel data for the specified URI using the supplied channel data reader.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="reader">The channel data reader.</param>
        protected virtual void UpdateChannelData(EtpUri uri, ChannelDataReader reader)
        {
            var dataProvider = _container.Resolve<IChannelDataProvider>(new ObjectName(uri.ObjectType, uri.Family, uri.Version));
            dataProvider.UpdateChannelData(uri, reader);
        }

        /// <summary>
        /// Handles the channel metadata.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="channelMetadata">The channel metadata.</param>
        protected override void HandleChannelMetadata(IMessageHeader header, ChannelMetadata channelMetadata)
        {
            // Base implementation caches ChannelMetadataRecord items sent from the producer
            base.HandleChannelMetadata(header, channelMetadata);

            // Remove invalid channels
            EvaluateChannelMetadata(header.MessageId, channelMetadata);

            // Ensure there are still channels to stream
            if (channelMetadata.Channels.Count < 1)
                return;

            InitializeDataBlocks(header.MessageId, channelMetadata.Channels);

            // Do not send ChannelStreamingStart to a SimpleStreamer
            if (IsSimpleStreamer) return;

            var infos = channelMetadata.Channels
                .Select(ToChannelStreamingInfo)
                .ToList();

            // Send ChannelStreamingStart message
            ChannelStreamingStart(infos);
        }

        /// <summary>
        /// Evaluates the channel metadata.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="channelMetadata">The channel metadata.</param>
        protected virtual void EvaluateChannelMetadata(long messageId, ChannelMetadata channelMetadata)
        {
            var channelsToBeStopped = new List<ChannelMetadataRecord>();
            var channelIndex = new List<int>();

            for (var i = 0; i < channelMetadata.Channels.Count; i++)
            {
                var channel = channelMetadata.Channels[i];
                var uri = new EtpUri(channel.ChannelUri);

                // Ensure that all parent UIDs are populated
                foreach (var segment in uri.GetObjectIds())
                {
                    if (string.IsNullOrWhiteSpace(segment.ObjectId))
                    {
                        this.InvalidUri($"Channel {channel.ChannelName}({channel.ChannelId}) is missing the objectId of a parent.", messageId);
                        channelsToBeStopped.Add(channel);
                        channelIndex.Add(i);
                    }
                }
            }

            // Do not send ChannelStreamingStop to a SimpleStreamer
            if (!IsSimpleStreamer)
            {
                // Notify producer to stop streaming the channels
                ChannelStreamingStop(channelsToBeStopped.Select(x => x.ChannelId).ToList());
            }

            // Remove the channels from the metadata
            channelIndex.Reverse();
            channelIndex.ForEach(x => channelMetadata.Channels.RemoveAt(x));
        }

        /// <summary>
        /// Handles the channel data.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="channelData">The channel data.</param>
        protected override void HandleChannelData(IMessageHeader header, ChannelData channelData)
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
        /// Appends the channel data to the appropriate data block.
        /// </summary>
        /// <param name="data">The data items.</param>
        protected virtual void AppendChannelData(IList<DataItem> data)
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

        /// <summary>
        /// Downscales the index values.
        /// </summary>
        /// <param name="indexMetadata">The index metadata.</param>
        /// <param name="indexValues">The index values.</param>
        /// <returns>The downscaled index values.</returns>
        protected virtual IList<object> DownscaleIndexValues(IList<IndexMetadataRecord> indexMetadata, IList<long> indexValues)
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

        /// <summary>
        /// Initializes the data blocks.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="channels">The channel metadata.</param>
        protected virtual void InitializeDataBlocks(long messageId, IList<ChannelMetadataRecord> channels)
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
                        "long",
                        index.Direction == IndexDirections.Increasing,
                        index.IndexType == ChannelIndexTypes.Time);
                }

                dataBlock.AddChannel(channel.ChannelId, channel.ChannelName, channel.Uom, channel.DataType);
            }
        }

        /// <summary>
        /// Processes the data blocks.
        /// </summary>
        /// <param name="flush">if set to <c>true</c> flush immediately.</param>
        protected virtual void ProcessDataBlocks(bool flush = false)
        {
            foreach (var item in _dataBlocks)
            {
                if (flush || item.Value.Count() >= ChannelDataBlock.BatchSize)
                    ProcessDataBlock(item.Key, item.Value);
            }
        }

        /// <summary>
        /// Flushes the data blocks.
        /// </summary>
        /// <param name="state">The state.</param>
        protected virtual void FlushDataBlocks(object state)
        {
            ProcessDataBlocks(true);
        }

        /// <summary>
        /// Processes the data block.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="dataBlock">The data block.</param>
        protected virtual void ProcessDataBlock(EtpUri uri, ChannelDataBlock dataBlock)
        {
            var reader = dataBlock.GetReader();
            dataBlock.Clear();

            UpdateChannelData(uri, reader);
        }

        /// <summary>
        /// Creates the channel streaming information for the specified channel metadata.
        /// </summary>
        /// <param name="channel">The channel metadata.</param>
        /// <returns>A new <see cref="ChannelStreamingInfo"/> instance.</returns>
        protected virtual ChannelStreamingInfo ToChannelStreamingInfo(ChannelMetadataRecord channel)
        {
            return new ChannelStreamingInfo
            {
                ChannelId = channel.ChannelId,
                ReceiveChangeNotification = false,
                StartIndex = new StreamingStartIndex
                {
                    // "null" indicates a request for the latest value
                    Item = null
                }
            };
        }
    }
}
