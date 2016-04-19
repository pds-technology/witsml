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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Energistics.Common;
using Energistics.Properties;
using Energistics.Protocol.Core;

namespace Energistics
{
    /// <summary>
    /// An ETP server session implementation that can be used with .NET WebSockets.
    /// </summary>
    /// <seealso cref="Energistics.Common.EtpSession" />
    public class EtpServerHandler : EtpSession
    {
        private const int BufferSize = 4096;
        private WebSocket _socket;

        /// <summary>
        /// Initializes the <see cref="EtpServerHandler"/> class.
        /// </summary>
        static EtpServerHandler()
        {
            Clients = new ConcurrentDictionary<string, EtpServerHandler>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpServerHandler"/> class.
        /// </summary>
        /// <param name="webSocket">The web socket.</param>
        /// <param name="application">The server application name.</param>
        /// <param name="version">The server application version.</param>
        /// <param name="headers">The WebSocket or HTTP headers.</param>
        public EtpServerHandler(WebSocket webSocket, string application, string version, IDictionary<string, string> headers) : base(application, version, headers)
        {
            _socket = webSocket;
            Register<ICoreServer, CoreServerHandler>();
        }

        /// <summary>
        /// Gets the collection of active clients.
        /// </summary>
        /// <value>The clients.</value>
        public static ConcurrentDictionary<string, EtpServerHandler> Clients { get; }

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
        /// Closes the WebSocket connection for the specified reason.
        /// </summary>
        /// <param name="reason">The reason.</param>
        public override void Close(string reason)
        {
            if (!IsOpen) return;
            _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
            Logger.Debug(Format("[{0}] Socket session closed.", SessionId));
        }

        /// <summary>
        /// Accepts the WebSocket connection represented by the specified context.
        /// </summary>
        /// <param name="context">The WebSocket context.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        public async Task Accept(WebSocketContext context)
        {
            SessionId = Guid.NewGuid().ToString();

            Logger.Debug(Format("[{0}] Socket session connected.", SessionId));

            using (var stream = new MemoryStream())
            {
                try
                {
                    // keep track of connected clients
                    Clients.AddOrUpdate(SessionId, this, (id, client) => this);

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
                    Format("Error: {0}", ex.Message);
                    Logger.Error(ex);
                    throw;
                }
                finally
                {
                    EtpServerHandler item;
                    
                    // remove client after connection ends
                    if (Clients.TryRemove(SessionId, out item))
                    {
                        if (item != this)
                        {
                            Clients.AddOrUpdate(item.SessionId, item, (id, client) => item);
                        }
                    }
                }
            }
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

            var buffer = new ArraySegment<byte>(data, offset, length);
            _socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        /// <summary>
        /// Validates the WebSocket headers.
        /// </summary>
        /// <exception cref="HttpException">412;JSON Encoding not supported</exception>
        protected override void ValidateHeaders()
        {
            if (Headers.ContainsKey(Settings.Default.EtpEncodingHeader) &&
                string.Equals(Headers[Settings.Default.EtpEncodingHeader], Settings.Default.EtpEncodingJson, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new HttpException(412, "JSON Encoding not supported");
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
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
