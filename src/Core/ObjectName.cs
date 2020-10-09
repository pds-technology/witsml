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

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Represents a version-qualified data object name.
    /// </summary>
    public struct ObjectName
    {
        private readonly string _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectName"/> struct.
        /// </summary>
        /// <param name="version">The version.</param>
        public ObjectName(string version) : this(string.Empty, ObjectFamilies.Witsml, version)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectName"/> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="family">The family.</param>
        /// <param name="version">The version.</param>
        public ObjectName(string name, string family, string version)
        {
            // Get the ObjectType identifier
            Name = ObjectTypes.ObjectTypeMap.ContainsKey(name) ? ObjectTypes.ObjectTypeMap[name] : name;
            Family = family.ToUpperInvariant();
            Version = version;

            _value = $"{Name}_{Family}_{Version.Replace(".", string.Empty).Substring(0, 2)}";
        }

        /// <summary>
        /// The Energistics standard family.
        /// </summary>
        public string Family { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return _value;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="ObjectName"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="objectName">Name of the object.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator string(ObjectName objectName)
        {
            return objectName.ToString();
        }
    }
}
