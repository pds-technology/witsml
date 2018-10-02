//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
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
using System.Collections.Specialized;
using System.Configuration;
using System.Web.Security;
using PDS.WITSMLstudio.Store.Security;

namespace PDS.WITSMLstudio.Store.UserAdmin
{
    public class UserAdmin
    {
        public static string AddUser(AddOptions opts)
        {
            var errorMessage = string.Empty;
            MembershipCreateStatus status;

            var username = opts.Username;
            var password = opts.Password ?? Membership.GeneratePassword(8, 2);
            var email = opts.Email ?? "witsml@pds.nl";

            var provider = GetProvider();
            var saved = provider.GetUser(username, false);

            if (saved != null)
            {
                errorMessage = $"User '{username}' already exists";
                return errorMessage;
            }

            var user = provider.CreateUser(
                username: username,
                password: password,
                email: email,
                passwordQuestion: null,
                passwordAnswer: null,
                isApproved: true,
                providerUserKey: null,
                status: out status);

            if (user == null || status != MembershipCreateStatus.Success)
            {
                errorMessage = $"Error creating user '{username}'";
                return errorMessage;
            }
            saved = provider.GetUser(username, false);

            if (saved == null || !provider.ValidateUser(username, password))
            {
                errorMessage = $"Error validating user '{username}'";
                return errorMessage;
            }
            Console.WriteLine("email:     {0}", email);
            Console.WriteLine("username:  {0}", username);
            Console.WriteLine("password:  {0}", password);
            return errorMessage;
        }

        public static string RemoveUser(RemoveOptions opts)
        {
            var errorMessage = string.Empty;
            var provider = GetProvider();

            if (provider.DeleteUser(opts.Username, true)) return errorMessage;

            errorMessage = $"Error deleting user '{opts.Username}'";
            return errorMessage;
        }

        /// <summary>
        /// Gets the user email.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <returns>The email of the specified user.</returns>
        public static string GetUserEmail(string userName)
        {
            var provider = GetProvider();
            return provider.GetUser(userName, false)?.Email;
        }

        private static MongoDbMembershipProvider GetProvider()
        {
            var config = new NameValueCollection(ConfigurationManager.AppSettings);

            var provider = new MongoDbMembershipProvider();
            provider.Initialize(MongoDbMembershipProvider.ProviderName, config);

            return provider;
        }
    }
}
