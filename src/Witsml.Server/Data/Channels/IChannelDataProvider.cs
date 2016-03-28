using System.Collections.Generic;
using Energistics.Datatypes;
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
        /// Gets the channel data records for the specified data object URI and range.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="indexChannel">The index channel.</param>
        /// <param name="range">The data range to retrieve.</param>
        /// <param name="increasing">if set to <c>true</c> the primary index is increasing.</param>
        /// <returns></returns>
        IEnumerable<IChannelDataRecord> GetChannelData(EtpUri uri, string indexChannel, Range<double?> range, bool increasing);
    }
}
