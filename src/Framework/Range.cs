using System;

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
        public T Start { get; private set; }

        /// <summary>
        /// Gets the end of the range.
        /// </summary>
        /// <value>The end value.</value>
        public T End { get; private set; }

        public TimeSpan? Offset { get; private set; }
    }
}
