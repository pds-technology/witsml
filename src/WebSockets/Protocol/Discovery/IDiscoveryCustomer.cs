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
using Energistics.Datatypes;

namespace Energistics.Protocol.Discovery
{
    /// <summary>
    /// Describes the interface that must be implemented by the customer role of the Discovery protocol.
    /// </summary>
    /// <seealso cref="Energistics.Common.IProtocolHandler" />
    [ProtocolRole(Protocols.Discovery, "customer", "store")]
    public interface IDiscoveryCustomer : IProtocolHandler
    {
        /// <summary>
        /// Sends a GetResources message to a store.
        /// </summary>
        /// <param name="uri">The URI.</param>
        void GetResources(string uri);

        /// <summary>
        /// Handles the GetResourcesResponse event from a store.
        /// </summary>
        event ProtocolEventHandler<GetResourcesResponse, string> OnGetResourcesResponse;
    }
}
