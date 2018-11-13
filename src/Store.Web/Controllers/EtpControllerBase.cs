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

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data;
using PDS.WITSMLstudio.Store.Providers;

namespace PDS.WITSMLstudio.Store.Controllers
{
    /// <summary>
    /// Defines the Web API methods used to initiate an ETP Web Socket connection.
    /// </summary>
    /// <seealso cref="System.Web.Http.ApiController" />
    public abstract class EtpControllerBase : ApiController
    {
        private static readonly string _defaultServerName = WitsmlSettings.DefaultServerName;
        private static readonly string _overrideServerVersion = WitsmlSettings.OverrideServerVersion;
        private static readonly string[] _supportedEncodings = { "binary", "JSON" };

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpControllerBase"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        protected EtpControllerBase(IContainer container)
        {
            Container = container;
            DataAdapters = new List<IEtpDataProvider>();
        }

        /// <summary>
        /// Gets the composition container.
        /// </summary>
        /// <value>The composition container.</value>
        public IContainer Container { get; }

        /// <summary>
        /// Gets or sets the list of data adapters.
        /// </summary>
        /// <value>The list of data adapters.</value>
        [ImportMany(AllowRecomposition = true)]
        public List<IEtpDataProvider> DataAdapters { get; set; }

        /// <summary>
        /// Gets the list of supported ETP versions.
        /// </summary>
        /// <returns>A list of supported ETP versions.</returns>
        protected virtual IList<string> GetEtpVersions()
        {
            return new[] { EtpSettings.Etp11SubProtocol, EtpSettings.Etp12SubProtocol };
        }

        /// <summary>
        /// Gets the server's capabilities.
        /// </summary>
        /// <returns>A <see cref="ServerCapabilities"/> object.</returns>
        protected virtual IHttpActionResult ServerCapabilities()
        {
            var parameters = HttpContext.Current?.Request.QueryString;

            if (parameters?[EtpSettings.GetVersionsHeader].EqualsIgnoreCase(bool.TrueString) ?? false)
            {
                return Ok(GetEtpVersions());
            }

            var etpSubProtocol = GetRequestedEtpSubProtocol(parameters);
            var buffer = WebSocket.CreateClientBuffer(ushort.MaxValue, ushort.MaxValue);

            using (var stream = new MemoryStream())
            using (var webSocket = WebSocket.CreateClientWebSocket(stream, etpSubProtocol, ushort.MaxValue, ushort.MaxValue, WebSocket.DefaultKeepAliveInterval, false, buffer))
            using (var etpServer = CreateEtpServer(webSocket, null))
            {
                var supportedObjects = GetSupportedObjects();
                var capServer = etpServer.CreateServerCapabilities(supportedObjects, _supportedEncodings);

                return Ok(capServer);
            }
        }

        /// <summary>
        /// Get the list of client Web Socket connections.
        /// </summary>
        /// <returns>An <see cref="IHttpActionResult"/> containing the list of clients.</returns>
        protected virtual IHttpActionResult ClientList()
        {
            var clients = Energistics.Etp.Native.EtpServer.Clients.Select(c =>
            {
                var handler = c.Value;
                //var core = handler.Handler<ICoreServer>() as CoreServerHandler;

                return new
                {
                    handler.SessionId,
                    //core?.ClientApplicationName,
                    //core?.RequestedProtocols
                };
            });

            return Ok(clients);
        }

        /// <summary>
        /// Upgrades the HTTP request to a Web Socket request.
        /// </summary>
        /// <returns>An <see cref="HttpResponseMessage"/> with the appropriate status code.</returns>
        protected HttpResponseMessage UpgradeRequest()
        {
            var context = HttpContext.Current;

            // Verify web socket handshake
            if (!context.IsWebSocketRequest && !context.IsWebSocketRequestUpgrading)
            {
                return Request.CreateResponse(
                    HttpStatusCode.UpgradeRequired,
                    new { error = "Invalid web socket request" });
            }

            var options = CreateWebSocketOptions(context.WebSocketRequestedProtocols);

            // Validate web socket protocol matched energistics-tp
            if (options == null)
            {
                return Request.CreateResponse(
                    HttpStatusCode.BadRequest,
                    new { error = "Invalid web socket protocol" });
            }

            var headers = GetWebSocketHeaders(context.Request.Headers, context.Request.QueryString);
            string encoding;

            // Validate etp-encoding header is either binary, json or not specified
            if (headers.TryGetValue(EtpSettings.EtpEncodingHeader, out encoding) &&
                !string.IsNullOrWhiteSpace(encoding) &&
                !_supportedEncodings.ContainsIgnoreCase(encoding))
            {
                return Request.CreateResponse(
                    HttpStatusCode.PreconditionFailed,
                    new { error = "Invalid etp-encoding header" });
            }

            // Accept WebSocket request
            context.AcceptWebSocketRequest(AcceptWebSocketRequest, options);

            // Update response headers
            var response = Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
            UpdateHandshakeResponse(response);

            return response;
        }

