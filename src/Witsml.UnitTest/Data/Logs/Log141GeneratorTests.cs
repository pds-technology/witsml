using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Data.Logs
{
    [TestClass]
    public class Log141GeneratorTests
    {
        private Log141Generator LogGenerator;
        private Log DepthLog;
        private Log TimeLog;

        [TestInitialize]
        public void TestSetUp()
        {
            LogGenerator = new Log141Generator();
            DepthLog = new Log() { IndexType = LogIndexType.measureddepth };
            TimeLog = new Log() { IndexType = LogIndexType.datetime };
        }

        [TestMethod]
        public void Can_generate_depth_log()
        {
            Log log = LogGenerator.CreateLog(DepthLog, LogIndexDirection.increasing, 10);
            Assert.IsNotNull(log);
            Assert.IsNotNull(log.LogData);
            Assert.IsNotNull(log.LogData[0].Data);
            Assert.AreEqual(10, log.LogData[0].Data.Count);
        }

        [TestMethod]
        public void Can_generate_time_log()
        {
            Log log = LogGenerator.CreateLog(TimeLog, LogIndexDirection.increasing, 10);
            Assert.IsNotNull(log);
            Assert.IsNotNull(log.LogData);
            Assert.IsNotNull(log.LogData[0].Data);
            Assert.AreEqual(10, log.LogData[0].Data.Count);
        }
    }
}
