//----------------------------------------------------------------------- 
// PDS WITSMLstudio StoreSync, 2018.2
// Copyright 2018 PDS Americas LLC
//-----------------------------------------------------------------------

using System.Collections.Generic;

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
        /// <param name="uri">The channel URI.</param>
        /// <param name="id">The channel identifier.</param>
        /// <returns>The message identifier.</returns>
        long OpenChannel(string uri, long id);
    }
}