//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2017.1
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

namespace PDS.WITSMLstudio.Framework
{
    /// <summary>
    /// Represents the start and end values of a range.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    public struct Range<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Range{T}" /> struct.
        /// </summary>
        /// <param name="start">The start value.</param>
        /// <param name="end">The end value.</param>
        /// <param name="offset">The offset.</param>
        public Range(T start, T end, TimeSpan? offset = null)
        {
            Start = start;
            End = end;
            Offset = offset;
        }

        /// <summary>
        /// Gets the start of the range.
        /// </summary>
        /// <value>The start value.</value>
        public T Start { get; }

        /// <summary>
        /// Gets the end of the range.
        /// </summary>
        /// <value>The end value.</value>
        public T End { get; }

        /// <summary>
        /// Gets the offset.
        /// </summary>
        /// <value>The offset.</value>
        public TimeSpan? Offset { get; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"Range: {{ Start: { Start }, End: { End }, Offset: { Offset } }}";
        }
    }
}
