using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using log4net;
using Witsml141 = Energistics.DataAccess.WITSML141;

namespace PDS.Witsml.Server.Configuration
{
    /// <summary>
    /// Provides common WITSML server capabilities for data schema version 1.4.1.1.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Configuration.CapServerProvider{Energistics.DataAccess.WITSML141.CapServers}" />
    [Export(typeof(ICapServerProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class CapServer141Provider : CapServerProvider<Witsml141.CapServers>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CapServer141Provider));

        /// <summary>
        /// Gets the data schema version.
        /// </summary>
        /// <value>The data schema version.</value>
        public override string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version141.Value; }
        }

        /// <summary>
        /// Gets or sets the collection of <see cref="IWitsml141Configuration"/> providers.
        /// </summary>
        /// <value>The collection of providers.</value>
        [ImportMany]
        public IEnumerable<IWitsml141Configuration> Providers { get; set; }

        /// <summary>
        /// Performs validation for the specified function and supplied parameters.
        /// </summary>
        /// <param name="function">The WITSML Store API function.</param>
        /// <param name="objectType">The type of the data object.</param>
        /// <param name="xml">The XML string for the data object.</param>
        /// <param name="options">The options.</param>
        /// <param name="capabilities">The client's capabilities object (capClient).</param>
        public override void ValidateRequest(Functions function, string objectType, string xml, string options, string capabilities)
        {
            base.ValidateRequest(function, objectType, xml, options, capabilities);

            var optionsIn = OptionsIn.Parse(options);

            if (function == Functions.GetFromStore)
            {
            }
            else if (function == Functions.AddToStore)
            {
                ValidateKeywords(optionsIn, OptionsIn.CompressionMethod.Keyword);
                ValidateCompressionMethod(optionsIn, GetCapServer().CapServer.CompressionMethod);
                ValidateSingleChildElement(objectType, xml);
            }
            else if (function == Functions.UpdateInStore)
            {
                ValidateKeywords(optionsIn, OptionsIn.CompressionMethod.Keyword);
                ValidateCompressionMethod(optionsIn, GetCapServer().CapServer.CompressionMethod);
                ValidateSingleChildElement(objectType, xml);
            }
            else if (function == Functions.UpdateInStore)
            {
                //ValidateKeywords(optionsIn, OptionsIn.CascadedDelete.Keyword);
                //ValidateCascadedDelete(optionsIn, GetCapServer().CapServer.CascadedDelete.GetValueOrDefault());
                ValidateSingleChildElement(objectType, xml);
            }
        }

        /// <summary>
        /// Creates the capServers instance for a specific data schema version.
        /// </summary>
        /// <returns>The capServers instance.</returns>
        protected override Witsml141.CapServers CreateCapServer()
        {
            if (!Providers.Any())
            {
                _log.WarnFormat("No WITSML configuration providers loaded for data schema version {0}", DataSchemaVersion);
                return null;
            }

            var capServer = new Witsml141.CapServer();

            foreach (var config in Providers)
            {
                config.GetCapabilities(capServer);
            }

            capServer.ApiVers = "1.4.1";
            capServer.SchemaVersion = DataSchemaVersion;
            capServer.SupportUomConversion = false; // TODO: update after UoM conversion implemented
            capServer.CompressionMethod = OptionsIn.CompressionMethod.None.Value; // TODO: update when compression is supported

            // TODO: move these to Settings
            capServer.Name = "PDS Witsml Server";
            capServer.Vendor = "PDS";
            capServer.Version = "1.0";

            return new Witsml141.CapServers()
            {
                CapServer = capServer,
                Version = capServer.ApiVers
            };
        }
    }
}
