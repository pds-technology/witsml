using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Energistics.Common;
using Energistics.DataAccess;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Store;
using PDS.Framework;

namespace PDS.Witsml.Server.Providers.Store
{
    /// <summary>
    /// Process messages received for the Store role of the Store protocol.
    /// </summary>
    /// <seealso cref="Energistics.Protocol.Store.StoreStoreHandler" />
    [Export(typeof(IStoreStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class StoreStoreProvider : StoreStoreHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StoreStoreProvider"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        [ImportingConstructor]
        public StoreStoreProvider(IContainer container)
        {
            Container = container;
        }

        /// <summary>
        /// Gets the composition container.
        /// </summary>
        /// <value>The container.</value>
        public IContainer Container { get; private set; }

        /// <summary>
        /// Handles the GetObject message of the Store protocol.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetObject, DataObject}"/> instance containing the event data.</param>
        protected override void HandleGetObject(ProtocolEventArgs<GetObject, DataObject> args)
        {
            var version = args.Message.Uri.StartsWith(UriFormats.Witsml141.Root)
                ? OptionsIn.DataVersion.Version141.Value
                : OptionsIn.DataVersion.Version200.Value;

            try
            {
                var provider = Container.Resolve<IStoreStoreProvider>(version);
                provider.GetObject(args);
            }
            catch (ContainerException ex)
            {
                Logger.Error(ex);
                ProtocolException(1000, "Unknown URI format: " + args.Message.Uri, args.Header.MessageId);
            }
        }

        /// <summary>
        /// Sets the properties of the <see cref="DataObject"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of entity.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="name">The name.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="uid">The uid.</param>
        /// <param name="contentType">Type of the content.</param>
        public static void SetDataObject<T>(DataObject dataObject, T entity, string name, string uri, string uid, string contentType)
        {
            var xml = EnergisticsConverter.ObjectToXml(entity);

            dataObject.ContentEncoding = string.Empty;
            dataObject.Data = Encoding.UTF8.GetBytes(xml);
            dataObject.Resource = new Resource()
            {
                Uri = uri,
                Uuid = uid,
                Name = name,
                HasChildren = -1,
                ContentType = contentType,
                ResourceType = ResourceTypes.DataObject.ToString(),
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
