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

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryStore141Provider"/> class.
        /// </summary>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        [ImportingConstructor]
        public DiscoveryStore141Provider(
            IEtpDataAdapter<Well> wellDataAdapter,
            IEtpDataAdapter<Wellbore> wellboreDataAdapter)
        {
            _wellDataAdapter = wellDataAdapter;
            _wellboreDataAdapter = wellboreDataAdapter;
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
            if (DiscoveryStoreProvider.RootUri.Equals(args.Message.Uri))
            {
                args.Context.Add(
                    DiscoveryStoreProvider.New(
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
            return DiscoveryStoreProvider.New(
                uri: string.Format(UriFormats.Witsml141.Well, entity.Uid),
                resourceType: ResourceTypes.DataObject,
                contentType: ContentTypes.Witsml141 + "type=" + ObjectTypes.Well,
                name: entity.Name,
                count: -1);
        }

        private Resource ToResource(Wellbore entity)
        {
            return DiscoveryStoreProvider.New(
                uri: string.Format(UriFormats.Witsml141.Wellbore, entity.UidWell, entity.Uid),
                resourceType: ResourceTypes.DataObject,
                contentType: ContentTypes.Witsml141 + "type=" + ObjectTypes.Wellbore,
                name: entity.Name,
                count: -1);
        }
    }
}
