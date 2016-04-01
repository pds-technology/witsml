using System.Collections.Generic;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using PDS.Framework;
using PDS.Witsml.Data.Channels;

namespace PDS.Witsml.Server.Data.Channels
{
    /// <summary>
    /// Defines a method that can be used to retrieve channel data.
    /// </summary>
    public interface IChannelDataProvider : IEtpDataAdapter
    {
        /// <summary>
        /// Gets the channel metadata for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <returns>A collection of channel metadata.</returns>
        IList<ChannelMetadataRecord> GetChannelMetadata(EtpUri uri);

        /// <summary>
        /// Gets the channel data records for the specified data object URI and range.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="range">The data range to retrieve.</param>
        /// <returns>A collection of channel data.</returns>
        IEnumerable<IChannelDataRecord> GetChannelData(EtpUri uri, Range<double?> range);

        /// <summary>
        /// Updates the channel data for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="reader">The update reader.</param>
        void UpdateChannelData(EtpUri uri, ChannelDataReader reader);
    }
}
