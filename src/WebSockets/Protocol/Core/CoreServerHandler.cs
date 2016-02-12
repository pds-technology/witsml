using System.Collections.Generic;
using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;

namespace Energistics.Protocol.Core
{
    public class CoreServerHandler : EtpProtocolHandler, ICoreServer
    {
        public CoreServerHandler() : base(Protocols.Core, "server")
        {
            RequestedProtocols = new List<SupportedProtocol>(0);
        }

        public string ClientApplicationName { get; private set; }

        public IList<SupportedProtocol> RequestedProtocols { get; private set; }

        public virtual void OpenSession(MessageHeader request, IList<SupportedProtocol> supportedProtocols)
        {
            var header = CreateMessageHeader(Protocols.Core, MessageTypes.Core.OpenSession, request.MessageId);

            var openSession = new OpenSession()
            {
                ApplicationName = Session.ApplicationName,
                SupportedProtocols = supportedProtocols,
                SessionId = Session.SessionId
            };

            Session.SendMessage(header, openSession);
        }

        public virtual void CloseSession(string reason = null)
        {
            var header = CreateMessageHeader(Protocols.Core, MessageTypes.Core.CloseSession);

            var closeSession = new CloseSession()
            {
                Reason = reason ?? "Session closed"
            };

            Session.SendMessage(header, closeSession);
        }

        public event ProtocolEventHandler<RequestSession> OnRequestSession;

        public event ProtocolEventHandler<CloseSession> OnCloseSession;

        protected override void HandleMessage(MessageHeader header, Decoder decoder)
        {
            switch (header.MessageType)
            {
                case (int)MessageTypes.Core.RequestSession:
                    HandleRequestSession(header, decoder.Decode<RequestSession>());
                    break;

                case (int)MessageTypes.Core.CloseSession:
                    HandleCloseSession(header, decoder.Decode<CloseSession>());
                    break;

                default:
                    base.HandleMessage(header, decoder);
                    break;
            }
        }

        protected virtual void HandleRequestSession(MessageHeader header, RequestSession requestSession)
        {
            Notify(OnRequestSession, header, requestSession);

            var supportedProtocols = Session.GetSupportedProtocols();

            ClientApplicationName = requestSession.ApplicationName;
            RequestedProtocols = requestSession.RequestedProtocols;

            OpenSession(header, supportedProtocols);
        }

        protected virtual void HandleCloseSession(MessageHeader header, CloseSession closeSession)
        {
            Notify(OnCloseSession, header, closeSession);
            Session.Close(closeSession.Reason);
        }
    }
}
