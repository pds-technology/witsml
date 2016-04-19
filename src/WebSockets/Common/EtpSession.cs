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
using System.IO;
using System.Linq;
using System.Threading;
using Avro.IO;
using Avro.Specific;
using Energistics.Datatypes;

namespace Energistics.Common
{
    /// <summary>
    /// Provides common functionality for all ETP sessions.
    /// </summary>
    /// <seealso cref="Energistics.Common.EtpBase" />
    /// <seealso cref="Energistics.Common.IEtpSession" />
    public abstract class EtpSession : EtpBase, IEtpSession
    {
        private long _messageId;

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpSession"/> class.
        /// </summary>
        /// <param name="application">The application name.</param>
        /// <param name="version">The application version.</param>
        /// <param name="headers">The WebSocket or HTTP headers.</param>
        protected EtpSession(string application, string version, IDictionary<string, string> headers)
        {
            Headers = headers ?? new Dictionary<string, string>();
            Handlers = new Dictionary<object, IProtocolHandler>();
            ApplicationName = application;
            ApplicationVersion = version;
            ValidateHeaders();
        }

        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        /// <value>The name of the application.</value>
        public string ApplicationName { get; }

        /// <summary>
        /// Gets the application version.
        /// </summary>
        /// <value>The application version.</value>
        public string ApplicationVersion { get; }

        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        /// <value>The session identifier.</value>
        public string SessionId { get; set; }

        /// <summary>
        /// Gets the collection of WebSocket or HTTP headers.
        /// </summary>
        /// <value>The headers.</value>
        protected IDictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets the collection of registered protocol handlers.
        /// </summary>
        /// <value>The handlers.</value>
        protected IDictionary<object, IProtocolHandler> Handlers { get; }

        /// <summary>
        /// Gets the registered protocol handler for the specified ETP interface.
        /// </summary>
        /// <typeparam name="T">The protocol handler interface.</typeparam>
        /// <returns>The registered protocol handler instance.</returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public T Handler<T>() where T : IProtocolHandler
        {
            IProtocolHandler handler;

            if (Handlers.TryGetValue(typeof(T), out handler) && handler is T)
            {
                return (T)handler;
            }

            Logger.Error(Format("[{0}] Protocol handler not registered for {1}.", SessionId, typeof(T).FullName));
            throw new NotSupportedException(string.Format("Protocol handler not registered for {0}.", typeof(T).FullName));
        }

