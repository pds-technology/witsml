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

using System.Collections.Generic;
using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;

namespace Energistics.Protocol.Core
{
    /// <summary>
    /// Base implementation of the <see cref="ICoreClient"/> interface.
    /// </summary>
    /// <seealso cref="Energistics.Common.EtpProtocolHandler" />
    /// <seealso cref="Energistics.Protocol.Core.ICoreClient" />
    public class CoreClientHandler : EtpProtocolHandler, ICoreClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoreClientHandler"/> class.
        /// </summary>
        public CoreClientHandler() : base(Protocols.Core, "client", "server")
        {
            ServerProtocols = new List<SupportedProtocol>(0);
            ServerObjects = new List<string>(0);
        }

        /// <summary>
        /// Gets the list of supported server protocols.
        /// </summary>
        /// <value>The server protocols.</value>
        public IList<SupportedProtocol> ServerProtocols { get; private set; }

        /// <summary>
        /// Gets the list of supported server objects.
        /// </summary>
        /// <value>The server objects.</value>
        public IList<string> ServerObjects { get; private set; }

        /// <summary>
        /// Sends a RequestSession message to a server.
        /// </summary>
        /// <param name="applicationName">The application name.</param>
        /// <param name="applicationVersion">The application version.</param>
        /// <param name="requestedProtocols">The requested protocols.</param>
        public virtual void RequestSession(string applicationName, string applicationVersion, IList<SupportedProtocol> requestedProtocols)
        {
            var header = CreateMessageHeader(Protocols.Core, MessageTypes.Core.RequestSession);

            var requestSession = new RequestSession()
            {
                ApplicationName = applicationName,
                ApplicationVersion = applicationVersion,
                RequestedProtocols = requestedProtocols,
                SupportedObjects = new List<string>()
            };

            Session.SendMessage(header, requestSession);
        }

        /// <summary>
        /// Sends a CloseSession message to a server.
        /// </summary>
        /// <param name="reason">The reason.</param>
        public virtual void CloseSession(string reason = null)
        {
            var header = CreateMessageHeader(Protocols.Core, MessageTypes.Core.CloseSession);

            var closeSession = new CloseSession()
            {
                Reason = reason ?? "Session closed"
            };

            Session.SendMessage(header, closeSession);
        }

        /// <summary>
        /// Handles the OpenSession event from a server.
        /// </summary>
        public event ProtocolEventHandler<OpenSession> OnOpenSession;

        /// <summary>
        /// Handles the CloseSession event from a server.
        /// </summary>
        public event ProtocolEventHandler<CloseSession> OnCloseSession;

        /// <summary>
        /// Decodes the message based on the message type contained in the specified <see cref="MessageHeader" />.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="decoder">The message decoder.</param>
        protected override void HandleMessage(MessageHeader header, Decoder decoder)
        {
            switch (header.MessageType)
            {
                case (int)MessageTypes.Core.OpenSession:
                    HandleOpenSession(header, decoder.Decode<OpenSession>());
                    break;

                case (int)MessageTypes.Core.CloseSession:
                    HandleCloseSession(header, decoder.Decode<CloseSession>());
                    break;

                default:
                    base.HandleMessage(header, decoder);
                    break;
            }
        }

        /// <summary>
        /// Handles the OpenSession message from the server.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="openSession">The OpenSession message.</param>
        protected virtual void HandleOpenSession(MessageHeader header, OpenSession openSession)
        {
            ServerProtocols = openSession.SupportedProtocols;
            ServerObjects = openSession.SupportedObjects;
            Session.SessionId = openSession.SessionId;
            Notify(OnOpenSession, header, openSession);
            Session.OnSessionOpened(ServerProtocols);
        }

        /// <summary>
        /// Handles the CloseSession message from the server.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="closeSession">The CloseSession message.</param>
        protected virtual void HandleCloseSession(MessageHeader header, CloseSession closeSession)
        {
            Notify(OnCloseSession, header, closeSession);
            Session.Close(closeSession.Reason);
        }
    }
}
