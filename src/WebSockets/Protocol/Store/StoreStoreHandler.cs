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

using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;

namespace Energistics.Protocol.Store
{
    public class StoreStoreHandler : EtpProtocolHandler, IStoreStore
    {
        public StoreStoreHandler() : base(Protocols.Store, "store", "customer")
        {
        }

        public virtual void Object(DataObject dataObject)
        {
            var header = CreateMessageHeader(Protocols.Store, MessageTypes.Store.Object, messageFlags: MessageFlags.FinalPart);

            var @object = new Object()
            {
                DataObject = dataObject
            };

            Session.SendMessage(header, @object);
        }

        public event ProtocolEventHandler<GetObject, DataObject> OnGetObject;

        public event ProtocolEventHandler<PutObject> OnPutObject;

        public event ProtocolEventHandler<DeleteObject> OnDeleteObject;

        protected override void HandleMessage(MessageHeader header, Decoder decoder)
        {
            switch (header.MessageType)
            {
                case (int)MessageTypes.Store.GetObject:
                    HandleGetObject(header, decoder.Decode<GetObject>());
                    break;

                case (int)MessageTypes.Store.PutObject:
                    HandlePutObject(header, decoder.Decode<PutObject>());
                    break;

                case (int)MessageTypes.Store.DeleteObject:
                    HandleDeleteObject(header, decoder.Decode<DeleteObject>());
                    break;

                default:
                    base.HandleMessage(header, decoder);
                    break;
            }
        }

        protected virtual void HandleGetObject(MessageHeader header, GetObject getObject)
        {
            var args = Notify(OnGetObject, header, getObject, new DataObject());
            HandleGetObject(args);

            Object(args.Context);
        }

        protected virtual void HandleGetObject(ProtocolEventArgs<GetObject, DataObject> args)
        {
        }

        protected virtual void HandlePutObject(MessageHeader header, PutObject putObject)
        {
            Notify(OnPutObject, header, putObject);
        }

        protected virtual void HandleDeleteObject(MessageHeader header, DeleteObject deleteObject)
        {
            Notify(OnDeleteObject, header, deleteObject);
        }
    }
}
