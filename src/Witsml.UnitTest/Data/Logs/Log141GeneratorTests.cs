using System.Collections.Generic;
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
        private Log DepthLogIncreasing;
        private Log DepthLogDecreasing;
        private Log TimeLog;

        [TestInitialize]
        public void TestSetUp()
        {
            LogGenerator = new Log141Generator();
            DepthLogIncreasing = Create(LogIndexType.measureddepth, LogIndexDirection.increasing);
            DepthLogDecreasing = Create(LogIndexType.measureddepth, LogIndexDirection.decreasing);
            TimeLog = Create(LogIndexType.datetime, LogIndexDirection.increasing);
        }

        [TestMethod]
        public void Can_generate_depth_log()
        {
            LogGenerator.GenerateLogData(DepthLogDecreasing);

            Assert.IsNotNull(DepthLogDecreasing);
            Assert.IsNotNull(DepthLogDecreasing.LogData);
            Assert.IsNotNull(DepthLogDecreasing.LogData[0].Data);
            Assert.AreEqual(5, DepthLogDecreasing.LogData[0].Data.Count);
        }

        [TestMethod]
        public void Can_generate_increasing_depth_log_in_loop()
        {
            var startIndex = 0.0;
            var numOfRows = 3;
            var interval = 1.0;
            for (int i = 0; i < 10; i++)
            {
                var nextStartIndex = LogGenerator.GenerateLogData(DepthLogIncreasing, numOfRows, startIndex);
                Assert.AreEqual(startIndex + numOfRows * interval, nextStartIndex);
                startIndex = nextStartIndex;
            }

            Assert.IsNotNull(DepthLogIncreasing);
            Assert.IsNotNull(DepthLogIncreasing.LogData);
            Assert.IsNotNull(DepthLogIncreasing.LogData[0].Data);
            Assert.AreEqual(30, DepthLogIncreasing.LogData[0].Data.Count);

            double index = 0;
            foreach (string row in DepthLogIncreasing.LogData[0].Data)
            {
                string[] columns = row.Split(',');
                Assert.AreEqual(index, double.Parse(columns[0]));
                index += interval;
            }
        }
        [TestMethod]
        public void Can_generate_decreasing_depth_log_in_loop()
        {
            var startIndex = 0.0;
            var numOfRows = 3;
            var interval = -1.0;
            for (int i = 0; i < 10; i++)
            {
                var nextStartIndex = LogGenerator.GenerateLogData(DepthLogDecreasing, numOfRows, startIndex);
                Assert.AreEqual(startIndex + numOfRows*interval, nextStartIndex);
                startIndex = nextStartIndex;
            }

            Assert.IsNotNull(DepthLogDecreasing);
            Assert.IsNotNull(DepthLogDecreasing.LogData);
            Assert.IsNotNull(DepthLogDecreasing.LogData[0].Data);
            Assert.AreEqual(30, DepthLogDecreasing.LogData[0].Data.Count);

            double index = 0;
            foreach (string row in DepthLogDecreasing.LogData[0].Data)
            {
                string[] columns = row.Split(',');
                Assert.AreEqual(index, double.Parse(columns[0]));
                index += interval;
            }
        }

        [TestMethod]
        public void Can_generate_time_log()
        {
            LogGenerator.GenerateLogData(TimeLog, 10);

            Assert.IsNotNull(TimeLog);
            Assert.IsNotNull(TimeLog.LogData);
            Assert.IsNotNull(TimeLog.LogData[0].Data);
            Assert.AreEqual(10, TimeLog.LogData[0].Data.Count);
        }

        private Log Create(LogIndexType indexType, LogIndexDirection direction)
        {
            var log = new Log();

            log.IndexType = indexType;
            log.Direction = direction;
            log.LogCurveInfo = new List<LogCurveInfo>();

            if (indexType == LogIndexType.datetime)
            {
                log.IndexCurve = "TIME";
                log.LogCurveInfo.Add(LogGenerator.CreateDateTimeLogCurveInfo(log.IndexCurve, "s"));
            }
            else
            {
                log.IndexCurve = "MD";
                log.LogCurveInfo.Add(LogGenerator.CreateDoubleLogCurveInfo(log.IndexCurve, "m"));
            }

            log.LogCurveInfo.Add(LogGenerator.CreateDoubleLogCurveInfo("ROP", "m/h"));
            log.LogCurveInfo.Add(LogGenerator.CreateDoubleLogCurveInfo("GR", "gAPI"));

            return log;
        }
    }
}
