using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Energistics.DataAccess.Reflection;

namespace Witsml.Studio.Plugins.ObjectInspector.Models
{
    /// <summary>
    /// Static methods for working with Energistics Data Objects in the DevKit
    /// </summary>
    public static class EnergisticsHelper
    {
        /// <summary>
        /// Gets the list of all types in the DevKit that are Energistics Data Objects
        /// </summary>
        /// <returns>All types for Energistics Data Objects</returns>
        public static IEnumerable<Type> GetAllDataObjectTypes()
        {
            var devKit = Assembly.GetAssembly(typeof(StandardFamily));

            return devKit.GetTypes().Where(t => t.GetCustomAttribute<EnergisticsDataObjectAttribute>() != null);
        }

        /// <summary>
        /// Gets the list of all types in the DevKit that are Energistics Data Objects in the specified standard family and data schema version.
        /// </summary>
        /// <param name="standardFamily">The specified standard family.</param>
        /// <param name="dataSchemaVersion">The specified data schema version.</param>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllDataObjectTypes(StandardFamily standardFamily, Version dataSchemaVersion)
        {
            Assembly devKit = Assembly.GetAssembly(typeof(StandardFamily));

            return devKit.GetTypes().Where(t =>
                {
                    var edo = t.GetCustomAttribute<EnergisticsDataObjectAttribute>();
                    return edo != null && edo.StandardFamily == standardFamily && edo.DataSchemaVersion == dataSchemaVersion;
                });
        }
    }
}
