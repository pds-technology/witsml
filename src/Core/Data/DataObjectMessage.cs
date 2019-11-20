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

using System;
using System.Collections.Generic;

namespace PDS.WITSMLstudio.Data
{
    /// <summary>
    /// Encapsulates common data object message properties.
    /// </summary>
    public class DataObjectMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataObjectMessage"/> class.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="dataObject">The optional data object instance.</param>
        public DataObjectMessage(string uri, object dataObject = null)
        {
            Uuid = Guid.NewGuid();
            Uri = uri;
            ObjectMetadata = dataObject;
            ObjectMetadataType = dataObject?.GetType().AssemblyQualifiedName;
            ExecutionDateTime = DateTime.UtcNow;
            //CustomData = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets or sets the unique identifier for the message.
        /// </summary>
        public Guid? Uuid { get; set; }

        /// <summary>
        /// Gets or sets the user host.
        /// </summary>
        public string UserHost { get; set; }

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the WITSML Store API function.
        /// </summary>
        public Functions? Function { get; set; }

        /// <summary>
        /// Gets or sets the WITSML optionsIn.
        /// </summary>
        public string OptionsIn { get; set; }

        /// <summary>
        /// Gets or sets the type of the object.
        /// </summary>
        public string ObjectType { get; set; }

        /// <summary>
        /// Gets or sets the data object URI.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the created date time in UTC.
        /// </summary>
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the last update date time in UTC.
        /// </summary>
        public DateTime? LastUpdateDateTime { get; set; }

        /// <summary>
        /// Gets or sets the execution date time.
        /// </summary>
        public DateTime? ExecutionDateTime { get; set; }

        /// <summary>
        /// Gets or sets the type of the object metadata.
        /// </summary>
        public string ObjectMetadataType { get; set; }

        /// <summary>
        /// Gets or sets the data object instance.
        /// </summary>
        public object ObjectMetadata { get; set; }

        /// <summary>
        /// Gets or sets the custom data dictionary.
        /// </summary>
        public Dictionary<string, object> CustomData { get; set; }
    }
}
