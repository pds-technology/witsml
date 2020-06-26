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

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using Energistics.DataAccess;

namespace PDS.WITSMLstudio.Data.Logs
{
    [TestClass]
    public class LogExtensionsTests
    {
        private Log131Generator _log131Generator;
        private Log141Generator _log141Generator;
        private Log200Generator _log20Generator;

        [TestInitialize]
        public void TestSetUp()
        {
            _log131Generator = new Log131Generator();
            _log141Generator = new Log141Generator();
            _log20Generator = new Log200Generator();
        }

        [TestMethod]
        public void LogExtensions_141_GetNullValues_Can_Return_Null_Values_In_The_Order_Of_Mnemonics()
        {
            var log = new Witsml141.Log
            {
                LogCurveInfo = new List<Witsml141.ComponentSchemas.LogCurveInfo>()
            };

            var lci0 = _log141Generator.CreateDoubleLogCurveInfo("DEPTH", "m");
            lci0.NullValue = "-1000.00";
            log.LogCurveInfo.Add(lci0);
            var lci1 = _log141Generator.CreateDoubleLogCurveInfo("CH1", "m/h");
            lci1.NullValue = "-1111.11";
            log.LogCurveInfo.Add(lci1);
            var lci2 = _log141Generator.CreateDoubleLogCurveInfo("CH2", "gAPI");
            lci2.NullValue = "-2222.22";
            log.LogCurveInfo.Add(lci2);
            var lci3 = _log141Generator.CreateDoubleLogCurveInfo("CH3", "gAPI");
            lci3.NullValue = "-3333.33";
            log.LogCurveInfo.Add(lci3);
            var lci4 = _log141Generator.CreateDoubleLogCurveInfo("CH4", "gAPI");
            log.LogCurveInfo.Add(lci4);

            string[] mnemonic = new string[] { "CH3", "CH4", "CH1" };
            var nullValueList = log.GetNullValues(mnemonic).ToArray();
            Assert.AreEqual(3, nullValueList.Length);
            Assert.AreEqual("-3333.33", nullValueList[0]);
            Assert.IsNull(nullValueList[1]);
            Assert.AreEqual("-1111.11", nullValueList[2]);
        }

        [TestMethod]
        public void LogExtensions_141_GetNullValues_Has_Null_Indicator_Can_Return_Null_Values_In_The_Order_Of_Mnemonics()
        {
            var log = new Witsml141.Log
            {
                NullValue = "-9999.25",
                LogCurveInfo = new List<Witsml141.ComponentSchemas.LogCurveInfo>()
            };

            var lci0 = _log141Generator.CreateDoubleLogCurveInfo("DEPTH", "m");
            lci0.NullValue = "-1000.00";
            log.LogCurveInfo.Add(lci0);
            var lci1 = _log141Generator.CreateDoubleLogCurveInfo("CH1", "m/h");
            lci1.NullValue = "-1111.11";
            log.LogCurveInfo.Add(lci1);
            var lci2 = _log141Generator.CreateDoubleLogCurveInfo("CH2", "gAPI");
            log.LogCurveInfo.Add(lci2);
            var lci3 = _log141Generator.CreateDoubleLogCurveInfo("CH3", "gAPI");
            lci3.NullValue = "-3333.33";
            log.LogCurveInfo.Add(lci3);
            var lci4 = _log141Generator.CreateDoubleLogCurveInfo("CH4", "gAPI");
            lci4.NullValue = "-4444.44";
            log.LogCurveInfo.Add(lci4);

            string[] mnemonic = new string[] { "DEPTH", "CH2", "CH4", "CH3", "CH1" };
            var nullValueList = log.GetNullValues(mnemonic).ToArray();
            Assert.AreEqual(5, nullValueList.Length);
            Assert.AreEqual("-1000.00", nullValueList[0]);
            Assert.AreEqual("-9999.25", nullValueList[1]);
            Assert.AreEqual("-4444.44", nullValueList[2]);
            Assert.AreEqual("-3333.33", nullValueList[3]);
            Assert.AreEqual("-1111.11", nullValueList[4]);
        }

