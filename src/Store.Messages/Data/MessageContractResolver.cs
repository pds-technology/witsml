//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.Reflection;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Resolves member mappings for common message types.
    /// </summary>
    /// <seealso cref="Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver" />
    public class MessageContractResolver : CamelCasePropertyNamesContractResolver
    {
        /// <summary>
        /// Creates a <see cref="JsonProperty" /> for the given <see cref="MemberInfo" />.
        /// </summary>
        /// <param name="member">The member to create a <see cref="JsonProperty" /> for.</param>
        /// <param name="memberSerialization">The member's parent <see cref="MemberSerialization" />.</param>
        /// <returns>A created <see cref="JsonProperty" /> for the given <see cref="MemberInfo" />.</returns>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (XmlAttributeCache<XmlIgnoreAttribute>.IsDefined(member))
            {
                property.ShouldSerialize = instance => false;
            }

            return property;
        }
    }
}