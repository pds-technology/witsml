using System.Collections.Generic;
using Energistics.Datatypes.Object;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Defines the methods needed to support ETP.
    /// </summary>
    /// <typeparam name="T">The typed WITSML object</typeparam>
    public interface IEtpDataAdapter<T>
    {
        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        List<T> GetAll(string parentUri = null);

        //IList<string> GetUris();

        //IList<T> GetAll();

        //T GetByUri(string uri);

        //T GetById(string uuid);

        //void Put(T entity);

        //void DeleteByUri(string uri);

        //void DeleteById(string uuid);
    }
}
