//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
//----------------------------------------------------------------------- 

using System.ComponentModel;

namespace PDS.WITSMLstudio.Data
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
        [Description("Real-Time")]
        RealTime = 0,

        /// <summary>
        /// Latest value mode - The most recent value can be streamed as 
        /// real-time even outside of a specified real-time threshold.
        /// </summary>
        [Description("Latest Value")]
        LatestValue = 1
    }
}