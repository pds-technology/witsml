using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson.Serialization;

namespace PDS.Witsml.Server.Data
{
    [TestClass]
    public class MongoDbClassMapperTests
    {
        private MongoDbClassMapper Mapper;

        [TestInitialize]
        public void TestSetUp()
        {
            Mapper = new MongoDbClassMapper();
            Mapper.Register();
        }

        [TestMethod]
        public void Mapper_can_provide_id_mapping_details()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(ChannelIndex));

            Assert.IsNotNull(classMap.IdMemberMap);
            Assert.AreEqual("Mnemonic", classMap.IdMemberMap.MemberName);
        }
    }
}
