using Energistics.DataAccess.WITSML141;

namespace PDS.Witsml.Server.Configuration
{
    /// <summary>
    /// Defines a method that can be used to supply server capabilities for WITSML API version 1.4.1.
    /// </summary>
    public interface IWitsml141Configuration
    {
        /// <summary>
        /// Gets the server capabilities.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        void GetCapabilities(CapServer capServer);
    }
}
