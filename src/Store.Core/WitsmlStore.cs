//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.ServiceModel.Web;
using Energistics.DataAccess;
using log4net;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data;
using PDS.WITSMLstudio.Store.Data.Security;
using PDS.WITSMLstudio.Store.Logging;
using PDS.WITSMLstudio.Store.Properties;

namespace PDS.WITSMLstudio.Store
{
    /// <summary>
    /// The WITSML Store API server implementation.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.IWitsmlStore" />
    [Export(typeof(IWitsmlStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class WitsmlStore : IWitsmlStore
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlStore));
        private static readonly string _defaultDataSchemaVersion = Settings.Default.DefaultDataSchemaVersion;

        private readonly IDictionary<string, ICapServerProvider> _capServerMap;
        private string _supportedVersions;

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlStore"/> class.
        /// </summary>
        public WitsmlStore()
        {
            _capServerMap = new Dictionary<string, ICapServerProvider>();
        }

        /// <summary>
        /// Gets or sets the composition container used for dependency injection.
        /// </summary>
        /// <value>The composition container.</value>
        [Import]
        public IContainer Container { get; set; }

        /// <summary>
        /// Gets or sets the user authorization provider.
        /// </summary>
        /// <value>The user authorization provider.</value>
        [Import]
        public IUserAuthorizationProvider UserAuthorizationProvider { get; set; }

        /// <summary>
        /// Gets or sets the cap server providers.
        /// </summary>
        /// <value>The cap server providers.</value>
        [ImportMany]
        public IEnumerable<ICapServerProvider> CapServerProviders { get; set; }

        /// <summary>
        /// Returns a string containing the Data Schema Version(s) that a server supports.
        /// </summary>
        /// <param name="request">The request object encapsulating the method input parameters.</param>
        /// <returns>A comma-separated list of Data Schema Versions (without spaces) that the server supports.</returns>
        public WMLS_GetVersionResponse WMLS_GetVersion(WMLS_GetVersionRequest request)
        {
            try
            {
                WitsmlOperationContext.Current.Request = request.ToContext();
                EnsureCapServerProviders();

                _log.Debug(WebOperationContext.Current.ToLogMessage());
                _log.Debug(request.ToLogMessage());

                UserAuthorizationProvider.CheckSoapAccess();

                var response = new WMLS_GetVersionResponse(_supportedVersions);
                _log.Debug(response.ToLogMessage());
                return response;
            }
            catch (WitsmlException ex)
            {
                var response = new WMLS_GetVersionResponse(ex.Message);
                _log.Error(response.ToLogMessage(_log.IsWarnEnabled));
                return response;
            }
        }

        /// <summary>
        /// Returns the capServer object that describes the capabilities of the server for one Data Schema Version.
        /// </summary>
        /// <param name="request">The request object encapsulating the method input parameters.</param>
        /// <returns>A positive value indicates a success; a negative value indicates an error.</returns>
        public WMLS_GetCapResponse WMLS_GetCap(WMLS_GetCapRequest request)
        {
            try
            {
                WitsmlOperationContext.Current.Request = request.ToContext();
                EnsureCapServerProviders();

                _log.Debug(WebOperationContext.Current.ToLogMessage());
                _log.Debug(request.ToLogMessage());

                UserAuthorizationProvider.CheckSoapAccess();

                var options = OptionsIn.Parse(request.OptionsIn);
                var version = OptionsIn.GetValue(options, new OptionsIn.DataVersion(_defaultDataSchemaVersion));

                // return error if WITSML 1.3.1 not supported AND dataVersion not specified (required in WITSML 1.4.1)
                if (!_capServerMap.ContainsKey(OptionsIn.DataVersion.Version131.Value) && !options.ContainsKey(OptionsIn.DataVersion.Keyword))
                {
                    throw new WitsmlException(ErrorCodes.MissingDataVersion);
                }

                if (_capServerMap.ContainsKey(version))
                {
                    var response = new WMLS_GetCapResponse((short)ErrorCodes.Success, _capServerMap[version].ToXml(), string.Empty);
                    _log.Debug(response.ToLogMessage());
                    return response;
                }

                throw new WitsmlException(ErrorCodes.DataVersionNotSupported, "Data schema version not supported: " + version);
            }
            catch (WitsmlException ex)
            {
                var response = new WMLS_GetCapResponse((short)ex.ErrorCode, string.Empty, ex.Message);
                _log.Error(response.ToLogMessage(_log.IsWarnEnabled));
                return response;
            }
        }

        /// <summary>
        /// Returns one or more WITSML data-objects from the server.
        /// </summary>
        /// <param name="request">The request object encapsulating the method input parameters.</param>
        /// <returns>
        /// A positive value indicating success along with one or more WITSML data-objects from the server, or a negative value indicating an error.
        /// </returns>
        public WMLS_GetFromStoreResponse WMLS_GetFromStore(WMLS_GetFromStoreRequest request)
        {
            var context = WitsmlOperationContext.Current.Request = request.ToContext();
            var version = string.Empty;

            try
            {
                _log.Debug(WebOperationContext.Current.ToLogMessage());
                _log.Debug(context);

                UserAuthorizationProvider.CheckSoapAccess();
                WitsmlValidator.ValidateRequest(CapServerProviders);
                version = WitsmlOperationContext.Current.DataSchemaVersion;

                var dataProvider = Container.Resolve<IWitsmlDataProvider>(new ObjectName(context.ObjectType, version));
                var result = dataProvider.GetFromStore(context);

                var response = new WMLS_GetFromStoreResponse(
                    (short)result.Code,
                    GetXmlOut(request, result.Results),
                    result.Message);

                _log.Debug(response.ToLogMessage());

                return response;
            }
            catch (ContainerException)
            {
                var response = new WMLS_GetFromStoreResponse((short)ErrorCodes.DataObjectNotSupported, string.Empty,
                    "WITSML object type not supported: " + context.ObjectType + "; Version: " + version);

                _log.Error(response.ToLogMessage(_log.IsWarnEnabled));

                return response;
            }
            catch (WitsmlException ex)
            {
                var errorCode = ex.ErrorCode.CorrectNonConformingErrorCodes(WitsmlOperationContext.Current.RequestCompressed);
                var response = new WMLS_GetFromStoreResponse((short)errorCode, string.Empty, ex.Message);
                _log.Error(response.ToLogMessage(_log.IsWarnEnabled));
                return response;
            }
        }

        /// <summary>
        /// Returns the response for adding one WITSML data-object to the server
        /// </summary>
        /// <param name="request">The request object encapsulating the method input parameters.</param>
        /// <returns>A positive value indicates a success; a negative value indicates an error.</returns>
        public WMLS_AddToStoreResponse WMLS_AddToStore(WMLS_AddToStoreRequest request)
        {
            var context = WitsmlOperationContext.Current.Request = request.ToContext();
            var version = string.Empty;

            try
            {
                _log.Debug(WebOperationContext.Current.ToLogMessage());
                _log.Debug(context);

                UserAuthorizationProvider.CheckSoapAccess();
                WitsmlValidator.ValidateRequest(CapServerProviders);
                version = WitsmlOperationContext.Current.DataSchemaVersion;

                var dataWriter = Container.Resolve<IWitsmlDataProvider>(new ObjectName(context.ObjectType, version));
                var result = dataWriter.AddToStore(context);

                var response = new WMLS_AddToStoreResponse((short)result.Code, result.Message);
                _log.Debug(response.ToLogMessage());
                return response;
            }
            catch (ContainerException)
            {
                var response = new WMLS_AddToStoreResponse((short)ErrorCodes.DataObjectNotSupported,
                    "WITSML object type not supported: " + context.ObjectType + "; Version: " + version);

                _log.Error(response.ToLogMessage(_log.IsWarnEnabled));

                return response;
            }
            catch (WitsmlException ex)
            {
                var errorCode = ex.ErrorCode.CorrectNonConformingErrorCodes(WitsmlOperationContext.Current.RequestCompressed);
                var response = new WMLS_AddToStoreResponse((short)errorCode, ex.Message);
                _log.Error(response.ToLogMessage(_log.IsWarnEnabled));
                return response;
            }
        }

        /// <summary>
        /// Returns the response for updating one WITSML data-object to the server.
        /// </summary>
        /// <param name="request">he request object encapsulating the method input parameters.</param>
        /// <returns>A positive value indicates a success; a negative value indicates an error.</returns>
        public WMLS_UpdateInStoreResponse WMLS_UpdateInStore(WMLS_UpdateInStoreRequest request)
        {
            var context = WitsmlOperationContext.Current.Request = request.ToContext();
            var version = string.Empty;

            try
            {
                _log.Debug(WebOperationContext.Current.ToLogMessage());
                _log.Debug(context);

                UserAuthorizationProvider.CheckSoapAccess();
                WitsmlValidator.ValidateRequest(CapServerProviders);
                version = WitsmlOperationContext.Current.DataSchemaVersion;

                var dataWriter = Container.Resolve<IWitsmlDataProvider>(new ObjectName(context.ObjectType, version));
                var result = dataWriter.UpdateInStore(context);

                var response = new WMLS_UpdateInStoreResponse((short)result.Code, result.Message);
                _log.Debug(response.ToLogMessage());
                return response;
            }
            catch (ContainerException)
            {
                var response = new WMLS_UpdateInStoreResponse((short)ErrorCodes.DataObjectNotSupported,
                    "WITSML object type not supported: " + context.ObjectType + "; Version: " + version);

                _log.Error(response.ToLogMessage(_log.IsWarnEnabled));

                return response;
            }
            catch (WitsmlException ex)
            {
                var errorCode = ex.ErrorCode.CorrectNonConformingErrorCodes(WitsmlOperationContext.Current.RequestCompressed);
                var response = new WMLS_UpdateInStoreResponse((short)errorCode, ex.Message);
                _log.Error(response.ToLogMessage(_log.IsWarnEnabled));
                return response;
            }
        }

        /// <summary>
        /// Returns the response for deleting one WITSML data-object to the server.
        /// </summary>
        /// <param name="request">he request object encapsulating the method input parameters.</param>
        /// <returns>A positive value indicates a success; a negative value indicates an error.</returns>
        public WMLS_DeleteFromStoreResponse WMLS_DeleteFromStore(WMLS_DeleteFromStoreRequest request)
        {
            var context = WitsmlOperationContext.Current.Request = request.ToContext();
            var version = string.Empty;

            try
            {
                _log.Debug(WebOperationContext.Current.ToLogMessage());
                _log.Debug(context);

                UserAuthorizationProvider.CheckSoapAccess();
                WitsmlValidator.ValidateRequest(CapServerProviders);
                version = WitsmlOperationContext.Current.DataSchemaVersion;

                var dataWriter = Container.Resolve<IWitsmlDataProvider>(new ObjectName(context.ObjectType, version));
                var result = dataWriter.DeleteFromStore(context);

                var response = new WMLS_DeleteFromStoreResponse((short)result.Code, result.Message);
                _log.Debug(response.ToLogMessage());
                return response;
            }
            catch (ContainerException)
            {
                var response = new WMLS_DeleteFromStoreResponse((short)ErrorCodes.DataObjectNotSupported,
                    "WITSML object type not supported: " + context.ObjectType + "; Version: " + version);

                _log.Error(response.ToLogMessage(_log.IsWarnEnabled));

                return response;
            }
            catch (WitsmlException ex)
            {
                var response = new WMLS_DeleteFromStoreResponse((short)ex.ErrorCode, ex.Message);
                _log.Error(response.ToLogMessage(_log.IsWarnEnabled));
                return response;
            }
        }

        /// <summary>
        /// Returns a string containing only the fixed (base) message text associated with a defined Return Value.
        /// </summary>
        /// <param name="request">The request object encapsulating the method input parameters.</param>
        /// <returns>The fixed descriptive message text associated with the Return Value.</returns>
        public WMLS_GetBaseMsgResponse WMLS_GetBaseMsg(WMLS_GetBaseMsgRequest request)
        {
            WitsmlOperationContext.Current.Request = request.ToContext();
            _log.Debug(WebOperationContext.Current.ToLogMessage());

            UserAuthorizationProvider.CheckSoapAccess();
            string message;

            if (request.ReturnValueIn == (short)ErrorCodes.Unset)
            {
                message = string.Format("Error {0}: {1}", (short)ErrorCodes.InvalidReturnValueIn, ErrorCodes.InvalidReturnValueIn.GetDescription());
                _log.DebugFormat("{0} - {1}", request.ReturnValueIn, message);
            }
            else if (Enum.IsDefined(typeof(ErrorCodes), request.ReturnValueIn))
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
        /// Converts a data object collection to XML and optionally converts to a requested version.
        /// </summary>
        /// <param name="request">The GetFromStore request.</param>
        /// <param name="collection">The data object collection.</param>
        /// <returns></returns>
        private string GetXmlOut(WMLS_GetFromStoreRequest request, IEnergisticsCollection collection)
        {
            if (collection == null) return string.Empty;
            EnsureCapServerProviders();

            var optionsIn = WitsmlOperationContext.Current.OptionsIn;
            string requestedVersion;

            // Attempt transformation if client requested a different version
            if (optionsIn.TryGetValue(OptionsIn.DataVersion.Keyword, out requestedVersion) &&
                _capServerMap.ContainsKey(requestedVersion) &&
                collection.GetVersion() != requestedVersion)
            {
                _log.Debug($"Transforming XMLOut to data schema version {requestedVersion}");
                collection = WitsmlParser.Transform(collection, requestedVersion);
            }

            return WitsmlParser.ToXml(collection);
        }

        /// <summary>
        /// Ensures the <see cref="ICapServerProvider"/>s are loaded.
        /// </summary>
        private void EnsureCapServerProviders()
        {
            if (_capServerMap.Any())
                return;

            foreach (var provider in CapServerProviders)
            {
                var capServerXml = provider.ToXml();

                if (!string.IsNullOrWhiteSpace(capServerXml))
                {
                    _capServerMap[provider.DataSchemaVersion] = provider;
                }
            }

            var versions = _capServerMap.Keys.ToList();
            versions.Sort();

            _supportedVersions = string.Join(",", versions);
        }
    }
}
