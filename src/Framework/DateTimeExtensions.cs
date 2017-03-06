//----------------------------------------------------------------------- 
// PDS.Framework, 2017.1
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

namespace PDS.Framework
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
    }
}