        [TestMethod]
        public void LogExtensions_GetNullValues_For_131_Log()
        {
            var log = new Witsml131.Log
            {
                NullValue = "-9999.25",
                LogCurveInfo = new List<Witsml131.ComponentSchemas.LogCurveInfo>()
            };

            var lci0 = _log131Generator.CreateDoubleLogCurveInfo("DEPTH", "m", 0);
            lci0.NullValue = "-1000.00";
            log.LogCurveInfo.Add(lci0);
            var lci1 = _log131Generator.CreateDoubleLogCurveInfo("CH1", "m/h", 1);
            lci1.NullValue = "-1111.11";
            log.LogCurveInfo.Add(lci1);
            var lci2 = _log131Generator.CreateDoubleLogCurveInfo("CH2", "gAPI", 2);
            log.LogCurveInfo.Add(lci2);

            string[] mnemonic = new string[] { "DEPTH", "CH2", "CH1" };
            var nullValueList = log.GetNullValues(mnemonic).ToArray();
            Assert.AreEqual(3, nullValueList.Length);
            Assert.AreEqual("-1000.00", nullValueList[0]);
            Assert.AreEqual("-9999.25", nullValueList[1]);
            Assert.AreEqual("-1111.11", nullValueList[2]);
        }

        [TestMethod]
        public void LogExtensions_141_IsValidDataDelimiter_Validation_Fail_Tests()
        {
            var log = new Witsml141.Log
            {
                DataDelimiter = "long"
            };

            Assert.IsFalse(log.IsValidDataDelimiter());

            // Validate that all digits are invalid
            for (int i = 0; i < 10; i++)
            {
                log.DataDelimiter = i.ToString();
                Assert.IsFalse(log.IsValidDataDelimiter());
            }

            // A space in the delimiter is not allowed
            log.DataDelimiter = "# ";
            Assert.IsFalse(log.IsValidDataDelimiter());

            // A decimal in the delimiter is not allowed
            log.DataDelimiter = ".";
            Assert.IsFalse(log.IsValidDataDelimiter());

            // A "+" in the delimiter is not allowed
            log.DataDelimiter = "+";
            Assert.IsFalse(log.IsValidDataDelimiter());

            // A "-" in the delimiter is not allowed
            log.DataDelimiter = "-";
            Assert.IsFalse(log.IsValidDataDelimiter());
        }

        [TestMethod]
        public void LogExtensions_141_IsValidDataDelimiter_Data_Delimiter_Validation_Pass_Tests()
        {
            var log = new Witsml141.Log();

            Assert.IsTrue(log.IsValidDataDelimiter());

            // Test symbols that should pass validation
            log.DataDelimiter = "#";
            Assert.IsTrue(log.IsValidDataDelimiter());

            log.DataDelimiter = "*";
            Assert.IsTrue(log.IsValidDataDelimiter());

            log.DataDelimiter = "~";
            Assert.IsTrue(log.IsValidDataDelimiter());

            log.DataDelimiter = "^";
            Assert.IsTrue(log.IsValidDataDelimiter());

            log.DataDelimiter = "$";
            Assert.IsTrue(log.IsValidDataDelimiter());

            log.DataDelimiter = "(";
            Assert.IsTrue(log.IsValidDataDelimiter());

            log.DataDelimiter = ")";
            Assert.IsTrue(log.IsValidDataDelimiter());

            log.DataDelimiter = "@";
            Assert.IsTrue(log.IsValidDataDelimiter());

            log.DataDelimiter = "!";
            Assert.IsTrue(log.IsValidDataDelimiter());

            log.DataDelimiter = "|";
            Assert.IsTrue(log.IsValidDataDelimiter());
        }

        [TestMethod]
        public void LogExtensions_IsIncreasing_Returns_Bool_For_131_Log_Direction()
        {
            var log = new Witsml131.Log
            {
                Direction = Witsml131.ReferenceData.LogIndexDirection.increasing
            };

            Assert.IsTrue(log.IsIncreasing());

            log.Direction = Witsml131.ReferenceData.LogIndexDirection.decreasing;
            Assert.IsFalse(log.IsIncreasing());
        }

