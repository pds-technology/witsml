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

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryStore200Provider" /> class.
        /// </summary>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        [ImportingConstructor]
        public DiscoveryStore200Provider(
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
            get { return OptionsIn.DataVersion.Version200.Value; }
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
                        uri: UriFormats.Witsml200.Root,
                        contentType: ContentTypes.Witsml200,
                        resourceType: ResourceTypes.UriProtocol,
                        name: "WITSML Store (2.0)",
                        count: -1));
            }
            else if (args.Message.Uri == UriFormats.Witsml200.Root)
            {
                _wellDataAdapter.GetAll()
                    .ForEach(x => args.Context.Add(ToResource(x)));
            }
            else if (args.Message.Uri.StartsWith(UriFormats.Witsml200.Wells))
            {
                _wellboreDataAdapter.GetAll(args.Message.Uri)
                    .ForEach(x => args.Context.Add(ToResource(x)));
            }
        }

        private Resource ToResource(Well entity)
        {
            return DiscoveryStoreProvider.New(
                uri: string.Format(UriFormats.Witsml200.Well, entity.Uuid),
                resourceType: ResourceTypes.DataObject,
                contentType: ContentTypes.Witsml200 + "type=" + ObjectTypes.Well,
                name: entity.Citation.Title,
                count: -1);
        }

        private Resource ToResource(Wellbore entity)
        {
            return DiscoveryStoreProvider.New(
                uri: string.Format(UriFormats.Witsml200.Wellbore, entity.Uuid),
                resourceType: ResourceTypes.DataObject,
                contentType: ContentTypes.Witsml200 + "type=" + ObjectTypes.Wellbore,
                name: entity.Citation.Title,
                count: -1);
        }
    }
}
