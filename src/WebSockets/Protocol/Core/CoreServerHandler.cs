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
using System.Linq;
using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;

namespace Energistics.Protocol.Core
{
    public class CoreServerHandler : EtpProtocolHandler, ICoreServer
    {
        public CoreServerHandler() : base(Protocols.Core, "server", "client")
        {
            RequestedProtocols = new List<SupportedProtocol>(0);
        }

        public string ClientApplicationName { get; private set; }

        public IList<SupportedProtocol> RequestedProtocols { get; private set; }

        public virtual void OpenSession(MessageHeader request, IList<SupportedProtocol> supportedProtocols)
        {
            var header = CreateMessageHeader(Protocols.Core, MessageTypes.Core.OpenSession, request.MessageId);

            var openSession = new OpenSession()
            {
                ApplicationName = Session.ApplicationName,
                ApplicationVersion = Session.ApplicationVersion,
                SupportedProtocols = supportedProtocols,
                SupportedObjects = new List<string>(),
                SessionId = Session.SessionId
            };

            Session.SendMessage(header, openSession);
        }

        public virtual void CloseSession(string reason = null)
        {
            var header = CreateMessageHeader(Protocols.Core, MessageTypes.Core.CloseSession);

            var closeSession = new CloseSession()
            {
                Reason = reason ?? "Session closed"
            };

            Session.SendMessage(header, closeSession);
        }

        public event ProtocolEventHandler<RequestSession> OnRequestSession;

        public event ProtocolEventHandler<CloseSession> OnCloseSession;

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

        protected virtual void HandleRequestSession(MessageHeader header, RequestSession requestSession)
        {
            ClientApplicationName = requestSession.ApplicationName;
            RequestedProtocols = requestSession.RequestedProtocols;
            Notify(OnRequestSession, header, requestSession);

            var protocols = RequestedProtocols
                .Select(x => new { x.Protocol, x.Role })
                .ToArray();

            // only return details for requested protocols
            var supportedProtocols = Session.GetSupportedProtocols()
                .Where(x => protocols.Any(y => x.Protocol == y.Protocol && x.Role == y.Role))
                .ToList();

            OpenSession(header, supportedProtocols);
        }

        protected virtual void HandleCloseSession(MessageHeader header, CloseSession closeSession)
        {
            Notify(OnCloseSession, header, closeSession);
            Session.Close(closeSession.Reason);
        }
    }
}
