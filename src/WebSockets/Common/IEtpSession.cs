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
using Avro.Specific;
using Energistics.Datatypes;

namespace Energistics.Common
{
    public interface IEtpSession : IDisposable
    {
        string ApplicationName { get; }

        string ApplicationVersion { get; }

        string SessionId { get; set; }

        Action<string> Output { get; set; }

        string Format(string message);

        string Format(string message, params object[] args);

        void OnDataReceived(byte[] data);

        void SendMessage<T>(MessageHeader header, T body) where T : ISpecificRecord;

        IList<SupportedProtocol> GetSupportedProtocols();

        T Handler<T>() where T : IProtocolHandler;

        bool CanHandle<T>() where T : IProtocolHandler;

        long NewMessageId();

        void Close(string reason = null);
    }
}
