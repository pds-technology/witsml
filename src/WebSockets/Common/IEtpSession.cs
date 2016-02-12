using System;
using System.Collections.Generic;
using Avro.Specific;
using Energistics.Datatypes;

namespace Energistics.Common
{
    public interface IEtpSession : IDisposable
    {
        string ApplicationName { get; }

        string SessionId { get; set; }

        void OnDataReceived(byte[] data);

        void SendMessage<T>(MessageHeader header, T body) where T : ISpecificRecord;

        IList<SupportedProtocol> GetSupportedProtocols();

        long NewMessageId();

        void Close(string reason = null);
    }
}
