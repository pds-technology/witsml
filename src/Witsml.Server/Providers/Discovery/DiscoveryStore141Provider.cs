using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.Common;
using Energistics.DataAccess.WITSML141;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Discovery;
using PDS.Witsml.Server.Data;

namespace PDS.Witsml.Server.Providers.Discovery
{
    [Export(typeof(IDiscoveryStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DiscoveryStore141Provider : DiscoveryStoreHandler
    {
        private const string RootUri = "/";

        private readonly IEtpDataAdapter<Well> _wellDataAdapter;
        private readonly IEtpDataAdapter<Wellbore> _wellboreDataAdapter;

        [ImportingConstructor]
        public DiscoveryStore141Provider(
            IEtpDataAdapter<Well> wellDataAdapter,
            IEtpDataAdapter<Wellbore> wellboreDataAdapter)
        {
            _wellDataAdapter = wellDataAdapter;
            _wellboreDataAdapter = wellboreDataAdapter;
        }

        protected override void HandleGetResources(ProtocolEventArgs<GetResources, IList<Resource>> args)
        {
            if (RootUri.Equals(args.Message.Uri))
            {
                args.Context.Add(New(
                    uri: UriFormats.Witsml141.Root,
                    contentType: ContentTypes.Witsml141,
                    resourceType: ResourceTypes.UriProtocol,
                    name: "WITSML Store (1.4.1.1)",
                    count: -1));
            }
            else if (args.Message.Uri == UriFormats.Witsml141.Root)
            {
                _wellDataAdapter.GetAll()
                    .ForEach(x => args.Context.Add(ToResource(x)));
            }
            else if (args.Message.Uri.StartsWith(UriFormats.Witsml141.Wells))
            {
                _wellboreDataAdapter.GetAll(args.Message.Uri)
                    .ForEach(x => args.Context.Add(ToResource(x)));
            }
        }

        private Resource ToResource(Well entity)
        {
            return New(
                uri: string.Format(UriFormats.Witsml141.Well, entity.Uid),
                resourceType: ResourceTypes.DataObject,
                contentType: ContentTypes.Witsml141 + "type=obj_Well",
                name: entity.Name,
                count: -1);
        }

        private Resource ToResource(Wellbore entity)
        {
            return New(
                uri: string.Format(UriFormats.Witsml141.Wellbore, entity.UidWell, entity.Uid),
                resourceType: ResourceTypes.DataObject,
                contentType: ContentTypes.Witsml141 + "type=obj_Wellbore",
                name: entity.Name,
                count: -1);
        }

        private Resource New(string uri, ResourceTypes resourceType, string contentType, string name, int count = 0)
        {
            return new Resource()
            {
                Uuid = Guid.NewGuid().ToString(),
                Uri = uri,
                Name = name,
                HasChildren = count,
                ContentType = contentType,
                ResourceType = resourceType.ToString(),
                CustomData = new Dictionary<string, string>(),
                LastChanged = new Energistics.Datatypes.DateTime()
                {
                    Offset = 0,
                    Time = 0
                }
            };
        }
    }
}
