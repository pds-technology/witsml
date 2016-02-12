using Energistics.Common;
using Energistics.Datatypes.Object;

namespace Energistics.Protocol.Store
{
    public interface IStoreStore : IProtocolHandler
    {
        void Object(DataObject dataObject);

        event ProtocolEventHandler<GetObject, DataObject> OnGetObject;

        event ProtocolEventHandler<PutObject> OnPutObject;

        event ProtocolEventHandler<DeleteObject> OnDeleteObject;
    }
}
