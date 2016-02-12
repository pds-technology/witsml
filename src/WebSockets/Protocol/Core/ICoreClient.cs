using System.Collections.Generic;
using Energistics.Common;
using Energistics.Datatypes;

namespace Energistics.Protocol.Core
{
    public interface ICoreClient : IProtocolHandler
    {
        void RequestSession(string applicationName, IList<SupportedProtocol> requestedProtocols);

        void CloseSession(string reason = null);

        event ProtocolEventHandler<OpenSession> OnOpenSession;

        event ProtocolEventHandler<CloseSession> OnCloseSession;
    }
}
