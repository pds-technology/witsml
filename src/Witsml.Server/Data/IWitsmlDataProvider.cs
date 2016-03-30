using Energistics.DataAccess;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Represents a data provider that implements support for WITSML queries
    /// </summary>
    public interface IWitsmlDataProvider
    {
        /// <summary>
        /// Retrieves data objects from the data store.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <returns>Queried objects.</returns>
        WitsmlResult<IEnergisticsCollection> GetFromStore(RequestContext context);
    }
}
