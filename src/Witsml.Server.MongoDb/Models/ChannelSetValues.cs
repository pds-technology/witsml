using System;
using System.Collections.Generic;

namespace PDS.Witsml.Server.Models
{
    [Serializable]
    public class ChannelSetValues
    {
        public string Uid { get; set; }

        public string UidLog { get; set; }

        public string UidChannelSet { get; set; }

        public List<ChannelIndexInfo> Indices { get; set; }

        public string Data { get; set; }
    }
}
