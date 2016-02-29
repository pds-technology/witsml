using Energistics.Common;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Store;

namespace PDS.Witsml.Server.Providers.Store
{
    /// <summary>
    /// Defines methods that can be used to perform CRUD operations via ETP.
    /// </summary>
    public interface IStoreStoreProvider
    {
        /// <summary>
        /// Gets the data schema version supported by the provider.
        /// </summary>
        /// <value>The data schema version.</value>
        string DataSchemaVersion { get; }

        /// <summary>
        /// Gets the object details for the specified URI.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetObject, DataObject}"/> instance containing the event data.</param>
        void GetObject(ProtocolEventArgs<GetObject, DataObject> args);
    }
}
