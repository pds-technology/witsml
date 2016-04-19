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

using System.Reflection;
using Energistics.Protocol.Core;
using NUnit.Framework;

namespace Energistics.Datatypes
{
    [TestFixture]
    public class ProtocolRoleAttributeTests
    {
        [Test]
        public void ProtocolRoleAttribute_Can_Provide_Protocol_And_Role_Details()
        {
            var attrib = typeof(ICoreClient).GetCustomAttribute(typeof(ProtocolRoleAttribute)) as ProtocolRoleAttribute;

            Assert.IsNotNull(attrib);
            Assert.AreEqual((int)Protocols.Core, attrib.Protocol);
            Assert.AreEqual("client", attrib.Role);
            Assert.AreEqual("server", attrib.RequestedRole);
        }
    }
}
