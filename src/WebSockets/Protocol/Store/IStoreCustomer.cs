using System.Collections.Generic;
using Energistics.Common;
using Energistics.Datatypes.Object;

namespace Energistics.Protocol.Store
{
    public interface IStoreCustomer : IProtocolHandler
    {
        void GetObject(string uri);

        void PutObject(DataObject dataObject);

        void DeleteObject(IList<string> uris);

        event ProtocolEventHandler<Object> OnObject;
    }
}