        /// <summary>
        /// Creates the web socket options for the requested protocols.
        /// </summary>
        /// <param name="requestedProtocols">The requested protocols.</param>
        /// <returns>A new <see cref="AspNetWebSocketOptions"/> instance.</returns>
        protected virtual AspNetWebSocketOptions CreateWebSocketOptions(IList<string> requestedProtocols)
        {
            var preferredProtocol = requestedProtocols
                .FirstOrDefault(protocol => EtpSettings.EtpSubProtocols.ContainsIgnoreCase(protocol));

            if (string.IsNullOrWhiteSpace(preferredProtocol))
                return null;

            return new AspNetWebSocketOptions
            {
                SubProtocol = preferredProtocol
            };
        }

        /// <summary>
        /// Gets the requested ETP sub protocol.
        /// </summary>
        /// <param name="parameters">The query string parameters.</param>
        /// <returns>The requested ETP sub protocol.</returns>
        protected virtual string GetRequestedEtpSubProtocol(NameValueCollection parameters)
        {
            var etpVersion = parameters?[EtpSettings.GetVersionHeader] ?? string.Empty;
            return EtpSettings.EtpSubProtocols.Contains(etpVersion) ? etpVersion : EtpSettings.Etp11SubProtocol;
        }

        /// <summary>
        /// Accepts the web socket request.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        protected virtual async Task AcceptWebSocketRequest(AspNetWebSocketContext context)
        {
            var headers = GetWebSocketHeaders(context.Headers, context.QueryString);
            using (var etpServer = CreateEtpServer(context.WebSocket, headers))
            {
                etpServer.SupportedObjects = GetSupportedObjects();
                await etpServer.HandleConnection(CancellationToken.None);
            }
        }

        /// <summary>
        /// Creates the ETP server handler.
        /// </summary>
        /// <param name="socket">The WebSocket.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        protected virtual Energistics.Etp.Native.EtpServer CreateEtpServer(WebSocket socket, IDictionary<string, string> headers)
        {
            var etpServer = new Energistics.Etp.Native.EtpServer(socket, _defaultServerName, _overrideServerVersion, headers);
            RegisterProtocolHandlers(etpServer);
            return etpServer;
        }

        /// <summary>
        /// Registers the protocol handlers supported by the specified <see cref="IEtpServer" />.
        /// </summary>
        /// <param name="etpServer">The ETP server.</param>
        protected virtual void RegisterProtocolHandlers(IEtpServer etpServer)
        {
        }

        /// <summary>
        /// Updates the WebSocket handshake response.
        /// </summary>
        /// <param name="response">The response.</param>
        protected virtual void UpdateHandshakeResponse(HttpResponseMessage response)
        {
            response.Headers.Server.TryParseAdd(WitsmlSettings.DefaultServerName);
            response.ReasonPhrase = "Web Socket Protocol Handshake";

            response.Headers.Add("Access-Control-Allow-Headers", new[]
            {
                "content-type",
                "authorization",
                "x-websocket-extensions",
                "x-websocket-version",
                "x-websocket-protocol"
            });
        }

        private IDictionary<string, string> GetWebSocketHeaders(NameValueCollection headers, NameValueCollection queryString)
        {
            var combined = new Dictionary<string, string>();

            foreach (var key in queryString.AllKeys)
                if (!string.IsNullOrWhiteSpace(key))
                    combined[key] = queryString[key];

            foreach (var key in headers.AllKeys)
                if (!string.IsNullOrWhiteSpace(key))
                    combined[key] = headers[key];

            return combined;
        }

        private List<string> GetSupportedObjects()
        {
            var contentTypes = new List<EtpContentType>();

            DataAdapters.ForEach(x => x.GetSupportedObjects(contentTypes));

            return contentTypes
                .Select(x => x.ToString())
                .OrderBy(x => x)
                .ToList();
        }
    }
}