        [TestMethod]
        public void LogExtensions_IsIncreasing_Returns_Bool_For_141_Log_Direction()
        {
            var log = new Witsml141.Log
            {
                Direction = Witsml141.ReferenceData.LogIndexDirection.increasing
            };

            Assert.IsTrue(log.IsIncreasing());

            log.Direction = Witsml141.ReferenceData.LogIndexDirection.decreasing;
            Assert.IsFalse(log.IsIncreasing());
        }

        [TestMethod]
        public void LogExtensions_IsIncreasing_Returns_Bool_If_131_Log_Is_Timelog()
        {
            var log = new Witsml131.Log
            {
                IndexType = Witsml131.ReferenceData.LogIndexType.datetime
            };

            Assert.IsTrue(log.IsTimeLog());

            log.IndexType = Witsml131.ReferenceData.LogIndexType.elapsedtime;
            Assert.IsTrue(log.IsTimeLog(true));

            log.IndexType = Witsml131.ReferenceData.LogIndexType.measureddepth;
            Assert.IsFalse(log.IsTimeLog());

            log.IndexType = null;
            Assert.IsTrue(log.IsTimeLog());

            log.LogData=new List<string>()
            {
                "2016-01-01T00:00:00.001Z,1,2,3"
            };

            Assert.IsTrue(log.IsTimeLog());

            log.LogData = new List<string>()
            {
                "1023.3,1,2,3"
            };

            Assert.IsFalse(log.IsTimeLog());
        }

        [TestMethod]
        public void LogExtensions_141_GetDataDelimiterOrDefault_When_Log_Is_Null()
        {
            var log = new Witsml141.Log();

            log = null;

            var result = log.GetDataDelimiterOrDefault();

            Assert.AreEqual(",", result);
        }

        [TestMethod]
        public void LogExtensions_IsIncreasing_Returns_Bool_If_141_Log_Is_Timelog()
        {
            var log = new Witsml141.Log
            {
                IndexType = Witsml141.ReferenceData.LogIndexType.datetime
            };

            Assert.IsTrue(log.IsTimeLog());

            log.IndexType = Witsml141.ReferenceData.LogIndexType.elapsedtime;
            Assert.IsTrue(log.IsTimeLog(true));

            log.IndexType = Witsml141.ReferenceData.LogIndexType.measureddepth;
            Assert.IsFalse(log.IsTimeLog());

            log.IndexType = null;
            Assert.IsTrue(log.IsTimeLog());

            log.LogData = new List<Witsml141.ComponentSchemas.LogData>()
            {
                new Witsml141.ComponentSchemas.LogData
                {
                    MnemonicList = "TIME,A,B,C",
                }
            };

            Assert.IsTrue(log.IsTimeLog());

            log.LogData = new List<Witsml141.ComponentSchemas.LogData>()
            {
                new Witsml141.ComponentSchemas.LogData
                {
                    Data = new List<string>()
                    {
                        "2016-01-01T00:00:00.001Z,1,2,3"
                    },
                    MnemonicList = "TIME,A,B,C",
                    UnitList = "unitless,m,m,m"
                }
            };

            log.LogData = new List<Witsml141.ComponentSchemas.LogData>()
            {
                new Witsml141.ComponentSchemas.LogData
                {
                    Data = new List<string>()
                    {
                        "1023.1,1,2,3"
                    },
                    MnemonicList = "DEPTH,A,B,C",
                    UnitList = "m,m,m,m"
                }
            };

            Assert.IsFalse(log.IsTimeLog());
        }

        [TestMethod]
        public void LogExtensions_GetByUid_Returns_131_LogCurveInfo_By_UID()
        {
            var log = new Witsml131.Log();

            var logCurveInfo = log.LogCurveInfo.GetByUid("depth");
            Assert.IsNull(logCurveInfo);

            log.LogCurveInfo = new List<Witsml131.ComponentSchemas.LogCurveInfo>();

            logCurveInfo = log.LogCurveInfo.GetByUid("depth");
            Assert.IsNull(logCurveInfo);

            // Add curves
            log.LogCurveInfo = new List<Witsml131.ComponentSchemas.LogCurveInfo>();
            var lci0 = _log131Generator.CreateDoubleLogCurveInfo("DEPTH", "m", 0);
            lci0.NullValue = "-1000.00";
            log.LogCurveInfo.Add(lci0);
            var lci1 = _log131Generator.CreateDoubleLogCurveInfo("CH1", "m/h", 1);
            lci1.NullValue = "-1111.11";
            log.LogCurveInfo.Add(lci1);

            logCurveInfo = log.LogCurveInfo.GetByUid("depth");
            Assert.IsNotNull(logCurveInfo);
            Assert.AreEqual(lci0, logCurveInfo);
        }

