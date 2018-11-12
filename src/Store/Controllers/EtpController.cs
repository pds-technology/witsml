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
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Etp11 = Energistics.Etp.v11;
using Etp12 = Energistics.Etp.v12;
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
        [ResponseType(typeof(List<string>))]
        [ResponseType(typeof(Etp11.Datatypes.ServerCapabilities))]
        [ResponseType(typeof(Etp12.Datatypes.ServerCapabilities))]
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

        protected override void RegisterProtocolHandlers(IEtpServer etpServer)
        {
            if (etpServer.SupportedVersion == EtpVersion.v11)
            {
                etpServer.Register(() => Container.Resolve<Etp11.Protocol.ChannelStreaming.IChannelStreamingProducer>());
                etpServer.Register(() => Container.Resolve<Etp11.Protocol.ChannelStreaming.IChannelStreamingConsumer>());
                etpServer.Register(() => Container.Resolve<Etp11.Protocol.Discovery.IDiscoveryStore>());
                etpServer.Register(() => Container.Resolve<Etp11.Protocol.Store.IStoreStore>());
                etpServer.Register(() => Container.Resolve<Etp11.Protocol.StoreNotification.IStoreNotificationStore>());
                etpServer.Register(() => Container.Resolve<Etp11.Protocol.GrowingObject.IGrowingObjectStore>());
            }
            else
            {
                etpServer.Register(() => Container.Resolve<Etp12.Protocol.ChannelStreaming.IChannelStreamingProducer>());
                etpServer.Register(() => Container.Resolve<Etp12.Protocol.ChannelStreaming.IChannelStreamingConsumer>());
                //etpServer.Register(() => Container.Resolve<Etp12.Protocol.ChannelDataLoad.IChannelDataLoadConsumer>());
                etpServer.Register(() => Container.Resolve<Etp12.Protocol.Discovery.IDiscoveryStore>());
                etpServer.Register(() => Container.Resolve<Etp12.Protocol.Store.IStoreStore>());
                etpServer.Register(() => Container.Resolve<Etp12.Protocol.StoreNotification.IStoreNotificationStore>());
                etpServer.Register(() => Container.Resolve<Etp12.Protocol.GrowingObject.IGrowingObjectStore>());
                //etpServer.Register(() => Container.Resolve<Etp12.Protocol.GrowingObjectNotification.IGrowingObjectNotificationStore>());
                //etpServer.Register(() => Container.Resolve<Etp12.Protocol.GrowingObjectQuery.IGrowingObjectQueryStore>());
                etpServer.Register(() => Container.Resolve<Etp12.Protocol.DiscoveryQuery.IDiscoveryQueryStore>());
                etpServer.Register(() => Container.Resolve<Etp12.Protocol.StoreQuery.IStoreQueryStore>());
            }
        }
    }
}
