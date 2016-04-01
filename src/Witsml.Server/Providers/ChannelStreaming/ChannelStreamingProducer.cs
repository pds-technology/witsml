using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol.ChannelStreaming;
using Newtonsoft.Json.Linq;
using PDS.Framework;
using PDS.Witsml.Server.Data.Channels;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Providers.ChannelStreaming
{
    [Export(typeof(IChannelStreamingProducer))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ChannelStreamingProducer : ChannelStreamingProducerHandler
    {
        private static readonly IList<DataItem> EmptyChannelData = new List<DataItem>(0);
        private static readonly int RangeSize = Settings.Default.ChannelDataChunkRangeSize;

        private readonly IContainer _container;
        private CancellationTokenSource _tokenSource;

        [ImportingConstructor]
        public ChannelStreamingProducer(IContainer container)
        {
            _container = container;
            Uris = new List<EtpUri>();
            Channels = new Dictionary<EtpUri, List<ChannelMetadataRecord>>();
        }

        public MessageHeader Request { get; private set; }

        public List<EtpUri> Uris { get; private set; }

        public Dictionary<EtpUri, List<ChannelMetadataRecord>> Channels { get; private set; }

        protected override void HandleChannelDescribe(ProtocolEventArgs<ChannelDescribe, IList<ChannelMetadataRecord>> args)
        {
            Channels.Clear();

            foreach (var uri in args.Message.Uris.Select(x => new EtpUri(x)))
            {
                if (!ObjectTypes.Log.EqualsIgnoreCase(uri.ObjectType) && 
                    !ObjectTypes.ChannelSet.EqualsIgnoreCase(uri.ObjectType))
                    continue;

                Uris.Add(uri);
                Channels[uri] = new List<ChannelMetadataRecord>();

                var dataProvider = GetDataProvider(uri);
                var metadata = dataProvider.GetChannelMetadata(uri);

                metadata.ForEach(args.Context.Add);
                Channels[uri].AddRange(metadata);
            }
        }

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

        protected override void HandleChannelStreamingStop(MessageHeader header, ChannelStreamingStop channelStreamingStop)
        {
            // no action needed if streaming not in progress
            if (_tokenSource == null)
                return;

            base.HandleChannelStreamingStop(header, channelStreamingStop);

            if (_tokenSource != null)
                _tokenSource.Cancel();
        }

        private async Task StartChannelStreaming(IList<ChannelStreamingInfo> infos, CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    break;

                foreach (var uri in Channels.Keys)
                    await StreamChannelData(infos, uri);
            }
        }

        private IChannelDataProvider GetDataProvider(EtpUri uri)
        {
            return _container.Resolve<IChannelDataProvider>(new ObjectName(uri.ObjectType, uri.Version));
        }

        private async Task<bool> StreamChannelData(IList<ChannelStreamingInfo> infos, EtpUri uri)
        {
            var channels = Channels[uri];
            var channelIds = channels.Select(x => x.ChannelId).ToArray();
            var channelInfos = infos.Where(x => channelIds.Contains(x.ChannelId)).ToArray();
            var minStart = channelInfos.Min(x => Convert.ToDouble(x.StartIndex.Item));

            channelIds = channelInfos.Select(x => x.ChannelId).ToArray();
            channels = channels.Where(x => channelIds.Contains(x.ChannelId)).ToList();

            // TODO: Handle decreasing data
            var dataProvider = GetDataProvider(uri);
            var channelData = dataProvider.GetChannelData(uri, new Range<double?>(minStart, minStart + RangeSize));

            // TODO: Stream Channel Data with IndexedDataItems if StreamIndexValuePairs setting is true
            await StreamIndexedChannelData(infos, channels, channelData);

            return true;
        }

        private async Task StreamIndexedChannelData(IList<ChannelStreamingInfo> infos, List<ChannelMetadataRecord> channels, IEnumerable<IChannelDataRecord> channelData)
        {
            var dataItemList = new List<DataItem>();

            using (var channelDataEnum = channelData.GetEnumerator())
            {
                var endOfChannelData = !channelDataEnum.MoveNext();

                while (!endOfChannelData)
                {
                    foreach (var dataItem in CreateIndexedDataItems(channels, infos, channelDataEnum.Current))
                    {
                        if (dataItemList.Count + 1 >= MaxDataItems)
                        {
                            await SendChannelData(dataItemList);
                            dataItemList.Clear();
                        }

                        dataItemList.Add(dataItem.IndexDataItem);
                        dataItemList.Add(dataItem.ValueDataItem);
                    }
                    endOfChannelData = !channelDataEnum.MoveNext();
                }

                if (dataItemList.Any())
                {
                    await SendChannelData(dataItemList);
                }
            }
        }

        private async Task SendChannelData(List<DataItem> dataItemList)
        {
            ChannelData(Request, dataItemList);
            await Task.Delay(MaxMessageRate);
        }

        private IEnumerable<IndexedDataItem> CreateIndexedDataItems(IList<ChannelMetadataRecord> channels, IList<ChannelStreamingInfo> infos, IChannelDataRecord record)
        {
            // Get the value and ChannelId of the primary index
            var index = record.GetIndexValue();
            var indexChannelId = channels
                .Where(x => x.Mnemonic.EqualsIgnoreCase(x.Indexes[0].Mnemonic))
                .Select(x => x.ChannelId)
                .FirstOrDefault();
            var indexMnemonics = channels
                .Take(1)
                .SelectMany(x => x.Indexes.Select(y => y.Mnemonic))
                .ToArray();

            // Create a DataItem for the Primary Index
            var indexDataItem = new DataItem()
            {
                ChannelId = indexChannelId,
                Indexes = new long[0],
                ValueAttributes = new DataAttribute[0],
                Value = new DataValue()
                {
                    Item = index
                }
            };

            foreach (var info in infos)
            {
                var channel = channels.FirstOrDefault(c => c.ChannelId == info.ChannelId);
                var start = Convert.ToDouble(info.StartIndex.Item);

                // TODO: Handle decreasing data
                if (index <= start || indexMnemonics.Any(x => x.EqualsIgnoreCase(channel.Mnemonic)))
                {
                    continue;
                }

                // update ChannelStreamingInfo index value
                info.StartIndex.Item = index;

                var value = Format(record.GetValue(record.GetOrdinal(channel.Mnemonic)));

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

        private object Format(object value)
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