        [TestMethod]
        public void LogExtensions_GetByUid_Returns_141_LogCurveInfo_By_UID()
        {
            var log = new Witsml141.Log();

            var logCurveInfo = log.LogCurveInfo.GetByUid("depth");
            Assert.IsNull(logCurveInfo);

            log.LogCurveInfo = new List<Witsml141.ComponentSchemas.LogCurveInfo>();

            logCurveInfo = log.LogCurveInfo.GetByUid("depth");
            Assert.IsNull(logCurveInfo);

            // Add curves
            log.LogCurveInfo = new List<Witsml141.ComponentSchemas.LogCurveInfo>();
            var lci0 = _log141Generator.CreateDoubleLogCurveInfo("DEPTH", "m");
            lci0.NullValue = "-1000.00";
            log.LogCurveInfo.Add(lci0);
            var lci1 = _log141Generator.CreateDoubleLogCurveInfo("CH1", "m/h");
            lci1.NullValue = "-1111.11";
            log.LogCurveInfo.Add(lci1);

            logCurveInfo = log.LogCurveInfo.GetByUid("depth");
            Assert.IsNotNull(logCurveInfo);
            Assert.AreEqual(lci0, logCurveInfo);
        }

        [TestMethod]
        public void LogExtensions_GetByUid_Returns_200_Channel_By_UID()
        {
            var log = new Energistics.DataAccess.WITSML200.Log
            {
                Uuid = "uid",
                Citation = new Witsml200.ComponentSchemas.Citation(),
                Wellbore = new Witsml200.ComponentSchemas.DataObjectReference(),
                SchemaVersion = "2.0"
            };
            // Create channel set
            var channelSet = _log20Generator.CreateChannelSet(log);
            channelSet.Index.Add(_log20Generator.CreateMeasuredDepthIndex(Witsml200.ReferenceData.IndexDirection.increasing));

            channelSet.Channel = null;
            var channel = channelSet.Channel.GetByUuid("uuid");
            Assert.IsNull(channel);

            channelSet.Channel = new List<Energistics.DataAccess.WITSML200.Channel>();

            channel = channelSet.Channel.GetByUuid("uuid");
            Assert.IsNull(channel);

            // Add curves
            channelSet.Channel.Add(_log20Generator.CreateChannel(log, channelSet.Index, "HKLD", "HKLD", Witsml200.ReferenceData.UnitOfMeasure.klbf, "hookload", Witsml200.ReferenceData.EtpDataType.@double, new List<Witsml200.ComponentSchemas.PointMetadata>()));
            var gammaChannel = _log20Generator.CreateChannel(log, channelSet.Index, "GR", "GR", Witsml200.ReferenceData.UnitOfMeasure.gAPI, "gamma_ray", Witsml200.ReferenceData.EtpDataType.@double, new List<Witsml200.ComponentSchemas.PointMetadata>());
            channelSet.Channel.Add(gammaChannel);

            channel = channelSet.Channel.GetByUuid(gammaChannel.Uuid.ToLower());
            Assert.IsNotNull(channel);
            Assert.AreEqual(gammaChannel, channel);
        }

