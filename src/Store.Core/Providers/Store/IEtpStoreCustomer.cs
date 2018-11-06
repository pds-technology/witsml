//----------------------------------------------------------------------- 
// PDS WITSMLstudio StoreSync, 2018.2
// Copyright 2018 PDS Americas LLC
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Energistics.Etp.Common;

namespace PDS.WITSMLstudio.Store.Providers.Store
{
    /// <summary>
    /// Defines methods used by the ADI Store Customer.
    /// </summary>
    public interface IEtpStoreCustomer : IProtocolHandler
    {
        /// <summary>
        /// Initializes the store customer with the specified uri list.
        /// </summary>
        /// <param name="uris">The uris.</param>
        void InitializeStoreCustomer(IList<string> uris);
    }
}