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
using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;

namespace Energistics.Protocol.Core
{
    /// <summary>
    /// Base implementation of the <see cref="ICoreServer"/> interface.
    /// </summary>
    /// <seealso cref="Energistics.Common.EtpProtocolHandler" />
    /// <seealso cref="Energistics.Protocol.Core.ICoreServer" />
    public class CoreServerHandler : EtpProtocolHandler, ICoreServer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoreServerHandler"/> class.
        /// </summary>
        public CoreServerHandler() : base(Protocols.Core, "server", "client")
        {
            RequestedProtocols = new List<SupportedProtocol>(0);
        }

        /// <summary>
        /// Gets the name of the client application.
        /// </summary>
        /// <value>The name of the client application.</value>
        public string ClientApplicationName { get; private set; }

        /// <summary>
        /// Gets the requested protocols.
        /// </summary>
        /// <value>The requested protocols.</value>
        public IList<SupportedProtocol> RequestedProtocols { get; private set; }

        /// <summary>
        /// Sends an OpenSession message to a client.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="supportedProtocols">The supported protocols.</param>
        public virtual void OpenSession(MessageHeader request, IList<SupportedProtocol> supportedProtocols)
        {
            var header = CreateMessageHeader(Protocols.Core, MessageTypes.Core.OpenSession, request.MessageId);

            var openSession = new OpenSession()
            {
                ApplicationName = Session.ApplicationName,
                ApplicationVersion = Session.ApplicationVersion,
                SupportedProtocols = supportedProtocols,
                SupportedObjects = Session.SupportedObjects,
                SessionId = Session.SessionId
            };

            Session.SendMessage(header, openSession);
            Session.OnSessionOpened(supportedProtocols);
        }

        /// <summary>
        /// Sends a CloseSession message to a client.
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
        /// Handles the RequestSession event from a client.
        /// </summary>
        public event ProtocolEventHandler<RequestSession> OnRequestSession;

        /// <summary>
        /// Handles the CloseSession event from a client.
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
                case (int)MessageTypes.Core.RequestSession:
                    HandleRequestSession(header, decoder.Decode<RequestSession>());
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
        /// Handles the RequestSession message from a client.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="requestSession">The RequestSession message.</param>
        protected virtual void HandleRequestSession(MessageHeader header, RequestSession requestSession)
        {
            ClientApplicationName = requestSession.ApplicationName;
            RequestedProtocols = requestSession.RequestedProtocols;
            Notify(OnRequestSession, header, requestSession);

            var protocols = RequestedProtocols
                .Select(x => new { x.Protocol, x.Role })
                .ToArray();

            // Only return details for requested protocols
            var supportedProtocols = Session.GetSupportedProtocols()
                .Where(x => protocols.Any(y => x.Protocol == y.Protocol && string.Equals(x.Role, y.Role, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();

            // Only send OpenSession if there are protocols supported
            if (supportedProtocols.Any())
            {
                OpenSession(header, supportedProtocols);
            }
            else // Otherwise, ProtocolException is sent
            {
                ProtocolException((int)ErrorCodes.ENOSUPPORTEDPROTOCOLS, "No protocols supported", header.MessageId);
            }
        }

        /// <summary>
        /// Handles the CloseSession message from a client.
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
