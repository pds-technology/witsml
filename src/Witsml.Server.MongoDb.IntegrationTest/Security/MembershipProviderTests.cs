using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Web.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework.Web;

namespace PDS.Witsml.Server.Security
{
    [TestClass]
    public class MembershipProviderTests
    {
        private const string DbCollectionName = MongoDbMembershipProvider.DbCollectionName;
        private MongoDbMembershipProvider Provider;

        [TestInitialize]
        public void TestSetUp()
        {
            var config = new NameValueCollection(ConfigurationManager.AppSettings);

            ContainerConfiguration.Register(".");
            Provider = new MongoDbMembershipProvider();
            Provider.Initialize(MongoDbMembershipProvider.ProviderName, config);
        }

        [TestMethod]
        public void MongoDbMembershipProvider_Initialize_resolves_mongo_database_successfully()
        {
            Assert.IsNotNull(Provider.Db);
        }

        [TestMethod]
        public void MongoDbMembershipProvider_CreateUser_creates_default_user_successfully()
        {
            MembershipCreateStatus status;
            var name = "witsml.user";

            var user = Provider.CreateUser(
                username: name,
                password: "Pd$@meric@$",
                email: name + "@pds.nl",
                passwordQuestion: null,
                passwordAnswer: null,
                isApproved: true,
                providerUserKey: null,
                status: out status);

            Assert.IsNotNull(user);
            Assert.AreEqual(MembershipCreateStatus.Success, status);

            var saved = Provider.GetUser(name, false);
            Assert.IsNotNull(saved);
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

        //[Ignore]
        [TestMethod]
        public void MongoDbMembershipProvider_DeleteUser_removes_successfully()
        {
            var result = Provider.DeleteUser("witsml.user", true);
            Assert.AreEqual(true, result);
        }
    }
}
