//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.Etp;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.Object;
using Energistics.Etp.v12.Datatypes;
using Energistics.Etp.v12.Datatypes.Object;
using Energistics.Etp.v12.Protocol.Store;
using PDS.WITSMLstudio.Data;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data;

namespace PDS.WITSMLstudio.Store.Providers.Store
{
    /// <summary>
    /// Process messages received for the Store role of the Store protocol.
    /// </summary>
    [Export(typeof(IStoreStore))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Store12StoreProvider : StoreStoreHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Store12StoreProvider"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        [ImportingConstructor]
        public Store12StoreProvider(IContainer container)
        {
            Container = container;
        }

        /// <summary>
        /// Gets the composition container.
        /// </summary>
        /// <value>The container.</value>
        public IContainer Container { get; private set; }

        /// <summary>
        /// Sets the properties of the <see cref="IDataObject"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of entity.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="name">The name.</param>
        /// <param name="childCount">The child count.</param>
        /// <param name="lastChanged">The last changed in microseconds.</param>
        public static void SetDataObject<T>(IDataObject dataObject, T entity, EtpUri uri, string name, int childCount = -1, long lastChanged = 0)
        {
            if (entity == null)
            {
                dataObject.SetString(null);
            }
            else if (entity is string)
            {
                dataObject.SetString(entity.ToString());
            }
            else
            {
                var data = EtpContentType.Json.EqualsIgnoreCase(uri.ContentType.Format)
                    ? Energistics.Etp.Common.EtpExtensions.Serialize(entity)
                    : WitsmlParser.ToXml(entity, nilOnly: true, removeTypePrefix: true);

                dataObject.SetString(data, false);
            }

            double version;
            var uuid = double.TryParse(uri.Version, out version) && version >= 2.0 ? uri.ObjectId : null;

            if (string.IsNullOrWhiteSpace(uuid))
            {
                uuid = entity.GetPropertyValue<string>(ObjectTypes.Uuid);
            }

            dataObject.Resource = new Resource
            {
                Uri = uri,
                Uuid = uuid ?? string.Empty,
                Name = name,
                TargetCount = childCount,
                ContentType = uri.ContentType,
                ResourceType = ResourceKind.DataObject,
                CustomData = new Dictionary<string, string>(),
                LastChanged = lastChanged,
                ChannelSubscribable = uri.IsChannelSubscribable(),
                ObjectNotifiable = uri.IsObjectNotifiable()
            };
        }

        /// <summary>
        /// Handles the GetObject message of the Store protocol.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetObject, DataObject}" /> instance containing the event data.</param>
        /// <param name="dataObjects">The data objects.</param>
        /// <param name="errors">The errors.</param>
        protected override void HandleGetDataObjects(ProtocolEventArgs<GetDataObjects> args, IList<DataObject> dataObjects, IList<ErrorInfo> errors)
        {
            foreach (var uri in args.Message.Uris)
            {
                GetObject(args, uri, dataObjects, errors);
            }
        }

        /// <summary>
        /// Handles the PutObject message of the Store protocol.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="putDataObjects">The put data objects message.</param>
        protected override void HandlePutDataObjects(IMessageHeader header, PutDataObjects putDataObjects)
        {
            base.HandlePutDataObjects(header, putDataObjects);

            foreach (var dataObject in putDataObjects.DataObjects)
            {
                PutObject(header, dataObject);
            }
        }

        /// <summary>
        /// Handles the DeleteObject message of the Store protocol.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="deleteDataObjects">The delete data objects message.</param>
        protected override void HandleDeleteDataObjects(IMessageHeader header, DeleteDataObjects deleteDataObjects)
        {
            base.HandleDeleteDataObjects(header, deleteDataObjects);

            foreach (var uri in deleteDataObjects.Uris)
            {
                DeleteObject(header, uri);
            }
        }

        /// <summary>
        /// Gets the data object from the data store.
        /// </summary>
        /// <param name="args">The event args.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="dataObjects">The data objects.</param>
        /// <param name="errors">The errors.</param>
        private void GetObject(ProtocolEventArgs<GetDataObjects> args, string uri, IList<DataObject> dataObjects, IList<ErrorInfo> errors)
        {
            try
            {
                var dataObject = new DataObject();
                var etpUri = new EtpUri(uri);

                if (!etpUri.IsValid)
                {
                    errors.Add(new ErrorInfo
                    {
                        Code = (int) EtpErrorCodes.InvalidUri,
                        Message = "Invalid data object URI",
                        Uri = uri
                    });
                    return;
                }

                if (string.IsNullOrWhiteSpace(etpUri.ObjectType))
                {
                    errors.Add(new ErrorInfo
                    {
                        Code = (int) EtpErrorCodes.UnsupportedObject,
                        Message = "Data object not supported",
                        Uri = uri
                    });
                    return;
                }

                // Validate that objectId was provided
                if (string.IsNullOrWhiteSpace(etpUri.ObjectId))
                {
                    errors.Add(new ErrorInfo
                    {
                        Code = (int) EtpErrorCodes.InvalidUri,
                        Message = "Invalid data object URI",
                        Uri = uri
                    });
                    return;
                }

                WitsmlOperationContext.Current.Request = new RequestContext(Functions.GetObject, etpUri.ObjectType, null, null, null);

                var provider = Container.Resolve<IStoreStoreProvider>(new ObjectName(etpUri.GetDataSchemaVersion()));
                provider.GetObject(Session.Adapter, args, uri, dataObject);

                if (dataObject.Data == null || dataObject.Data.Length < 1)
                {
                    errors.Add(new ErrorInfo
                    {
                        Code = (int) EtpErrorCodes.NotFound,
                        Message = "Data object not found",
                        Uri = uri
                    });
                    return;
                }

                // Success
                dataObjects.Add(dataObject);
            }
            catch (ContainerException)
            {
                errors.Add(new ErrorInfo
                {
                    Code = (int) EtpErrorCodes.UnsupportedObject,
                    Message = "Data object not supported",
                    Uri = uri
                });
            }
        }

        /// <summary>
        /// Puts the data object into the data store.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="dataObject">The data object.</param>
        private void PutObject(IMessageHeader header, DataObject dataObject)
        {
            try
            {
                var uri = this.CreateAndValidateUri(dataObject.Resource.Uri, header.MessageId);

                if (!uri.IsValid) return;
                if (!this.ValidateUriParentHierarchy(uri, header.MessageId)) return;
                if (!this.ValidateUriObjectType(uri, header.MessageId)) return;

                var data = dataObject.GetString();

                if (EtpContentType.Json.EqualsIgnoreCase(uri.ContentType.Format))
                {
                    var objectType = uri.IsRelatedTo(EtpUris.Witsml200) || uri.IsRelatedTo(EtpUris.Eml210)
                        ? ObjectTypes.GetObjectType(uri.ObjectType, uri.Family, OptionsIn.DataVersion.Version200.Value)
                        : ObjectTypes.GetObjectGroupType(uri.ObjectType, uri.Family, uri.Version);

                    var instance = Energistics.Etp.Common.EtpExtensions.Deserialize(objectType, data);
                    data = WitsmlParser.ToXml(instance);
                }

                WitsmlOperationContext.Current.Request = new RequestContext(Functions.PutObject, uri.ObjectType, data, null, null);

                var dataAdapter = Container.Resolve<IEtpDataProvider>(new ObjectName(uri.ObjectType, uri.Family, uri.GetDataSchemaVersion()));
                dataAdapter.Put(dataObject);

                Acknowledge(header.MessageId);
            }
            catch (ContainerException ex)
            {
                this.UnsupportedObject(ex, dataObject.Resource.Uri, header.MessageId);
            }
            catch (WitsmlException ex)
            {
                ProtocolException((int)EtpErrorCodes.InvalidObject, $"Invalid object: {ex.Message}; Error code: {(int)ex.ErrorCode}", header.MessageId);
            }
        }

        /// <summary>
        /// Deletes the object from the data store.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="uri">The URI.</param>
        private void DeleteObject(IMessageHeader header, string uri)
        {
            try
            {
                var etpUri = this.CreateAndValidateUri(uri, header.MessageId);

                if (!etpUri.IsValid) return;
                if (!this.ValidateUriObjectType(etpUri, header.MessageId)) return;

                WitsmlOperationContext.Current.Request = new RequestContext(Functions.DeleteObject, etpUri.ObjectType, null, null, null);

                var dataAdapter = Container.Resolve<IEtpDataProvider>(new ObjectName(etpUri.ObjectType, etpUri.Family, etpUri.GetDataSchemaVersion()));
                dataAdapter.Delete(etpUri);

                Acknowledge(header.MessageId);
            }
            catch (ContainerException ex)
            {
                this.UnsupportedObject(ex, uri, header.MessageId);
            }
            catch (WitsmlException ex)
            {
                if (ex.ErrorCode.Equals(ErrorCodes.NotBottomLevelDataObject))
                {
                    this.NoCascadeDelete(uri, header.MessageId);
                }
                else
                {
                    this.InvalidObject(ex, uri, header.MessageId);
                }
            }
        }
    }
}
