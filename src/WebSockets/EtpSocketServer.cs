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

using Energistics.Common;
using Energistics.Properties;
using Energistics.Protocol.Core;
using SuperSocket.SocketBase;
using SuperWebSocket;
using SuperWebSocket.SubProtocol;

namespace Energistics
{
    /// <summary>
    /// A wrapper for the SuperWebSocket library providing a base ETP server implementation.
    /// </summary>
    /// <seealso cref="Energistics.Common.EtpBase" />
    public class EtpSocketServer : EtpBase
    {
        private static readonly string EtpSubProtocolName = Settings.Default.EtpSubProtocolName;
        private static readonly object EtpSessionKey = typeof(IEtpSession);
        private WebSocketServer _server;

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpSocketServer"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="application">The server application name.</param>
        /// <param name="version">The server application version.</param>
        public EtpSocketServer(int port, string application, string version)
        {
            _server = new WebSocketServer(new BasicSubProtocol(EtpSubProtocolName));
            _server.Setup(port);

            _server.NewSessionConnected += OnNewSessionConnected;
            _server.NewDataReceived += OnNewDataReceived;
            _server.SessionClosed += OnSessionClosed;

            ApplicationName = application;
            ApplicationVersion = version;
            Register<ICoreServer, CoreServerHandler>();
        }

        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        /// <value>The name of the application.</value>
        public string ApplicationName { get; private set; }

        /// <summary>
        /// Gets the application version.
        /// </summary>
        /// <value>The application version.</value>
        public string ApplicationVersion { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the WebSocket server is running.
        /// </summary>
        /// <value>
        /// <c>true</c> if the WebSocket server is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning
        {
            get
            {
                CheckDisposed();
                return _server.State == ServerState.Running;
            }
        }

        /// <summary>
        /// Starts the WebSocket server.
        /// </summary>
        public void Start()
        {
            if (!IsRunning)
            {
                _server.Start();
            }
        }

        /// <summary>
        /// Stops the WebSocket server.
        /// </summary>
        public void Stop()
        {
            if (IsRunning)
            {
                CloseSessions();
                _server.Stop();
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _server != null)
            {
                Stop();
                _server.Dispose();
            }

            _server = null;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Called when a WebSocket session is connected.
        /// </summary>
        /// <param name="session">The session.</param>
        private void OnNewSessionConnected(WebSocketSession session)
        {
            Logger.Debug(Format("[{0}] Socket session connected.", session.SessionID));

            var etpServer = new EtpServer(session, ApplicationName, ApplicationVersion, null);
            etpServer.SupportedObjects = SupportedObjects;
            RegisterAll(etpServer);

            session.Items[EtpSessionKey] = etpServer;
        }

        /// <summary>
        /// Called when a WebSocket session is closed.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="value">The value.</param>
        private void OnSessionClosed(WebSocketSession session, CloseReason value)
        {
            Logger.Debug(Format("[{0}] Socket session closed.", session.SessionID));

            var etpSession = GetEtpSession(session);

            if (etpSession != null)
            {
                etpSession.Dispose();
                session.Items[EtpSessionKey] = null;
            }
        }

        /// <summary>
        /// Called when new WebSocket data is received.
        /// </summary>
        /// <param name="session">The WebSocket session.</param>
        /// <param name="data">The data.</param>
        private void OnNewDataReceived(WebSocketSession session, byte[] data)
        {
            var etpSession = GetEtpSession(session);
            etpSession?.OnDataReceived(data);
        }

        /// <summary>
        /// Closes all connected WebSocket sessions.
        /// </summary>
        private void CloseSessions()
        {
            CheckDisposed();
            const string reason = "Server stopping";

            foreach (var session in _server.GetAllSessions())
            {
                var etpSession = GetEtpSession(session);
                etpSession?.Close(reason);
            }
        }

        /// <summary>
        /// Gets the ETP session associated with the specified WebSocket session.
        /// </summary>
        /// <param name="session">The WebSocket session.</param>
        /// <returns>The <see cref="IEtpSession"/> associated with the WebSocket session.</returns>
        private IEtpSession GetEtpSession(WebSocketSession session)
        {
            IEtpSession etpSession = null;
            object item;

            if (session.Items.TryGetValue(EtpSessionKey, out item))
            {
                etpSession = item as IEtpSession;
            }

            return etpSession;
        }
    }
}
