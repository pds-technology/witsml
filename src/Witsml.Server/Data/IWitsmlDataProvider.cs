using Energistics.DataAccess;

namespace PDS.Witsml.Server.Data
{
    public interface IWitsmlDataProvider
    {
        WitsmlResult<IEnergisticsCollection> GetFromStore(string witsmlType, string query, string options, string capabilities);
    }
}
