//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.1
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
using System.IO;
using System.Web.Security;
using CommandLine;
using PDS.WITSMLstudio.Framework.Web;
using PDS.WITSMLstudio.Store.Security;

namespace PDS.WITSMLstudio.Store.UserAdmin
{
    public static class Program
    {
        private const int Success = 0;
        private const int Error = 1;

        public static void Main(string[] args)
        {
            ContainerConfiguration.Register(".");
            Console.WriteLine();

            var writer = new StringWriter();
            new Parser(with =>
                {
                    with.EnableDashDash = true;
                    with.HelpWriter = writer;
                })
                .ParseArguments<AddOptions, RemoveOptions>(args)
                .MapResult(
                    (AddOptions opts) => AddUser(opts),
                    (RemoveOptions opts) => RemoveUser(opts),
                    errors =>
                    {
                        Console.WriteLine(writer);
                        return Error;
                    });
        }

        private static MongoDbMembershipProvider GetProvider()
        {
            var config = new NameValueCollection(ConfigurationManager.AppSettings);

            var provider = new MongoDbMembershipProvider();
            provider.Initialize(MongoDbMembershipProvider.ProviderName, config);

            return provider;
        }

        private static int AddUser(AddOptions opts)
        {
            MembershipCreateStatus status;

            var username = opts.Username;
            var password = opts.Password ?? Membership.GeneratePassword(8, 2);
            var email = opts.Email ?? "witsml@pds.nl";

            var provider = GetProvider();
            var saved = provider.GetUser(username, false);

            if (saved != null)
            {
                Console.WriteLine($"User '{username}' already exists");
                return Error;
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
                Console.WriteLine($"Error creating user '{username}'");
                return Error;
            }

            saved = provider.GetUser(username, false);

            if (saved == null || !provider.ValidateUser(username, password))
            {
                Console.WriteLine($"Error validating user '{username}'");
                return Error;
            }

            Console.WriteLine("email:     {0}", email);
            Console.WriteLine("username:  {0}", username);
            Console.WriteLine("password:  {0}", password);

            return Success;
        }

        private static int RemoveUser(RemoveOptions opts)
        {
            var provider = GetProvider();

            if (!provider.DeleteUser(opts.Username, true))
            {
                Console.WriteLine($"Error deleting user '{opts.Username}'");
                return Error;
            }

            return Success;
        }
    }
}
