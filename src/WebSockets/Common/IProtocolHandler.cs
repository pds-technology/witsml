using System.Collections.Generic;
using Avro.IO;
using Energistics.Datatypes;
using Energistics.Protocol.Core;

namespace Energistics.Common
{
    public interface IProtocolHandler
    {
        IEtpSession Session { get; set; }

        int Protocol { get; }

        string Role { get; }

        string RequestedRole { get; }

        IDictionary<string, DataValue> GetCapabilities();

        void Acknowledge(int correlationId);

        void ProtocolException(int errorCode, string errorMessage, long correlationId = 0);

        void HandleMessage(MessageHeader header, Decoder decoder);

        event ProtocolEventHandler<Acknowledge> OnAcknowledge;

        event ProtocolEventHandler<ProtocolException> OnProtocolException;
    }
}
