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
    [Export(typeof(IWitsmlStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class WitsmlStore : IWitsmlStore
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlStore));
        private static readonly string DefaultDataSchemaVersion = Settings.Default.DefaultDataSchemaVersion;

        private readonly IDictionary<string, string> _capServerXml;
        private string _supportedVersions;

        public WitsmlStore()
        {
            _capServerXml = new Dictionary<string, string>();
        }

        [Import]
        public IContainer Container { get; set; }

        public WMLS_GetVersionResponse WMLS_GetVersion(WMLS_GetVersionRequest request)
        {
            EnsureCapServerXml();

            _log.DebugFormat("Supported Versions: {0}", _supportedVersions);

            return new WMLS_GetVersionResponse(_supportedVersions);
        }

        public WMLS_GetCapResponse WMLS_GetCap(WMLS_GetCapRequest request)
        {
            _log.DebugFormat("Options: {0}", request.OptionsIn);

            EnsureCapServerXml();

            var options = OptionsIn.Parse(request.OptionsIn);
            var version = OptionsIn.GetValue(options, new OptionsIn.DataVersion(DefaultDataSchemaVersion));

            // return error if WITSML 1.3.1 not supported AND dataVersion not specified (required in WITSML 1.4.1)
            if (!_capServerXml.ContainsKey(OptionsIn.DataVersion.Version131.Value) && !options.ContainsKey(OptionsIn.DataVersion.Keyword))
            {
                return new WMLS_GetCapResponse(
                    (short)ErrorCodes.MissingDataVersion,
                    string.Empty,
                    ErrorCodes.MissingDataVersion.GetDescription());
            }

            if (_capServerXml.ContainsKey(version))
            {
                return new WMLS_GetCapResponse(
                    (short)ErrorCodes.Success,
                    _capServerXml[version],
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
        }

        /// <summary>
        /// WITSML store service method for adding one WITSML data-object to the server
        /// </summary>
        /// <param name="request">An object that encapsulates parameters for AddToStore WITSML request</param>
        /// <returns>A WITSML response that includes return code and/or message</returns>
        public WMLS_AddToStoreResponse WMLS_AddToStore(WMLS_AddToStoreRequest request)
        {
            var version = ObjectTypes.GetVersion(request.XMLin);

            try
            {
                _log.DebugFormat("Type: {0}; Options: {1}; XML:{3}{2}{3}", request.WMLtypeIn, request.OptionsIn, request.XMLin, Environment.NewLine);

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
        }

        public WMLS_UpdateInStoreResponse WMLS_UpdateInStore(WMLS_UpdateInStoreRequest request)
        {
            var version = ObjectTypes.GetVersion(request.XMLin);

            try
            {
                _log.DebugFormat("Type: {0}; Options: {1}; XML:{3}{2}{3}", request.WMLtypeIn, request.OptionsIn, request.XMLin, Environment.NewLine);

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
        }

        public WMLS_DeleteFromStoreResponse WMLS_DeleteFromStore(WMLS_DeleteFromStoreRequest request)
        {
            var version = ObjectTypes.GetVersion(request.QueryIn);

            try
            {
                _log.DebugFormat("Type: {0}; Options: {1}; Query:{3}{2}{3}", request.WMLtypeIn, request.OptionsIn, request.QueryIn, Environment.NewLine);

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
        }

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
                message = "Unknown WITSML error code: " + request.ReturnValueIn;
                _log.Warn(message);
            }

            return new WMLS_GetBaseMsgResponse(message);
        }

        private void EnsureCapServerXml()
        {
            if (_capServerXml.Any())
                return;

            var providers = Container.ResolveAll<ICapServerProvider>();

            foreach (var provider in providers)
            {
                var capServerXml = provider.ToXml();

                if (!string.IsNullOrWhiteSpace(capServerXml))
                {
                    _capServerXml[provider.DataSchemaVersion] = capServerXml;
                }
            }

            var versions = _capServerXml.Keys.ToList();
            versions.Sort();

            _supportedVersions = string.Join(",", versions);
        }
    }
}
