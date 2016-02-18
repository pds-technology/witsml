using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess;
using log4net;
using PDS.Framework;
using PDS.Witsml.Properties;
using PDS.Witsml.Server.Data;

namespace PDS.Witsml.Server
{
    /// <summary>
    /// The WISML Store API server implementation.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.IWitsmlStore" />
    [Export(typeof(IWitsmlStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class WitsmlStore : IWitsmlStore
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlStore));
        private static readonly string DefaultDataSchemaVersion = Settings.Default.DefaultDataSchemaVersion;

        private readonly IDictionary<string, ICapServerProvider> _capServer;
        private string _supportedVersions;

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlStore"/> class.
        /// </summary>
        public WitsmlStore()
        {
            _capServer = new Dictionary<string, ICapServerProvider>();
        }

        /// <summary>
        /// Gets or sets the composition container used for dependency injection.
        /// </summary>
        /// <value>
        /// The composition container.
        /// </value>
        [Import]
        public IContainer Container { get; set; }

        /// <summary>
        /// Returns a string containing the Data Schema Version(s) that a server supports.
        /// </summary>
        /// <param name="request">The request object encapsulating the method input parameters.</param>
        /// <returns>A comma-separated list of Data Schema Versions (without spaces) that the server supports.</returns>
        public WMLS_GetVersionResponse WMLS_GetVersion(WMLS_GetVersionRequest request)
        {
            EnsureCapServerProviders();

            _log.DebugFormat("Supported Versions: {0}", _supportedVersions);

            return new WMLS_GetVersionResponse(_supportedVersions);
        }

        /// <summary>
        /// Returns the capServer object that describes the capabilities of the server for one Data Schema Version.
        /// </summary>
        /// <param name="request">The request object encapsulating the method input parameters.</param>
        /// <returns>A positive value indicates a success; a negative value indicates an error.</returns>
        public WMLS_GetCapResponse WMLS_GetCap(WMLS_GetCapRequest request)
        {
            EnsureCapServerProviders();

            _log.DebugFormat("Options: {0}", request.OptionsIn);

            var options = OptionsIn.Parse(request.OptionsIn);
            var version = OptionsIn.GetValue(options, new OptionsIn.DataVersion(DefaultDataSchemaVersion));

            // return error if WITSML 1.3.1 not supported AND dataVersion not specified (required in WITSML 1.4.1)
            if (!_capServer.ContainsKey(OptionsIn.DataVersion.Version131.Value) && !options.ContainsKey(OptionsIn.DataVersion.Keyword))
            {
                return new WMLS_GetCapResponse(
                    (short)ErrorCodes.MissingDataVersion,
                    string.Empty,
                    ErrorCodes.MissingDataVersion.GetDescription());
            }

            if (_capServer.ContainsKey(version))
            {
                return new WMLS_GetCapResponse(
                    (short)ErrorCodes.Success,
                    _capServer[version].ToXml(),
                    String.Empty);
            }

            return new WMLS_GetCapResponse(
                (short)ErrorCodes.DataVersionNotSupported,
                string.Empty,
                ErrorCodes.DataVersionNotSupported.GetDescription() + " Data schema version not supported: " + version);
        }

        public WMLS_GetFromStoreResponse WMLS_GetFromStore(WMLS_GetFromStoreRequest request)
        {
            var version = ObjectTypes.GetVersion(request.QueryIn);

            try
            {
                _log.DebugFormat("Type: {0}; Options: {1}; Query:{3}{2}{3}", request.WMLtypeIn, request.OptionsIn, request.QueryIn, Environment.NewLine);

                ValidateObjectType(request.WMLtypeIn);
                ValidateInputTemplate(request.QueryIn);

                var dataProvider = Container.Resolve<IWitsmlDataProvider>(new ObjectName(request.WMLtypeIn, version));
                var result = dataProvider.GetFromStore(request.WMLtypeIn, request.QueryIn, request.OptionsIn, request.CapabilitiesIn);

                return new WMLS_GetFromStoreResponse(
                    (short)result.Code,
                    result.Results != null
                        ? EnergisticsConverter.ObjectToXml(result.Results)
                        : String.Empty,
                    result.Message);
            }
            catch (ContainerException)
            {
                return new WMLS_GetFromStoreResponse(
                    (short)ErrorCodes.DataObjectNotSupported,
                    String.Empty,
                    "WITSML object type not supported: " + request.WMLtypeIn + "; Version: " + version);
            }
            catch (WitsmlException ex)
            {
                return new WMLS_GetFromStoreResponse((short)ex.ErrorCode, string.Empty, ex.Message);
            }
        }

        /// <summary>
        /// Returns the response for adding one WITSML data-object to the server
        /// </summary>
        /// <param name="request">The request object encapsulating the method input parameters.</param>
        /// <returns>A positive value indicates a success; a negative value indicates an error.</returns>
        public WMLS_AddToStoreResponse WMLS_AddToStore(WMLS_AddToStoreRequest request)
        {
            var version = ObjectTypes.GetVersion(request.XMLin);

            try
            {
                _log.DebugFormat("Type: {0}; Options: {1}; XML:{3}{2}{3}", request.WMLtypeIn, request.OptionsIn, request.XMLin, Environment.NewLine);

                ValidateObjectType(version, request.WMLtypeIn, ObjectTypes.GetObjectType(request.XMLin));
                ValidateInputTemplate(request.XMLin);

                var dataWriter = Container.Resolve<IWitsmlDataWriter>(new ObjectName(request.WMLtypeIn, version));
                var result = dataWriter.AddToStore(request.WMLtypeIn, request.XMLin, request.OptionsIn, request.CapabilitiesIn);

                return new WMLS_AddToStoreResponse((short)result.Code, result.Message);
            }
            catch (ContainerException)
            {
                return new WMLS_AddToStoreResponse(
                    (short)ErrorCodes.DataObjectNotSupported,
                    "WITSML object type not supported: " + request.WMLtypeIn + "; Version: " + version);
            }
            catch (WitsmlException ex)
            {
                return new WMLS_AddToStoreResponse((short)ex.ErrorCode, ex.Message);
            }
        }

        public WMLS_UpdateInStoreResponse WMLS_UpdateInStore(WMLS_UpdateInStoreRequest request)
        {
            var version = ObjectTypes.GetVersion(request.XMLin);

            try
            {
                _log.DebugFormat("Type: {0}; Options: {1}; XML:{3}{2}{3}", request.WMLtypeIn, request.OptionsIn, request.XMLin, Environment.NewLine);

                ValidateObjectType(request.WMLtypeIn);
                ValidateInputTemplate(request.XMLin);

                var dataWriter = Container.Resolve<IWitsmlDataWriter>(new ObjectName(request.WMLtypeIn, version));
                var result = dataWriter.UpdateInStore(request.WMLtypeIn, request.XMLin, request.OptionsIn, request.CapabilitiesIn);

                return new WMLS_UpdateInStoreResponse((short)result.Code, result.Message);
            }
            catch (ContainerException)
            {
                return new WMLS_UpdateInStoreResponse(
                    (short)ErrorCodes.DataObjectNotSupported,
                    "WITSML object type not supported: " + request.WMLtypeIn + "; Version: " + version);
            }
            catch (WitsmlException ex)
            {
                return new WMLS_UpdateInStoreResponse((short)ex.ErrorCode, ex.Message);
            }
        }

        public WMLS_DeleteFromStoreResponse WMLS_DeleteFromStore(WMLS_DeleteFromStoreRequest request)
        {
            var version = ObjectTypes.GetVersion(request.QueryIn);

            try
            {
                _log.DebugFormat("Type: {0}; Options: {1}; Query:{3}{2}{3}", request.WMLtypeIn, request.OptionsIn, request.QueryIn, Environment.NewLine);

                ValidateObjectType(request.WMLtypeIn);
                ValidateInputTemplate(request.QueryIn);

                var dataWriter = Container.Resolve<IWitsmlDataWriter>(new ObjectName(request.WMLtypeIn, version));
                var result = dataWriter.DeleteFromStore(request.WMLtypeIn, request.QueryIn, request.OptionsIn, request.CapabilitiesIn);

                return new WMLS_DeleteFromStoreResponse((short)result.Code, result.Message);
            }
            catch (ContainerException)
            {
                return new WMLS_DeleteFromStoreResponse(
                    (short)ErrorCodes.DataObjectNotSupported,
                    "WITSML object type not supported: " + request.WMLtypeIn + "; Version: " + version);
            }
            catch (WitsmlException ex)
            {
                return new WMLS_DeleteFromStoreResponse((short)ex.ErrorCode, ex.Message);
            }
        }

        /// <summary>
        /// Returns a string containing only the fixed (base) message text associated with a defined Return Value.
        /// </summary>
        /// <param name="request">The request object encapsulating the method input parameters.</param>
        /// <returns>The fixed descriptive message text associated with the Return Value.</returns>
        public WMLS_GetBaseMsgResponse WMLS_GetBaseMsg(WMLS_GetBaseMsgRequest request)
        {
            var message = string.Empty;

            if (Enum.IsDefined(typeof(ErrorCodes), request.ReturnValueIn))
            {
                var errorCode = (ErrorCodes)request.ReturnValueIn;
                message = errorCode.GetDescription();

                _log.DebugFormat("{0} - {1}", request.ReturnValueIn, message);
            }
            else
            {
                _log.Warn("Unknown WITSML error code: " + request.ReturnValueIn);
                message = null;
            }

            return new WMLS_GetBaseMsgResponse(message);
        }

        /// <summary>
        /// Validates the required WITSML object type parameter for the WMLS_AddToStore method.
        /// </summary>
        /// <param name="version">The data schema version.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="xmlType">Type of the object in the XML.</param>
        /// <exception cref="WitsmlException"></exception>
        private void ValidateObjectType(string version, string objectType, string xmlType)
        {
            EnsureCapServerProviders();
            ValidateObjectType(objectType);

            if (!objectType.Equals(xmlType))
            {
                throw new WitsmlException(ErrorCodes.DataObjectTypesDontMatch);
            }

            if (_capServer.ContainsKey(version) && !_capServer[version].IsSupported(Functions.AddToStore, objectType))
            {
                throw new WitsmlException(ErrorCodes.DataObjectTypeNotSupported);
            }
        }

        /// <summary>
        /// Validates the required WITSML object type parameter.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <exception cref="WitsmlException"></exception>
        private void ValidateObjectType(string objectType)
        {
            if (string.IsNullOrWhiteSpace(objectType))
            {
                throw new WitsmlException(ErrorCodes.MissingWMLtypeIn);
            }
        }

        /// <summary>
        /// Validates the required WITSML input template.
        /// </summary>
        /// <param name="xml">The XML input template.</param>
        /// <exception cref="WitsmlException"></exception>
        private void ValidateInputTemplate(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new WitsmlException(ErrorCodes.MissingInputTemplate);
            }
        }

        /// <summary>
        /// Ensures the <see cref="ICapServerProvider"/>s are loaded.
        /// </summary>
        private void EnsureCapServerProviders()
        {
            if (_capServer.Any())
                return;

            var providers = Container.ResolveAll<ICapServerProvider>();

            foreach (var provider in providers)
            {
                var capServerXml = provider.ToXml();

                if (!string.IsNullOrWhiteSpace(capServerXml))
                {
                    _capServer[provider.DataSchemaVersion] = provider;
                }
            }

            var versions = _capServer.Keys.ToList();
            versions.Sort();

            _supportedVersions = string.Join(",", versions);
        }
    }
}
