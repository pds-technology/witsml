using System;
using Avro.Specific;
using Energistics.Datatypes;

namespace Energistics.Common
{
    public class ProtocolEventArgs<T> : EventArgs where T : ISpecificRecord
    {
        public ProtocolEventArgs(MessageHeader header, T message)
        {
            Header = header;
            Message = message;
        }

        public MessageHeader Header { get; private set; }

        public T Message { get; private set; }
    }

    public class ProtocolEventArgs<T, V> : ProtocolEventArgs<T> where T : ISpecificRecord
    {
        public ProtocolEventArgs(MessageHeader header, T message, V context) : base(header, message)
        {
            Context = context;
        }

        public V Context { get; private set; }
    }
}
