using Energistics.Datatypes.ChannelData;

namespace PDS.Witsml.Server.Providers.ChannelStreaming
{
    /// <summary>
    /// Container class to hold an Index and Channel value DataItem
    /// </summary>
    public struct IndexedDataItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexedDataItem"/> struct.
        /// </summary>
        /// <param name="indexDataItem">The index data item.</param>
        /// <param name="valueDataItem">The value data item.</param>
        public IndexedDataItem(DataItem indexDataItem, DataItem valueDataItem)
        {
            IndexDataItem = indexDataItem;
            ValueDataItem = valueDataItem;
        }

        /// <summary>
        /// Gets the index data item.
        /// </summary>
        /// <value>
        /// The index data item.
        /// </value>
        public DataItem IndexDataItem { get; private set; }

        /// <summary>
        /// Gets the channel value data item.
        /// </summary>
        /// <value>
        /// The value data item.
        /// </value>
        public DataItem ValueDataItem { get; private set; }
    }
}
