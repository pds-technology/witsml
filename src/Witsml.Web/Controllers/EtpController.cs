using System.ComponentModel.Composition;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;
using Energistics;
using Energistics.Protocol.Discovery;
using Energistics.Protocol.Store;
using PDS.Framework;
using PDS.Witsml.Web.Properties;

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
        private static readonly string EtpSocketServerName = Settings.Default.EtpSocketServerName;
        private static readonly string EtpSocketServerVersion = Settings.Default.EtpSocketServerVersion;
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

        private async Task AcceptWebSocketRequest(AspNetWebSocketContext context)
        {
            var socket = context.WebSocket as AspNetWebSocket;
            var handler = new EtpServerHandler(socket, EtpSocketServerName, EtpSocketServerVersion);

            handler.Register(() => _container.Resolve<IDiscoveryStore>());
            handler.Register(() => _container.Resolve<IStoreStore>());

            await handler.Accept(context);
        }
    }
}
