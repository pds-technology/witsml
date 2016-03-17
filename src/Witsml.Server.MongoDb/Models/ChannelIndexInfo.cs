using System;

namespace PDS.Witsml.Server.Models
{
    [Serializable]
    public class ChannelIndexInfo
    {
        public string Mnemonic { get; set; }

        public bool Increasing { get; set; }

        public bool IsTimeIndex { get; set; }

        public double Start { get; set; }

        public double End { get; set; }
    }
}
