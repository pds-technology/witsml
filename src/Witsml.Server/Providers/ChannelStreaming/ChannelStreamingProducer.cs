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
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
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
                var entity = dataProvider.Get(uri.ToDataObjectId());

                if (entity is Witsml131.Log)
                    DescribeChannels((Witsml131.Log)entity, args.Context);

                else if (entity is Witsml141.Log)
                    DescribeChannels((Witsml141.Log)entity, args.Context);

                else if (entity is Witsml200.Log)
                    DescribeChannels((Witsml200.Log)entity, args.Context);

                else if (entity is Witsml200.ChannelSet)
                    DescribeChannels((Witsml200.ChannelSet)entity, args.Context);

                Channels[uri].AddRange(args.Context);
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

        private IChannelDataProvider GetDataProvider(EtpUri uri)
        {
            return _container.Resolve<IChannelDataProvider>(new ObjectName(uri.ObjectType, uri.Version));
        }

        private object GetEntity(EtpUri uri)
        {
            var dataProvider = GetDataProvider(uri);
            return dataProvider.Get(uri.ToDataObjectId());
        }

        private void DescribeChannels(Witsml131.Log entity, IList<ChannelMetadataRecord> channels)
        {
            if (entity.LogCurveInfo == null || !entity.LogCurveInfo.Any())
                return;

            entity.LogCurveInfo.ForEach((c, i) =>
            {
                var channel = ToChannelMetadataRecord(entity, c);
                channel.ChannelId = i;
                channels.Add(channel);
            });
        }

        private void DescribeChannels(Witsml141.Log entity, IList<ChannelMetadataRecord> channels)
        {
            if (entity.LogCurveInfo == null || !entity.LogCurveInfo.Any())
                return;

            entity.LogCurveInfo.ForEach((c, i) =>
            {
                var channel = ToChannelMetadataRecord(entity, c);
                channel.ChannelId = i;
                channels.Add(channel);
            });
        }

        private void DescribeChannels(Witsml200.Log entity, IList<ChannelMetadataRecord> channels)
        {
        }

        private void DescribeChannels(Witsml200.ChannelSet entity, IList<ChannelMetadataRecord> channels)
        {
        }

        private ChannelMetadataRecord ToChannelMetadataRecord(Witsml131.Log log, Witsml131.ComponentSchemas.LogCurveInfo curve)
        {
            var uri = curve.GetUri(log);

            return new ChannelMetadataRecord()
            {
                ChannelUri = uri,
                ContentType = uri.ContentType,
                DataType = curve.TypeLogData.GetValueOrDefault(Witsml131.ReferenceData.LogDataType.@double).ToString().Replace("@", string.Empty),
                Description = curve.CurveDescription ?? curve.Mnemonic,
                Mnemonic = curve.Mnemonic,
                Uom = curve.Unit,
                MeasureClass = curve.ClassWitsml == null ? ObjectTypes.Unknown : curve.ClassWitsml.Name,
                Source = curve.DataSource ?? ObjectTypes.Unknown,
                Uuid = curve.Mnemonic,
                Status = ChannelStatuses.Active,
                ChannelAxes = new List<ChannelAxis>(),
                Indexes = new List<IndexMetadataRecord>(),
            };
        }

        private ChannelMetadataRecord ToChannelMetadataRecord(Witsml141.Log log, Witsml141.ComponentSchemas.LogCurveInfo curve)
        {
            var uri = curve.GetUri(log);

            return new ChannelMetadataRecord()
            {
                ChannelUri = uri,
                ContentType = uri.ContentType,
                DataType = curve.TypeLogData.GetValueOrDefault(Witsml141.ReferenceData.LogDataType.@double).ToString().Replace("@", string.Empty),
                Description = curve.CurveDescription ?? curve.Mnemonic.Value,
                Mnemonic = curve.Mnemonic.Value,
                Uom = curve.Unit,
                MeasureClass = curve.ClassWitsml ?? ObjectTypes.Unknown,
                Source = curve.DataSource ?? ObjectTypes.Unknown,
                Uuid = curve.Mnemonic.Value,
                Status = ChannelStatuses.Active,
                ChannelAxes = new List<ChannelAxis>(),
                Indexes = new List<IndexMetadataRecord>(),
            };
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

        private Task<bool> StreamChannelData(IList<ChannelStreamingInfo> infos, EtpUri uri)
        {
            var dataProvider = GetDataProvider(uri);
            var entity = dataProvider.Get(uri.ToDataObjectId());

            var channels = Channels[uri];
            var channelIds = channels.Select(x => x.ChannelId).ToArray();
            var channelInfos = infos.Where(x => channelIds.Contains(x.ChannelId)).ToArray();
            var minStart = channelInfos.Min(x => Convert.ToDouble(x.StartIndex.Item));

            IEnumerable<IChannelDataRecord> channelData = Enumerable.Empty<IChannelDataRecord>();

            // TODO: calculate range based on MaxDataItems instead of using Take()
            var take = (int)Math.Ceiling((double)MaxDataItems / (double)channels.Count);

            if (entity is Witsml131.Log)
                channelData = GetChannelData(dataProvider, (Witsml131.Log)entity, uri, minStart);

            else if (entity is Witsml141.Log)
                channelData = GetChannelData(dataProvider, (Witsml141.Log)entity, uri, minStart);

            else if (entity is Witsml200.Log)
                channelData = GetChannelData(dataProvider, (Witsml200.Log)entity, uri, minStart);

            else if (entity is Witsml200.ChannelSet)
                channelData = GetChannelData(dataProvider, (Witsml200.ChannelSet)entity, uri, minStart);

            channelIds = channelInfos.Select(x => x.ChannelId).ToArray();
            channels = channels.Where(x => channelIds.Contains(x.ChannelId)).ToList();

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

                        return new DataItem()
                        {
                            ChannelId = y.ChannelId,
                            Indexes = new List<long>(),
                            ValueAttributes = new DataAttribute[0],
                            Value = new DataValue()
                            {
                                Item = record.GetValue(record.GetOrdinal(channel.Mnemonic))
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

        private IEnumerable<IChannelDataRecord> GetChannelData(IChannelDataProvider dataProvider, Witsml131.Log entity, EtpUri uri, double minStart)
        {
            var range = new Range<double?>(minStart, null);
            var mnemonics = entity.LogCurveInfo.Select(x => x.Mnemonic);
            var increasing = entity.Direction.GetValueOrDefault() == Witsml131.ReferenceData.LogIndexDirection.increasing;

            return dataProvider.GetChannelData(entity.GetUri(), mnemonics.First(), range, increasing);
        }

        private IEnumerable<IChannelDataRecord> GetChannelData(IChannelDataProvider dataProvider, Witsml141.Log entity, EtpUri uri, double minStart)
        {
            var range = new Range<double?>(minStart, null);
            var mnemonics = entity.LogCurveInfo.Select(x => x.Mnemonic.Value);
            var increasing = entity.Direction.GetValueOrDefault() == Witsml141.ReferenceData.LogIndexDirection.increasing;

            return dataProvider.GetChannelData(entity.GetUri(), mnemonics.First(), range, increasing);
        }

        private IEnumerable<IChannelDataRecord> GetChannelData(IChannelDataProvider dataProvider, Witsml200.Log entity, EtpUri uri, double minStart)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<IChannelDataRecord> GetChannelData(IChannelDataProvider dataProvider, Witsml200.ChannelSet entity, EtpUri uri, double minStart)
        {
            throw new NotImplementedException();
        }
    }
}
