using System;
using System.Collections.Generic;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Discovery;
using PDS.Framework;

namespace PDS.Witsml.Studio.Plugins.DataReplay.Providers
{
    public class SimulationDiscoveryProvider : DiscoveryStoreHandler
    {
        public SimulationDiscoveryProvider(Models.Simulation simulation)
        {
            Simulation = simulation;
        }

        public Models.Simulation Simulation { get; private set; }

        protected override void HandleGetResources(ProtocolEventArgs<GetResources, IList<Resource>> args)
        {
            if (args.Message.Uri == "/")
            {
                args.Context.Add(New(
                    Guid.NewGuid().ToString(),
                    UriFormats.Witsml141.Root,
                    contentType: ContentTypes.Witsml141,
                    resourceType: ResourceTypes.UriProtocol,
                    name: "WITSML 1.4.1.1 Store"));
            }
            else if (UriFormats.Witsml141.Root.EqualsIgnoreCase(args.Message.Uri))
            {
                args.Context.Add(New(
                    Simulation.WellUid,
                    string.Format("{0}/well({1})", UriFormats.Witsml141.Root, Simulation.WellUid),
                    contentType: ContentTypes.Witsml141 + "type=obj_Well",
                    resourceType: ResourceTypes.DataObject,
                    name: Simulation.WellName));
            }
            else if (string.Format("{0}/well({1})", UriFormats.Witsml141.Root, Simulation.WellUid).EqualsIgnoreCase(args.Message.Uri))
            {
                args.Context.Add(New(
                    Simulation.WellboreUid,
                    string.Format("{0}/well({1})/wellbore({2})", UriFormats.Witsml141.Root, Simulation.WellUid, Simulation.WellboreUid),
                    contentType: ContentTypes.Witsml141 + "type=obj_Wellbore",
                    resourceType: ResourceTypes.DataObject,
                    name: Simulation.WellboreName));
            }
            else if (string.Format("{0}/well({1})/wellbore({2})", UriFormats.Witsml141.Root, Simulation.WellUid, Simulation.WellboreUid).EqualsIgnoreCase(args.Message.Uri))
            {
                args.Context.Add(New(
                    Simulation.LogUid,
                    string.Format("{0}/well({1})/wellbore({2})/log({3})", UriFormats.Witsml141.Root, Simulation.WellUid, Simulation.WellboreUid, Simulation.LogUid),
                    contentType: ContentTypes.Witsml141 + "type=obj_Log",
                    resourceType: ResourceTypes.DataObject,
                    name: Simulation.LogName));
            }
            else if (string.Format("{0}/well({1})/wellbore({2})/log({3})", UriFormats.Witsml141.Root, Simulation.WellUid, Simulation.WellboreUid, Simulation.LogUid).EqualsIgnoreCase(args.Message.Uri))
            {
                foreach (var channel in Simulation.Channels)
                {
                    channel.ChannelUri = string.Format("{0}/well({1})/wellbore({2})/log({3})/curve({4})", UriFormats.Witsml141.Root, Simulation.WellUid, Simulation.WellboreUid, Simulation.LogUid, channel.Uuid);

                    args.Context.Add(New(
                        channel.Uuid,
                        channel.ChannelUri,
                        contentType: ContentTypes.Witsml141 + "type=obj_LogCurveInfo",
                        resourceType: ResourceTypes.DataObject,
                        name: channel.Mnemonic,
                        count: 0));
                }
            }
        }

        private Resource New(string uuid, string uri, ResourceTypes resourceType, string contentType, string name, int count = -1)
        {
            return new Resource()
            {
                Uuid = uuid,
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
