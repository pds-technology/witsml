//----------------------------------------------------------------------- 
// PDS WITSMLstudio StoreSync, 2018.2
// Copyright 2018 PDS Americas LLC
//-----------------------------------------------------------------------

using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes.ChannelData;

namespace PDS.WITSMLstudio.Store.Providers.ChannelDataLoad
{
    /// <summary>
    /// Defines the properties and methods used by the Channel Data Load Consumer to stream data.
    /// </summary>
    public interface IDataLoadConsumer : IProtocolHandler
    {
        /// <summary>
        /// Gets the channel metadata record for the specified channelId
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <returns>An <see cref="IChannelMetadataRecord"/> for the specified channelId</returns>
        IChannelMetadataRecord GetChannelMetadataRecord(long channelId);
    }
}