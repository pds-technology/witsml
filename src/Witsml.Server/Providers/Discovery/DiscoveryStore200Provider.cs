using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.Common;
using Energistics.DataAccess.WITSML200;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Discovery;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryStore200Provider" /> class.
        /// </summary>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        /// <param name="logDataAdapter">The log data adapter.</param>
        [ImportingConstructor]
        public DiscoveryStore200Provider(
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
                args.Context.Add(
                    DiscoveryStoreProvider.New(
                        uuid: Guid.NewGuid().ToString(),
                        uri: EtpUris.Witsml200,
                        resourceType: ResourceTypes.UriProtocol,
                        name: "WITSML Store (2.0)",
                        count: -1));

                return;
            }

            var uri = new EtpUri(args.Message.Uri);

            if (args.Message.Uri == EtpUris.Witsml200)
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
        }

        private Resource ToResource(Well entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.ToUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Citation.Title,
                count: -1);
        }

        private Resource ToResource(Wellbore entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.ToUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Citation.Title,
                count: -1);
        }

        private Resource ToResource(Log entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.ToUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Citation.Title,
                count: -1);
        }
    }
}
