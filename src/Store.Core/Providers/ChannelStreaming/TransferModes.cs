//----------------------------------------------------------------------- 
// PDS WITSMLstudio StoreSync, 2018.2
//
// Copyright 2018 PDS Americas LLC
//----------------------------------------------------------------------- 

namespace PDS.WITSMLstudio.Store.Providers.ChannelStreaming
{
    /// <summary>
    /// Enumeration values for different modes of variable transfer.
    /// </summary>
    public enum TransferModes
    {
        /// <summary>
        /// Real time mode - The default behavior for streaming data 
        /// within a specified real-time threshold.
        /// </summary>
        RealTime = 0,

        /// <summary>
        /// Latest value mode - The most recent value can be streamed as 
        /// real-time even outside of a specified real-time threshold.
        /// </summary>
        LatestValue = 1
    }
}