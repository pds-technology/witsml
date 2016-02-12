using System;
using Avro.IO;
using Avro.Specific;
using Energistics.Datatypes;
using Energistics.Protocol;
using Energistics.Protocol.Core;

namespace Energistics.Common
{
    public abstract class EtpProtocolHandler : EtpBase, IProtocolHandler
    {
        public EtpProtocolHandler(Protocols protocol, string role) : this((int)protocol, role)
        { 
        }

        public EtpProtocolHandler(int protocol, string role)
        {
            Protocol = protocol;
            Role = role;
        }

        public IEtpSession Session { get; set; }

        public int Protocol { get; private set; }

        public string Role { get; private set; }

        public string RequestedRole { get; protected set; }

        public virtual void Acknowledge(int correlationId)
        {
            var header = CreateMessageHeader(Protocol, (int)MessageTypes.Core.Acknowledge, correlationId);
            var acknowledge = new Acknowledge();

            Session.SendMessage(header, acknowledge);
        }

        public virtual void ProtocolException(int errorCode, string errorMessage, long correlationId = 0)
        {
            var header = CreateMessageHeader(Protocol, (int)MessageTypes.Core.ProtocolException, correlationId);

            var error = new ProtocolException()
            {
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };

            Session.SendMessage(header, error);
        }

        public event ProtocolEventHandler<Acknowledge> OnAcknowledge;

        public event ProtocolEventHandler<ProtocolException> OnProtocolException;

        protected virtual void InvalidMessage(MessageHeader header)
        {
            ProtocolException(2, "Invalid message type: " + header.MessageType, header.MessageId);
        }

        void IProtocolHandler.HandleMessage(MessageHeader header, Decoder decoder)
        {
            HandleMessage(header, decoder);
        }

        protected virtual void HandleMessage(MessageHeader header, Decoder decoder)
        {
            switch (header.MessageType)
            {
                case (int)MessageTypes.Core.ProtocolException:
                    HandleProtocolException(header, decoder.Decode<ProtocolException>());
                    break;

                case (int)MessageTypes.Core.Acknowledge:
                    HandleAcknowledge(header, decoder.Decode<Acknowledge>());
                    break;

                default:
                    InvalidMessage(header);
                    break;
            }
        }

        protected virtual void HandleAcknowledge(MessageHeader header, Acknowledge acknowledge)
        {
            Notify(OnAcknowledge, header, acknowledge);
        }

        protected virtual void HandleProtocolException(MessageHeader header, ProtocolException protocolException)
        {
            Notify(OnProtocolException, header, protocolException);
            Logger.ErrorFormat("[{0}] Protocol exception: {1} - {2}", Session.SessionId, protocolException.ErrorCode, protocolException.ErrorMessage);
        }

        protected ProtocolEventArgs<T> Notify<T>(ProtocolEventHandler<T> handler, MessageHeader header, T message) where T : ISpecificRecord
        {
            var args = new ProtocolEventArgs<T>(header, message);

            if (handler != null)
            {
                handler(this, args);
            }

            return args;
        }

        protected ProtocolEventArgs<T, V> Notify<T, V>(ProtocolEventHandler<T, V> handler, MessageHeader header, T message, V context) where T : ISpecificRecord
        {
            var args = new ProtocolEventArgs<T, V>(header, message, context);

            if (handler != null)
            {
                handler(this, args);
            }

            return args;
        }

        protected MessageHeader CreateMessageHeader<TEnum>(Protocols protocol, TEnum messageType, long correlationId = 0, MessageFlags messageFlags = MessageFlags.None) where TEnum : IConvertible
        {
            return CreateMessageHeader(protocol, Convert.ToInt32(messageType), correlationId, messageFlags);
        }

        protected MessageHeader CreateMessageHeader(Protocols protocol, int messageType, long correlationId = 0, MessageFlags messageFlags = MessageFlags.None)
        {
            return CreateMessageHeader((int)protocol, messageType, correlationId, messageFlags);
        }

        protected MessageHeader CreateMessageHeader(int protocol, int messageType, long correlationId = 0, MessageFlags messageFlags = MessageFlags.None)
        {
            return new MessageHeader()
            {
                Protocol = protocol,
                MessageType = messageType,
                MessageId = Session.NewMessageId(),
                MessageFlags = (int)messageFlags,
                CorrelationId = correlationId
            };
        }
    }
}