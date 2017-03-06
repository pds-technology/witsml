//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2017.1
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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.Common;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Discovery;
using PDS.Framework;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data;

namespace PDS.Witsml.Server.Providers.Discovery
{
    /// <summary>
    /// Provides information about resources available in a WITSML store for version 1.4.1.1.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Providers.Discovery.IDiscoveryStoreProvider" />
    [Export(typeof(IDiscoveryStoreProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DiscoveryStore141Provider : IDiscoveryStoreProvider
    {
        private readonly IContainer _container;
        private readonly IEtpDataProvider<Well> _wellDataProvider;
        private readonly IEtpDataProvider<Wellbore> _wellboreDataProvider;
        private readonly IEtpDataProvider<Log> _logDataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryStore141Provider" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="wellDataProvider">The well data provider.</param>
        /// <param name="wellboreDataProvider">The wellbore data provider.</param>
        /// <param name="logDataProvider">The log data provider.</param>
        [ImportingConstructor]
        public DiscoveryStore141Provider(
            IContainer container,
            IEtpDataProvider<Well> wellDataProvider,
            IEtpDataProvider<Wellbore> wellboreDataProvider,
            IEtpDataProvider<Log> logDataProvider)
        {
            _container = container;
            _wellDataProvider = wellDataProvider;
            _wellboreDataProvider = wellboreDataProvider;
            _logDataProvider = logDataProvider;
        }

        /// <summary>
        /// Gets the data schema version supported by the provider.
        /// </summary>
        /// <value>The data schema version.</value>
        public string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version141.Value; }
        }

        /// <summary>
        /// Gets or sets the collection of <see cref="IWitsml141Configuration"/> providers.
        /// </summary>
        /// <value>The collection of providers.</value>
        [ImportMany]
        public IEnumerable<IWitsml141Configuration> Providers { get; set; }

        /// <summary>
        /// Gets a collection of resources associated to the specified URI.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetResources, IList}"/> instance containing the event data.</param>
        public void GetResources(ProtocolEventArgs<GetResources, IList<Resource>> args)
        {
            if (EtpUri.IsRoot(args.Message.Uri))
            {
                args.Context.Add(DiscoveryStoreProvider.NewProtocol(EtpUris.Witsml141, "WITSML Store (1.4.1.1)"));
                return;
            }

            var uri = new EtpUri(args.Message.Uri);
            var parentUri = uri.Parent;

            // Append query string, if any
            if (!string.IsNullOrWhiteSpace(uri.Query))
                parentUri = new EtpUri(parentUri + uri.Query);

            if (!uri.IsRelatedTo(EtpUris.Witsml141))
            {
                return;
            }
            if (uri.IsBaseUri || (string.IsNullOrWhiteSpace(uri.ObjectId) && ObjectTypes.Well.EqualsIgnoreCase(uri.ObjectType)))
            {
                _wellDataProvider.GetAll(uri)
                    .ForEach(x => args.Context.Add(ToResource(x)));
            }
            else if (string.IsNullOrWhiteSpace(uri.ObjectId) && ObjectTypes.Wellbore.EqualsIgnoreCase(parentUri.ObjectType))
            {
                var objectType = ObjectTypes.PluralToSingle(uri.ObjectType);
                var dataProvider = _container.Resolve<IEtpDataProvider>(new ObjectName(objectType, uri.Version));

                dataProvider
                    .GetAll(parentUri)
                    .Cast<IWellboreObject>()
                    .ForEach(x => args.Context.Add(ToResource(x)));
            }
            else if (ObjectTypes.Well.EqualsIgnoreCase(uri.ObjectType))
            {
                _wellboreDataProvider.GetAll(uri)
                    .ForEach(x => args.Context.Add(ToResource(x)));
            }
            else if (ObjectTypes.Wellbore.EqualsIgnoreCase(uri.ObjectType))
            {
                var wellboreObjectType = typeof (IWellboreObject);

                Providers
                    .OfType<IWitsmlDataAdapter>()
                    .Where(x => wellboreObjectType.IsAssignableFrom(x.DataObjectType))
                    .Select(x => ObjectTypes.GetObjectType(x.DataObjectType))
                    .OrderBy(x => x)
                    .ForEach(x => args.Context.Add(DiscoveryStoreProvider.NewFolder(uri, x, ObjectTypes.SingleToPlural(x, false))));
            }
            else if (ObjectTypes.Log.EqualsIgnoreCase(uri.ObjectType))
            {
                var log = _logDataProvider.Get(uri);
                log?.LogCurveInfo?.ForEach(x => args.Context.Add(ToResource(log, x)));
            }
        }

        private Resource ToResource(Well entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uid,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Name,
                count: -1);
        }

        private Resource ToResource(Wellbore entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uid,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Name,
                count: -1);
        }

        private Resource ToResource(IWellboreObject entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uid,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Name,
                count: entity is Log ? -1 : 0);
        }

        private Resource ToResource(Log log, LogCurveInfo curve)
        {
            return DiscoveryStoreProvider.New(
                uuid: curve.Uid,
                uri: curve.GetUri(log),
                resourceType: ResourceTypes.DataObject,
                name: curve.Mnemonic.Value);
        }
    }
}
