//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

namespace PDS.Witsml.Server.Configuration
{
    /// <summary>
    /// Defines properties and methods used for retrieving WITSML Store capabilities. 
    /// </summary>
    public interface ICapServerProvider
    {
        /// <summary>
        /// Gets the data schema version.
        /// </summary>
        /// <value>The data schema version.</value>
        string DataSchemaVersion { get; }

        /// <summary>
        /// Returns the server capabilities object as XML.
        /// </summary>
        /// <returns>A capServers object as an XML string.</returns>
        string ToXml();

        /// <summary>
        /// Determines whether the specified function is supported for the object type.
        /// </summary>
        /// <param name="function">The WITSML Store API function.</param>
        /// <param name="objectType">The type of the data object.</param>
        /// <returns>true if the WITSML Store supports the function for the specified object type, otherwise, false</returns>
        bool IsSupported(Functions function, string objectType);

        /// <summary>
        /// Performs validation for the specified function and supplied parameters.
        /// </summary>
        void ValidateRequest();
    }
}
