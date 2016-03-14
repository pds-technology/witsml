using System.Collections.Generic;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using PDS.Witsml.Properties;

namespace PDS.Witsml
{
    /// <summary>
    /// Provides extension methods that can be used with common WITSML types and interfaces.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets the description associated with the specified WITSML error code.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <returns>The description for the error code.</returns>
        public static string GetDescription(this ErrorCodes errorCode)
        {
            return Resources.ResourceManager.GetString(errorCode.ToString(), Resources.Culture);
        }

        /// <summary>
        /// Gets the value of the Version property for specified container object.
        /// </summary>
        /// <typeparam name="T">The data object type.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <returns>The value of the Version property.</returns>
        public static string GetVersion<T>(this T dataObject) where T : IEnergisticsCollection
        {
            return (string)dataObject.GetType().GetProperty("Version").GetValue(dataObject, null);
        }

        /// <summary>
        /// Sets the value of the Version property for the specified container object.
        /// </summary>
        /// <typeparam name="T">The data object type.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <param name="version">The version.</param>
        /// <returns>The data object instance.</returns>
        public static T SetVersion<T>(this T dataObject, string version) where T : IEnergisticsCollection
        {
            dataObject.GetType().GetProperty("Version").SetValue(dataObject, version);
            return dataObject;
        }

        /// <summary>
        /// Wraps the specified data object in a <see cref="List{TObject}"/>.
        /// </summary>
        /// <typeparam name="TObject">The type of data object.</typeparam>
        /// <param name="instance">The data object instance.</param>
        /// <returns>A <see cref="List{TObject}"/> instance containing a single item.</returns>
        public static List<TObject> AsList<TObject>(this TObject instance) where TObject : IDataObject
        {
            return new List<TObject>() { instance };
        }

        /// <summary>
        /// Gets the data object identifier, i.e. uid.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="instance">The instance.</param>
        /// <returns>The identifier object.</returns>
        public static DataObjectId GetDataObjectId<TObject>(this TObject instance) where TObject : IDataObject
        {
            return new DataObjectId(instance.Uid, instance.Name);
        }

        /// <summary>
        /// Gets the well object identifier, i.e. uid and uidWell.
        /// </summary>
        /// <typeparam name="TObject">The type of the well object.</typeparam>
        /// <param name="instance">The instance.</param>
        /// <returns>The identified object.</returns>
        public static WellObjectId GetWellObjectId<TObject>(this TObject instance) where TObject : IWellObject
        {
            return new WellObjectId(instance.Uid, instance.UidWell, instance.Name);
        }

        /// <summary>
        /// Gets the wellbore object identifier, i.e. uid, uidWell, and uidWellbore.
        /// </summary>
        /// <typeparam name="TObject">The type of the wellbore object.</typeparam>
        /// <param name="instance">The instance.</param>
        /// <returns>The identified object.</returns>
        public static WellboreObjectId GetWellboreObjectId<TObject>(this TObject instance) where TObject : IWellboreObject
        {
            return new WellboreObjectId(instance.Uid, instance.UidWell, instance.UidWellbore, instance.Name);
        }

        /// <summary>
        /// Gets the  WITSML 2.0 data object identifier, i.e. uuid.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="instance">The instance.</param>
        /// <returns>The identified object.</returns>
        public static DataObjectId GetAbstractDataObjectId<TObject>(this TObject instance) where TObject : AbstractObject
        {
            return new DataObjectId(instance.Uuid, null);
        }
    }
}
