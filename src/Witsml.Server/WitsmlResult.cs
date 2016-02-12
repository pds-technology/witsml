namespace PDS.Witsml.Server
{
    public class WitsmlResult
    {
        public WitsmlResult(ErrorCodes code) : this(code, string.Empty)
        {
        }

        public WitsmlResult(ErrorCodes code, string message)
        {
            Code = code;
            Message = message;
        }

        public ErrorCodes Code { get; private set; }

        public string Message { get; private set; }
    }

    public class WitsmlResult<T> : WitsmlResult
    {
        public WitsmlResult(ErrorCodes errorCode, T results) : this(errorCode, string.Empty, results)
        {
        }

        public WitsmlResult(ErrorCodes errorCode, string message, T results) : base(errorCode, message)
        {
            Results = results;
        }

        public T Results { get; private set; }
    }
}
