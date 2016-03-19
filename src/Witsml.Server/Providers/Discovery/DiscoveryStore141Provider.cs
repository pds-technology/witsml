using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.Common;
using Energistics.DataAccess.WITSML141;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Discovery;
using PDS.Witsml.Server.Data;
using Energistics.DataAccess.WITSML141.ComponentSchemas;

namespace PDS.Witsml.Server.Providers.Discovery
{
    /// <summary>
    /// Provides information about resources available in a WITSML store for version 1.4.1.1.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Providers.Discovery.IDiscoveryStoreProvider" />
    [Export(typeof(IDiscoveryStoreProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DiscoveryStore141Provider : IDiscoveryStoreProvider
    {
        private readonly IEtpDataAdapter<Well> _wellDataAdapter;
        private readonly IEtpDataAdapter<Wellbore> _wellboreDataAdapter;
        private readonly IEtpDataAdapter<Log> _logDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryStore141Provider" /> class.
        /// </summary>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        /// <param name="logDataAdapter">The log data adapter.</param>
        [ImportingConstructor]
        public DiscoveryStore141Provider(
            IEtpDataAdapter<Well> wellDataAdapter,
            IEtpDataAdapter<Wellbore> wellboreDataAdapter,
            IEtpDataAdapter<Log> logDataAdapter)
        {
            _wellDataAdapter = wellDataAdapter;
            _wellboreDataAdapter = wellboreDataAdapter;
            _logDataAdapter = logDataAdapter;
        }

        /// <summary>
        /// Gets the data schema version supported by the provider.
        /// </summary>
        /// <value>The data schema version.</value>
        public string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version141.Value; }
        }

        /// <summary>
        /// Gets a collection of resources associated to the specified URI.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetResources, IList{Resource}}"/> instance containing the event data.</param>
        public void GetResources(ProtocolEventArgs<GetResources, IList<Resource>> args)
        {
            if (EtpUri.IsRoot(args.Message.Uri))
            {
                args.Context.Add(DiscoveryStoreProvider.NewProtocol(EtpUris.Witsml141, "WITSML Store (1.4.1.1)"));
                return;
            }

            var uri = new EtpUri(args.Message.Uri);

            if (!uri.IsRelatedTo(EtpUris.Witsml141))
            {
                return;
            }
            else if (args.Message.Uri == EtpUris.Witsml141)
            {
                _wellDataAdapter.GetAll()
                    .ForEach(x => args.Context.Add(ToResource(x)));
            }
            else if (uri.ObjectType == ObjectTypes.Well)
            {
                _wellboreDataAdapter.GetAll(uri)
                    .ForEach(x => args.Context.Add(ToResource(x)));
            }
            else if (uri.ObjectType == ObjectTypes.Wellbore)
            {
                _logDataAdapter.GetAll(uri)
                    .ForEach(x => args.Context.Add(ToResource(x)));
            }
            else if (uri.ObjectType == ObjectTypes.Log)
            {
                var log = _logDataAdapter.Get(uri.ToDataObjectId());
                log.LogCurveInfo.ForEach(x => args.Context.Add(ToResource(log, x)));
            }
        }

        private Resource ToResource(Well entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uid,
                uri: entity.ToUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Name,
                count: -1);
        }

        private Resource ToResource(Wellbore entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uid,
                uri: entity.ToUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Name,
                count: -1);
        }

        private Resource ToResource(Log entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uid,
                uri: entity.ToUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Name,
                count: -1);
        }

        private Resource ToResource(Log log, LogCurveInfo curve)
        {
            return DiscoveryStoreProvider.New(
                uuid: curve.Uid,
                uri: curve.ToUri(log),
                resourceType: ResourceTypes.DataObject,
                name: curve.Mnemonic.Value,
                count: 0);
        }
    }
}
