using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;
using Energistics;
using PDS.Witsml.Web.Properties;

namespace PDS.Witsml.Web.Controllers
{
    /// <summary>
    /// Defines the Web API method used to initiate an ETP Web Socket connection.
    /// </summary>
    /// <seealso cref="System.Web.Http.ApiController" />
    public class EtpController : ApiController
    {
        private static readonly string EtpSocketServerName = Settings.Default.EtpSocketServerName;

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
            }

            return Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
        }

        private async Task AcceptWebSocketRequest(AspNetWebSocketContext context)
        {
            var socket = context.WebSocket as AspNetWebSocket;
            var handler = new EtpServerHandler(socket, EtpSocketServerName);

            //handler.Register<IDiscoveryStore, DiscoveryStoreHandler>();

            await handler.Accept(context);
        }
    }
}