        [TestMethod]
        public void LogExtensions_GetByMnemonic_Returns_131_LogCurveInfo_By_Mnemonic()
        {
            var log = new Witsml131.Log();

            var logCurveInfo = log.LogCurveInfo.GetByMnemonic("depth");
            Assert.IsNull(logCurveInfo);

            log.LogCurveInfo = new List<Witsml131.ComponentSchemas.LogCurveInfo>();

            logCurveInfo = log.LogCurveInfo.GetByMnemonic("depth");
            Assert.IsNull(logCurveInfo);

            // Add curves
            log.LogCurveInfo = new List<Witsml131.ComponentSchemas.LogCurveInfo>();
            var lci0 = _log131Generator.CreateDoubleLogCurveInfo("DEPTH", "m", 0);
            lci0.NullValue = "-1000.00";
            log.LogCurveInfo.Add(lci0);
            var lci1 = _log131Generator.CreateDoubleLogCurveInfo("CH1", "m/h", 1);
            lci1.NullValue = "-1111.11";
            log.LogCurveInfo.Add(lci1);

            logCurveInfo = log.LogCurveInfo.GetByMnemonic("depth");
            Assert.IsNotNull(logCurveInfo);
            Assert.AreEqual(lci0, logCurveInfo);
        }

        [TestMethod]
        public void LogExtensions_GetByMnemonic_Returns_141_LogCurveInfo_By_Mnemonic()
        {
            var log = new Witsml141.Log();

            var logCurveInfo = log.LogCurveInfo.GetByMnemonic("depth");
            Assert.IsNull(logCurveInfo);

            log.LogCurveInfo = new List<Witsml141.ComponentSchemas.LogCurveInfo>();

            logCurveInfo = log.LogCurveInfo.GetByMnemonic("depth");
            Assert.IsNull(logCurveInfo);

            // Add curves
            log.LogCurveInfo = new List<Witsml141.ComponentSchemas.LogCurveInfo>();
            var lci0 = _log141Generator.CreateDoubleLogCurveInfo("DEPTH", "m");
            lci0.NullValue = "-1000.00";
            log.LogCurveInfo.Add(lci0);
            var lci1 = _log141Generator.CreateDoubleLogCurveInfo("CH1", "m/h");
            lci1.NullValue = "-1111.11";
            log.LogCurveInfo.Add(lci1);

            logCurveInfo = log.LogCurveInfo.GetByMnemonic("depth");
            Assert.IsNotNull(logCurveInfo);
            Assert.AreEqual(lci0, logCurveInfo);
        }

        [TestMethod]
        public void LogExtensions_GetByMnemonic_Returns_200_Channel_By_Mnemonic()
        {
            var log = new Energistics.DataAccess.WITSML200.Log
            {
                Uuid = "uid",
                Citation = new Witsml200.ComponentSchemas.Citation(),
                Wellbore = new Witsml200.ComponentSchemas.DataObjectReference(),
                SchemaVersion = "2.0"
            };
            // Create channel set
            var channelSet = _log20Generator.CreateChannelSet(log);
            channelSet.Index.Add(_log20Generator.CreateMeasuredDepthIndex(Witsml200.ReferenceData.IndexDirection.increasing));

            channelSet.Channel = null;
            var channel = channelSet.Channel.GetByMnemonic("gr");
            Assert.IsNull(channel);

            channelSet.Channel = new List<Energistics.DataAccess.WITSML200.Channel>();

            channel = channelSet.Channel.GetByMnemonic("gr");
            Assert.IsNull(channel);

            // Add curves
            channelSet.Channel.Add(_log20Generator.CreateChannel(log, channelSet.Index, "HKLD", "HKLD", Witsml200.ReferenceData.UnitOfMeasure.klbf, "hookload", Witsml200.ReferenceData.EtpDataType.@double, new List<Witsml200.ComponentSchemas.PointMetadata>()));
            var gammaChannel = _log20Generator.CreateChannel(log, channelSet.Index, "GR", "GR", Witsml200.ReferenceData.UnitOfMeasure.gAPI, "gamma_ray", Witsml200.ReferenceData.EtpDataType.@double, new List<Witsml200.ComponentSchemas.PointMetadata>());
            channelSet.Channel.Add(gammaChannel);

            channel = channelSet.Channel.GetByMnemonic("gr");
            Assert.IsNotNull(channel);
            Assert.AreEqual(gammaChannel, channel);
        }

