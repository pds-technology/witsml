using System.Collections.Generic;

namespace PDS.Witsml.Server.Data
{
    public interface IWitsmlDataAdapter<T>
    {
        WitsmlResult<List<T>> Query(WitsmlQueryParser parser);

        WitsmlResult Add(T entity);

        WitsmlResult Update(T entity);

        WitsmlResult Delete(WitsmlQueryParser parser);
    }
}
