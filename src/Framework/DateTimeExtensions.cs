//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2018.3
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

namespace PDS.WITSMLstudio.Framework
{
    /// <summary>
    /// Provides static helper methods that can be used when working with the
    /// <see cref="DateTime"/> and <see cref="DateTimeOffset"/> types.
    /// </summary>
    public static class DateTimeExtensions
    {
        private static readonly DateTimeOffset _epochTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private const long TicksToMicroSeconds = 10L;

        /// <summary>
        /// Converts a Unix time expressed as the number of microseconds that have elapsed
        /// since 1970-01-01T00:00:00Z to a <see cref="System.DateTimeOffset"/> value.
        /// </summary>
        /// <param name="microseconds">The microseconds.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>A <see cref="System.DateTimeOffset"/> instance.</returns>
        public static DateTimeOffset FromUnixTimeMicroseconds(long microseconds, TimeSpan? offset = null)
        {
            return new DateTimeOffset(ToUnixTicks(microseconds), offset ?? TimeSpan.Zero);
        }

        /// <summary>
        /// Returns the number of microseconds that have elapsed since 1970-01-01T00:00:00.000Z.
        /// </summary>
        /// <param name="dateTimeOffset">The date time offset.</param>
        /// <returns>The number of microseconds that have elapsed since 1970-01-01T00:00:00.000Z.</returns>
        public static long ToUnixTimeMicroseconds(this DateTimeOffset dateTimeOffset)
        {
            return FromUnixTicks(dateTimeOffset.UtcTicks);
        }

        /// <summary>
        /// Returns the number of microseconds that have elapsed since 1970-01-01T00:00:00.000Z.
        /// </summary>
        /// <param name="dateTimeOffset">The date time offset.</param>
        /// <returns>The number of microseconds that have elapsed since 1970-01-01T00:00:00.000Z.</returns>
        public static long? ToUnixTimeMicroseconds(this DateTimeOffset? dateTimeOffset)
        {
            return dateTimeOffset?.ToUnixTimeMicroseconds();
        }

        /// <summary>
        /// Returns the number of microseconds that have elapsed since 1970-01-01T00:00:00.000Z.
        /// </summary>
        /// <param name="dateTime">The date time offset.</param>
        /// <returns>The number of microseconds that have elapsed since 1970-01-01T00:00:00.000Z.</returns>
        public static long ToUnixTimeMicroseconds(this DateTime dateTime)
        {
            return DateTimeOffset.Parse(dateTime.ToString("o")).ToUnixTimeMicroseconds();
        }

        /// <summary>
        /// Returns the number of microseconds that have elapsed since 1970-01-01T00:00:00.000Z.
        /// </summary>
        /// <param name="dateTime">The date time offset.</param>
        /// <returns>The number of microseconds that have elapsed since 1970-01-01T00:00:00.000Z.</returns>
        public static long? ToUnixTimeMicroseconds(this DateTime? dateTime)
        {
            return dateTime?.ToUnixTimeMicroseconds();
        }

        /// <summary>
        /// Applies a time zone offset to the current <see cref="DateTimeOffset"/> instance.
        /// </summary>
        /// <param name="value">The date time value.</param>
        /// <param name="offset">The offset time span.</param>
        /// <returns>A <see cref="DateTimeOffset"/> instance, or null.</returns>
        public static DateTimeOffset ToOffsetTime(this DateTimeOffset value, TimeSpan? offset)
        {
            if (!offset.HasValue)
                return value;

            if (value.Offset.CompareTo(offset) == 0)
                return value;

            return FromUnixTimeMicroseconds(value.ToUnixTimeMicroseconds(), value.Offset).ToOffset(offset.Value);
        }

        /// <summary>
        /// Converts microseconds to ticks.
        /// </summary>
        /// <param name="microseconds">The microseconds.</param>
        /// <returns>The value converted to ticks.</returns>
        private static long ToUnixTicks(long microseconds)
        {
            return microseconds * TicksToMicroSeconds + _epochTime.UtcTicks;
        }

        /// <summary>
        /// Converts ticks to microseconds.
        /// </summary>
        /// <param name="ticks">The ticks.</param>
        /// <returns>The value converted to microseconds.</returns>
        private static long FromUnixTicks(long ticks)
        {
            return (ticks - _epochTime.UtcTicks) / TicksToMicroSeconds;
        }

        /// <summary>
        /// Converts a TimeSpan to a WITSML TimeZone string.
        /// </summary>
        /// <param name="offset">The Date Time offset.</param>
        /// <returns></returns>
        public static string ToTimeZone(this TimeSpan offset)
        {
            return $"{offset.Hours:00}:{offset.Minutes:00}";
        }

        /// <summary>
        /// To convert a specified timestamp to a specified timespan offset and format 
        /// the date time for display as a string that includes fractional seconds without a timezone.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="toOffset">The time span offset to convert the timestamp to.</param>
        /// <returns>A string representation of the Timestamp if it is not null, otherwise an empty string.</returns>
        public static string ToDisplayDateTime(this Timestamp? timestamp, TimeSpan toOffset)
        {
            if (!timestamp.HasValue) return string.Empty;

            return timestamp.Value.ToDisplayDateTime(toOffset);
        }


