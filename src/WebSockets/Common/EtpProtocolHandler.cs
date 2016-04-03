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

using System;
using System.Collections.Generic;
using Avro.IO;
using Avro.Specific;
using Energistics.Datatypes;
using Energistics.Protocol;
using Energistics.Protocol.Core;

namespace Energistics.Common
{
    public abstract class EtpProtocolHandler : EtpBase, IProtocolHandler
    {
        public EtpProtocolHandler(Protocols protocol, string role, string requestedRole) : this((int)protocol, role, requestedRole)
        { 
        }

        public EtpProtocolHandler(int protocol, string role, string requestedRole)
        {
            Protocol = protocol;
            Role = role;
            RequestedRole = requestedRole;
        }

        public virtual IEtpSession Session { get; set; }

        public int Protocol { get; private set; }

        public string Role { get; private set; }

        public string RequestedRole { get; private set; }

        public virtual IDictionary<string, DataValue> GetCapabilities()
        {
            return new Dictionary<string, DataValue>();
        }

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
            ProtocolException((int)ErrorCodes.EINVALID_MESSAGETYPE, "Invalid message type: " + header.MessageType, header.MessageId);
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

            Received(header, message);

            if (handler != null)
            {
                handler(this, args);
            }

            return args;
        }

        protected ProtocolEventArgs<T, V> Notify<T, V>(ProtocolEventHandler<T, V> handler, MessageHeader header, T message, V context) where T : ISpecificRecord
        {
            var args = new ProtocolEventArgs<T, V>(header, message, context);

            Received(header, message);

            if (handler != null)
            {
                handler(this, args);
            }

            return args;
        }

        protected void Received<T>(MessageHeader header, T message)
        {
            if (Session.Output != null)
            {
                Session.Format("[{0}] Message received at {1}", Session.SessionId, System.DateTime.Now);
                Session.Format(this.Serialize(header));
                Session.Format(this.Serialize(message, true));
            }
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
