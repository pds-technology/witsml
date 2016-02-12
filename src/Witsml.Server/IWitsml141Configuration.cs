using Energistics.DataAccess.WITSML141;

namespace PDS.Witsml.Server
{
    public interface IWitsml141Configuration
    {
        void GetCapabilities(CapServer capServer);
    }
}
