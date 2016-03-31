using System;
using System.Collections.Generic;
using Avro.Specific;
using Energistics.Datatypes;

namespace Energistics.Common
{
    public interface IEtpSession : IDisposable
    {
        string ApplicationName { get; }

        string ApplicationVersion { get; }

        string SessionId { get; set; }

        Action<string> Output { get; set; }

        string Format(string message);

        string Format(string message, params object[] args);

        void OnDataReceived(byte[] data);

        void SendMessage<T>(MessageHeader header, T body) where T : ISpecificRecord;

        IList<SupportedProtocol> GetSupportedProtocols();

        T Handler<T>() where T : IProtocolHandler;

        bool CanHandle<T>() where T : IProtocolHandler;

        long NewMessageId();

        void Close(string reason = null);
    }
}
