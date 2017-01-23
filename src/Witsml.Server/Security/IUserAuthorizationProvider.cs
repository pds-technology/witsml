//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
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

namespace PDS.Witsml.Server.Security
{
    /// <summary>
    /// Defines methods that can be used to determine if a user is authorized to execute
    /// the requested actions and has access to any requested data objects.
    /// </summary>
    public interface IUserAuthorizationProvider
    {
        /// <summary>
        /// Verifies that the current user is authorized to execute the requested action.
        /// </summary>
        void CheckAccess();

        /// <summary>
        /// Determines whether the current user is authorized to execute the requested action.
        /// </summary>
        /// <returns><c>true</c> if the current user is authorized; otherwise, <c>false</c>.</returns>
        bool IsAuthorized();
    }
}