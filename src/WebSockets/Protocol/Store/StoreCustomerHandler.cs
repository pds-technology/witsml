using System.Collections.Generic;
using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;

namespace Energistics.Protocol.Store
{
    public class StoreCustomerHandler : EtpProtocolHandler, IStoreCustomer
    {
        public StoreCustomerHandler() : base(Protocols.Store, "customer")
        {
            RequestedRole = "store";
        }

        public virtual void GetObject(string uri)
        {
            var header = CreateMessageHeader(Protocols.Store, MessageTypes.Store.GetObject);

            var getObject = new GetObject()
            {
                Uri = uri
            };

            Session.SendMessage(header, getObject);
        }

        public virtual void PutObject(DataObject dataObject)
        {
            var header = CreateMessageHeader(Protocols.Store, MessageTypes.Store.PutObject);

            var putObject = new PutObject()
            {
                Data = dataObject
            };

            Session.SendMessage(header, putObject);
        }

        public virtual void DeleteObject(IList<string> uris)
        {
            var header = CreateMessageHeader(Protocols.Store, MessageTypes.Store.DeleteObject);

            var deleteObject = new DeleteObject()
            {
                Uri = uris
            };

            Session.SendMessage(header, deleteObject);
        }

        public event ProtocolEventHandler<Object> OnObject;

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

        protected virtual void HandleObject(MessageHeader header, Object @object)
        {
            Notify(OnObject, header, @object);
        }
    }
}
