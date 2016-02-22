using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Energistics.Common;
using Energistics.Protocol.Core;

namespace Energistics
{
    public class EtpServerHandler : EtpSession
    {
        private const int BufferSize = 4096;
        private WebSocket _socket;

        public EtpServerHandler(WebSocket webSocket, string application) : base(application)
        {
            _socket = webSocket;
            Register<ICoreServer, CoreServerHandler>();
        }

        public bool IsOpen
        {
            get
            {
                CheckDisposed();
                return _socket.State == WebSocketState.Open;
            }
        }

        public override void Close(string reason = null)
        {
            if (IsOpen)
            {
                _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);

                Logger.DebugFormat("[{0}] Socket session closed.", SessionId);
            }
        }

        public async Task Accept(WebSocketContext context)
        {
            SessionId = Guid.NewGuid().ToString();

            Logger.DebugFormat("[{0}] Socket session connected.", SessionId);

            using (var stream = new MemoryStream())
            {
                try
                {
                    while (true)
                    {
                        var buffer = new ArraySegment<byte>(new byte[BufferSize]);
                        var result = await _socket.ReceiveAsync(buffer, CancellationToken.None);

                        if (_socket.State == WebSocketState.Open)
                        {
                            stream.Write(buffer.Array, 0, result.Count);

                            if (result.EndOfMessage)
                            {
                                OnDataReceived(stream.GetBuffer());
                                stream.SetLength(0);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    throw;
                }
            }
        }

        protected override void Send(byte[] data, int offset, int length)
        {
            CheckDisposed();

            var buffer = new ArraySegment<byte>(data, offset, length);
            _socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _socket != null)
            {
                _socket.Dispose();
            }

            _socket = null;
            base.Dispose(disposing);
        }
    }
}
