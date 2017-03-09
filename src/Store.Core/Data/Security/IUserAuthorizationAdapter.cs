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

using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Security
{
    /// <summary>
    /// Defines methods that can be used to retrieve user authorization
    /// information from the underlying data store.
    /// </summary>
    public interface IUserAuthorizationAdapter
    {
        /// <summary>
        /// Determines whether the specified user is authorized to execute the function.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="request">The request context.</param>
        /// <param name="endpointType">The type of endpoint.</param>
        /// <returns><c>true</c> if the user is authorized; otherwise, <c>false</c>.</returns>
        bool IsAuthorized(string username, RequestContext request, WitsmlEndpointTypes endpointType);
    }
}
