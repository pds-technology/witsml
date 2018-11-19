//----------------------------------------------------------------------- 
// PDS WITSMLstudio StoreSync, 2018.2
// Copyright 2018 PDS Americas LLC
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Energistics.Etp.v12.Datatypes.ChannelData;

namespace PDS.WITSMLstudio.Store.Providers.ChannelDataLoad
{
    /// <summary>
    /// Defines the properties and methods used by the Channel Data Load Producer to stream data.
    /// </summary>
    public interface IDataLoadProducer
    {
        /// <summary>
        /// Gets or sets the simple streamer uris.
        /// </summary>
        IList<string> DataLoadUris { get; set; }

        /// <summary>
        /// Initializes the channel streaming producer as a simple streamer.
        /// </summary>
        /// <param name="uris">The uris to stream.</param>
        void InitializeDataLoad(IList<string> uris);

        /// <summary>
        /// Sends a OpenChannel message to a store.
        /// </summary>
        /// <param name="channels">The channels.</param>
        /// <returns>The message identifier.</returns>
        long OpenChannel(IList<ChannelMetadataRecord> channels);

        /// <summary>
        /// Sends a RealtimeData message with the specified data items.
        /// </summary>
        /// <param name="dataItems">The data items.</param>
        /// <returns>The message identifier.</returns>
        long RealtimeData(IList<DataItem> dataItems);

        /// <summary>
        /// Sends an InfillData message with the specified data items.
        /// </summary>
        /// <param name="dataItems">The data items.</param>
        /// <returns>The message identifier.</returns>
        long InfillData(IList<DataItem> dataItems);
    }
}