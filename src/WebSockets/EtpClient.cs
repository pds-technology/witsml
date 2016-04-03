//----------------------------------------------------------------------- 
// ETP DevKit, 1.0
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

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

        public EtpClient(string uri, string application, string version) : this(uri, application, version, _default)
        {
        }

        public EtpClient(string uri, string application, string version, IDictionary<string, string> headers) : base(application, version)
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

        public event EventHandler SocketOpened
        {
            add { _socket.Opened += value; }
            remove { _socket.Opened -= value; }
        }

        public event EventHandler SocketClosed
        {
            add { _socket.Closed += value; }
            remove { _socket.Closed -= value; }
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

            var requestedProtocols = GetSupportedProtocols(true);

            Handler<ICoreClient>()
                .RequestSession(ApplicationName, ApplicationVersion, requestedProtocols);
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
