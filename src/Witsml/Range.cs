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

using System;
using PDS.Framework;

namespace PDS.Witsml
{
    /// <summary>
    /// Provides helper methods for common range operations.
    /// </summary>
    public static class Range
    {
        public static readonly Range<double?> Empty = new Range<double?>(null, null);

        /// <summary>
        /// Parses the specified start and end range values.
        /// </summary>
        /// <param name="start">The range start value.</param>
        /// <param name="end">The range end value.</param>
        /// <param name="isTime">if set to <c>true</c> the range values are date/time.</param>
        /// <returns></returns>
        public static Range<double?> Parse(object start, object end, bool isTime)
        {
            double? rangeStart = null, rangeEnd = null;
            TimeSpan? offset = null;

            if (isTime)
            {
                DateTimeOffset time;

                if (start != null && DateTimeOffset.TryParse(start.ToString(), out time))
                {
                    rangeStart = time.ToUnixTimeMicroseconds();
                    offset = time.Offset;
                }
                    

                if (end != null && DateTimeOffset.TryParse(end.ToString(), out time))
                {
                    rangeEnd = time.ToUnixTimeMicroseconds();
                    offset = time.Offset;
                }                  
            }
            else
            {
                double depth;

                if (start != null && double.TryParse(start.ToString(), out depth))
                    rangeStart = depth;

                if (end != null && double.TryParse(end.ToString(), out depth))
                    rangeEnd = depth;
            }

            return new Range<double?>(rangeStart, rangeEnd, offset);
        }

        /// <summary>
        /// Sorts the specified range in numeric order.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <param name="increasing">if set to <c>true</c>, index values are increasing.</param>
        /// <returns>A range sorted based on the specified increasing flag.</returns>
        public static Range<double?> Sort(this Range<double?> range, bool increasing = true)
        {
            if (range.Start.HasValue && range.End.HasValue)
            {
                if ((increasing && range.Start.Value > range.End.Value) ||
                    (!increasing && range.Start.Value < range.End.Value))
                {
                    range = new Range<double?>(range.End, range.Start);
                }
            }

            return range;
        }

        /// <summary>
        /// Determines whether a range starts after the specified value.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <param name="value">The value.</param>
        /// <param name="increasing">if set to <c>true</c> the range is increasing.</param>
        /// <param name="inclusive">if set to <c>true</c> the comparison should include value, false otherwise.</param>
        /// <returns><c>true</c> if the range starts after the specified value; otherwise, <c>false</c>.</returns>
        public static bool StartsAfter(this Range<double?> range, double value, bool increasing = true, bool inclusive = false)
        {
            if (!range.Start.HasValue)
                return false;

            return increasing
                ? (inclusive ? value <= range.Start.Value : value < range.Start.Value)
                : (inclusive ? value >= range.Start.Value : value > range.Start.Value);
        }

        /// <summary>
        /// Determines whether a range ends before the specified value.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <param name="value">The value.</param>
        /// <param name="increasing">if set to <c>true</c> the range is increasing.</param>
        /// <param name="inclusive">if set to <c>true</c> the comparison should include value, false otherwise.</param>
        /// <returns><c>true</c> if the range ends before the specified value; otherwise, <c>false</c>.</returns>
        public static bool EndsBefore(this Range<double?> range, double value, bool increasing = true, bool inclusive = false)
        {
            if (!range.End.HasValue)
                return false;

            return increasing
                ? (inclusive ? value >= range.End.Value : value > range.End.Value)
                : (inclusive ? value <= range.End.Value : value < range.End.Value);
        }

        /// <summary>
        /// Determines whether a range contains the specified value.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <param name="value">The value.</param>
        /// <param name="increasing">if set to <c>true</c> the range is increasing.</param>
        /// <returns><c>true</c> if the range contains the specified value; otherwise, <c>false</c>.</returns>
        public static bool Contains(this Range<double?> range, double value, bool increasing = true)
        {
            if (!range.Start.HasValue || !range.End.HasValue)
                return false;

            return increasing
                ? (value >= range.Start.Value && value <= range.End.Value)
                : (value <= range.Start.Value && value >= range.End.Value);
        }

        /// <summary>
        /// Computes the range of a data chunk that contains the given index.
        /// </summary>
        /// <param name="index">The index contained within the computed range.</param>
        /// <param name="rangeSize">The range size of one chunk.</param>
        /// <param name="increasing">if set to <c>true</c> the index is increasing.</param>
        /// <returns>The range.</returns>
        public static Range<long> ComputeRange(double index, long rangeSize, bool increasing = true)
        {
            var rangeIndex = increasing ? (long)Math.Floor(index / rangeSize) : (long)Math.Ceiling(index / rangeSize);
            return new Range<long>(rangeIndex * rangeSize, rangeIndex * rangeSize + (increasing ? rangeSize : -rangeSize));
        }
    }
}
