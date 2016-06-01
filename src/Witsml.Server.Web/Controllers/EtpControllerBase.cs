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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;
using Energistics;
using Energistics.Datatypes;
using Energistics.Protocol.Core;
using PDS.Framework;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Controllers
{
    /// <summary>
    /// Defines the Web API methods used to initiate an ETP Web Socket connection.
    /// </summary>
    /// <seealso cref="System.Web.Http.ApiController" />
    public abstract class EtpControllerBase : ApiController
    {
        private static readonly string DefaultServerName = WitsmlSettings.DefaultServerName;
        private static readonly string DefaultServerVersion = WitsmlSettings.DefaultServerVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpControllerBase"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        protected EtpControllerBase(IContainer container)
        {
            Container = container;
            DataAdapters = new List<IEtpDataProvider>();
        }

        public IContainer Container { get; }

        [ImportMany]
        public List<IEtpDataProvider> DataAdapters { get; set; }

        protected HttpResponseMessage UpgradeRequest()
        {
            var context = HttpContext.Current;

            if (context.IsWebSocketRequest || context.IsWebSocketRequestUpgrading)
            {
                context.AcceptWebSocketRequest(AcceptWebSocketRequest, new AspNetWebSocketOptions()
                {
                    SubProtocol = Energistics.Properties.Settings.Default.EtpSubProtocolName
                });

                return Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
            }

            return Request.CreateResponse(
                HttpStatusCode.UpgradeRequired,
                new { error = "Invalid web socket request" });
        }

        protected IHttpActionResult ServerCapabilities()
        {
            var handler = CreateEtpServerHandler(null, null);
            var supportedObjects = GetSupportedObjects();

            var capServer = new ServerCapabilities()
            {
                ApplicationName = handler.ApplicationName,
                ApplicationVersion = handler.ApplicationVersion,
                SupportedProtocols = handler.GetSupportedProtocols(),
                SessionId = Guid.NewGuid().ToString(),
                SupportedObjects = supportedObjects,
                ContactInfomration = new Contact()
                {
                    OrganizationName = WitsmlSettings.DefaultVendorName,
                    ContactName = WitsmlSettings.DefaultContactName,
                    ContactEmail = WitsmlSettings.DefaultContactEmail,
                    ContactPhone = WitsmlSettings.DefaultContactPhone
                }
            };

            return Ok(capServer);
        }

        protected IHttpActionResult ClientList()
        {
            var clients = EtpServerHandler.Clients.Select(c =>
            {
                var handler = c.Value;
                var core = handler.Handler<ICoreServer>() as CoreServerHandler;

                return new
                {
                    handler.SessionId,
                    core?.ClientApplicationName,
                    core?.RequestedProtocols
                };
            });

            return Ok(clients);
        }

        protected virtual void RegisterProtocolHandlers(EtpServerHandler handler)
        {
        }

        private EtpServerHandler CreateEtpServerHandler(WebSocket socket, IDictionary<string, string> headers)
        {
            var handler = new EtpServerHandler(socket, DefaultServerName, DefaultServerVersion, headers);
            RegisterProtocolHandlers(handler);
            return handler;
        }

        private async Task AcceptWebSocketRequest(AspNetWebSocketContext context)
        {
            var headers = GetWebSocketHeaders(context.Headers, context.QueryString);
            var handler = CreateEtpServerHandler(context.WebSocket, headers);
            handler.SupportedObjects = GetSupportedObjects();
            await handler.Accept(context);
        }

        private IDictionary<string, string> GetWebSocketHeaders(NameValueCollection headers, NameValueCollection queryString)
        {
            var combined = new Dictionary<string, string>();

            foreach (var key in headers.AllKeys)
                combined[key] = headers[key];

            foreach (var key in queryString.AllKeys)
                combined[key] = queryString[key];

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
