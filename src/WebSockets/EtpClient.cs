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
using Energistics.Datatypes;
using Energistics.Properties;
using Energistics.Protocol.Core;
using WebSocket4Net;

namespace Energistics
{
    /// <summary>
    /// A wrapper for the WebSocket4Net library providing client connectivity to an ETP server.
    /// </summary>
    /// <seealso cref="Energistics.Common.EtpSession" />
    public class EtpClient : EtpSession
    {
        private static readonly string EtpSubProtocolName = Settings.Default.EtpSubProtocolName;
        private static readonly IDictionary<string, string> EmptyHeaders = new Dictionary<string, string>();
        private static readonly IDictionary<string, string> BinaryHeaders = new Dictionary<string, string>()
        {
            { Settings.Default.EtpEncodingHeader, Settings.Default.EtpEncodingBinary }
        };

        private WebSocket _socket;

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpClient"/> class.
        /// </summary>
        /// <param name="uri">The ETP server URI.</param>
        /// <param name="application">The client application name.</param>
        /// <param name="version">The client application version.</param>
        public EtpClient(string uri, string application, string version) : this(uri, application, version, EmptyHeaders)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpClient"/> class.
        /// </summary>
        /// <param name="uri">The ETP server URI.</param>
        /// <param name="application">The client application name.</param>
        /// <param name="version">The client application version.</param>
        /// <param name="headers">The WebSocket headers.</param>
        public EtpClient(string uri, string application, string version, IDictionary<string, string> headers) : base(application, version, headers)
        {
            _socket = new WebSocket(uri, EtpSubProtocolName, null, BinaryHeaders.Union(Headers).ToList());

            _socket.Opened += OnWebSocketOpened;
            _socket.Closed += OnWebSocketClosed;
            _socket.DataReceived += OnWebSocketDataReceived;

            Register<ICoreClient, CoreClientHandler>();
        }

        /// <summary>
        /// Gets a value indicating whether the connection is open.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the connection is open; otherwise, <c>false</c>.
        /// </value>
        public bool IsOpen
        {
            get
            {
                CheckDisposed();
                return _socket.State == WebSocketState.Open;
            }
        }

        /// <summary>
        /// Opens the WebSocket connection.
        /// </summary>
        public void Open()
        {
            if (!IsOpen)
            {
                Logger.Debug(Format("Opening web socket connection..."));
                _socket.Open();
            }
        }

        /// <summary>
        /// Closes the WebSocket connection for the specified reason.
        /// </summary>
        /// <param name="reason">The reason.</param>
        public override void Close(string reason)
        {
            if (!IsOpen) return;
            Logger.Debug(Format("Closing web socket connection: {0}", reason));
            _socket.Close(reason);
        }

        /// <summary>
        /// Occurs when the WebSocket is opened.
        /// </summary>
        public event EventHandler SocketOpened
        {
            add { _socket.Opened += value; }
            remove { _socket.Opened -= value; }
        }

        /// <summary>
        /// Occurs when the WebSocket is closed.
        /// </summary>
        public event EventHandler SocketClosed
        {
            add { _socket.Closed += value; }
            remove { _socket.Closed -= value; }
        }

        /// <summary>
        /// Sends the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        protected override void Send(byte[] data, int offset, int length)
        {
            CheckDisposed();
            _socket.Send(data, offset, length);
        }

        /// <summary>
        /// Handles the unsupported protocols.
        /// </summary>
        /// <param name="supportedProtocols">The supported protocols.</param>
        protected override void HandleUnsupportedProtocols(IList<SupportedProtocol> supportedProtocols)
        {
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
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

        /// <summary>
        /// Called when the WebSocket is opened.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnWebSocketOpened(object sender, EventArgs e)
        {
            Logger.Debug(Format("[{0}] Socket opened.", SessionId));

            var requestedProtocols = GetSupportedProtocols(true);

            Handler<ICoreClient>()
                .RequestSession(ApplicationName, ApplicationVersion, requestedProtocols);
        }

        /// <summary>
        /// Called when the WebSocket is closed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnWebSocketClosed(object sender, EventArgs e)
        {
            Logger.Debug(Format("[{0}] Socket closed.", SessionId));
            SessionId = null;
        }

        /// <summary>
        /// Called when WebSocket data is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DataReceivedEventArgs"/> instance containing the event data.</param>
        private void OnWebSocketDataReceived(object sender, DataReceivedEventArgs e)
        {
            OnDataReceived(e.Data);
        }
    }
}
