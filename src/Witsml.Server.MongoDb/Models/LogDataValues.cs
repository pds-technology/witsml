using System;

namespace PDS.Witsml.Server.Models
{
    [Serializable]
    internal class LogDataValues
    {
        public string Uid { get; set; }

        public string UidLog { get; set; }

        public double StartIndex { get; set; }

        public double EndIndex { get; set; }

        public string Data { get; set; }
    }
}
