using System.Collections.Generic;
using Energistics.Common;
using Energistics.Datatypes;

namespace Energistics.Protocol.Core
{
    public interface ICoreServer : IProtocolHandler
    {
        void OpenSession(MessageHeader request, IList<SupportedProtocol> supportedProtocols);

        void CloseSession(string reason = null);

        event ProtocolEventHandler<RequestSession> OnRequestSession;

        event ProtocolEventHandler<CloseSession> OnCloseSession;
    }
}
