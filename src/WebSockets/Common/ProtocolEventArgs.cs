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
using Avro.Specific;
using Energistics.Datatypes;

namespace Energistics.Common
{
    public class ProtocolEventArgs<T> : EventArgs where T : ISpecificRecord
    {
        public ProtocolEventArgs(MessageHeader header, T message)
        {
            Header = header;
            Message = message;
        }

        public MessageHeader Header { get; private set; }

        public T Message { get; private set; }
    }

    public class ProtocolEventArgs<T, V> : ProtocolEventArgs<T> where T : ISpecificRecord
    {
        public ProtocolEventArgs(MessageHeader header, T message, V context) : base(header, message)
        {
            Context = context;
        }

        public V Context { get; private set; }
    }
}
