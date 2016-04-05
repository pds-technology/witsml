//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energistics;
using Energistics.Common;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Core;
using PDS.Framework;
using PDS.Witsml.Studio.Core.Runtime;

namespace PDS.Witsml.Studio.Plugins.DataReplay.ViewModels.Proxies
{
    public class Etp141ProxyViewModel : EtpProxyViewModel
    {
        private Random _random;

        public Etp141ProxyViewModel(IRuntimeService runtime, Action<string> log) : base(runtime, log)
        {
            _random = new Random(246);
            Channels = new List<ChannelMetadataRecord>();
            ChannelStreamingInfo = new List<ChannelStreamingInfo>();
        }

        public IList<ChannelMetadataRecord> Channels { get; private set; }

        public IList<ChannelStreamingInfo> ChannelStreamingInfo { get; private set; }

        public override async Task Start(Models.Simulation model, CancellationToken token, int interval = 5000)
        {
            Model = model;

            var headers = EtpClient.Authorization(Model.EtpConnection.Username, Model.EtpConnection.Password);

            using (Client = new EtpClient(Model.EtpConnection.Uri, Model.Name, Model.Version, headers))
            {
                Client.Register<IChannelStreamingProducer, ChannelStreamingProducerHandler>();
                Client.Handler<IChannelStreamingProducer>().OnStart += OnStart;
                Client.Handler<IChannelStreamingProducer>().OnChannelDescribe += OnChannelDescribe;
                Client.Handler<IChannelStreamingProducer>().OnChannelStreamingStart += OnChannelStreamingStart;
                Client.Handler<IChannelStreamingProducer>().OnChannelStreamingStop += OnChannelStreamingStop;
                Client.Handler<IChannelStreamingProducer>().IsSimpleStreamer = Model.IsSimpleStreamer;
                Client.Output = Log;
                Client.Open();

                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    await Task.Delay(interval);
                }

                TaskRunner.Stop();

                Client.Handler<ICoreClient>()
                    .CloseSession("Streaming stopped.");
            }
        }

        private void OnStart(object sender, ProtocolEventArgs<Start> e)
        {
            TaskRunner = new TaskRunner(e.Message.MaxMessageRate)
            {
                OnExecute = StreamChannelData,
                OnError = LogStreamingError
            };

            if (Client.Handler<IChannelStreamingProducer>().IsSimpleStreamer)
            {
                var channelMetadata = GetChannelMetadata(e.Header);

                Client.Handler<IChannelStreamingProducer>()
                    .ChannelMetadata(e.Header, channelMetadata);

                foreach (var channel in channelMetadata.Select(ToChannelStreamingInfo))
                    ChannelStreamingInfo.Add(channel);

                TaskRunner.Start();
            }
        }

        private void OnChannelDescribe(object sender, ProtocolEventArgs<ChannelDescribe, IList<ChannelMetadataRecord>> e)
        {
            GetChannelMetadata(e.Header)
                .ForEach(e.Context.Add);
        }

        private void OnChannelStreamingStart(object sender, ProtocolEventArgs<ChannelStreamingStart> e)
        {
            e.Message.Channels.ForEach(ChannelStreamingInfo.Add);
            TaskRunner.Start();
        }

        private void OnChannelStreamingStop(object sender, ProtocolEventArgs<ChannelStreamingStop> e)
        {
            TaskRunner.Stop();
        }

        private void StreamChannelData()
        {
            var dataItems = ChannelStreamingInfo
                .Select(ToChannelDataItem)
                .ToList();

            Client.Handler<IChannelStreamingProducer>()
                .ChannelData(null, dataItems);
        }

        private List<ChannelMetadataRecord> GetChannelMetadata(MessageHeader header)
        {
            var indexMetadata = ToIndexMetadataRecord(Model.Channels.First());

            // Skip index channel
            var channelMetadata = Model.Channels
                .Skip(1)
                .Select(x => ToChannelMetadataRecord(x, indexMetadata))
                .ToList();

            return channelMetadata;
        }

        private EtpUri GetChannelUri(string mnemonic)
        {
            return EtpUris.Witsml141
                .Append(ObjectTypes.Well, Model.WellUid)
                .Append(ObjectTypes.Wellbore, Model.WellboreUid)
                .Append(ObjectTypes.Log, Model.LogUid)
                .Append(ObjectTypes.LogCurveInfo, mnemonic);
        }

        private static ChannelStreamingInfo ToChannelStreamingInfo(ChannelMetadataRecord record)
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

        private ChannelMetadataRecord ToChannelMetadataRecord(ChannelMetadataRecord record, IndexMetadataRecord indexMetadata)
        {
            var uri = GetChannelUri(record.Mnemonic);

            var channel = new ChannelMetadataRecord()
            {
                ChannelUri = uri,
                ContentType = uri.ContentType,
                ChannelId = record.ChannelId,
                Mnemonic = record.Mnemonic,
                Uom = record.Uom,
                MeasureClass = record.MeasureClass,
                DataType = record.DataType,
                Description = record.Description,
                Uuid = record.Uuid,
                Status = record.Status,
                Source = record.Source,
                ChannelAxes = new ChannelAxis[0],
                Indexes = new IndexMetadataRecord[]
                {
                    indexMetadata
                }
            };

            Channels.Add(channel);
            return channel;
        }

        private IndexMetadataRecord ToIndexMetadataRecord(ChannelMetadataRecord record, int scale = 3)
        {
            return new IndexMetadataRecord()
            {
                Uri = GetChannelUri(record.Mnemonic),
                Mnemonic = record.Mnemonic,
                Description = record.Description,
                Uom = record.Uom,
                Scale = scale,
                IndexType = Model.LogIndexType == LogIndexType.datetime || Model.LogIndexType == LogIndexType.elapsedtime
                    ? ChannelIndexTypes.Time
                    : ChannelIndexTypes.Depth,
                Direction = IndexDirections.Increasing,
                CustomData = new Dictionary<string, DataValue>(0),
            };
        }

        private DataItem ToChannelDataItem(ChannelStreamingInfo streamingInfo)
        {
            var channel = Channels.FirstOrDefault(x => x.ChannelId == streamingInfo.ChannelId);

            return new DataItem()
            {
                ChannelId = channel.ChannelId,
                Indexes = channel.Indexes
                    .Select(x => ToChannelIndexValue(streamingInfo, x))
                    .ToList(),
                ValueAttributes = new DataAttribute[0],
                Value = new DataValue()
                {
                    Item = _random.NextDouble()
                }
            };
        }

        private long ToChannelIndexValue(ChannelStreamingInfo streamingInfo, IndexMetadataRecord index)
        {
            if (index.IndexType == ChannelIndexTypes.Time)
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var value = 0d;

            if (streamingInfo.StartIndex.Item is double)
            {
                value = (double)streamingInfo.StartIndex.Item 
                      + Math.Pow(10, index.Scale) * 0.1;
            }

            streamingInfo.StartIndex.Item = value;

            return (long)value;
        }

        private void LogStreamingError(Exception ex)
        {
            Log("An error occurred: " + ex);
        }
    }
}
