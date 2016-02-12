using System.ComponentModel.Composition;
using PDS.Witsml.Server;

namespace PDS.Witsml.Web
{
    public class WitsmlStore : IWitsmlStore
    {
        private readonly IWitsmlStore _store;

        [ImportingConstructor]
        public WitsmlStore(IWitsmlStore store)
        {
            _store = store;
        }

        public WMLS_GetVersionResponse WMLS_GetVersion(WMLS_GetVersionRequest request)
        {
            return _store.WMLS_GetVersion(request);
        }

        public WMLS_GetCapResponse WMLS_GetCap(WMLS_GetCapRequest request)
        {
            return _store.WMLS_GetCap(request);
        }

        public WMLS_GetFromStoreResponse WMLS_GetFromStore(WMLS_GetFromStoreRequest request)
        {
            return _store.WMLS_GetFromStore(request);
        }

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