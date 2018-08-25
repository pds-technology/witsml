//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.ComponentModel.Composition;

namespace PDS.WITSMLstudio.Store
{
    /// <summary>
    /// A wrapper for the WITSML Store API that can be hosted as a WCF service.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.IWitsmlStore" />
    public class WitsmlStoreService : IWitsmlStore
    {
        private readonly IWitsmlStore _store;

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlStoreService"/> class.
        /// </summary>
        /// <param name="store">A WITSML store instance.</param>
        [ImportingConstructor]
        public WitsmlStoreService(IWitsmlStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Returns a string containing the Data Schema Version(s) that a server supports.
        /// </summary>
        /// <param name="request">The request object containing the method input parameters.</param>
        /// <returns>A comma-separated list of Data Schema Versions (without spaces) that the server supports.</returns>
        public WMLS_GetVersionResponse WMLS_GetVersion(WMLS_GetVersionRequest request)
        {
            return _store.WMLS_GetVersion(request);
        }

        /// <summary>
        /// Returns the capServer object that describes the capabilities of the server for one Data Schema Version.
        /// </summary>
        /// <param name="request">The request object containing the method input parameters.</param>
        /// <returns>A positive value indicates a success; a negative value indicates an error.</returns>
        public WMLS_GetCapResponse WMLS_GetCap(WMLS_GetCapRequest request)
        {
            return _store.WMLS_GetCap(request);
        }

        /// <summary>
        /// Returns one or more WITSML data-objects from the server.
        /// </summary>
        /// <param name="request">The request object encapsulating the method input parameters.</param>
        /// <returns>
        /// A positive value indicating success along with one or more WITSML data-objects from the server, or a negative value indicating an error.
        /// </returns>
        public WMLS_GetFromStoreResponse WMLS_GetFromStore(WMLS_GetFromStoreRequest request)
        {
            return _store.WMLS_GetFromStore(request);
        }

        /// <summary>
        /// Adds a WITSML data-object to the server.  
        /// </summary>
        /// <param name="request">The request object encapsulating the method input parameters.</param>
        /// <returns>
        /// A positive value indicating success along with one or more WITSML data-objects from the server, or a negative value indicating an error.
        /// If successful the Uid of the inserted object is returned in the Response's SuppMsgOut.
        /// </returns>
        public WMLS_AddToStoreResponse WMLS_AddToStore(WMLS_AddToStoreRequest request)
        {
            return _store.WMLS_AddToStore(request);
        }

        public WMLS_UpdateInStoreResponse WMLS_UpdateInStore(WMLS_UpdateInStoreRequest request)
        {
            return _store.WMLS_UpdateInStore(request);
        }

        public WMLS_DeleteFromStoreResponse WMLS_DeleteFromStore(WMLS_DeleteFromStoreRequest request)
        {
            return _store.WMLS_DeleteFromStore(request);
        }

        public WMLS_GetBaseMsgResponse WMLS_GetBaseMsg(WMLS_GetBaseMsgRequest request)
        {
            return _store.WMLS_GetBaseMsg(request);
        }
    }
}
