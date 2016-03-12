using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.Common;
using Energistics.Properties;
using Energistics.Protocol.Core;
using WebSocket4Net;

namespace Energistics
{
    public class EtpClient : EtpSession
    {
        private static readonly string EtpSubProtocolName = Settings.Default.EtpSubProtocolName;
        private static readonly IDictionary<string, string> _default = new Dictionary<string, string>();
        private static readonly IDictionary<string, string> _headers = new Dictionary<string, string>()
        {
            { Settings.Default.EtpEncodingHeader, Settings.Default.EtpEncodingBinary }
        };

        private WebSocket _socket;

        public EtpClient(string uri, string application) : this(uri, application, _default)
        {
        }

        public EtpClient(string uri, string application, IDictionary<string, string> headers) : base(application)
        {
            _socket = new WebSocket(uri, EtpSubProtocolName, null, _headers.Union(headers).ToList());

            _socket.Opened += OnWebSocketOpened;
            _socket.Closed += OnWebSocketClosed;
            _socket.DataReceived += OnWebSocketDataReceived;

            Register<ICoreClient, CoreClientHandler>();
        }

        public bool IsOpen
        {
            get
            {
                CheckDisposed();
                return _socket.State == WebSocketState.Open;
            }
        }

        public void Open()
        {
            if (!IsOpen)
            {
                Logger.Debug(Format("Opening web socket connection..."));
                _socket.Open();
            }
        }

        public override void Close(string reason = null)
        {
            if (IsOpen)
            {
                Logger.Debug(Format("Closing web socket connection: {0}", reason));
                _socket.Close(reason);
            }
        }

        protected override void Send(byte[] data, int offset, int length)
        {
            CheckDisposed();
            _socket.Send(data, offset, length);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _socket != null)
            {
                _socket.Close();
                _socket.Dispose();
            }

            _socket = null;
            base.Dispose(disposing);
        }

        private void OnWebSocketOpened(object sender, EventArgs e)
        {
            Logger.Debug(Format("[{0}] Socket opened.", SessionId));

            var requestedProtocols = GetSupportedProtocols();

            Handler<ICoreClient>()
                .RequestSession(ApplicationName, requestedProtocols);
        }

        private void OnWebSocketClosed(object sender, EventArgs e)
        {
            Logger.Debug(Format("[{0}] Socket closed.", SessionId));
            SessionId = null;
        }

        private void OnWebSocketDataReceived(object sender, DataReceivedEventArgs e)
        {
            OnDataReceived(e.Data);
        }
    }
}