        /// <summary>
        /// Determines whether this instance can handle the specified protocol.
        /// </summary>
        /// <typeparam name="T">The protocol handler interface.</typeparam>
        /// <returns>
        ///   <c>true</c> if the specified protocol handler has been registered; otherwise, <c>false</c>.
        /// </returns>
        public bool CanHandle<T>() where T : IProtocolHandler
        {
            return Handlers.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Called when the ETP session is opened.
        /// </summary>
        /// <param name="supportedProtocols">The supported protocols.</param>
        public override void OnSessionOpened(IList<SupportedProtocol> supportedProtocols)
        {
            HandleUnsupportedProtocols(supportedProtocols);

            // notify protocol handlers about new session
            foreach (var item in Handlers)
            {
                if (item.Key is Type)
                    item.Value.OnSessionOpened(supportedProtocols);
            }
        }

        /// <summary>
        /// Called when WebSocket data is received.
        /// </summary>
        /// <param name="data">The data.</param>
        public virtual void OnDataReceived(byte[] data)
        {
            Decode(data);
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="header">The header.</param>
        /// <param name="body">The body.</param>
        public void SendMessage<T>(MessageHeader header, T body) where T : ISpecificRecord
        {
            byte[] data;

            try
            {
                data = body.Encode(header);
            }
            catch (Exception ex)
            {
                Handler(header.Protocol)
                    .ProtocolException(1000, ex.Message, header.MessageId);

                return;
            }

            Send(data, 0, data.Length);
            Sent(header, body);
        }

        /// <summary>
        /// Gets the supported protocols.
        /// </summary>
        /// <param name="isSender">if set to <c>true</c> the current session is the sender.</param>
        /// <returns>A list of supported protocols.</returns>
        public IList<SupportedProtocol> GetSupportedProtocols(bool isSender = false)
        {
            var supportedProtocols = new List<SupportedProtocol>();
            var version = new Datatypes.Version()
            {
                Major = 1
            };

            // Skip Core protocol (0)
            foreach (var handler in Handlers.Values.Where(x => x.Protocol > 0))
            {
                var role = isSender ? handler.RequestedRole : handler.Role;

                if (supportedProtocols.Contains(handler.Protocol, role))
                    continue;

                supportedProtocols.Add(new SupportedProtocol()
                {
                    Protocol = handler.Protocol,
                    ProtocolVersion = version,
                    ProtocolCapabilities = handler.GetCapabilities(),
                    Role = role
                });
            }

            return supportedProtocols;
        }

        /// <summary>
        /// Generates a new unique message identifier for the current session.
        /// </summary>
        /// <returns>The message identifier.</returns>
        public long NewMessageId()
        {
            return Interlocked.Increment(ref _messageId);
        }

        /// <summary>
        /// Closes the WebSocket connection for the specified reason.
        /// </summary>
        /// <param name="reason">The reason.</param>
        public abstract void Close(string reason);

        /// <summary>
        /// Sends the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        protected abstract void Send(byte[] data, int offset, int length);

        /// <summary>
        /// Decodes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        protected void Decode(byte[] data)
        {
            using (var inputStream = new MemoryStream(data))
            {
                // create avro binary decoder to read from memory stream
                var decoder = new BinaryDecoder(inputStream);
                // deserialize the header
                var header = decoder.Decode<MessageHeader>();

                // log message metadata
                if (Logger.IsDebugEnabled)
                {
                    Logger.DebugFormat("[{0}] Message received: {1}", SessionId, this.Serialize(header));
                }

                // call processing action
                HandleMessage(header, decoder);
            }
        }

        /// <summary>
        /// Handles the message.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="decoder">The decoder.</param>
        protected void HandleMessage(MessageHeader header, Decoder decoder)
        {
            if (Handlers.ContainsKey(header.Protocol))
            {
                Handler(header.Protocol)
                    .HandleMessage(header, decoder);
            }
            else
            {
                var message = string.Format("Protocol handler not registered for protocol {0}.", header.Protocol);

                Handler((int)Protocols.Core)
                    .ProtocolException((int)ErrorCodes.EUNSUPPORTED_PROTOCOL, message, header.MessageId);
            }
        }

        /// <summary>
        /// Registers a protocol handler for the specified contract type.
        /// </summary>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="handlerType">Type of the handler.</param>
        protected override void Register(Type contractType, Type handlerType)
        {
            base.Register(contractType, handlerType);

            var handler = CreateInstance(contractType);

            if (handler != null)
            {
                handler.Session = this;
                Handlers[contractType] = handler;
                Handlers[handler.Protocol] = handler;
            }
        }

        /// <summary>
        /// Get the registered handler for the specified protocol.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <returns>The registered protocol handler instance.</returns>
        /// <exception cref="System.NotSupportedException"></exception>
        protected IProtocolHandler Handler(int protocol)
        {
            if (Handlers.ContainsKey(protocol))
            {
                return Handlers[protocol];
            }

            Logger.Error(Format("[{0}] Protocol handler not registered for protocol {1}.", SessionId, protocol));
            throw new NotSupportedException(string.Format("Protocol handler not registered for protocol {0}.", protocol));
        }

        /// <summary>
        /// Handles the unsupported protocols.
        /// </summary>
        /// <param name="supportedProtocols">The supported protocols.</param>
        protected virtual void HandleUnsupportedProtocols(IList<SupportedProtocol> supportedProtocols)
        {
            // remove unsupported handler mappings (excluding Core protocol)
            Handlers
                .Where(x => x.Value.Protocol > 0 && !supportedProtocols.Contains(x.Value.Protocol, x.Value.Role))
                .ToList()
                .ForEach(x =>
                {
                    x.Value.Session = null;
                    Handlers.Remove(x.Key);
                    Handlers.Remove(x.Value.Protocol);
                });

            // update remaining handler mappings by protocol
            foreach (var handler in Handlers.Values.ToArray())
            {
                if (!Handlers.ContainsKey(handler.Protocol))
                    Handlers[handler.Protocol] = handler;
            }
        }

        /// <summary>
        /// Logs the specified header and message body.
        /// </summary>
        /// <typeparam name="T">The type of message.</typeparam>
        /// <param name="header">The header.</param>
        /// <param name="body">The message body.</param>
        protected void Sent<T>(MessageHeader header, T body)
        {
            if (Output != null)
            {
                Format("[{0}] Message sent at {1}", SessionId, DateTime.Now);
                Format(this.Serialize(header));
                Format(this.Serialize(body, true));
            }

            if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("[{0}] Message sent: {1}", SessionId, this.Serialize(header));
            }
        }

        /// <summary>
        /// Validates the headers.
        /// </summary>
        protected virtual void ValidateHeaders()
        {
        }
    }
}
