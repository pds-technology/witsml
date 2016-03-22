using System;

namespace PDS.Witsml.Data
{
    public class DataGenerator
    {
        public readonly string TimestampFormat = "yyMMdd-HHmmss-fff";

        public string Uid()
        {
            return Guid.NewGuid().ToString();
        }

        public string Name(string prefix = null)
        {
            if (String.IsNullOrWhiteSpace(prefix))
                return DateTime.Now.ToString(TimestampFormat);

            return String.Format("{0} - {1}", prefix, DateTime.Now.ToString(TimestampFormat));
        }
    }
}
