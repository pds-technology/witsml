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
using System.Collections.Generic;
using System.Linq;
using PDS.Framework;

namespace PDS.Witsml
{
    /// <summary>
    /// Provides helper methods for common range operations.
    /// </summary>
    public static class Range
    {
        /// <summary>
        /// The empty range
        /// </summary>
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
        /// Determines whether a range starts before the specified value.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <param name="value">The value.</param>
        /// <param name="increasing">if set to <c>true</c> the range is increasing.</param>
        /// <param name="inclusive">if set to <c>true</c> the comparison should include value, false otherwise.</param>
        /// <returns><c>true</c> if the range starts before the specified value; otherwise, <c>false</c>.</returns>
        public static bool StartsBefore(this Range<double?> range, double value, bool increasing = true, bool inclusive = false)
        {
            if (!range.Start.HasValue)
                return false;

            return increasing
                ? (inclusive ? value >= range.Start.Value : value > range.Start.Value)
                : (inclusive ? value <= range.Start.Value : value < range.Start.Value);
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
        /// Determines whether a range ends after the specified value.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <param name="value">The value.</param>
        /// <param name="increasing">if set to <c>true</c> the range is increasing.</param>
        /// <param name="inclusive">if set to <c>true</c> the comparison should include value, false otherwise.</param>
        /// <returns><c>true</c> if the range ends after the specified value; otherwise, <c>false</c>.</returns>
        public static bool EndsAfter(this Range<double?> range, double value, bool increasing = true, bool inclusive = false)
        {
            if (!range.End.HasValue)
                return false;

            return increasing
                ? (inclusive ? value <= range.End.Value : value < range.End.Value)
                : (inclusive ? value >= range.End.Value : value > range.End.Value);
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

        /// <summary>
        /// Gets the minimum range start from a list of ranges.
        /// </summary>
        /// <param name="ranges">The list of ranges.</param>
        /// <param name="increasing">if set to <c>true</c> if ranges are from increasing data, otherwise false.</param>
        /// <returns>Min start range.</returns>
        public static double? GetMinRangeStart(this List<Range<double?>> ranges, bool increasing)
        {
            return increasing ? ranges.Min(r => r.Start) : ranges.Max(r => r.Start);
        }

        /// <summary>
        /// Gets the optimize range start from a list of ranges.
        /// This is the same as the min range end or max range start.
        /// </summary>
        /// <param name="ranges">The list of ranges.</param>
        /// <param name="increasing">if set to <c>true</c> if ranges are from increasing data, otherwise false.</param>
        /// <returns>The optimized range start.</returns>
        public static double? GetOptimizeRangeStart(this List<Range<double?>> ranges, bool increasing)
        {
            return increasing ? ranges.Min(r => r.End) : ranges.Max(r => r.End);
        }

        /// <summary>
        /// Gets the maximum range end from a list of ranges.
        /// </summary>
        /// <param name="ranges">The list of ranges.</param>
        /// <param name="increasing">if set to <c>true</c> if ranges are from increasing data, otherwise false.</param>
        /// <returns>The maximum end range.</returns>
        public static double? GetMaxRangeEnd(this List<Range<double?>> ranges, bool increasing)
        {
            return increasing ? ranges.Max(r => r.End) : ranges.Min(r => r.End);
        }

        /// <summary>
        /// Optimizes the latest values range.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <param name="requestLatestValues">The number of request latest values.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> range is a time index, othewise depth.</param>
        /// <param name="increasing">if set to <c>true</c> index is increasing, otherwise decreasing.</param>
        /// <param name="rangeStart">The range start.</param>
        /// <param name="optimizeRangeStart">The optimize range start.</param>
        /// <param name="rangeEnd">The range end.</param>
        /// <param name="requestFactor">The request factor.</param>
        /// <param name="rangeStepSize">Size of the range step.</param>
        /// <returns>The optimized range</returns>
        public static Range<double?> OptimizeLatestValuesRange(this Range<double?> range, int? requestLatestValues, bool isTimeIndex, bool increasing, double? rangeStart, double? optimizeRangeStart, double? rangeEnd, int requestFactor, long rangeStepSize)
        {
            if (requestLatestValues.HasValue && optimizeRangeStart.HasValue)
            {
                var optimizationEstimate =
                    (requestFactor * (requestLatestValues.Value + 1) * rangeStepSize);

                range = increasing
                    ? new Range<double?>(optimizeRangeStart.Value - optimizationEstimate, rangeEnd)
                    : new Range<double?>(optimizeRangeStart.Value + optimizationEstimate, rangeEnd);

                if (rangeStart.HasValue && range.StartsBefore(rangeStart.Value, increasing))
                {
                    range = new Range<double?>(rangeStart, rangeEnd);
                }
            }

            return range;
        }
    }
}
