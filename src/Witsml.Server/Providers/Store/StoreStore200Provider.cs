using System.ComponentModel.Composition;
using Energistics.Common;
using Energistics.DataAccess.WITSML200;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreStore200Provider"/> class.
        /// </summary>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        [ImportingConstructor]
        public StoreStore200Provider(
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
        /// Gets the object details for the specified URI.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetObject, DataObject}" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void GetObject(ProtocolEventArgs<GetObject, DataObject> args)
        {
            var uri = new EtpUri(args.Message.Uri);

            if (uri.ObjectType == ObjectTypes.Well)
            {
                var entity = _wellDataAdapter.Get(uri.ObjectId);

                StoreStoreProvider.SetDataObject(args.Context, entity, uri, entity.Citation.Title);
            }
            else if (uri.ObjectType == ObjectTypes.Wellbore)
            {
                var entity = _wellboreDataAdapter.Get(uri.ObjectId);

                StoreStoreProvider.SetDataObject(args.Context, entity, uri, entity.Citation.Title);
            }
            else if (uri.ObjectType == ObjectTypes.Log)
            {
                var entity = _logDataAdapter.Get(uri.ObjectId);

                StoreStoreProvider.SetDataObject(args.Context, entity, uri, entity.Citation.Title);
            }
        }
    }
}
