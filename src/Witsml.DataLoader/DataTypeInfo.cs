//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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

using System.Runtime.Serialization;

namespace PDS.Witsml.DataLoader
{
    /// <summary>
    /// Defines the settings used by the data loader.
    /// </summary>
    [DataContract]
    public class DataTypeInfo
    {
        /// <summary>
        /// Gets or sets the type of the object.
        /// </summary>
        /// <value>The type of the object.</value>
        [DataMember]
        public string ObjectType { get; set; }

        /// <summary>
        /// Gets or sets the folder name pattern.
        /// </summary>
        /// <value>The folder name pattern.</value>
        [DataMember]
        public string FolderNamePattern { get; set; }

        /// <summary>
        /// Gets or sets the file name pattern.
        /// </summary>
        /// <value>The file name pattern.</value>
        [DataMember]
        public string FileNamePattern { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="DataTypeInfo"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        [DataMember]
        public bool Enabled { get; set; }
    }
}
