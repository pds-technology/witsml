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
    public class EtpSocketServer : EtpBase
    {
        private static readonly string EtpSubProtocolName = Settings.Default.EtpSubProtocolName;
        private static readonly object EtpSessionKey = typeof(IEtpSession);
        private WebSocketServer _server;

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

        public string ApplicationName { get; private set; }

        public string ApplicationVersion { get; private set; }

        public bool IsRunning
        {
            get
            {
                CheckDisposed();
                return _server.State == ServerState.Running;
            }
        }

        public void Start()
        {
            if (!IsRunning)
            {
                _server.Start();
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                CloseSessions();
                _server.Stop();
            }
        }

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

        private void OnNewSessionConnected(WebSocketSession session)
        {
            Logger.Debug(Format("[{0}] Socket session connected.", session.SessionID));

            var etpServer = new EtpServer(session, ApplicationName, ApplicationVersion, null);
            etpServer.SupportedObjects = SupportedObjects;
            RegisterAll(etpServer);

            session.Items[EtpSessionKey] = etpServer;
        }

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

        private void OnNewDataReceived(WebSocketSession session, byte[] data)
        {
            var etpSession = GetEtpSession(session);

            if (etpSession != null)
            {
                etpSession.OnDataReceived(data);
            }
        }

        private void CloseSessions()
        {
            CheckDisposed();

            foreach (var session in _server.GetAllSessions())
            {
                var etpSession = GetEtpSession(session);

                if (etpSession != null)
                {
                    etpSession.Close();
                }
            }
        }

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
