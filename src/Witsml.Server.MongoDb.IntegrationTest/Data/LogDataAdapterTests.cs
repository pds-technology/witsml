using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Server.Data.Logs;

namespace PDS.Witsml.Server.Data
{
    [TestClass]
    public class LogDataAdapterTests
    {
        DatabaseProvider _databaseProvider = new DatabaseProvider(new MongoDbClassMapper());
        private Log141DataAdapter _logDataAdapter;

        [TestInitialize]
        public void TestSetup()
        {
            _databaseProvider = new DatabaseProvider(new MongoDbClassMapper());
            _logDataAdapter = new Log141DataAdapter(_databaseProvider, new ChannelDataAdapter(_databaseProvider));
        }

        [TestMethod]
        public void index_range_test()
        {
            var startIndex = 99.0;
            //var endIndex = 250.0;
            var rangeSize = 100;

            var range = _logDataAdapter.ComputeRange(startIndex, rangeSize);

            Assert.IsNotNull(range);
            Assert.AreEqual(100, range.Item2);
        }
    }
}
