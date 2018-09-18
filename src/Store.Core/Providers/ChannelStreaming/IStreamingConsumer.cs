//----------------------------------------------------------------------- 
// PDS WITSMLstudio StoreSync, 2018.2
// Copyright 2018 PDS Americas LLC
//-----------------------------------------------------------------------

using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes.ChannelData;

namespace PDS.WITSMLstudio.Store.Providers.ChannelStreaming
{
    /// <summary>
    /// Defines a common interface for an ETP channel streaming consumer implementation.
    /// </summary>
    public interface IStreamingConsumer : IProtocolHandler
    {
        /// <summary>
        /// Gets a value indicating whether this instance is simple streamer.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is simple streamer; otherwise, <c>false</c>.
        /// </value>
        bool IsSimpleStreamer { get; set; }

        /// <summary>
        /// Gets the channel metadata record for the specified channelId
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <returns>An <see cref="IChannelMetadataRecord"/> for the specified channelId</returns>
        IChannelMetadataRecord GetChannelMetadataRecord(long channelId);
    }
}