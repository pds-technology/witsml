//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio
{
    [TestClass]
    public class DateTimeExtensionsTests
    {
        [TestMethod]
        public void DateTimeExtensions_ToUnixTimeMicrosecods_Converts_From_DateTimeOffset_Correctly()
        {
            var value = DateTimeOffset.Parse("2016-10-19T16:36:55.569Z");
            var expected = 1476895015569000L;
            var actual = value.ToUnixTimeMicroseconds();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_FromUnixTimeMicroseconds_Converts_To_DateTimeOffset_Correctly()
        {
            var value = 1476895015569000L;
            var expected = DateTimeOffset.Parse("2016-10-19T16:36:55.569Z");
            var actual = DateTimeExtensions.FromUnixTimeMicroseconds(value);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_FromUnixTimeMicroseconds_Converts_To_DateTimeOffset_Correctly_With_Timespan()
        {
            var value = 1476895015569000L;
            var expected = DateTimeOffset.Parse("2016-10-19T16:36:55.569+06:00");
            var timeSpan = new TimeSpan(6, 0, 0);
            var actual = DateTimeExtensions.FromUnixTimeMicroseconds(value, timeSpan);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_ToUnixTimeMicroseconds_Converts_DateTimeOffset_To_Long_Correctly()
        {
            var expected = 1451606400001000L;
            var timeSpan = new TimeSpan(6, 0, 0);

            DateTimeOffset? dto = null;
            var actual = dto.ToUnixTimeMicroseconds();

            Assert.IsNull(actual);

            dto = new DateTimeOffset(2016, 1, 1, 6, 0, 0, 1, timeSpan);
            actual = dto.ToUnixTimeMicroseconds();

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_ToUnixTimeMicroseconds_Converts_DateTime_To_Long_Correctly()
        {
            var expected = 1451606400001000L;
            DateTime? dt = null;
            var actual = dt.ToUnixTimeMicroseconds();
            Assert.IsNull(actual);

            dt = new DateTime(2016, 1, 1, 0, 0, 0, 1);
            dt = dt.Value.AddHours(TimeZoneInfo.Local.GetUtcOffset(dt.Value).Hours);
            actual = dt.ToUnixTimeMicroseconds();

            Assert.IsNotNull(actual);
            Assert.IsTrue(actual.HasValue);
            Assert.AreEqual(expected,actual);
        }

        [TestMethod]
        public void DateTimeExtensions_ToOffsetTime_Applies_Timezone_To_DateTimeOffset_Correctly()
        {
            // Null timespan and datetime without offset
            var timeSpan = new TimeSpan?();
            var dto = new DateTimeOffset(new DateTime(2016, 1, 1, 0, 0, 0, 1));
            var actual = dto.ToOffsetTime(timeSpan);

            Assert.AreEqual(dto, actual);

            // Matching -06:00 Timespan
            timeSpan = new TimeSpan(6, 0, 0);
            dto = new DateTimeOffset(2016, 1, 1, 0, 0, 0, 1, timeSpan.Value);
            actual = dto.ToOffsetTime(timeSpan);

            Assert.AreEqual(dto, actual);

            // Different timespan values
            timeSpan = new TimeSpan(2, 0, 0);
            dto = new DateTimeOffset(2016, 1, 1, 0, 0, 0, 1, timeSpan.Value);

            timeSpan = new TimeSpan(4, 0, 0);
            actual = dto.ToOffsetTime(timeSpan);
            dto = new DateTimeOffset(2016, 1, 1, 0, 0, 0, 1, timeSpan.Value);

            Assert.AreEqual(dto, actual);
        }
    }
}
