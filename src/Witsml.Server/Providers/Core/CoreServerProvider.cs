using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.Datatypes;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Core;

namespace PDS.Witsml.Server.Providers.Core
{
    [Export(typeof(ICoreServer))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CoreServerProvider : CoreServerHandler
    {
        protected override void HandleRequestSession(MessageHeader header, RequestSession requestSession)
        {
            // The base implementation send the OpenSession message
            base.HandleRequestSession(header, requestSession);

            // Is the client requesting the ChannelStreaming consumer role
            var isClientProducer = HasConsumerRole(RequestedProtocols);

            // Does the server support the ChannelStreaming consumer role
            var isServerConsumer = HasConsumerRole(Session.GetSupportedProtocols());

            if (isClientProducer && isServerConsumer)
            {
                // Start a ChannelStreaming session
                Session.Handler<IChannelStreamingConsumer>().Start(maxMessageRate: 5000);
            }
        }

        private bool HasConsumerRole(IList<SupportedProtocol> protocols)
        {
            return protocols.Any(x => x.Protocol == (int)Protocols.ChannelStreaming && x.Role.Contains("consumer"));
        }
    }
}