        [TestMethod]
        public void LogExtensions_GetIndexRange_Returns_Range_From_131_TimeLog()
        {
            Timestamp start, end;
            InitTimeIndexes(out start, out end);
            var startAsLong = start.ToUnixTimeMicroseconds();
            var endAsLong = end.ToUnixTimeMicroseconds();

            var log = new Witsml131.Log
            {
                LogCurveInfo = new List<Witsml131.ComponentSchemas.LogCurveInfo>()
            };

            var logCurveInfo = new Witsml131.ComponentSchemas.LogCurveInfo();
            logCurveInfo = null;
            var result = logCurveInfo.GetIndexRange();

            Assert.IsNull(result.Start);
            Assert.IsNull(result.End);

            // Add logCurveInfo with just start index
            var lci0 = _log131Generator.CreateDoubleLogCurveInfo("DEPTH", "m", 0);
            lci0.NullValue = "-1000.00";
            lci0.MinDateTimeIndex = start;
            log.LogCurveInfo.Add(lci0);

            result = log.LogCurveInfo[0].GetIndexRange(true, true);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Start.HasValue);
            Assert.IsFalse(result.End.HasValue);
            Assert.AreEqual(startAsLong, result.Start.Value);

            // Decreasing log
            result = log.LogCurveInfo[0].GetIndexRange(false, true);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.End.HasValue);
            Assert.IsFalse(result.Start.HasValue);
            Assert.AreEqual(startAsLong, result.End.Value);

            // Update end index
            lci0.MaxDateTimeIndex = end;

