//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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

using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Data.Logs
{
    [TestClass]
    public class Log141GeneratorTests
    {
        private Log141Generator _logGenerator;
        private Log _depthLogIncreasing;
        private Log _depthLogDecreasing;
        private Log _timeLog;

        [TestInitialize]
        public void TestSetUp()
        {
            _logGenerator = new Log141Generator();
            _depthLogIncreasing = CreateLog(LogIndexType.measureddepth, LogIndexDirection.increasing);
            _depthLogDecreasing = CreateLog(LogIndexType.measureddepth, LogIndexDirection.decreasing);
            _timeLog = CreateLog(LogIndexType.datetime, LogIndexDirection.increasing);
        }

        [TestMethod]
        public void Log141Generator_Can_Generate_Depth_Log_Data_Decreasing()
        {
            _logGenerator.GenerateLogData(_depthLogDecreasing);

            Assert.IsNotNull(_depthLogDecreasing);
            Assert.IsNotNull(_depthLogDecreasing.LogData);
            Assert.IsNotNull(_depthLogDecreasing.LogData[0].Data);
            Assert.AreEqual(5, _depthLogDecreasing.LogData[0].Data.Count);
        }

        [TestMethod]
        public void Log141Generator_Can_Generate_Depth_Log_Data_Increasing()
        {
            _logGenerator.GenerateLogData(_depthLogIncreasing, 50);

            Assert.IsNotNull(_depthLogIncreasing);
            Assert.IsNotNull(_depthLogIncreasing.LogData);
            Assert.IsNotNull(_depthLogIncreasing.LogData[0].Data);
            Assert.AreEqual(50, _depthLogIncreasing.LogData[0].Data.Count);
        }

        [TestMethod]
        public void Log141Generator_Can_Generate_Depth_Log_Data_Increasing_Repeatedly()
        {
            var startIndex = 0.0;
            var logDatas = new List<LogData>();
            const int numOfRows = 3;
            const double interval = 1.0;

            for (var i = 0; i < 10; i++)
            {
                var nextStartIndex = _logGenerator.GenerateLogData(_depthLogIncreasing, numOfRows, startIndex);
                
                // Log data is cleared each time generate log data is called
                logDatas.Add(_depthLogIncreasing.LogData[0]);

                Assert.AreEqual(startIndex + numOfRows * interval, nextStartIndex);
                startIndex = nextStartIndex;
            }

            Assert.IsNotNull(_depthLogIncreasing);
            Assert.IsNotNull(_depthLogIncreasing.LogData);
            Assert.IsNotNull(_depthLogIncreasing.LogData[0].Data);
            Assert.AreEqual(30, logDatas.Sum(x => x.Data.Count));

            double index = 0;
            foreach (var logData in logDatas)
            {
                foreach (var row in logData.Data)
                {
                    var columns = row.Split(',');
                    Assert.AreEqual(index, double.Parse(columns[0]));
                    index += interval;
                }
            }
        }

        [TestMethod]
        public void Log141Generator_Can_Generate_Depth_Log_Data_Decreasing_Repeatedly()
        {
            var startIndex = 0.0;
            var logDatas = new List<LogData>();
            const int numOfRows = 3;
            const double interval = -1.0;

            for (var i = 0; i < 10; i++)
            {
                var nextStartIndex = _logGenerator.GenerateLogData(_depthLogDecreasing, numOfRows, startIndex);

                // Log data is cleared each time generate log data is called
                logDatas.Add(_depthLogDecreasing.LogData[0]);

                Assert.AreEqual(startIndex + numOfRows * interval, nextStartIndex);
                startIndex = nextStartIndex;
            }

            Assert.IsNotNull(_depthLogDecreasing);
            Assert.IsNotNull(_depthLogDecreasing.LogData);
            Assert.IsNotNull(_depthLogDecreasing.LogData[0].Data);
            Assert.AreEqual(30, logDatas.Sum(x => x.Data.Count));

            double index = 0;
            foreach (var logData in logDatas)
            {
                foreach (var row in logData.Data)
                {
                    var columns = row.Split(',');
                    Assert.AreEqual(index, double.Parse(columns[0]));
                    index += interval;
                }
            }
        }

        [TestMethod]
        public void Log141Generator_Can_Generate_Time_Log_Data()
        {
            _logGenerator.GenerateLogData(_timeLog, 10);

            Assert.IsNotNull(_timeLog);
            Assert.IsNotNull(_timeLog.LogData);
            Assert.IsNotNull(_timeLog.LogData[0].Data);
            Assert.AreEqual(10, _timeLog.LogData[0].Data.Count);
        }

        private Log CreateLog(LogIndexType indexType, LogIndexDirection direction)
        {
            var log = new Log
            {
                IndexType = indexType,
                Direction = direction,
                LogCurveInfo = new List<LogCurveInfo>()
            };

            if (indexType == LogIndexType.datetime)
            {
                log.IndexCurve = "TIME";
                log.LogCurveInfo.Add(_logGenerator.CreateDateTimeLogCurveInfo(log.IndexCurve, "s"));
            }
            else
            {
                log.IndexCurve = "MD";
                log.LogCurveInfo.Add(_logGenerator.CreateDoubleLogCurveInfo(log.IndexCurve, "m"));
            }

            log.LogCurveInfo.Add(_logGenerator.CreateDoubleLogCurveInfo("ROP", "m/h"));
            log.LogCurveInfo.Add(_logGenerator.CreateDoubleLogCurveInfo("GR", "gAPI"));

            return log;
        }
    }
}
