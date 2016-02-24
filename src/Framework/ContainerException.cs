using System;
using System.Runtime.Serialization;

namespace PDS.Framework
{
    /// <summary>
    /// Represents errors that occur during dependency resolution.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class ContainerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerException"/> class.
        /// </summary>
        public ContainerException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ContainerException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public ContainerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected ContainerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
