//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Xml.Linq;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using log4net;
using PDS.Witsml.Server.Properties;

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

        private const string Namespace141 = "http://www.witsml.org/schemas/1series";

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
        /// <param name="context">The request context.</param>
        /// <param name="document">The XML document.</param>
        public override void ValidateRequest(RequestContext context, XDocument document)
        {
            base.ValidateRequest(context, document);

            var optionsIn = OptionsIn.Parse(context.Options);

            if (context.Function == Functions.GetFromStore)
            {
                ValidateKeywords(optionsIn, OptionsIn.ReturnElements.Keyword, OptionsIn.RequestObjectSelectionCapability.Keyword, OptionsIn.RequestPrivateGroupOnly.Keyword, OptionsIn.CompressionMethod.Keyword);  
                ValidateRequestObjectSelectionCapability(optionsIn, context.ObjectType, document);
                ValidateEmptyRootElement(context.ObjectType, document);
                ValidateReturnElements(optionsIn, context.ObjectType);
                ValidateSelectionCriteria(document);
            }
            else if (context.Function == Functions.AddToStore)
            {
                ValidateKeywords(optionsIn, OptionsIn.CompressionMethod.Keyword);
                ValidateCompressionMethod(optionsIn, GetCapServer().CapServer.CompressionMethod);
                ValidateEmptyRootElement(context.ObjectType, document);
                ValidateSingleChildElement(context.ObjectType, document);
            }
            else if (context.Function == Functions.UpdateInStore)
            {
                ValidateKeywords(optionsIn, OptionsIn.CompressionMethod.Keyword);
                ValidateCompressionMethod(optionsIn, GetCapServer().CapServer.CompressionMethod);
                ValidateEmptyRootElement(context.ObjectType, document);
                ValidateSingleChildElement(context.ObjectType, document);
            }
            else if (context.Function == Functions.DeleteFromStore)
            {
                //ValidateKeywords(optionsIn, OptionsIn.CascadedDelete.Keyword);
                //ValidateCascadedDelete(optionsIn, GetCapServer().CapServer.CascadedDelete.GetValueOrDefault());
                ValidateEmptyRootElement(context.ObjectType, document);
                ValidateSingleChildElement(context.ObjectType, document);
            }
        }

        /// <summary>
        /// Validates the namespace for a specific WITSML data schema version.
        /// </summary>
        /// <param name="document">The document.</param>
        protected override void ValidateNamespace(XDocument document)
        {
            if (!Namespace141.Equals(GetNamespace(document)))
            {
                throw new WitsmlException(ErrorCodes.MissingDefaultWitsmlNamespace);
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

            capServer.Name = Settings.Default.DefaultServerName;
            capServer.Version = Settings.Default.DefaultServerVersion;
            capServer.Description = Settings.Default.DefaultServerDescription;
            capServer.Vendor = Settings.Default.DefaultVendorName;
            capServer.Contact = new Contact()
            {
                Name = Settings.Default.DefaultContactName,
                Email = Settings.Default.DefaultContactEmail,
                Phone = Settings.Default.DefaultContactPhone
            };

            return new Witsml141.CapServers()
            {
                CapServer = capServer,
                Version = capServer.ApiVers
            };
        }
    }
}
