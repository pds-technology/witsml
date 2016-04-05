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

namespace PDS.Framework
{
    /// <summary>
    /// Represents the start and end values of a range.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    public struct Range<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Range{T}"/> struct.
        /// </summary>
        /// <param name="start">The start value.</param>
        /// <param name="end">The end value.</param>
        public Range(T start, T end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Gets the start of the range.
        /// </summary>
        /// <value>The start value.</value>
        public T Start { get; private set; }

        /// <summary>
        /// Gets the end of the range.
        /// </summary>
        /// <value>The end value.</value>
        public T End { get; private set; }
    }
}