        /// <summary>
        /// To convert a specified timestamp to a specified timespan offset and format 
        /// the date time for display as a string that includes fractional seconds without a timezone.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="toOffset">The time span offset to convert the timestamp to.</param>
        /// <returns>A string representation of the Timestamp if it is not null, otherwise an empty string.</returns>
        public static string ToDisplayDateTime(this Timestamp timestamp, TimeSpan toOffset)
        {
            // Convert the Timestamp to a DateTimeOffset
            var dateTimeOffset = (DateTimeOffset)timestamp;

            // Convert the DateTimeOffset to the timezone specified in the toOffset Timespan
            dateTimeOffset = dateTimeOffset.ToOffset(toOffset);

            // Return a display representation of the Timestamp that does not include the timezone
            return dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        /// <summary>
        /// Returns a new <see cref="DateTime"/> instance based on this one where the value is truncated to the hour.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> instance's to return a truncated version of.</param>
        /// <returns>A <see cref="DateTime"/> instance with the value truncated to the hour.</returns>
        public static DateTime TruncateToHours(this DateTime dateTime)
        {
            return dateTime.AddTicks(-dateTime.Ticks % TimeSpan.TicksPerHour);
        }

        /// <summary>
        /// Returns a new <see cref="DateTime"/> instance based on this one where the value is truncated to the minute.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> instance's to return a truncated version of.</param>
        /// <returns>A <see cref="DateTime"/> instance with the value truncated to the minute.</returns>
        public static DateTime TruncateToMinutes(this DateTime dateTime)
        {
            return dateTime.AddTicks(-dateTime.Ticks % TimeSpan.TicksPerMinute);
        }

        /// <summary>
        /// Returns a new <see cref="DateTime"/> instance based on this one where the value is truncated to the second.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> instance's to return a truncated version of.</param>
        /// <returns>A <see cref="DateTime"/> instance with the value truncated to the second.</returns>
        public static DateTime TruncateToSeconds(this DateTime dateTime)
        {
            return dateTime.AddTicks(-dateTime.Ticks % TimeSpan.TicksPerSecond);
        }

        /// <summary>
        /// Returns a new <see cref="DateTime"/> instance based on this one where the value is truncated to the millisecond.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> instance's to return a truncated version of.</param>
        /// <returns>A <see cref="DateTime"/> instance with the value truncated to the millisecond.</returns>
        public static DateTime TruncateToMilliseconds(this DateTime dateTime)
        {
            return dateTime.AddTicks(-dateTime.Ticks % TimeSpan.TicksPerMillisecond);
        }

        /// <summary>
        /// Returns a new <see cref="DateTimeOffset"/> instance based on this one where the value is truncated to the hour.
        /// </summary>
        /// <param name="DateTimeOffset">The <see cref="DateTimeOffset"/> instance's to return a truncated version of.</param>
        /// <returns>A <see cref="DateTimeOffset"/> instance with the value truncated to the hour.</returns>
        public static DateTimeOffset TruncateToHours(this DateTimeOffset DateTimeOffset)
        {
            return DateTimeOffset.AddTicks(-DateTimeOffset.Ticks % TimeSpan.TicksPerHour);
        }

        /// <summary>
        /// Returns a new <see cref="DateTimeOffset"/> instance based on this one where the value is truncated to the minute.
        /// </summary>
        /// <param name="DateTimeOffset">The <see cref="DateTimeOffset"/> instance's to return a truncated version of.</param>
        /// <returns>A <see cref="DateTimeOffset"/> instance with the value truncated to the minute.</returns>
        public static DateTimeOffset TruncateToMinutes(this DateTimeOffset DateTimeOffset)
        {
            return DateTimeOffset.AddTicks(-DateTimeOffset.Ticks % TimeSpan.TicksPerMinute);
        }

        /// <summary>
        /// Returns a new <see cref="DateTimeOffset"/> instance based on this one where the value is truncated to the second.
        /// </summary>
        /// <param name="DateTimeOffset">The <see cref="DateTimeOffset"/> instance's to return a truncated version of.</param>
        /// <returns>A <see cref="DateTimeOffset"/> instance with the value truncated to the second.</returns>
        public static DateTimeOffset TruncateToSeconds(this DateTimeOffset DateTimeOffset)
        {
            return DateTimeOffset.AddTicks(-DateTimeOffset.Ticks % TimeSpan.TicksPerSecond);
        }

        /// <summary>
        /// Returns a new <see cref="DateTimeOffset"/> instance based on this one where the value is truncated to the millisecond.
        /// </summary>
        /// <param name="DateTimeOffset">The <see cref="DateTimeOffset"/> instance's to return a truncated version of.</param>
        /// <returns>A <see cref="DateTimeOffset"/> instance with the value truncated to the millisecond.</returns>
        public static DateTimeOffset TruncateToMilliseconds(this DateTimeOffset DateTimeOffset)
        {
            return DateTimeOffset.AddTicks(-DateTimeOffset.Ticks % TimeSpan.TicksPerMillisecond);
        }
    }
}
