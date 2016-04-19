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

namespace Energistics.Datatypes
{
    /// <summary>
    /// Specifies protocol and role requirements for a protocol handler.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
    public class ProtocolRoleAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolRoleAttribute"/> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="role">The role.</param>
        /// <param name="requestedRole">The requested role.</param>
        public ProtocolRoleAttribute(Protocols protocol, string role, string requestedRole) : this((int)protocol, role, requestedRole)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolRoleAttribute"/> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="role">The role.</param>
        /// <param name="requestedRole">The requested role.</param>
        public ProtocolRoleAttribute(int protocol, string role, string requestedRole)
        {
            Protocol = protocol;
            Role = role;
            RequestedRole = requestedRole;
        }

        /// <summary>
        /// Gets the protocol.
        /// </summary>
        /// <value>The protocol.</value>
        public int Protocol { get; private set; }

        /// <summary>
        /// Gets the role.
        /// </summary>
        /// <value>The role.</value>
        public string Role { get; private set; }

        /// <summary>
        /// Gets the requested role.
        /// </summary>
        /// <value>The requested role.</value>
        public string RequestedRole { get; private set; }
    }
}
