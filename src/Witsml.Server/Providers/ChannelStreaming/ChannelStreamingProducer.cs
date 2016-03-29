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

namespace PDS.Witsml.Server.Providers.ChannelStreaming
{
    [Export(typeof(IChannelStreamingProducer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ChannelStreamingProducer : ChannelStreamingProducerHandler
    {
        private static readonly IList<DataItem> EmptyChannelData = new List<DataItem>(0);

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

                await Task.Delay(MaxMessageRate);
            }
        }

        private IChannelDataProvider GetDataProvider(EtpUri uri)
        {
            return _container.Resolve<IChannelDataProvider>(new ObjectName(uri.ObjectType, uri.Version));
        }

        private Task<bool> StreamChannelData(IList<ChannelStreamingInfo> infos, EtpUri uri)
        {
            var channels = Channels[uri];
            var channelIds = channels.Select(x => x.ChannelId).ToArray();
            var channelInfos = infos.Where(x => channelIds.Contains(x.ChannelId)).ToArray();
            var minStart = channelInfos.Min(x => Convert.ToDouble(x.StartIndex.Item));

            // TODO: calculate range based on MaxDataItems instead of using Take()
            var take = (int)Math.Ceiling((double)MaxDataItems / (double)channels.Count);

            channelIds = channelInfos.Select(x => x.ChannelId).ToArray();
            channels = channels.Where(x => channelIds.Contains(x.ChannelId)).ToList();

            var dataProvider = GetDataProvider(uri);
            var channelData = dataProvider.GetChannelData(uri, new Range<double?>(minStart, null));

            StreamChannelData(channels, infos, channelData.Take(take));

            return Task.FromResult(true);
        }

        private void StreamChannelData(IList<ChannelMetadataRecord> channels, IList<ChannelStreamingInfo> infos, IEnumerable<IChannelDataRecord> channelData)
        {
            foreach (var record in channelData)
            {
                var index = record.GetIndexValue();

                var dataItems = infos
                    .Select(y =>
                    {
                        var channel = channels.FirstOrDefault(c => c.ChannelId == y.ChannelId);
                        var start = Convert.ToDouble(y.StartIndex.Item);

                        if (index <= start)
                            return null;

                        // update ChannelStreamingInfo index value
                        y.StartIndex.Item = index;

                        var value = Format(record.GetValue(record.GetOrdinal(channel.Mnemonic)));

                        return new DataItem()
                        {
                            ChannelId = y.ChannelId,
                            Indexes = new List<long>(),
                            ValueAttributes = new DataAttribute[0],
                            Value = new DataValue()
                            {
                                Item = value
                            }
                        };
                    })
                    .Where(x => x != null)
                    .ToList();

                if (dataItems.Any())
                {
                    ChannelData(Request, dataItems);
                }
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
