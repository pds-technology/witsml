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
using Energistics.Common;
using SuperWebSocket;

namespace Energistics
{
    public class EtpServer : EtpSession
    {
        private WebSocketSession _session;

        public EtpServer(WebSocketSession session, string application, string version, IDictionary<string, string> headers) : base(application, version, headers)
        {
            SessionId = session.SessionID;
            _session = session;
        }

        public override void Close(string reason = null)
        {
            CheckDisposed();
            _session.CloseWithHandshake(reason);
        }

        protected override void Send(byte[] data, int offset, int length)
        {
            CheckDisposed();
            _session.Send(data, offset, length);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _session != null)
            {
                _session.Close();
            }

            _session = null;
            base.Dispose(disposing);
        }
    }
}
