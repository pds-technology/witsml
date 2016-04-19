//----------------------------------------------------------------------- 
// ETP DevKit, 1.0
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
using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;

namespace Energistics.Protocol.Store
{
    /// <summary>
    /// Base implementation of the <see cref="IStoreCustomer"/> interface.
    /// </summary>
    /// <seealso cref="Energistics.Common.EtpProtocolHandler" />
    /// <seealso cref="Energistics.Protocol.Store.IStoreCustomer" />
    public class StoreCustomerHandler : EtpProtocolHandler, IStoreCustomer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StoreCustomerHandler"/> class.
        /// </summary>
        public StoreCustomerHandler() : base(Protocols.Store, "customer", "store")
        {
        }

        /// <summary>
        /// Sends a GetObject message to a store.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public virtual void GetObject(string uri)
        {
            var header = CreateMessageHeader(Protocols.Store, MessageTypes.Store.GetObject);

            var getObject = new GetObject()
            {
                Uri = uri
            };

            Session.SendMessage(header, getObject);
        }

        /// <summary>
        /// Sends a PutObject message to a store.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        public virtual void PutObject(DataObject dataObject)
        {
            var header = CreateMessageHeader(Protocols.Store, MessageTypes.Store.PutObject);

            var putObject = new PutObject()
            {
                Data = dataObject
            };

            Session.SendMessage(header, putObject);
        }

        /// <summary>
        /// Sends a DeleteObject message to a store.
        /// </summary>
        /// <param name="uris">The list of URIs.</param>
        public virtual void DeleteObject(IList<string> uris)
        {
            var header = CreateMessageHeader(Protocols.Store, MessageTypes.Store.DeleteObject);

            var deleteObject = new DeleteObject()
            {
                Uri = uris
            };

            Session.SendMessage(header, deleteObject);
        }

        /// <summary>
        /// Handles the Object event from a store.
        /// </summary>
        public event ProtocolEventHandler<Object> OnObject;

        /// <summary>
        /// Decodes the message based on the message type contained in the specified <see cref="MessageHeader" />.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="decoder">The message decoder.</param>
        protected override void HandleMessage(MessageHeader header, Decoder decoder)
        {
            switch (header.MessageType)
            {
                case (int)MessageTypes.Store.Object:
                    HandleObject(header, decoder.Decode<Object>());
                    break;

                default:
                    base.HandleMessage(header, decoder);
                    break;
            }
        }

        /// <summary>
        /// Handles the Object message from a store.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="object">The Object message.</param>
        protected virtual void HandleObject(MessageHeader header, Object @object)
        {
            Notify(OnObject, header, @object);
        }
    }
}
