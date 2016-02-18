using Energistics.DataAccess;

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
        /// <param name="witsmlType">Type of WITSML data-object.</param>
        /// <param name="query">The XML query string.</param>
        /// <param name="options">The options.</param>
        /// <param name="capabilities">The client’s Capabilities Object (capClient).</param>
        /// <returns>
        /// Queried objects.
        /// </returns>
        WitsmlResult<IEnergisticsCollection> GetFromStore(string witsmlType, string query, string options, string capabilities);
    }
}
