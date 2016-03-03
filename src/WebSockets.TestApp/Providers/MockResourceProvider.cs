using System;
using System.Collections.Generic;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Discovery;

namespace Energistics.Providers
{
    public class MockResourceProvider : DiscoveryStoreHandler
    {
        private const string BaseUri = "eml://witsml1411";

        protected override void HandleGetResources(ProtocolEventArgs<GetResources, IList<Resource>> args)
        {
            if (EtpUri.IsRoot(args.Message.Uri))
            {
                args.Context.Add(New(
                    x => BaseUri,
                    contentType: ContentTypes.Witsml141,
                    resourceType: ResourceTypes.UriProtocol,
                    name: "WITSML Store"));
            }
            else if (EtpUri.IsRoot(args.Message.Uri))
            {
                args.Context.Add(New(
                    uuid => string.Format("{0}/well({1})", args.Message.Uri, uuid),
                    contentType: ContentTypes.Witsml141 + "type=obj_Well",
                    resourceType: ResourceTypes.DataObject,
                    name: "Well 01"));

                args.Context.Add(New(
                    uuid => string.Format("{0}/well({1})", args.Message.Uri, uuid),
                    contentType: ContentTypes.Witsml141 + "type=obj_Well",
                    resourceType: ResourceTypes.DataObject,
                    name: "Well 02"));
            }
            else if (args.Message.Uri.Contains("/well(") && !args.Message.Uri.Contains("/wellbore("))
            {
                args.Context.Add(New(
                    uuid => string.Format("{0}/wellbore({1})", args.Message.Uri, uuid),
                    contentType: ContentTypes.Witsml141 + "type=obj_Wellbore",
                    resourceType: ResourceTypes.DataObject,
                    name: "Wellbore 01-01"));

                args.Context.Add(New(
                    uuid => string.Format("{0}/wellbore({1})", args.Message.Uri, uuid),
                    contentType: ContentTypes.Witsml141 + "type=obj_Wellbore",
                    resourceType: ResourceTypes.DataObject,
                    name: "Wellbore 01-02"));
            }
            else if (args.Message.Uri.Contains("/wellbore("))
            {
                args.Context.Add(New(
                    uuid => string.Format("{0}/log({1})", args.Message.Uri, uuid),
                    contentType: ContentTypes.Witsml141 + "type=obj_Log",
                    resourceType: ResourceTypes.DataObject,
                    name: "Depth Log 01",
                    count: 0));

                args.Context.Add(New(
                    uuid => string.Format("{0}/log({1})", args.Message.Uri, uuid),
                    contentType: ContentTypes.Witsml141 + "type=obj_Log",
                    resourceType: ResourceTypes.DataObject,
                    name: "Time Log 01",
                    count: 0));
            }
        }

        private Resource New(Func<string, string> formatUri, ResourceTypes resourceType, string contentType, string name, int count = 1)
        {
            var uuid = Guid.NewGuid().ToString();

            return new Resource()
            {
                Uuid = uuid,
                Uri = formatUri(uuid),
                Name = name,
                HasChildren = count,
                ContentType = contentType,
                ResourceType = resourceType.ToString(),
                CustomData = new Dictionary<string, string>(),
                LastChanged = new Datatypes.DateTime()
                {
                    Offset = 0,
                    Time = 0
                }
            };
        }
    }
}
