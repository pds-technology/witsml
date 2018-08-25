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
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Range tests.
    /// </summary>
    [TestClass]
    public class RangeTests
    {
        private static readonly Random _random = new Random(123);
        private Range<double?> _rangeDecreasing;
        private Range<double?> _rangeIncreasing;

        [TestInitialize]
        public void TestSetUp()
        {
            var doubleValue = double.Parse((_random.NextDouble() * 100).ToString(CultureInfo.InvariantCulture));
            _rangeDecreasing = new Range<double?>(double.Parse((doubleValue * 5).ToString(CultureInfo.InvariantCulture)), doubleValue);
            _rangeIncreasing = new Range<double?>(doubleValue, double.Parse((doubleValue * 5).ToString(CultureInfo.InvariantCulture)));
        }

        [TestMethod]
        public void Range_Parse_Returns_Range_For_Empty_Start_Or_End_With_Time()
        {
            var result = Range.Parse(null, null, true);
            Assert.AreEqual(Range.Empty, result);

            var dateTimeOffset = new DateTimeOffset(2016, 12, 1, 0, 0, 0, new TimeSpan());
            var resultEmptyEnd = Range.Parse(dateTimeOffset, null, true);
            Assert.AreEqual(new Range<double?>(dateTimeOffset.ToUnixTimeMicroseconds(), null, dateTimeOffset.Offset), resultEmptyEnd);

            var resultEmptyStart = Range.Parse(null, dateTimeOffset, true);
            Assert.AreEqual(new Range<double?>(null, dateTimeOffset.ToUnixTimeMicroseconds(), dateTimeOffset.Offset), resultEmptyStart);
        }

        [TestMethod]
        public void Range_Parse_Returns_Range_For_Empty_Start_Or_End_With_Depth()
        {
            var result = Range.Parse(null, null, false);
            Assert.AreEqual(Range.Empty, result);

            var depth = _rangeDecreasing.Start;
            var resultEmptyEnd = Range.Parse(depth, null, false);
            Assert.AreEqual(new Range<double?>(depth, null), resultEmptyEnd);

            var resultEmptyStart = Range.Parse(null, depth, false);
            Assert.AreEqual(new Range<double?>(null, depth), resultEmptyStart);
        }

        [TestMethod]
        public void Range_Parse_Returns_Empty_Range_For_Invalid_DateTime()
        {
            var result = Range.Parse(string.Empty, string.Empty, true);
            Assert.AreEqual(Range.Empty, result);
        }

        [TestMethod]
        public void Range_Parse_Returns_Empty_Range_For_Invalid_Depth()
        {
            var invalidDepth = DateTime.Now;
            var result = Range.Parse(invalidDepth, invalidDepth, false);
            Assert.AreEqual(Range.Empty, result);
        }

        [TestMethod]
        public void Range_ParseReturns_Range_For_Valid_DateTime()
        {
            var start = new DateTimeOffset(2016, 12, 1, 12, 5, 17, 4, new TimeSpan());
            var end = new DateTimeOffset(2016, 12, 2, 17, 2, 36, 9, new TimeSpan());
            var result = Range.Parse(start, end, true);
            Assert.AreEqual(new Range<double?>(start.ToUnixTimeMicroseconds(), end.ToUnixTimeMicroseconds(), new TimeSpan()), result);
        }

        [TestMethod]
        public void Range_Parse_Returns_Range_For_Valid_Depth()
        {
            var start = _rangeDecreasing.Start;
            var end = _rangeDecreasing.End;
            var result = Range.Parse(start, end, false);
            Assert.AreEqual(new Range<double?>(start, end), result);
        }

        [TestMethod]
        public void Range_Sort_Returns_Empty_Range_On_Empty_Range_Sort()
        {
            var result = Range.Empty.Sort();
            Assert.AreEqual(Range.Empty, result);
        }

        [TestMethod]
        public void Range_Sort_Returns_Sorted_Range_For_Increasing_Range()
        {
            var start = _rangeDecreasing.Start;
            var end = _rangeDecreasing.End;
            Assert.AreEqual(new Range<double?>(end, start), new Range<double?>(start, end).Sort());
        }

        [TestMethod]
        public void Range_Sort_Returns_Sorted_Range_For_Decreasing_Range()
        {
            var start = _rangeIncreasing.Start;
            var end = _rangeIncreasing.End;
            Assert.AreEqual(new Range<double?>(end, start), new Range<double?>(end, start).Sort(false));
        }

        [TestMethod]
        public void Range_Sort_Presorted_Returns_Original_Increasing_Range()
        {
            var start = _rangeIncreasing.Start;
            var end = _rangeIncreasing.End;
            Assert.AreEqual(new Range<double?>(start, end), new Range<double?>(start, end).Sort());
        }

        [TestMethod]
        public void Range_Sort_Presorted_Returns_Original_Decreasing_Range()
        {
            var start = _rangeDecreasing.Start;
            var end = _rangeDecreasing.End;
            Assert.AreEqual(new Range<double?>(start, end), new Range<double?>(start, end).Sort(false));
        }

        [TestMethod]
        public void Range_StartsAfter_Returns_False_For_Empty_Start_Range()
        {
            var end = _rangeDecreasing.End;
            Assert.IsFalse(new Range<double?>(null, end).StartsAfter(end.Value/2, true, true));
        }

        [TestMethod]
        public void Range_StartsAfter_Returns_True_If_Increasing_Range_Starts_After_Value()
        {
            var start = _rangeIncreasing.Start;
            var end = _rangeIncreasing.End;
            Assert.IsTrue(new Range<double?>(start, end).StartsAfter(start.Value, true, true), "Inclusive comparison");        
            Assert.IsTrue(new Range<double?>(start, end).StartsAfter(start.Value / 2), "Not inclusive comparison");
        }

        [TestMethod]
        public void Range_StartsAfter_Returns_True_If_Decreasing_Range_Starts_After_Value()
        {
            var start = _rangeDecreasing.Start;
            var end = _rangeDecreasing.End;
            Assert.IsTrue(new Range<double?>(start, end).StartsAfter(start.Value, false, true), "Inclusive comparison");
            Assert.IsTrue(new Range<double?>(start, end).StartsAfter(start.Value + start.Value / 2, false), "Not inclusive comparison");
        }

        [TestMethod]
        public void Range_StartsBefore_Returns_False_For_Empty_Range_Start()
        {
            var end = _rangeDecreasing.End;
            Assert.IsFalse(new Range<double?>(null, end).StartsBefore(end.Value / 2, true, true));
        }

        [TestMethod]
        public void Range_StartsBefore_Returns_True_If_Increasing_Range_Starts_Before_Value()
        {
            var start = _rangeIncreasing.Start;
            var end = _rangeIncreasing.End;
            Assert.IsTrue(new Range<double?>(start, end).StartsBefore(start.Value, true, true), "Inclusive comparison");       
            Assert.IsTrue(new Range<double?>(start, end).StartsBefore(start.Value + start.Value / 2), "Not inclusive comparison");
        }

        [TestMethod]
        public void Range_StartsBefore_Returns_True_If_Decreasing_Range_Starts_Before_Value()
        {
            var start = _rangeDecreasing.Start;
            var end = _rangeDecreasing.End;
            Assert.IsTrue(new Range<double?>(start, end).StartsBefore(start.Value, false, true), "Inclusive comparison");        
            Assert.IsTrue(new Range<double?>(start, end).StartsBefore(start.Value - start.Value/2, false), "Not inclusive comparison");
        }

        [TestMethod]
        public void Range_EndsBefore_Returns_False_For_Empty_Range_End()
        {
            var start = _rangeDecreasing.Start;
            Assert.IsFalse(new Range<double?>(start, null).EndsBefore(start.Value / 2, true, true));
        }

        [TestMethod]
        public void Range_EndsBefore_Returns_True_If_Increasing_Range_Ends_Before_Value()
        {
            var start = _rangeIncreasing.Start;
            var end = _rangeIncreasing.End;
            Assert.IsTrue(new Range<double?>(start, end).EndsBefore(end.Value, true, true), "Inclusive comparison");        
            Assert.IsTrue(new Range<double?>(start, end).EndsBefore(end.Value + end.Value/ 2), "Not inclusive comparison");
        }

        [TestMethod]
        public void Range_EndsBefore_Returns_True_If_Decreasing_Range_Ends_Before_Value()
        {
            var start = _rangeDecreasing.Start;
            var end = _rangeDecreasing.End;
            Assert.IsTrue(new Range<double?>(start, end).EndsBefore(end.Value, false, true), "Inclusive comparison");        
            Assert.IsTrue(new Range<double?>(start, end).EndsBefore(end.Value - end.Value / 2, false), "Not inclusive comparison");
        }

        [TestMethod]
        public void Range_EndsAfter_Empty_Range_End()
        {
            var start = _rangeDecreasing.Start;
            Assert.IsFalse(new Range<double?>(start, null).EndsAfter(start.Value / 2, true, true));
        }

        [TestMethod]
        public void Range_EndsAfter_Returns_True_If_Increasing_Range_Ends_After_Value()
        {
            var start = _rangeIncreasing.Start;
            var end = _rangeIncreasing.End;
            Assert.IsTrue(new Range<double?>(start, end).EndsAfter(end.Value, true, true), "Inclusive comparison");        
            Assert.IsTrue(new Range<double?>(start, end).EndsAfter(end.Value - end.Value / 2), "Not inclusive comparison");
        }

        [TestMethod]
        public void Range_EndsAfter_Returns_True_If_Decreasing_Range_Ends_After_Value()
        {
            var start = _rangeDecreasing.Start;
            var end = _rangeDecreasing.End;
            Assert.IsTrue(new Range<double?>(start, end).EndsAfter(end.Value, false, true), "Inclusive comparison");        
            Assert.IsTrue(new Range<double?>(start, end).EndsAfter(end.Value + end.Value / 2, false), "Not inclusive comparison");
        }

        [TestMethod]
        public void Range_Contains_Returns_False_For_Empty_Range()
        {
            Assert.IsFalse(new Range<double?>(null, null).Contains(_rangeDecreasing.Start.Value));
        }

        [TestMethod]
        public void Range_Contains_Returns_True_If_Increasing_Range_Contains_Value()
        {
            var start = _rangeIncreasing.Start;
            var end = _rangeIncreasing.End;
            Assert.IsTrue(new Range<double?>(start, end).Contains(start.Value + (end.Value - start.Value)/2));
            Assert.IsTrue(new Range<double?>(start, end).Contains(end.Value - (end.Value - start.Value) / 2));
            Assert.IsTrue(new Range<double?>(start, end).Contains(start.Value));
            Assert.IsTrue(new Range<double?>(start, end).Contains(end.Value));
            Assert.IsTrue(new Range<double?>(start, start).Contains(start.Value));
            Assert.IsFalse(new Range<double?>(start, end).Contains(end.Value * 2));
            Assert.IsFalse(new Range<double?>(start, end).Contains(start.Value/2));
        }
        
        [TestMethod]
        public void Range_Contains_Returns_True_If_Decreasing_Range_Contains_Value()
        {
            var start = _rangeDecreasing.Start;
            var end = _rangeDecreasing.End;
            Assert.IsTrue(new Range<double?>(start, end).Contains(end.Value + (start.Value - end.Value)/2, false));
            Assert.IsTrue(new Range<double?>(start, end).Contains(start.Value, false));
            Assert.IsTrue(new Range<double?>(start, end).Contains(end.Value, false));
            Assert.IsTrue(new Range<double?>(start, start).Contains(start.Value, false));
            Assert.IsFalse(new Range<double?>(start, end).Contains(start.Value * 2));
            Assert.IsFalse(new Range<double?>(start, end).Contains(end.Value/2));
        }
        
        [TestMethod]
        public void Range_ComputeRange_Returns_Increasing_Range_For_Index()
        {
            var result = Range.ComputeRange(10000, 5000);
            var expectedRange = new Range<long>(10000, 15000);

            Assert.AreEqual(expectedRange, result);
        }

        [TestMethod]
        public void Range_ComputeRange_Returns_Decreasing_Range_For_Index()
        {
            var result = Range.ComputeRange(10000, 5000, false);
            var expectedRange = new Range<long>(10000, 5000);

            Assert.AreEqual(expectedRange, result);
        }
        
        [TestMethod]
        public void Range_GetMinRangeStart_Returns_Min_Start_Range_For_Increasing_Ranges()
        {
            var ranges = GetRanges(true);
            var result = ranges.GetMinRangeStart(true);
            var expectedMinRange = 200;

            Assert.AreEqual(expectedMinRange, result);
        }

        [TestMethod]
        public void Range_GetMinRangeStart_Returns_Min_Start_Range_For_Decreasing_Ranges()
        {
            var ranges = GetRanges(false);
            var result = ranges.GetMinRangeStart(false);
            var expectedMinRange = 5000;

            Assert.AreEqual(expectedMinRange, result);
        }

        [TestMethod]
        public void Range_GetOptimizeRangeStart_Returns_Optimized_Start_Range_For_Increasing_Ranges()
        {
            var ranges = GetRanges(true);
            var result = ranges.GetOptimizeRangeStart(true);
            var expectedMaxRange = 500;

            Assert.AreEqual(expectedMaxRange, result);
        }

        [TestMethod]
        public void Range_GetOptimizeRangeStart_Returns_Optimized_Start_Range_For_Decreasing_Ranges()
        {
            var ranges = GetRanges(false);
            var result = ranges.GetOptimizeRangeStart(false);
            var expectedMaxRange = 2000;

            Assert.AreEqual(expectedMaxRange, result);
        }

        [TestMethod]
        public void Range_GetMaxRangeEnd_Returns_Max_End_Range_For_Increasing_Ranges()
        {
            var ranges = GetRanges(true);
            var result = ranges.GetMaxRangeEnd(true);
            var expectedMaxRange = 5000;

            Assert.AreEqual(expectedMaxRange, result);
        }

        [TestMethod]
        public void Range_GetMaxRangeEnd_Returns_Max_End_Range_For_Decreasing_Ranges()
        {
            var ranges = GetRanges(false);
            var result = ranges.GetMaxRangeEnd(false);
            var expectedMaxRange = 200;

            Assert.AreEqual(expectedMaxRange, result);
        }

        [TestMethod]
        public void Range_OptimizeLatestValuesRange_Returns_Original_Range_For_Empty_Request_Latest_And_Optimize_Range_Start_Values()
        {
            var rangeSizeDepthIndex = 1;
            var requestFactor = 1;
            var range = _rangeIncreasing;
            var result = range.OptimizeLatestValuesRange(null, false, true, range.Start, null, range.End, requestFactor, rangeSizeDepthIndex);
            var expectedRange = range;

            Assert.AreEqual(expectedRange, result);        
        }

        [TestMethod]
        public void Range_OptimizeLatestValuesRange_Returns_Optimized_Range_For_Empty_Range_Start_Value()
        {
            var rangeSizeDepthIndex = 1;
            var requestFactor = 1;
            var range = _rangeIncreasing;
            var requestLatestValues = 10;

            var resultEmptyRangeStart = range.OptimizeLatestValuesRange(requestLatestValues, false, true, null, range.End, range.End, requestFactor, rangeSizeDepthIndex);
            var optimizationEstimate = requestFactor * (requestLatestValues + 1) * rangeSizeDepthIndex;
            var expectedEmptyRangeStart = new Range<double?>(range.End - optimizationEstimate, null);

            Assert.AreEqual(expectedEmptyRangeStart, resultEmptyRangeStart);
        }

        [TestMethod]
        public void Range_OptimizeLatestValuesRange_Returns_Optimized_Range_For_Increasing_DepthIndex()
        {
            var rangeSizeDepthIndex = 1;
            var requestFactor = 1;
            var requestLatestValues = 1;
            var range = _rangeIncreasing;

            var resultRequestValueOne = range.OptimizeLatestValuesRange(requestLatestValues, false, true, range.Start, range.Start, range.End, requestFactor, rangeSizeDepthIndex);
            var expectedRangeRequestValueOne = new Range<double?>(range.Start.Value, null);
            Assert.AreEqual(expectedRangeRequestValueOne, resultRequestValueOne);

            requestLatestValues = 10;
            var optimizationEstimate = requestFactor*(requestLatestValues + 1)*rangeSizeDepthIndex;
            var expectedRange = new Range<double?>(range.Start - optimizationEstimate, null);

            if (range.Start.Value > expectedRange.Start.Value)
                expectedRange = new Range<double?>(range.Start, range.End);

            var result = range.OptimizeLatestValuesRange(requestLatestValues, false, true, range.Start, range.Start, range.End, requestFactor, rangeSizeDepthIndex);
            Assert.AreEqual(expectedRange, result);            
        }

        [TestMethod]
        public void Range_OptimizeLatestValuesRange_Returns_Optimized_Range_For_Decreasing_DepthIndex()
        {
            var rangeSizeDepthIndex = 1;
            var requestFactor = 1;
            var requestLatestValues = 1;
            var range = _rangeDecreasing;

            var resultRequestValueOne = range.OptimizeLatestValuesRange(requestLatestValues, false, false, range.Start, range.Start, range.End, requestFactor, rangeSizeDepthIndex);
            var expectedRangeRequestValueOne = new Range<double?>(range.Start.Value, null); 
            Assert.AreEqual(expectedRangeRequestValueOne, resultRequestValueOne);

            requestLatestValues = 10;
            var optimizationEstimate = requestFactor * (requestLatestValues + 1) * rangeSizeDepthIndex;
            var expectedRange = new Range<double?>(range.Start + optimizationEstimate, null);

            if (range.Start.Value < expectedRange.Start.Value)
                expectedRange = new Range<double?>(range.Start, range.End);

            var result = range.OptimizeLatestValuesRange(requestLatestValues, false, false, range.Start, range.Start, range.End, requestFactor, rangeSizeDepthIndex);
            Assert.AreEqual(expectedRange, result);
        }

        private List<Range<double?>> GetRanges(bool increasing)
        {
            var ranges = new List<Range<double?>>();
            var start = 200;
            var end = 500;
            for (var i=1;i<=10;i++)
            {
                ranges.Add(new Range<double?>((increasing ? start : end) * i, (increasing ? end : start) * i));
            }

            return ranges;
        }

    }
}
