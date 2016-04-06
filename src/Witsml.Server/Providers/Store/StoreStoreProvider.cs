//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.Collections.Generic;
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
        /// Sets the properties of the <see cref="DataObject"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of entity.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="name">The name.</param>
        public static void SetDataObject<T>(DataObject dataObject, T entity, EtpUri uri, string name)
        {
            if (entity == null)
            {
                dataObject.SetXml(null);
            }
            else
            {
                var xml = EnergisticsConverter.ObjectToXml(entity);
                dataObject.SetXml(xml);
            }

            dataObject.Resource = new Resource()
            {
                Uri = uri,
                Uuid = uri.ObjectId,
                Name = name,
                HasChildren = -1,
                ContentType = uri.ContentType,
                ResourceType = ResourceTypes.DataObject.ToString(),
                CustomData = new Dictionary<string, string>(),
                LastChanged = 0 // TODO: provide LastChanged
            };
        }

        /// <summary>
        /// Handles the GetObject message of the Store protocol.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetObject, DataObject}"/> instance containing the event data.</param>
        protected override void HandleGetObject(ProtocolEventArgs<GetObject, DataObject> args)
        {
            try
            {
                var uri = new EtpUri(args.Message.Uri);
                var provider = Container.Resolve<IStoreStoreProvider>(new ObjectName(uri.Version));
                provider.GetObject(args);
            }
            catch (ContainerException ex)
            {
                Logger.Error(ex);
                ProtocolException(1000, "Data object not supported. URI: " + args.Message.Uri, args.Header.MessageId);
            }
        }

        /// <summary>
        /// Handles the PutObject message of the Store protocol.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="putObject">The put object message.</param>
        protected override void HandlePutObject(MessageHeader header, PutObject putObject)
        {
            base.HandlePutObject(header, putObject);

            var uri = new EtpUri(putObject.Data.Resource.Uri);

            try
            {
                var dataAdapter = Container.Resolve<IEtpDataAdapter>(new ObjectName(uri.ObjectType, uri.Version));
                dataAdapter.Put(putObject.Data);
            }
            catch (ContainerException ex)
            {
                Logger.Error(ex);
                ProtocolException(1000, "Data object not supported. URI: " + uri, header.MessageId);
            }
        }

        /// <summary>
        /// Handles the DeleteObject message of the Store protocol.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="deleteObject">The delete object message.</param>
        protected override void HandleDeleteObject(MessageHeader header, DeleteObject deleteObject)
        {
            base.HandleDeleteObject(header, deleteObject);
            deleteObject.Uri.ForEach(x => DeleteObject(header, new EtpUri(x)));
        }

        /// <summary>
        /// Deletes the object from the data store.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="uri">The URI.</param>
        private void DeleteObject(MessageHeader header, EtpUri uri)
        {
            try
            {
                var dataAdapter = Container.Resolve<IEtpDataAdapter>(new ObjectName(uri.ObjectType, uri.Version));
                dataAdapter.Delete(uri);
            }
            catch (ContainerException ex)
            {
                Logger.Error(ex);
                ProtocolException(1000, "Data object not supported. URI: " + uri, header.MessageId);
            }
        }
    }
}
