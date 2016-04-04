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
    [Export(typeof(IChannelStreamingConsumer))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ChannelStreamingConsumer : ChannelStreamingConsumerHandler
    {
        private static readonly int MaxMessageRate = Settings.Default.MaxMessageRate;

        private readonly IContainer _container;
        private readonly IDictionary<EtpUri, ChannelDataBlock> _dataBlocks;
        private readonly IDictionary<long, EtpUri> _channelParentUris;

        [ImportingConstructor]
        public ChannelStreamingConsumer(IContainer container)
        {
            _container = container;
            _dataBlocks = new Dictionary<EtpUri, ChannelDataBlock>();
            _channelParentUris = new Dictionary<long, EtpUri>();
        }

        public override void OnSessionOpened(IList<SupportedProtocol> supportedProtocols)
        {
            // Is the client requesting the ChannelStreaming consumer role
            if (supportedProtocols.Contains(Protocol, Role))
            {
                Start(maxMessageRate: MaxMessageRate);
            }
        }

        protected override void HandleChannelMetadata(MessageHeader header, ChannelMetadata channelMetadata)
        {
            // Base implementation caches ChannelMetadataRecord items sent from the producer
            base.HandleChannelMetadata(header, channelMetadata);

            InitializeDataBlocks(channelMetadata.Channels);

            var infos = channelMetadata.Channels
                .Select(ToChannelStreamingInfo)
                .ToList();

            // Send ChannelStreamingStart message
            ChannelStreamingStart(infos);
        }

        protected override void HandleChannelData(MessageHeader header, ChannelData channelData)
        {
            base.HandleChannelData(header, channelData);

            if (!channelData.Data.Any())
                return;

            AppendChannelData(channelData.Data);
            ProcessDataBlocks();
        }

        private void AppendChannelData(IList<DataItem> data)
        {
            foreach (var dataItem in data)
            {
                var parentUri = _channelParentUris[dataItem.ChannelId];
                var dataBlock = _dataBlocks[parentUri];

                var channel = ChannelMetadataRecords.FirstOrDefault(x => x.ChannelId == dataItem.ChannelId);
                var indexes = DownscaleIndexValues(channel.Indexes, dataItem.Indexes);

                dataBlock.Append(dataItem.ChannelId, indexes, dataItem.Value.Item);
            }
        }

        private IList<double> DownscaleIndexValues(IList<IndexMetadataRecord> indexMetadata, IList<long> indexValues)
        {
            return indexValues
                .Select((x, i) =>
                {
                    var index = indexMetadata[i];
                    return index.IndexType == ChannelIndexTypes.Depth
                        ? indexValues[i] / Math.Pow(10, index.Scale)
                        : indexValues[i];
                })
                .ToList();
        }

        private void InitializeDataBlocks(IList<ChannelMetadataRecord> channels)
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

                dataBlock.AddChannel(channel.ChannelId, channel.Mnemonic, channel.Uom);
            }
        }

        private void ProcessDataBlocks()
        {
            foreach (var item in _dataBlocks)
            {
                if (item.Value.Count() >= ChannelDataBlock.BatchSize)
                    ProcessDataBlock(item.Key, item.Value);
            }
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
