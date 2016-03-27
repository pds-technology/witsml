using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol.ChannelStreaming;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using PDS.Framework;
using PDS.Witsml.Server.Data;

namespace PDS.Witsml.Server.Providers.ChannelStreaming
{
    [Export(typeof(IChannelStreamingProducer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ChannelStreamingProducer : ChannelStreamingProducerHandler
    {
        private readonly IContainer _container;

        [ImportingConstructor]
        public ChannelStreamingProducer(IContainer container)
        {
            _container = container;
            Channels = new List<ChannelMetadataRecord>();
        }

        public List<ChannelMetadataRecord> Channels { get; private set; }

        protected override void HandleChannelDescribe(ProtocolEventArgs<ChannelDescribe, IList<ChannelMetadataRecord>> args)
        {
            foreach (var uri in args.Message.Uris.Select(x => new EtpUri(x)))
            {
                if (!ObjectTypes.Log.EqualsIgnoreCase(uri.ObjectType) && 
                    !ObjectTypes.ChannelSet.EqualsIgnoreCase(uri.ObjectType))
                    continue;

                var adapter = _container.Resolve<IEtpDataAdapter>(new ObjectName(uri.ObjectType, uri.Version));
                var entity = adapter.Get(uri.ToDataObjectId());

                if (entity is Witsml131.Log)
                    DescribeChannels((Witsml131.Log)entity, args.Context);

                else if (entity is Witsml141.Log)
                    DescribeChannels((Witsml141.Log)entity, args.Context);

                else if (entity is Witsml200.Log)
                    DescribeChannels((Witsml200.Log)entity, args.Context);

                else if (entity is Witsml200.ChannelSet)
                    DescribeChannels((Witsml200.ChannelSet)entity, args.Context);
            }

            Channels.Clear();
            Channels.AddRange(args.Context);
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
    }
}
