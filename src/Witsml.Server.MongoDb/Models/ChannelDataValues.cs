using System;
using System.Collections.Generic;
using PDS.Witsml.Data.Channels;

namespace PDS.Witsml.Server.Models
{
    /// <summary>
    /// Model class for unified log data
    /// </summary>
    [Serializable]
    public class ChannelDataValues
    {
        public string Id { get; set; }

        public string Uid { get; set; }

        public string UidLog { get; set; }

        public string UidChannelSet { get; set; }
        
        /// <summary>
        /// Gets or sets the indices.
        /// </summary>
        /// <value>
        /// The indices to handle multiple indices in a channel set for 2.0 log.
        /// </value>
        public List<ChannelIndexInfo> Indices { get; set; }

        public string Data { get; set; }

        public string MnemonicList { get; set; }

        public string UnitList { get; set; }
    }
}
