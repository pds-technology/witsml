using System;

namespace PDS.Witsml
{
    /// <summary>
    /// Represents errors that occur during WITSML Store API method execution.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class WitsmlException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlException" /> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        public WitsmlException(ErrorCodes errorCode) : this(errorCode, errorCode.GetDescription())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlException"/> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="message">The message.</param>
        public WitsmlException(ErrorCodes errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlException"/> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public WitsmlException(ErrorCodes errorCode, string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlException"/> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="innerException">The inner exception.</param>
        public WitsmlException(ErrorCodes errorCode, Exception innerException) : base(errorCode.GetDescription(), innerException)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Gets the WITSML error code.
        /// </summary>
        /// <value>The error code.</value>
        public ErrorCodes ErrorCode { get; private set; }
    }
}
