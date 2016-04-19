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
    /// <summary>
    /// Provides common functionality for ETP protocol handlers.
    /// </summary>
    /// <seealso cref="Energistics.Common.EtpBase" />
    /// <seealso cref="Energistics.Common.IProtocolHandler" />
    public abstract class EtpProtocolHandler : EtpBase, IProtocolHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EtpProtocolHandler"/> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="role">The role.</param>
        /// <param name="requestedRole">The requested role.</param>
        protected EtpProtocolHandler(Protocols protocol, string role, string requestedRole) : this((int)protocol, role, requestedRole)
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpProtocolHandler"/> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="role">The role.</param>
        /// <param name="requestedRole">The requested role.</param>
        protected EtpProtocolHandler(int protocol, string role, string requestedRole)
        {
            Protocol = protocol;
            Role = role;
            RequestedRole = requestedRole;
        }

        /// <summary>
        /// Gets or sets the ETP session.
        /// </summary>
        /// <value>The session.</value>
        public virtual IEtpSession Session { get; set; }

        /// <summary>
        /// Gets the protocol.
        /// </summary>
        /// <value>The protocol.</value>
        public int Protocol { get; }

        /// <summary>
        /// Gets the role.
        /// </summary>
        /// <value>The role.</value>
        public string Role { get; }

        /// <summary>
        /// Gets the requested role.
        /// </summary>
        /// <value>The requested role.</value>
        public string RequestedRole { get; }

        /// <summary>
        /// Gets the capabilities supported by the protocol handler.
        /// </summary>
        /// <returns>A collection of protocol capabilities.</returns>
        public virtual IDictionary<string, DataValue> GetCapabilities()
        {
            return new Dictionary<string, DataValue>();
        }

        /// <summary>
        /// Sends an Acknowledge message with the specified correlation identifier and message flag.
        /// </summary>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="messageFlag">The message flag.</param>
        public virtual void Acknowledge(long correlationId, MessageFlags messageFlag = MessageFlags.None)
        {
            var header = CreateMessageHeader(Protocol, (int)MessageTypes.Core.Acknowledge, correlationId, messageFlag);
            var acknowledge = new Acknowledge();

            Session.SendMessage(header, acknowledge);
        }

        /// <summary>
        /// Sends a ProtocolException message with the specified error code, message and correlation identifier.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="correlationId">The correlation identifier.</param>
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

        /// <summary>
        /// Occurs when an Acknowledge message is received for the current protocol.
        /// </summary>
        public event ProtocolEventHandler<Acknowledge> OnAcknowledge;

        /// <summary>
        /// Occurs when a ProtocolException message is received for the current protocol.
        /// </summary>
        public event ProtocolEventHandler<ProtocolException> OnProtocolException;

        /// <summary>
        /// Sends a ProtocolException message for an invalid message type.
        /// </summary>
        /// <param name="header">The message header.</param>
        protected virtual void InvalidMessage(MessageHeader header)
        {
            ProtocolException((int)ErrorCodes.EINVALID_MESSAGETYPE, "Invalid message type: " + header.MessageType, header.MessageId);
        }

        /// <summary>
        /// Decodes the message based on the message type contained in the specified <see cref="MessageHeader"/>.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="decoder">The message decoder.</param>
        void IProtocolHandler.HandleMessage(MessageHeader header, Decoder decoder)
        {
            HandleMessage(header, decoder);
        }

        /// <summary>
        /// Decodes the message based on the message type contained in the specified <see cref="MessageHeader"/>.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="decoder">The message decoder.</param>
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

        /// <summary>
        /// Handles the Acknowledge message.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="acknowledge">The Acknowledge message.</param>
        protected virtual void HandleAcknowledge(MessageHeader header, Acknowledge acknowledge)
        {
            Notify(OnAcknowledge, header, acknowledge);
        }

        /// <summary>
        /// Handles the ProtocolException message.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="protocolException">The ProtocolException message.</param>
        protected virtual void HandleProtocolException(MessageHeader header, ProtocolException protocolException)
        {
            Notify(OnProtocolException, header, protocolException);
            Logger.ErrorFormat("[{0}] Protocol exception: {1} - {2}", Session.SessionId, protocolException.ErrorCode, protocolException.ErrorMessage);
        }

        /// <summary>
        /// Notifies subscribers of the specified event handler.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="handler">The message handler.</param>
        /// <param name="header">The message header.</param>
        /// <param name="message">The message body.</param>
        /// <returns>The protocol event args.</returns>
        protected ProtocolEventArgs<T> Notify<T>(ProtocolEventHandler<T> handler, MessageHeader header, T message) where T : ISpecificRecord
        {
            var args = new ProtocolEventArgs<T>(header, message);
            Received(header, message);
            handler?.Invoke(this, args);
            return args;
        }

        /// <summary>
        /// Notifies subscribers of the specified event handler.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <typeparam name="TContext">The type of the context.</typeparam>
        /// <param name="handler">The message handler.</param>
        /// <param name="header">The message header.</param>
        /// <param name="message">The message body.</param>
        /// <param name="context">The message context.</param>
        /// <returns>The protocol event args.</returns>
        protected ProtocolEventArgs<T, TContext> Notify<T, TContext>(ProtocolEventHandler<T, TContext> handler, MessageHeader header, T message, TContext context) where T : ISpecificRecord
        {
            var args = new ProtocolEventArgs<T, TContext>(header, message, context);
            Received(header, message);
            handler?.Invoke(this, args);
            return args;
        }

        /// <summary>
        /// Logs the specified message header and body.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="header">The message header.</param>
        /// <param name="message">The message body.</param>
        protected void Received<T>(MessageHeader header, T message)
        {
            if (Session?.Output == null) return;
            Session.Format("[{0}] Message received at {1}", Session.SessionId, DateTime.Now);
            Session.Format(this.Serialize(header));
            Session.Format(this.Serialize(message, true));
        }

        /// <summary>
        /// Creates a message header for the specified protocol, message type, correlation identifier and message flag.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        /// <param name="protocol">The protocol.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="messageFlags">The message flags.</param>
        /// <returns>A new message header instance.</returns>
        protected MessageHeader CreateMessageHeader<TEnum>(Protocols protocol, TEnum messageType, long correlationId = 0, MessageFlags messageFlags = MessageFlags.None) where TEnum : IConvertible
        {
            return CreateMessageHeader(protocol, Convert.ToInt32(messageType), correlationId, messageFlags);
        }

        /// <summary>
        /// Creates a message header for the specified protocol, message type, correlation identifier and message flag.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="messageFlags">The message flags.</param>
        /// <returns>A new message header instance.</returns>
        protected MessageHeader CreateMessageHeader(Protocols protocol, int messageType, long correlationId = 0, MessageFlags messageFlags = MessageFlags.None)
        {
            return CreateMessageHeader((int)protocol, messageType, correlationId, messageFlags);
        }

        /// <summary>
        /// Creates a message header for the specified protocol, message type, correlation identifier and message flag.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="messageFlags">The message flags.</param>
        /// <returns>A new message header instance.</returns>
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
