using System;
using System.Collections.Generic;
using PDS.Witsml.Data.Channels;

namespace PDS.Witsml.Server.Models
{
    /// <summary>
    /// Model class for unified log data
    /// </summary>
    [Serializable]
    public class ChannelDataChunk
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the URI of the parent data object.
        /// </summary>
        /// <value>The parent data object URI.</value>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the collection of <see cref="ChannelIndexInfo"/>.
        /// </summary>
        /// <value>The indices to handle multiple indices in a channel set for 2.0 log.</value>
        public List<ChannelIndexInfo> Indices { get; set; }

        /// <summary>
        /// Gets or sets the channel data values.
        /// </summary>
        /// <value>The data in JSON format.</value>
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets the comma separated mnemonic list.
        /// </summary>
        /// <value>The mnemonic list.</value>
        public string MnemonicList { get; set; }

        /// <summary>
        /// Gets or sets the comma separated unit list.
        /// </summary>
        /// <value>The unit list.</value>
        public string UnitList { get; set; }
    }
}
