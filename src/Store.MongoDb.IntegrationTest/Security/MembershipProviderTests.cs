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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Data;

namespace PDS.WITSMLstudio.Store.Security
{
    [TestClass]
    public class MembershipProviderTests
    {
        private MongoDbMembershipProvider Provider;
        private DevKit141Aspect DevKit;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect(TestContext);
            var config = new NameValueCollection(ConfigurationManager.AppSettings);

            Provider = new MongoDbMembershipProvider();
            Provider.Initialize(MongoDbMembershipProvider.ProviderName, config);
        }

        [TestMethod]
        public void MongoDbMembershipProvider_Initialize_resolves_mongo_database_provider_successfully()
        {
            Assert.IsNotNull(Provider.DatabaseProvider);
        }

        [TestMethod]
        public void MongoDbMembershipProvider_CreateUser_creates_default_user_successfully()
        {
            MembershipCreateStatus status;
            var info = Tuple.Create("witsml.user", "", "witsmlstudio@pds.group");

            var user = Provider.CreateUser(
                username: info.Item1,
                password: info.Item2,
                email: info.Item3,
                passwordQuestion: null,
                passwordAnswer: null,
                isApproved: true,
                providerUserKey: null,
                status: out status);

            Assert.IsNotNull(user);
            Assert.AreEqual(MembershipCreateStatus.Success, status);

            var saved = Provider.GetUser(info.Item1, false);
            Assert.IsNotNull(saved);
        }

        [TestMethod]
        public void MongoDbMembershipProvider_CreateUser_creates_user_successfully()
        {
            MembershipCreateStatus status;
            var password = Membership.GeneratePassword(8, 2);

            var info = Tuple.Create("pds.user", password, "witsmlstudio@pds.group");

            var saved = Provider.GetUser(info.Item1, false);

            if (saved == null)
            {
                var user = Provider.CreateUser(
                    username: info.Item1,
                    password: info.Item2,
                    email: info.Item3,
                    passwordQuestion: null,
                    passwordAnswer: null,
                    isApproved: true,
                    providerUserKey: null,
                    status: out status);

                Assert.IsNotNull(user);
                Assert.AreEqual(MembershipCreateStatus.Success, status);

                saved = Provider.GetUser(info.Item1, false);
                Assert.IsNotNull(saved);

                var result = Provider.ValidateUser(info.Item1, info.Item2);
                Assert.IsTrue(result);

                Console.WriteLine("email:  {0}", info.Item3);
                Console.WriteLine("username:  {0}", info.Item1);
                Console.WriteLine("password:  {0}", info.Item2);
            }
        }

        [TestMethod]
        public void MongoDbMembershipProvider_can_validate_user()
        {
            var info = Tuple.Create("witsml.user", "", "witsmlstudio@pds.group");

            var result = Provider.ValidateUser(info.Item1, info.Item2);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MongoDbMembershipProvider_CreateUser_saves_successfully()
        {
            MembershipCreateStatus status;
            var name = "witsml.user";
            var id = "." + DateTime.Now.ToFileTimeUtc();

            var user = Provider.CreateUser(
                username: name + id,
                password: name,
                email: name + id + "@pds.nl",
                passwordQuestion: null,
                passwordAnswer: null,
                isApproved: true,
                providerUserKey: null,
                status: out status);

            Assert.IsNotNull(user);
            Assert.AreEqual(MembershipCreateStatus.Success, status);

            var saved = Provider.GetUser(name + id, false);
            Assert.IsNotNull(saved);

            var result = Provider.DeleteUser(name + id, true);
            Assert.AreEqual(true, result);
        }

        [Ignore]
        [TestMethod]
        public void MongoDbMembershipProvider_DeleteUser_removes_successfully()
        {
            var result = Provider.DeleteUser("witsml.user", true);
            Assert.AreEqual(true, result);
        }
    }
}
