using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using log4net;
using Witsml141 = Energistics.DataAccess.WITSML141;
using System.Reflection;
using System;

namespace PDS.Witsml.Server.Data.CapServers
{
    /// <summary>
    /// Provides common WITSML server capabilities for data schema version 1.4.1.1.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.CapServers.CapServerProvider{Energistics.DataAccess.WITSML141.CapServers}" />
    [Export(typeof(ICapServerProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class CapServer141Provider : CapServerProvider<Witsml141.CapServers>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CapServer141Provider));
        private PropertyInfo[] PropertyInfo;

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
        /// Validates the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="witsmlType">Type of the witsml.</param>
        /// <param name="xml">The XML.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="capabilities">The capabilities.</param>
        public override void Validate(Functions function, string witsmlType, string xml, string optionsIn, string capabilities)
        {
            base.Validate(function, witsmlType, xml, optionsIn, capabilities);

            Dictionary<string, string> options = OptionsIn.Parse(optionsIn);

            // Validate options for AddToStore
            if (Functions.AddToStore.Equals(function))
            {
                ValidateKeywords(options, "compressionMethod");
                ValidateCompressionMethod(options);
            }
        }

        /// <summary>
        /// Validates the options are supported.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="keywords">The supported keywords.</param>
        /// <exception cref="WitsmlException"></exception>
        private void ValidateKeywords(Dictionary<string, string> options, params string[] keywords)
        {
            foreach (var option in options.Where(x => !keywords.Contains(x.Key)))
            {
                throw new WitsmlException(ErrorCodes.KeywordNotSupportedByFunction, "Option not supported: " + option.Key);
            }
        }

        private void ValidateCompressionMethod(Dictionary<string, string> options)
        {
            string optionKey = "compressionMethod";
            string value;
            if (!options.TryGetValue(optionKey, out value))
                return;

            // Validate compression method
            string optionValue = value.ToLower();
            if (!optionValue.Equals("none") && !optionValue.Equals("gzip"))
            {
                throw new WitsmlException(ErrorCodes.InvalidKeywordValue);
            }

            // Validate method is supported
            var property = GetPropertyInfo().Where(x => x.Name.Equals(optionKey, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            string propertyValue = property.GetValue(GetCapServer().CapServer) as string;
            if (propertyValue == string.Empty && !optionValue.Equals("none"))
            {
                throw new WitsmlException(ErrorCodes.KeywordNotSupportedByServer);
            }
        }

        private PropertyInfo[] GetPropertyInfo()
        {
            if (PropertyInfo == null)
            {
                PropertyInfo = GetCapServer().CapServer.GetType().GetProperties();
            }
            return PropertyInfo;
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
            capServer.CompressionMethod = string.Empty; // TODO: update when compression is supported

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
