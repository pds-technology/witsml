using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;

namespace Energistics.Protocol.Store
{
    public class StoreStoreHandler : EtpProtocolHandler, IStoreStore
    {
        public StoreStoreHandler() : base(Protocols.Store, "store")
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
