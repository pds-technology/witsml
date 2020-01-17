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
using Energistics.DataAccess;
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

        [TestMethod]
        public void DateTimeExtensions_ToDisplayDateTime_Converts_UTC_Display()
        {
            // from +06:00 time offset
            var fromTime = new Timestamp(new DateTimeOffset(2016, 10, 19, 16, 36, 55, new TimeSpan(6, 0, 0))) as Timestamp?;

            // To UTC (Offset should be 00:00)
            var toOffset = new TimeSpan(0, 0, 0);
            var expected = "2016-10-19 10:36:55.000";

            var actual = fromTime.ToDisplayDateTime(toOffset);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTime_TruncateToMilliseconds_Removes_Unwanted_Resolution_In_UTC()
        {
            var kind = DateTimeKind.Utc;
            var test = new DateTime(new DateTime(2020, 01, 01, 12, 31, 19, 320, kind).Ticks + TimeSpan.TicksPerMillisecond / 4, kind);
            var expected = new DateTime(test.Year, test.Month, test.Day, test.Hour, test.Minute, test.Second, test.Millisecond, test.Kind);

            var actual = test.TruncateToMilliseconds();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Kind, actual.Kind);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTime_TruncateToMilliseconds_Removes_Unwanted_Resolution_In_LocalTime()
        {
            var kind = DateTimeKind.Local;
            var test = new DateTime(new DateTime(2020, 01, 01, 12, 31, 19, 320, kind).Ticks + TimeSpan.TicksPerMillisecond / 4, kind);
            var expected = new DateTime(test.Year, test.Month, test.Day, test.Hour, test.Minute, test.Second, test.Millisecond, test.Kind);

            var actual = test.TruncateToMilliseconds();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Kind, actual.Kind);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTime_TruncateToSeconds_Removes_Unwanted_Resolution_In_UTC()
        {
            var kind = DateTimeKind.Utc;
            var test = new DateTime(new DateTime(2020, 01, 01, 12, 31, 19, 320, kind).Ticks + TimeSpan.TicksPerMillisecond / 4, kind);
            var expected = new DateTime(test.Year, test.Month, test.Day, test.Hour, test.Minute, test.Second, test.Kind);

            var actual = test.TruncateToSeconds();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Kind, actual.Kind);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTime_TruncateToSeconds_Removes_Unwanted_Resolution_In_LocalTime()
        {
            var kind = DateTimeKind.Local;
            var test = new DateTime(new DateTime(2020, 01, 01, 12, 31, 19, 320, kind).Ticks + TimeSpan.TicksPerMillisecond / 4, kind);
            var expected = new DateTime(test.Year, test.Month, test.Day, test.Hour, test.Minute, test.Second, test.Kind);

            var actual = test.TruncateToSeconds();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Kind, actual.Kind);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTime_TruncateToMinutes_Removes_Unwanted_Resolution_In_UTC()
        {
            var kind = DateTimeKind.Utc;
            var test = new DateTime(new DateTime(2020, 01, 01, 12, 31, 19, 320, kind).Ticks + TimeSpan.TicksPerMillisecond / 4, kind);
            var expected = new DateTime(test.Year, test.Month, test.Day, test.Hour, test.Minute, 0, test.Kind);

            var actual = test.TruncateToMinutes();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Kind, actual.Kind);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTime_TruncateToMinutes_Removes_Unwanted_Resolution_In_LocalTime()
        {
            var kind = DateTimeKind.Local;
            var test = new DateTime(new DateTime(2020, 01, 01, 12, 31, 19, 320, kind).Ticks + TimeSpan.TicksPerMillisecond / 4, kind);
            var expected = new DateTime(test.Year, test.Month, test.Day, test.Hour, test.Minute, 0, test.Kind);

            var actual = test.TruncateToMinutes();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Kind, actual.Kind);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTime_TruncateToHours_Removes_Unwanted_Resolution_In_UTC()
        {
            var kind = DateTimeKind.Utc;
            var test = new DateTime(new DateTime(2020, 01, 01, 12, 31, 19, 320, kind).Ticks + TimeSpan.TicksPerMillisecond / 4, kind);
            var expected = new DateTime(test.Year, test.Month, test.Day, test.Hour, 0, 0, test.Kind);

            var actual = test.TruncateToHours();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Kind, actual.Kind);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTime_TruncateToHours_Removes_Unwanted_Resolution_In_LocalTime()
        {
            var kind = DateTimeKind.Local;
            var test = new DateTime(new DateTime(2020, 01, 01, 12, 31, 19, 320, kind).Ticks + TimeSpan.TicksPerMillisecond / 4, kind);
            var expected = new DateTime(test.Year, test.Month, test.Day, test.Hour, 0, 0, test.Kind);

            var actual = test.TruncateToHours();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Kind, actual.Kind);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTimeOffset_TruncateToMilliseconds_Removes_Unwanted_Resolution_In_UTC()
        {
            var offset = TimeSpan.Zero;
            var test = new DateTimeOffset(new DateTimeOffset(2020, 01, 01, 12, 31, 19, 320, offset).Ticks + TimeSpan.TicksPerMillisecond / 4, offset);
            var expected = new DateTimeOffset(test.Year, test.Month, test.Day, test.Hour, test.Minute, test.Second, test.Millisecond, test.Offset);

            var actual = test.TruncateToMilliseconds();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Offset, actual.Offset);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTimeOffset_TruncateToMilliseconds_Removes_Unwanted_Resolution_In_LocalTime()
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            var test = new DateTimeOffset(new DateTimeOffset(2020, 01, 01, 12, 31, 19, 320, offset).Ticks + TimeSpan.TicksPerMillisecond / 4, offset);
            var expected = new DateTimeOffset(test.Year, test.Month, test.Day, test.Hour, test.Minute, test.Second, test.Millisecond, test.Offset);

            var actual = test.TruncateToMilliseconds();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Offset, actual.Offset);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTimeOffset_TruncateToSeconds_Removes_Unwanted_Resolution_In_UTC()
        {
            var offset = TimeSpan.Zero;
            var test = new DateTimeOffset(new DateTimeOffset(2020, 01, 01, 12, 31, 19, 320, offset).Ticks + TimeSpan.TicksPerMillisecond / 4, offset);
            var expected = new DateTimeOffset(test.Year, test.Month, test.Day, test.Hour, test.Minute, test.Second, test.Offset);

            var actual = test.TruncateToSeconds();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Offset, actual.Offset);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTimeOffset_TruncateToSeconds_Removes_Unwanted_Resolution_In_LocalTime()
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            var test = new DateTimeOffset(new DateTimeOffset(2020, 01, 01, 12, 31, 19, 320, offset).Ticks + TimeSpan.TicksPerMillisecond / 4, offset);
            var expected = new DateTimeOffset(test.Year, test.Month, test.Day, test.Hour, test.Minute, test.Second, test.Offset);

            var actual = test.TruncateToSeconds();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Offset, actual.Offset);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTimeOffset_TruncateToMinutes_Removes_Unwanted_Resolution_In_UTC()
        {
            var offset = TimeSpan.Zero;
            var test = new DateTimeOffset(new DateTimeOffset(2020, 01, 01, 12, 31, 19, 320, offset).Ticks + TimeSpan.TicksPerMillisecond / 4, offset);
            var expected = new DateTimeOffset(test.Year, test.Month, test.Day, test.Hour, test.Minute, 0, test.Offset);

            var actual = test.TruncateToMinutes();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Offset, actual.Offset);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTimeOffset_TruncateToMinutes_Removes_Unwanted_Resolution_In_LocalTime()
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            var test = new DateTimeOffset(new DateTimeOffset(2020, 01, 01, 12, 31, 19, 320, offset).Ticks + TimeSpan.TicksPerMillisecond / 4, offset);
            var expected = new DateTimeOffset(test.Year, test.Month, test.Day, test.Hour, test.Minute, 0, test.Offset);

            var actual = test.TruncateToMinutes();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Offset, actual.Offset);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTimeOffset_TruncateToHours_Removes_Unwanted_Resolution_In_UTC()
        {
            var offset = TimeSpan.Zero;
            var test = new DateTimeOffset(new DateTimeOffset(2020, 01, 01, 12, 31, 19, 320, offset).Ticks + TimeSpan.TicksPerMillisecond / 4, offset);
            var expected = new DateTimeOffset(test.Year, test.Month, test.Day, test.Hour, 0, 0, test.Offset);

            var actual = test.TruncateToHours();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Offset, actual.Offset);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTimeOffset_TruncateToHours_Removes_Unwanted_Resolution_In_LocalTime()
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            var test = new DateTimeOffset(new DateTimeOffset(2020, 01, 01, 12, 31, 19, 320, offset).Ticks + TimeSpan.TicksPerMillisecond / 4, offset);
            var expected = new DateTimeOffset(test.Year, test.Month, test.Day, test.Hour, 0, 0, test.Offset);

            var actual = test.TruncateToHours();

            Assert.AreNotEqual(test, actual);
            Assert.AreEqual(expected.Offset, actual.Offset);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_DateTime_AddSecondsFullPrecision_Preserves_Original_Precision_In_Utc()
        {
            var kind = DateTimeKind.Utc;
            var original = new DateTime(new DateTime(2020, 01, 01, 12, 31, 19, 320, kind).Ticks + TimeSpan.TicksPerMillisecond / 4, kind);
            var modified = original.AddSeconds(1.0);

            var expected = TimeSpan.FromSeconds(1.0);
            var actual = modified - original;

            Assert.AreEqual(expected, actual);
        }
    }
}
