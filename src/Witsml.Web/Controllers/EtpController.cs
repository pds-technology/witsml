using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.WebSockets;
using Energistics;
using Energistics.Datatypes;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Core;
using Energistics.Protocol.Discovery;
using Energistics.Protocol.Store;
using PDS.Framework;
using PDS.Witsml.Server.Properties;
using PDS.Witsml.Server.Providers.ChannelStreaming;

namespace PDS.Witsml.Web.Controllers
{
    /// <summary>
    /// Defines the Web API method used to initiate an ETP Web Socket connection.
    /// </summary>
    /// <seealso cref="System.Web.Http.ApiController" />
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class EtpController : ApiController
    {
        private static readonly string DefaultServerName = Settings.Default.DefaultServerName;
        private static readonly string DefaultServerVersion = Settings.Default.DefaultServerVersion;
        private readonly IContainer _container;

        [ImportingConstructor]
        public EtpController(IContainer container)
        {
            _container = container;
        }

        // GET: api/etp
        public HttpResponseMessage Get()
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

        // GET: api/etp/ServerCapabilities
        [Route("api/etp/ServerCapabilities")]
        [ResponseType(typeof(ServerCapabilities))]
        public IHttpActionResult GetServerCapabilities()
        {
            var handler = CreateEtpServerHandler(null);

            var capServer = new ServerCapabilities()
            {
                ApplicationName = handler.ApplicationName,
                ApplicationVersion = handler.ApplicationVersion,
                SupportedProtocols = handler.GetSupportedProtocols(),
                SessionId = Guid.NewGuid().ToString(),
                SupportedObjects = new string[0],
                ContactInfomration = new Contact()
                {
                    OrganizationName = Settings.Default.DefaultVendorName,
                    ContactName = Settings.Default.DefaultContactName,
                    ContactEmail = Settings.Default.DefaultContactEmail,
                    ContactPhone = Settings.Default.DefaultContactPhone
                }
            };

            return Ok(capServer);
        }

        // GET: api/etp/Clients
        [Route("api/etp/Clients")]
        public IHttpActionResult GetClients()
        {
            var clients = EtpServerHandler.Clients.Select(c =>
            {
                var handler = c.Value;
                var core = handler.Handler<ICoreServer>() as CoreServerHandler;

                return new
                {
                    handler.SessionId,
                    core.ClientApplicationName,
                    core.RequestedProtocols
                };
            });

            return Ok(clients);
        }

        private async Task AcceptWebSocketRequest(AspNetWebSocketContext context)
        {
            var handler = CreateEtpServerHandler(context.WebSocket);
            await handler.Accept(context);
        }

        private EtpServerHandler CreateEtpServerHandler(WebSocket socket)
        {
            var handler = new EtpServerHandler(socket, DefaultServerName, DefaultServerVersion);
            var aggregator = _container.Resolve<ChannelStreamingAggregator>();

            handler.Register(() => _container.Resolve<ICoreServer>());
            handler.Register<IChannelStreamingProducer>(() => aggregator);
            handler.Register<IChannelStreamingConsumer>(() => aggregator);
            handler.Register(() => _container.Resolve<IDiscoveryStore>());
            handler.Register(() => _container.Resolve<IStoreStore>());

            return handler;
        }
    }
}
