using System;
using System.Linq;

namespace PDS.Witsml.Client.Linq
{
    public interface IWitsmlQuery<T> : IQueryable<T>
    {
        /// <summary>
        /// Provides a callback that can be used to include specific elements in the query response.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IWitsmlQuery<T> Include(Action<T> action);

        /// <summary>
        /// Sets the options that will be passed in to the GetFromStore query.
        /// </summary>
        /// <param name="optionsIn"></param>
        /// <returns></returns>
        IWitsmlQuery<T> With(OptionsIn optionsIn);
    }
}
