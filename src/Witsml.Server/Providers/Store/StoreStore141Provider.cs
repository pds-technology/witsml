using System;
using System.Collections;
using System.ComponentModel.Composition;
using Energistics.Common;
using Energistics.DataAccess;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Store;
using PDS.Framework;
using PDS.Witsml.Server.Data;

namespace PDS.Witsml.Server.Providers.Store
{
    /// <summary>
    /// Defines methods that can be used to perform CRUD operations via ETP for WITSML 1.4.1.1 objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Providers.Store.IStoreStoreProvider" />
    [Export141(typeof(IStoreStoreProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class StoreStore141Provider : IStoreStoreProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StoreStore141Provider" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        [ImportingConstructor]
        public StoreStore141Provider(IContainer container)
        {
            Container = container;
        }

        /// <summary>
        /// Gets the composition container.
        /// </summary>
        /// <value>The container.</value>
        public IContainer Container { get; private set; }

        /// <summary>
        /// Gets the data schema version supported by the provider.
        /// </summary>
        /// <value>The data schema version.</value>
        public string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version141.Value; }
        }

        /// <summary>
        /// Gets the object details for the specified URI.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetObject, DataObject}" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void GetObject(ProtocolEventArgs<GetObject, DataObject> args)
        {
            var uri = new EtpUri(args.Message.Uri);
            var dataAdapter = Container.Resolve<IEtpDataAdapter>(new ObjectName(uri.ObjectType, uri.Version));
            var entity = dataAdapter.Get(uri) as IDataObject;
            var list = GetList(uri.ObjectType, entity);

            StoreStoreProvider.SetDataObject(args.Context, list, uri, GetName(entity));
        }

        private IEnergisticsCollection GetList(string objectType, IDataObject entity)
        {
            var entityType = entity.GetType();
            var groupType = entityType.Assembly.GetType(entityType.FullName + "List");
            var property = groupType.GetProperty(entityType.Name);

            var group = Activator.CreateInstance(groupType) as IEnergisticsCollection;
            var list = Activator.CreateInstance(property.PropertyType) as IList;

            list.Add(entity);
            property.SetValue(group, list);

            return group;
        }
    
        private string GetName(IDataObject entity)
        {
            return entity == null ? string.Empty : entity.Name;
        }
    }
}
