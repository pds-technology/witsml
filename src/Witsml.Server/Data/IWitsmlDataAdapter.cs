using System.Collections.Generic;

namespace PDS.Witsml.Server.Data
{
    public interface IWitsmlDataAdapter<T>
    {
        WitsmlResult<List<T>> Query(WitsmlQueryParser parser);

        /// <summary>
        /// Adds a WITSML object into data store
        /// </summary>
        /// <param name="entity">WITSML object to be added</param>
        /// <returns>A WITSML result object that includes return code and/or message</returns>
        WitsmlResult Add(T entity);

        WitsmlResult Update(T entity);

        WitsmlResult Delete(WitsmlQueryParser parser);
    }
}
