using Energistics.Common;
using SuperWebSocket;

namespace Energistics
{
    public class EtpServer : EtpSession
    {
        private WebSocketSession _session;

        public EtpServer(WebSocketSession session, string application) : base(application)
        {
            SessionId = session.SessionID;
            _session = session;
        }

        public override void Close(string reason = null)
        {
            CheckDisposed();
            _session.CloseWithHandshake(reason);
        }

        protected override void Send(byte[] data, int offset, int length)
        {
            CheckDisposed();
            _session.Send(data, offset, length);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _session != null)
            {
                _session.Close();
            }

            _session = null;
            base.Dispose(disposing);
        }
    }
}
