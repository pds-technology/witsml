using Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Server
{
    public interface IWitsml131Configuration
    {
        void GetCapabilities(CapServer capServer);
    }
}
