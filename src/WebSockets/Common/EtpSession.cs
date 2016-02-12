using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Avro.IO;
using Avro.Specific;
using Energistics.Datatypes;

namespace Energistics.Common
{
    public abstract class EtpSession : EtpBase, IEtpSession
    {
        private long MessageId = 0;

        public EtpSession(string application)
        {
            Handlers = new Dictionary<object, IProtocolHandler>();
            ApplicationName = application;
        }

        public string ApplicationName { get; private set; }

        public string SessionId { get; set; }

        protected IDictionary<object, IProtocolHandler> Handlers { get; private set; }

        public T Handler<T>() where T : IProtocolHandler
        {
            IProtocolHandler handler;

            if (Handlers.TryGetValue(typeof(T), out handler) && handler is T)
            {
                return (T)handler;
            }

            Logger.ErrorFormat("[{0}] Protocol handler not registered for {1}.", SessionId, typeof(T).FullName);
            throw new NotSupportedException(String.Format("Protocol handler not registered for {0}.", typeof(T).FullName));
        }

        public virtual void OnDataReceived(byte[] data)
        {
            Decode(data);
        }

        public void SendMessage<T>(MessageHeader header, T body) where T : ISpecificRecord
        {
            var data = body.Encode(header);
            Send(data, 0, data.Length);

            if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("[{0}] Message sent: {1}", SessionId, this.Serialize(header));
            }
        }

        public IList<SupportedProtocol> GetSupportedProtocols()
        {
            var supportedProtocols = new List<SupportedProtocol>();
            var capabilities = new Dictionary<string, DataValue>();
            var version = new Energistics.Datatypes.Version()
            {
                Major = 1
            };

            foreach (var handler in Handlers.Values)
            {
                supportedProtocols.Add(new SupportedProtocol()
                {
                    Protocol = handler.Protocol,
                    ProtocolVersion = version,
                    ProtocolCapabilities = capabilities,
                    Role = handler.RequestedRole ?? handler.Role
                });
            }

            return supportedProtocols;
        }

        public long NewMessageId()
        {
            return Interlocked.Increment(ref MessageId);
        }

        public abstract void Close(string reason = null);

        protected abstract void Send(byte[] data, int offset, int length);

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

        protected void HandleMessage(MessageHeader header, Decoder decoder)
        {
            Handler(header.Protocol)
                .HandleMessage(header, decoder);
        }

        protected override void Register(Type contractType, Type handlerType)
        {
            base.Register(contractType, handlerType);

            var handler = CreateInstance(contractType);

            if (handler != null)
            {
                handler.Session = this;
                Handlers.Add(contractType, handler);
                Handlers.Add(handler.Protocol, handler);
            }
        }

        protected IProtocolHandler Handler(int protocol)
        {
            if (Handlers.ContainsKey(protocol))
            {
                return Handlers[protocol];
            }

            Logger.ErrorFormat("[{0}] Protocol handler not registed for protocol {1}.", SessionId, protocol);
            throw new NotSupportedException(String.Format("Protocol handler not registed for protocol {0}.", protocol));
        }
    }
}
