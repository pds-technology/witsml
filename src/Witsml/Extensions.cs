using System.Collections.Generic;
using Energistics.DataAccess;
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

        public static string ToDescription(this Functions function)
        {
            var description = string.Empty;

            switch (function)
            {
                case Functions.GetCap:
                    return "Get Capabilities";
                case Functions.GetVersion:
                    return "Get Version";
                case Functions.GetFromStore:
                    return "Get From Store";
                case Functions.AddToStore:
                    return "Add To Store";
                case Functions.UpdateInStore:
                    return "Update In Store";
                case Functions.DeleteFromStore:
                    return "Delete From Store";
                case Functions.GetObject:
                    return "Get Object";
                case Functions.PutObject:
                    return "Put Object";
                case Functions.DeleteObject:
                    return "Delete Object";
            }
            return description;
        }
    }
}
