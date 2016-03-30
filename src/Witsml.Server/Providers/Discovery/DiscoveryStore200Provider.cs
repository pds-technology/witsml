using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.Common;
using Energistics.DataAccess.WITSML200;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Discovery;
using PDS.Framework;
using PDS.Witsml.Server.Data;

namespace PDS.Witsml.Server.Providers.Discovery
{
    /// <summary>
    /// Provides information about resources available in a WITSML store for version 1.4.1.1.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Providers.Discovery.IDiscoveryStoreProvider" />
    [Export(typeof(IDiscoveryStoreProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DiscoveryStore200Provider : IDiscoveryStoreProvider
    {
        private readonly IEtpDataAdapter<Well> _wellDataAdapter;
        private readonly IEtpDataAdapter<Wellbore> _wellboreDataAdapter;
        private readonly IEtpDataAdapter<Log> _logDataAdapter;
        private readonly IEtpDataAdapter<ChannelSet> _channelSetDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryStore200Provider" /> class.
        /// </summary>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        /// <param name="logDataAdapter">The log data adapter.</param>
        /// <param name="channelSetDataAdapter">The channel set data adapter.</param>
        [ImportingConstructor]
        public DiscoveryStore200Provider(
            IEtpDataAdapter<Well> wellDataAdapter,
            IEtpDataAdapter<Wellbore> wellboreDataAdapter,
            IEtpDataAdapter<Log> logDataAdapter,
            IEtpDataAdapter<ChannelSet> channelSetDataAdapter)
        {
            _wellDataAdapter = wellDataAdapter;
            _wellboreDataAdapter = wellboreDataAdapter;
            _logDataAdapter = logDataAdapter;
            _channelSetDataAdapter = channelSetDataAdapter;
        }

        /// <summary>
        /// Gets the data schema version supported by the provider.
        /// </summary>
        /// <value>The data schema version.</value>
        public string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version200.Value; }
        }

        /// <summary>
        /// Gets a collection of resources associated to the specified URI.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetResources, IList{Resource}}"/> instance containing the event data.</param>
        public void GetResources(ProtocolEventArgs<GetResources, IList<Resource>> args)
        {
            if (EtpUri.IsRoot(args.Message.Uri))
            {
                args.Context.Add(DiscoveryStoreProvider.NewProtocol(EtpUris.Witsml200, "WITSML Store (2.0)"));
                return;
            }

            var uri = new EtpUri(args.Message.Uri);

            if (!uri.IsRelatedTo(EtpUris.Witsml200))
            {
                return;
            }
            else if (args.Message.Uri == EtpUris.Witsml200)
            {
                _wellDataAdapter.GetAll()
                    .ForEach(x => args.Context.Add(ToResource(x)));
            }
            else if (string.IsNullOrWhiteSpace(uri.ObjectId))
            {
                var parentUri = uri.Parent;

                if (uri.ObjectType == ObjectFolders.Logs)
                {
                    args.Context.Add(DiscoveryStoreProvider.NewFolder(uri, ObjectTypes.Log, ObjectFolders.Time));
                    args.Context.Add(DiscoveryStoreProvider.NewFolder(uri, ObjectTypes.Log, ObjectFolders.Depth));
                }
                else if (parentUri.ObjectType == ObjectFolders.Logs &&
                    (uri.ObjectType == ObjectFolders.Time || uri.ObjectType == ObjectFolders.Depth))
                {
                    var wellboreUri = parentUri.Parent;

                    _logDataAdapter.GetAll(wellboreUri)
                        .Where(x => x.TimeDepth.EqualsIgnoreCase(uri.ObjectType))
                        .ForEach(x => args.Context.Add(ToResource(x)));
                }
            }
            else if (uri.ObjectType == ObjectTypes.Well)
            {
                _wellboreDataAdapter.GetAll(uri)
                    .ForEach(x => args.Context.Add(ToResource(x)));
            }
            else if (uri.ObjectType == ObjectTypes.Wellbore)
            {
                args.Context.Add(DiscoveryStoreProvider.NewFolder(uri, ObjectTypes.Log, ObjectFolders.Logs));
                args.Context.Add(DiscoveryStoreProvider.NewFolder(uri, ObjectTypes.MudLog, ObjectFolders.MudLogs));
                args.Context.Add(DiscoveryStoreProvider.NewFolder(uri, ObjectTypes.Rig, ObjectFolders.Rigs));
                args.Context.Add(DiscoveryStoreProvider.NewFolder(uri, ObjectTypes.Trajectory, ObjectFolders.Trajectories));
            }
            else if (uri.ObjectType == ObjectTypes.Log)
            {
                var log = _logDataAdapter.Get(uri);
                log.ChannelSet.ForEach(x => args.Context.Add(ToResource(x)));
            }
            else if (uri.ObjectType == ObjectTypes.ChannelSet)
            {
                var uid = uri.GetObjectIds()
                    .Where(x => x.Key == ObjectTypes.Log)
                    .Select(x => x.Value)
                    .FirstOrDefault();

                var set = _channelSetDataAdapter.Get(uri);
                //var set = log.ChannelSet.FirstOrDefault(x => x.Uuid == uri.ObjectId);

                set.Channel.ForEach(x => args.Context.Add(ToResource(set, x)));
            }
        }

        private Resource ToResource(Well entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Citation.Title,
                count: -1);
        }

        private Resource ToResource(Wellbore entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Citation.Title,
                count: -1);
        }

        private Resource ToResource(Log entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Citation.Title,
                count: -1);
        }

        private Resource ToResource(ChannelSet entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Citation.Title,
                count: -1);
        }

        private Resource ToResource(ChannelSet channelSet, Channel entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.GetUri(channelSet),
                resourceType: ResourceTypes.DataObject,
                name: entity.Mnemonic,
                count: 0);
        }
    }
}
