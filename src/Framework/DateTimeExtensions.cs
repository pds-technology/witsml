//----------------------------------------------------------------------- 
// PDS.Framework, 2016.1
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

using System;

namespace PDS.Framework
{
    /// <summary>
    /// Provides static helper methods that can be used when working with the
    /// <see cref="DateTime"/> and <see cref="DateTimeOffset"/> types.
    /// </summary>
    public static class DateTimeExtensions
    {
        private const long EpochTimeInMicroseconds = 62135596800000000L;
        private const long MicroToNanoFactor = 10L;

        public static DateTimeOffset FromUnixTimeMicroseconds(long microseconds, TimeSpan? offset = null)
        {
            return new DateTimeOffset(ToUnixTicks(microseconds), offset ?? TimeSpan.Zero);
        }

        public static long ToUnixTimeMicroseconds(this DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.UtcTicks / MicroToNanoFactor;
        }

        public static long? ToUnixTimeMicroseconds(this DateTimeOffset? dateTimeOffset)
        {
            return dateTimeOffset?.ToUnixTimeMicroseconds();
        }

        public static long ToUnixTimeMicroseconds(this DateTime dateTime)
        {
            return DateTimeOffset.Parse(dateTime.ToString("o")).ToUnixTimeMicroseconds();
        }

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

        private static long ToUnixTicks(long microseconds)
        {
            return microseconds * MicroToNanoFactor;
        }
    }
}
