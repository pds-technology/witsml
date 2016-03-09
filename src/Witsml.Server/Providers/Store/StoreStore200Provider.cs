using System.ComponentModel.Composition;
using Energistics.Common;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Store;
using PDS.Witsml.Server.Data;

namespace PDS.Witsml.Server.Providers.Store
{
    /// <summary>
    /// Defines methods that can be used to perform CRUD operations via ETP for WITSML 2.0 objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Providers.Store.IStoreStoreProvider" />
    [Export200(typeof(IStoreStoreProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class StoreStore200Provider : IStoreStoreProvider
    {
        private readonly IEtpDataAdapter<Well> _wellDataAdapter;
        private readonly IEtpDataAdapter<Wellbore> _wellboreDataAdapter;
        private readonly IEtpDataAdapter<Log> _logDataAdapter;
        private readonly IEtpDataAdapter<ChannelSet> _channelSetDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreStore200Provider" /> class.
        /// </summary>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        /// <param name="logDataAdapter">The log data adapter.</param>
        /// <param name="channelSetDataAdapter">The channel set data adapter.</param>
        [ImportingConstructor]
        public StoreStore200Provider(
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
        /// Gets the object details for the specified URI.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetObject, DataObject}" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void GetObject(ProtocolEventArgs<GetObject, DataObject> args)
        {
            AbstractObject entity = null;

            var uri = new EtpUri(args.Message.Uri);

            if (uri.ObjectType == ObjectTypes.Well)
            {
                entity = _wellDataAdapter.Get(uri.ObjectId);
            }
            else if (uri.ObjectType == ObjectTypes.Wellbore)
            {
                entity = _wellboreDataAdapter.Get(uri.ObjectId);
            }
            else if (uri.ObjectType == ObjectTypes.Log)
            {
                entity = _logDataAdapter.Get(uri.ObjectId);
            }
            else if (uri.ObjectType == ObjectTypes.ChannelSet)
            {
                entity = _channelSetDataAdapter.Get(uri.ObjectId);
            }

            StoreStoreProvider.SetDataObject(args.Context, entity, uri, GetName(entity));
        }

        private string GetName(AbstractObject entity)
        {
            return entity == null ? string.Empty : entity.Citation.Title;
        }
    }
}
