using System.Linq;
using Energistics.Datatypes;

namespace PDS.Witsml.Server.Providers
{
    /// <summary>
    /// Provides extension methods for common ETP types.
    /// </summary>
    public static class EtpExtensions
    {
        /// <summary>
        /// Converts an <see cref="EtpUri"/> to a data object identifier.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>A <see cref="DataObjectId"/> instance.</returns>
        public static DataObjectId ToDataObjectId(this EtpUri uri)
        {
            if (uri.ObjectType == ObjectTypes.Well || uri.IsRelatedTo(EtpUris.Witsml200))
            {
                return new DataObjectId(uri.ObjectId, null);
            }
            else if (uri.ObjectType == ObjectTypes.Wellbore)
            {
                return uri.ToWellObjectId();
            }

            return uri.ToWellboreObjectId();
        }

        /// <summary>
        /// Converts an <see cref="EtpUri"/> to a well object identifier.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>A <see cref="WellObjectId"/> instance.</returns>
        public static WellObjectId ToWellObjectId(this EtpUri uri)
        {
            var ids = uri.GetObjectIds().ToDictionary(x => x.Key, x => x.Value);

            return new WellObjectId(
                uri.ObjectId,
                ids[ObjectTypes.Well],
                null);
        }

        /// <summary>
        /// Converts an <see cref="EtpUri"/> to a wellbore object identifier.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>A <see cref="WellboreObjectId"/> instance.</returns>
        public static WellboreObjectId ToWellboreObjectId(this EtpUri uri)
        {
            var ids = uri.GetObjectIds().ToDictionary(x => x.Key, x => x.Value);

            return new WellboreObjectId(
                uri.ObjectId,
                ids[ObjectTypes.Well],
                ids[ObjectTypes.Wellbore],
                null);
        }
    }
}
