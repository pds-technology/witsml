using System;

namespace PDS.Witsml
{
    public class WitsmlException : Exception
    {
        public WitsmlException(ErrorCodes errorCode)
        {
            ErrorCode = errorCode;
        }

        public WitsmlException(ErrorCodes errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        public WitsmlException(ErrorCodes errorCode, string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        public WitsmlException(ErrorCodes errorCode, Exception innerException) : base(errorCode.GetDescription(), innerException)
        {
            ErrorCode = errorCode;
        }

        public ErrorCodes ErrorCode { get; private set; }
    }
}