            result = log.LogCurveInfo[0].GetIndexRange(true, true);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Start.HasValue);
            Assert.IsTrue(result.End.HasValue);
            Assert.AreEqual(startAsLong, result.Start.Value);
            Assert.AreEqual(endAsLong, result.End.Value);

            // Decreasing with end index
            result = log.LogCurveInfo[0].GetIndexRange(false, true);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Start.HasValue);
            Assert.IsTrue(result.End.HasValue);
            Assert.AreEqual(startAsLong, result.End.Value);
            Assert.AreEqual(endAsLong, result.Start.Value);
        }

        [TestMethod]
        public void LogExtensions_GetIndexRange_Returns_Range_From_131_DepthLog()
        {
            double start, end;
            InitDepthIndexes(out start, out end);

            var log = new Witsml131.Log
            {
                LogCurveInfo = new List<Witsml131.ComponentSchemas.LogCurveInfo>()
            };

            var result = new Witsml131.ComponentSchemas.LogCurveInfo().GetIndexRange();

            Assert.IsNull(result.Start);
            Assert.IsNull(result.End);

            // Add logCurveInfo with just start index
            var lci0 = _log131Generator.CreateDoubleLogCurveInfo("DEPTH", "m", 0);
            lci0.NullValue = "-1000.00";
            lci0.MinIndex = new Witsml131.ComponentSchemas.GenericMeasure(start, "m");
            log.LogCurveInfo.Add(lci0);

            result = log.LogCurveInfo[0].GetIndexRange();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Start.HasValue);
            Assert.IsFalse(result.End.HasValue);
            Assert.AreEqual(start, result.Start.Value);

            // Decreasing log
            result = log.LogCurveInfo[0].GetIndexRange(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.End.HasValue);
            Assert.IsFalse(result.Start.HasValue);
            Assert.AreEqual(start, result.End.Value);

            // Update end index
            lci0.MaxIndex = new Witsml131.ComponentSchemas.GenericMeasure(end, "m");

            result = log.LogCurveInfo[0].GetIndexRange();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Start.HasValue);
            Assert.IsTrue(result.End.HasValue);
            Assert.AreEqual(start, result.Start.Value);
            Assert.AreEqual(end, result.End.Value);

            // Decreasing with end index
            result = log.LogCurveInfo[0].GetIndexRange(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Start.HasValue);
            Assert.IsTrue(result.End.HasValue);
            Assert.AreEqual(start, result.End.Value);
            Assert.AreEqual(end, result.Start.Value);
        }


        [TestMethod]
        public void LogExtensions_GetIndexRange_Returns_Range_From_141_TimeLog()
        {
            Timestamp start, end;
            InitTimeIndexes(out start, out end);
            var startAsLong = start.ToUnixTimeMicroseconds();
            var endAsLong = end.ToUnixTimeMicroseconds();

            var log = new Witsml141.Log
            {
                LogCurveInfo = new List<Witsml141.ComponentSchemas.LogCurveInfo>()
            };

            var logCurveInfo = new Witsml141.ComponentSchemas.LogCurveInfo();
            logCurveInfo = null;
            var result = logCurveInfo.GetIndexRange();

            Assert.IsNull(result.Start);
            Assert.IsNull(result.End);

            // Add logCurveInfo with just start index
            var lci0 = _log141Generator.CreateDoubleLogCurveInfo("DEPTH", "m");
            lci0.NullValue = "-1000.00";
            lci0.MinDateTimeIndex = start;
            log.LogCurveInfo.Add(lci0);

            result = log.LogCurveInfo[0].GetIndexRange(true, true);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Start.HasValue);
            Assert.IsFalse(result.End.HasValue);
            Assert.AreEqual(startAsLong, result.Start.Value);

            // Decreasing log
            result = log.LogCurveInfo[0].GetIndexRange(false, true);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.End.HasValue);
            Assert.IsFalse(result.Start.HasValue);
            Assert.AreEqual(startAsLong, result.End.Value);

            // Update end index
            lci0.MaxDateTimeIndex = end;

            result = log.LogCurveInfo[0].GetIndexRange(true, true);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Start.HasValue);
            Assert.IsTrue(result.End.HasValue);
            Assert.AreEqual(startAsLong, result.Start.Value);
            Assert.AreEqual(endAsLong, result.End.Value);

            // Decreasing with end index
            result = log.LogCurveInfo[0].GetIndexRange(false, true);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Start.HasValue);
            Assert.IsTrue(result.End.HasValue);
            Assert.AreEqual(startAsLong, result.End.Value);
            Assert.AreEqual(endAsLong, result.Start.Value);
        }

        [TestMethod]
        public void LogExtensions_GetIndexRange_Returns_Range_From_141_DepthLog()
        {
            double start, end;
            InitDepthIndexes(out start, out end);

            var log = new Witsml141.Log
            {
                LogCurveInfo = new List<Witsml141.ComponentSchemas.LogCurveInfo>()
            };

            var result = new Witsml141.ComponentSchemas.LogCurveInfo().GetIndexRange();

            Assert.IsNull(result.Start);
            Assert.IsNull(result.End);

            // Add logCurveInfo with just start index
            var lci0 = _log141Generator.CreateDoubleLogCurveInfo("DEPTH", "m");
            lci0.NullValue = "-1000.00";
            lci0.MinIndex = new Witsml141.ComponentSchemas.GenericMeasure(start, "m");
            log.LogCurveInfo.Add(lci0);

            result = log.LogCurveInfo[0].GetIndexRange();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Start.HasValue);
            Assert.IsFalse(result.End.HasValue);
            Assert.AreEqual(start, result.Start.Value);

            // Decreasing log
            result = log.LogCurveInfo[0].GetIndexRange(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.End.HasValue);
            Assert.IsFalse(result.Start.HasValue);
            Assert.AreEqual(start, result.End.Value);

            // Update end index
            lci0.MaxIndex = new Witsml141.ComponentSchemas.GenericMeasure(end, "m");

            result = log.LogCurveInfo[0].GetIndexRange();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Start.HasValue);
            Assert.IsTrue(result.End.HasValue);
            Assert.AreEqual(start, result.Start.Value);
            Assert.AreEqual(end, result.End.Value);

            // Decreasing with end index
            result = log.LogCurveInfo[0].GetIndexRange(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Start.HasValue);
            Assert.IsTrue(result.End.HasValue);
            Assert.AreEqual(start, result.End.Value);
            Assert.AreEqual(end, result.Start.Value);
        }

        #region Helper Methods

        private static void InitTimeIndexes(out Timestamp start, out Timestamp end)
        {
            var timeSpan = new TimeSpan(6, 0, 0);
            var dto = new DateTimeOffset(2016, 1, 1, 0, 0, 0, 1, timeSpan);
            start = new Timestamp(dto);
            end = new Timestamp(dto.AddHours(1));
        }

        private static void InitDepthIndexes(out double start, out double end)
        {
            start = 100.0;
            end = 200.0;
        } 

        #endregion
    }
}
