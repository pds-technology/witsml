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
using Energistics.Datatypes;
using Energistics.Datatypes.Object;

namespace Energistics.Protocol.Store
{
    /// <summary>
    /// Defines the interface that must be implemented by the customer role of the Store protocol.
    /// </summary>
    /// <seealso cref="Energistics.Common.IProtocolHandler" />
    [ProtocolRole(Protocols.Store, "customer", "store")]
    public interface IStoreCustomer : IProtocolHandler
    {
        /// <summary>
        /// Sends a GetObject message to a store.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="messageFlag">The message flag.</param>
        void GetObject(string uri, MessageFlags messageFlag = MessageFlags.FinalPart);

        /// <summary>
        /// Sends a PutObject message to a store.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        void PutObject(DataObject dataObject);

        /// <summary>
        /// Sends a DeleteObject message to a store.
        /// </summary>
        /// <param name="uris">The list of URIs.</param>
        void DeleteObject(IList<string> uris);

        /// <summary>
        /// Handles the Object event from a store.
        /// </summary>
        event ProtocolEventHandler<Object> OnObject;
    }
}
