using System.Collections.Generic;
using System.Linq;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using Witsml131Schemas = Energistics.DataAccess.WITSML131.ComponentSchemas;
using Witsml141Schemas = Energistics.DataAccess.WITSML141.ComponentSchemas;
using Witsml200Schemas = Energistics.DataAccess.WITSML200.ComponentSchemas;
using System;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Provides extension methods for common WITSML data objects.
    /// </summary>
    public static class WitsmlExtensions
    {
        /// <summary>
        /// Adds support for the specified function and data object to the capServer instance.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        /// <param name="function">The WITSML Store API function.</param>
        /// <param name="dataObject">The data object.</param>
        public static void Add(this Witsml131.CapServer capServer, Functions function, string dataObject)
        {
            if (capServer.Function == null)
                capServer.Function = new List<Witsml131Schemas.Function>();

            var name = "WMLS_" + function.ToString();
            var func = capServer.Function.FirstOrDefault(x => x.Name == name);

            if (func == null)
            {
                capServer.Function.Add(func = new Witsml131Schemas.Function()
                {
                    Name = name,
                    DataObject = new List<string>()
                });
            }

            func.DataObject.Add(dataObject);
        }

        /// <summary>
        /// Adds support for the specified function and data object to the capServer instance.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        /// <param name="function">The WITSML Store API function.</param>
        /// <param name="dataObject">The data object.</param>
        public static void Add(this Witsml141.CapServer capServer, Functions function, string dataObject)
        {
            Add(capServer, function, new Witsml141Schemas.ObjectWithConstraint(dataObject));
        }

        /// <summary>
        /// Adds support for the specified function and data object to the capServer instance.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        /// <param name="function">The WITSML Store API function.</param>
        /// <param name="dataObject">The data object.</param>
        public static void Add(this Witsml141.CapServer capServer, Functions function, Witsml141Schemas.ObjectWithConstraint dataObject)
        {
            if (capServer.Function == null)
                capServer.Function = new List<Witsml141Schemas.Function>();

            var name = "WMLS_" + function.ToString();
            var func = capServer.Function.FirstOrDefault(x => x.Name == name);

            if (func == null)
            {
                capServer.Function.Add(func = new Witsml141Schemas.Function()
                {
                    Name = name,
                    DataObject = new List<Witsml141Schemas.ObjectWithConstraint>()
                });
            }

            func.DataObject.Add(dataObject);
        }

        /// <summary>
        /// Updates the dTimCreation and dTimLastChange properties in common data.
        /// </summary>
        /// <param name="commonData">The common data.</param>
        /// <returns>The instance of common data.</returns>
        public static Witsml131Schemas.CommonData Update(this Witsml131Schemas.CommonData commonData, bool created = false)
        {
            if (commonData == null)
                commonData = new Witsml131Schemas.CommonData();

            if (created)
            {
                commonData.DateTimeCreation = DateTime.UtcNow;
            }
            else
            {
                commonData.DateTimeCreation = null;
                commonData.DateTimeCreationSpecified = false;
            }

            commonData.DateTimeLastChange = DateTime.UtcNow;

            return commonData;
        }

        /// <summary>
        /// Updates the dTimCreation and dTimLastChange properties in common data.
        /// </summary>
        /// <param name="commonData">The common data.</param>
        /// <returns>The instance of common data.</returns>
        public static Witsml141Schemas.CommonData Update(this Witsml141Schemas.CommonData commonData, bool created = false)
        {
            if (commonData == null)
                commonData = new Witsml141Schemas.CommonData();

            if (created)
            {
                commonData.DateTimeCreation = DateTimeOffset.UtcNow;
            }
            else
            {
                commonData.DateTimeCreation = null;
                commonData.DateTimeCreationSpecified = false;
            }

            commonData.DateTimeLastChange = DateTimeOffset.UtcNow;

            return commonData;
        }

        /// <summary>
        /// Updates the Creation and LastUpdate properties in the citation.
        /// </summary>
        /// <param name="citation">The citation.</param>
        /// <returns>The instance of the citation.</returns>
        public static Witsml200Schemas.Citation Update(this Witsml200Schemas.Citation citation, bool created = false)
        {
            if (citation == null)
                citation = new Witsml200Schemas.Citation();

            citation.Creation = (created)
                ? DateTime.UtcNow
                : (DateTime?)null;

            citation.LastUpdate = DateTime.UtcNow;

            return citation;
        }
    }
}
