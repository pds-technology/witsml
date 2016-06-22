//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Data.Logs
{
    [TestClass]
    public class LogExtensionsTests
    {
        private Log141Generator LogGenerator;

        [TestInitialize]
        public void TestSetUp()
        {
            LogGenerator = new Log141Generator();
        }

        [TestMethod]
        public void LogExtensions_Can_Return_Null_Values_In_The_Order_Of_Mnemonics()
        {
            var log = new Log();
            log.LogCurveInfo = new List<LogCurveInfo>();
            var lci0 = LogGenerator.CreateDoubleLogCurveInfo("DEPTH", "m");
            lci0.NullValue = "-1000.00";
            log.LogCurveInfo.Add(lci0);
            var lci1 = LogGenerator.CreateDoubleLogCurveInfo("CH1", "m/h");
            lci1.NullValue = "-1111.11";
            log.LogCurveInfo.Add(lci1);
            var lci2 = LogGenerator.CreateDoubleLogCurveInfo("CH2", "gAPI");
            lci2.NullValue = "-2222.22";
            log.LogCurveInfo.Add(lci2);
            var lci3 = LogGenerator.CreateDoubleLogCurveInfo("CH3", "gAPI");
            lci3.NullValue = "-3333.33";
            log.LogCurveInfo.Add(lci3);
            var lci4 = LogGenerator.CreateDoubleLogCurveInfo("CH4", "gAPI");
            log.LogCurveInfo.Add(lci4);

            string[] mnemonic = new string[] { "CH3", "CH4", "CH1" };
            var nullValueList = log.GetNullValues(mnemonic).ToArray();
            Assert.AreEqual(3, nullValueList.Length);
            Assert.AreEqual("-3333.33", nullValueList[0]);
            Assert.AreEqual("null", nullValueList[1]);
            Assert.AreEqual("-1111.11", nullValueList[2]);
        }

        [TestMethod]
        public void LogExtensions_Has_Null_Indicator_Can_Return_Null_Values_In_The_Order_Of_Mnemonics()
        {
            var log = new Log();
            log.NullValue = "-9999.25";
            log.LogCurveInfo = new List<LogCurveInfo>();
            var lci0 = LogGenerator.CreateDoubleLogCurveInfo("DEPTH", "m");
            lci0.NullValue = "-1000.00";
            log.LogCurveInfo.Add(lci0);
            var lci1 = LogGenerator.CreateDoubleLogCurveInfo("CH1", "m/h");
            lci1.NullValue = "-1111.11";
            log.LogCurveInfo.Add(lci1);
            var lci2 = LogGenerator.CreateDoubleLogCurveInfo("CH2", "gAPI");
            log.LogCurveInfo.Add(lci2);
            var lci3 = LogGenerator.CreateDoubleLogCurveInfo("CH3", "gAPI");
            lci3.NullValue = "-3333.33";
            log.LogCurveInfo.Add(lci3);
            var lci4 = LogGenerator.CreateDoubleLogCurveInfo("CH4", "gAPI");
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
        public void LogExtensions_Data_Delimiter_Validation_Fail_Tests()
        {
            var log = new Log();

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
        public void LogExtensions_Data_Delimiter_Validation_Pass_Tests()
        {
            var log = new Log();

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
    }
}
