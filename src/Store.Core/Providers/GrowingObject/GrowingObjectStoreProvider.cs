//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol;
using Energistics.Protocol.GrowingObject;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Data.GrowingObjects;

namespace PDS.WITSMLstudio.Store.Providers.GrowingObject
{
    /// <summary>
    /// Process messages received for the Store role of the GrowingObject protocol.
    /// </summary>
    /// <seealso cref="Energistics.Protocol.GrowingObject.GrowingObjectStoreHandler" />
    [Export(typeof(IGrowingObjectStore))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class GrowingObjectStoreProvider : GrowingObjectStoreHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrowingObjectStoreProvider" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        [ImportingConstructor]
        public GrowingObjectStoreProvider(IContainer container)
        {
            Container = container;
        }

        /// <summary>
        /// Gets the composition container.
        /// </summary>
        /// <value>The container.</value>
        public IContainer Container { get; }

        /// <summary>
        /// Handles the GrowingObjectGet message from a customer.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="message">The message.</param>
        protected override void HandleGrowingObjectGet(MessageHeader header, GrowingObjectGet message)
        {
            base.HandleGrowingObjectGet(header, message);

            EtpUri uri;
            var dataAdapter = GetDataAdapterAndValidateUri(header.MessageId, message.Uri, out uri);
            if (dataAdapter == null) return;

            DataObject dataObject;

            try
            {
                dataObject = dataAdapter.GetGrowingPart(uri, message.Uid);
            }
            catch (NotImplementedException)
            {
                this.NotSupported(header.MessageId);
                return;
            }

            if (dataObject == null)
            {
                NotFound(header.MessageId, message.Uri);
                return;
            }

            SendObjectFragments(header.MessageId, message.Uri, new[] { dataObject });
        }

        /// <summary>
        /// Handles the GrowingObjectGetRange message from a customer.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="message">The message.</param>
        protected override void HandleGrowingObjectGetRange(MessageHeader header, GrowingObjectGetRange message)
        {
            base.HandleGrowingObjectGetRange(header, message);

            EtpUri uri;
            var dataAdapter = GetDataAdapterAndValidateUri(header.MessageId, message.Uri, out uri);
            if (dataAdapter == null) return;

            IList<DataObject> dataObjects;

            try
            {
                dataObjects = dataAdapter.GetGrowingParts(uri, message.StartIndex.Item, message.EndIndex.Item);
            }
            catch (NotImplementedException)
            {
                this.NotSupported(header.MessageId);
                return;
            }

            if (!dataObjects.Any())
            {
                NotFound(header.MessageId, message.Uri);
                return;
            }

            SendObjectFragments(header.MessageId, message.Uri, dataObjects);
        }

        /// <summary>
        /// Handles the GrowingObjectPut message from a customer.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="message">The message.</param>
        protected override void HandleGrowingObjectPut(MessageHeader header, GrowingObjectPut message)
        {
            base.HandleGrowingObjectPut(header, message);

            EtpUri uri;
            var dataAdapter = GetDataAdapterAndValidateUri(header.MessageId, message.Uri, out uri);
            if (dataAdapter == null) return;

            try
            {
                dataAdapter.PutGrowingPart(uri, message.ContentType, message.Data);
            }
            catch (NotImplementedException)
            {
                this.NotSupported(header.MessageId);
            }
        }

        /// <summary>
        /// Handles the GrowingObjectDelete message from a customer.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="message">The message.</param>
        protected override void HandleGrowingObjectDelete(MessageHeader header, GrowingObjectDelete message)
        {
            base.HandleGrowingObjectDelete(header, message);

            EtpUri uri;
            var dataAdapter = GetDataAdapterAndValidateUri(header.MessageId, message.Uri, out uri);
            if (dataAdapter == null) return;

            try
            {
                dataAdapter.DeleteGrowingPart(uri, message.Uid);
            }
            catch (NotImplementedException)
            {
                this.NotSupported(header.MessageId);
            }
        }

        /// <summary>
        /// Handles the GrowingObjectDeleteRange message from a customer.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="message">The message.</param>
        protected override void HandleGrowingObjectDeleteRange(MessageHeader header, GrowingObjectDeleteRange message)
        {
            base.HandleGrowingObjectDeleteRange(header, message);

            EtpUri uri;
            var dataAdapter = GetDataAdapterAndValidateUri(header.MessageId, message.Uri, out uri);
            if (dataAdapter == null) return;

            try
            {
                dataAdapter.DeleteGrowingParts(uri, message.StartIndex.Item, message.EndIndex.Item);
            }
            catch (NotImplementedException)
            {
                this.NotSupported(header.MessageId);
            }
        }

        private IGrowingObjectDataAdapter GetDataAdapterAndValidateUri(long messageId, string uri, out EtpUri etpUri)
        {
            etpUri = this.CreateAndValidateUri(uri, messageId);

            if (!etpUri.IsValid || !this.ValidateUriObjectType(etpUri, messageId))
            {
                return null;
            }

            if (!ObjectTypes.IsGrowingDataObject(etpUri.ObjectType))
            {
                this.UnsupportedObject(null, uri, messageId);
                return null;
            }

            try
            {
                return Container.Resolve<IGrowingObjectDataAdapter>(new ObjectName(etpUri.ObjectType, etpUri.Version));
            }
            catch (ContainerException ex)
            {
                this.UnsupportedObject(ex, uri, messageId);
                return null;
            }
        }

        private void SendObjectFragments(long messageId, string uri, ICollection<DataObject> dataObjects)
        {
            dataObjects.ForEach((dataObject, i) =>
            {
                var flag = i >= dataObjects.Count - 1
                    ? MessageFlags.FinalPart
                    : MessageFlags.MultiPart;

                ObjectFragment(uri, dataObject.Resource.ContentType, dataObject.Data, messageId, flag);
            });
        }

        private void NotFound(long messageId, string uri)
        {
            ProtocolException(11, "Not Found: " + uri, messageId);
        }
    }
}
