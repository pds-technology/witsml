//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using System.ComponentModel.Composition;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using Energistics;
using Energistics.Datatypes;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Discovery;
using Energistics.Protocol.Store;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Controllers
{
    /// <summary>
    /// Defines the Web API method used to initiate an ETP Web Socket connection.
    /// </summary>
    /// <seealso cref="System.Web.Http.ApiController" />
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class EtpController : EtpControllerBase
    {
        [ImportingConstructor]
        public EtpController(IContainer container) : base(container)
        {
        }

        // GET: api/etp
        public HttpResponseMessage Get()
        {
            return UpgradeRequest();
        }

        // GET: .well-known/etp-server-capabilities
        [Route(".well-known/etp-server-capabilities")]
        [Route("api/etp/.well-known/etp-server-capabilities")]
        [ResponseType(typeof(ServerCapabilities))]
        public IHttpActionResult GetServerCapabilities()
        {
            return ServerCapabilities();
        }

        // GET: api/etp/Clients
        [Route("api/etp/Clients")]
        public IHttpActionResult GetClients()
        {
            return ClientList();
        }

        protected override void RegisterProtocolHandlers(EtpServerHandler handler)
        {
            handler.Register(() => Container.Resolve<IChannelStreamingProducer>());
            handler.Register(() => Container.Resolve<IChannelStreamingConsumer>());
            handler.Register(() => Container.Resolve<IDiscoveryStore>());
            handler.Register(() => Container.Resolve<IStoreStore>());
        }
    }
}
