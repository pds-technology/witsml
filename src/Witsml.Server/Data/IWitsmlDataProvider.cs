using Energistics.DataAccess;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Data provider that encapsulates read service calls for WITSML query
    /// </summary>
    public interface IWitsmlDataProvider
    {
        /// <summary>
        /// Gets object(s) from store.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <returns>Queried objects.</returns>
        WitsmlResult<IEnergisticsCollection> GetFromStore(RequestContext context);
    }
}
