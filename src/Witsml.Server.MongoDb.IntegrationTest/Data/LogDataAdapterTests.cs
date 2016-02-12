using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Server.Data.Logs;

namespace PDS.Witsml.Server.Data
{
    [TestClass]
    public class LogDataAdapterTests
    {
        private Log141DataAdapter _logDataAdapter = new Log141DataAdapter(new DatabaseProvider(new Mapper()));

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
