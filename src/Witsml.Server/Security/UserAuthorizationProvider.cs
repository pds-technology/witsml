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

using System.ComponentModel.Composition;
using log4net;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Security
{
    /// <summary>
    /// Provides a base implementation of <see cref="IUserAuthorizationProvider"/>.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Security.IUserAuthorizationProvider" />
    [Export(typeof(IUserAuthorizationProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class UserAuthorizationProvider : IUserAuthorizationProvider
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(UserAuthorizationProvider));

        /// <summary>
        /// Verifies that the current user is authorized to execute the requested action.
        /// </summary>
        public void CheckAccess()
        {
            if (!IsAuthorized())
            {
                throw new WitsmlException(ErrorCodes.InsufficientOperationRights);
            }
        }

        /// <summary>
        /// Determines whether the current user is authorized to execute the requested action.
        /// </summary>
        /// <returns><c>true</c> if the current user is authorized; otherwise, <c>false</c>.</returns>
        public bool IsAuthorized()
        {
            if (!WitsmlSettings.IsUserAuthorizationEnabled)
                return true;

            var context = WitsmlOperationContext.Current;
            var username = context.User;
            var request = context.Request;

            _log.Error($"Verifying authorization for user: {username}; function: {request.Function}");

            return false;
        }
    }
}
