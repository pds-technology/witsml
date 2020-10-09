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
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.Object;
using Etp11 = Energistics.Etp.v11;
using Etp12 = Energistics.Etp.v12;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data;

namespace PDS.WITSMLstudio.Store.Providers.Discovery
{
    /// <summary>
    /// Provides information about resources available in a WITSML store for version 1.3.1.1.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Providers.Discovery.IDiscoveryStoreProvider" />
    [Export(typeof(IDiscoveryStoreProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Witsml131Provider : IDiscoveryStoreProvider
    {
        private readonly IContainer _container;
        private readonly IEtpDataProvider<Well> _wellDataProvider;
        private readonly IEtpDataProvider<Wellbore> _wellboreDataProvider;
        private readonly IEtpDataProvider<Log> _logDataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="Witsml131Provider" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="wellDataProvider">The well data provider.</param>
        /// <param name="wellboreDataProvider">The wellbore data provider.</param>
        /// <param name="logDataProvider">The log data provider.</param>
        [ImportingConstructor]
        public Witsml131Provider(
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
        public string DataSchemaVersion => OptionsIn.DataVersion.Version131.Value;

        /// <summary>
        /// Gets or sets the collection of <see cref="IWitsml131Configuration"/> providers.
        /// </summary>
        /// <value>The collection of providers.</value>
        [ImportMany]
        public IEnumerable<IWitsml131Configuration> Providers { get; set; }

        /// <summary>
        /// Gets a collection of resources associated to the specified URI.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetResources, IList}" /> instance containing the event data.</param>
        public void GetResources(IEtpAdapter etpAdapter, ProtocolEventArgs<Etp11.Protocol.Discovery.GetResources, IList<Etp11.Datatypes.Object.Resource>> args)
        {
            string serverSortOrder;
            GetResources(etpAdapter, args.Message.Uri, args.Context, out serverSortOrder);
        }

        /// <summary>
        /// Gets a collection of resources associated to the specified URI.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetTreeResources, IList}" /> instance containing the event data.</param>
        public void GetResources(IEtpAdapter etpAdapter, ProtocolEventArgs<Etp12.Protocol.Discovery.GetTreeResources, IList<Etp12.Datatypes.Object.Resource>> args)
        {
            string serverSortOrder;
            GetResources(etpAdapter, args.Message.Context.Uri, args.Context, out serverSortOrder);
        }

        /// <summary>
        /// Gets a collection of resources associated to the specified URI.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="args">The <see cref="ProtocolEventArgs{FindResources, IList}"/> instance containing the event data.</param>
        public void FindResources(IEtpAdapter etpAdapter, ProtocolEventArgs<Etp12.Protocol.DiscoveryQuery.FindResources, Etp12.Protocol.DiscoveryQuery.ResourceResponse> args)
        {
            var count = args.Context.Resources.Count;
            string serverSortOrder;

            GetResources(etpAdapter, args.Message.Uri, args.Context.Resources, out serverSortOrder);

            if (args.Context.Resources.Count > count)
                args.Context.ServerSortOrder = serverSortOrder;
        }

        private void GetResources<T>(IEtpAdapter etpAdapter, string uri, IList<T> resources, out string serverSortOrder) where T : IResource
        {
            // Default to Name in IResource
            serverSortOrder = ObjectTypes.NameProperty;

            var etpUri = new EtpUri(uri);
            var parentUri = etpUri.Parent;

            if (EtpUris.IsRootUri(uri))
            {
                resources.Add(etpAdapter.NewProtocol(EtpUris.Witsml131, "WITSML Store (1.3.1.1)", _wellDataProvider.Count(etpUri)));
                return;
            }

            // Append query string, if any
            if (!string.IsNullOrWhiteSpace(etpUri.Query))
                parentUri = new EtpUri(parentUri + etpUri.Query);

            if (!etpUri.IsRelatedTo(EtpUris.Witsml131) || !IsValidUri(etpUri))
            {
                return;
            }
            if (etpUri.IsBaseUri || (string.IsNullOrWhiteSpace(etpUri.ObjectId) && ObjectTypes.Well.EqualsIgnoreCase(etpUri.ObjectType)))
            {
                _wellDataProvider.GetAll(etpUri)
                    .ForEach(x => resources.Add(ToResource(etpAdapter, x)));

                serverSortOrder = _wellDataProvider.ServerSortOrder;
            }
            else if (string.IsNullOrWhiteSpace(etpUri.ObjectId) && ObjectTypes.Wellbore.EqualsIgnoreCase(parentUri.ObjectType))
            {
                var dataProvider = _container.Resolve<IEtpDataProvider>(new ObjectName(etpUri.ObjectType, etpUri.Family, etpUri.Version));
                serverSortOrder = dataProvider.ServerSortOrder;

                dataProvider
                    .GetAll(parentUri)
                    .Cast<IWellboreObject>()
                    .ForEach(x => resources.Add(ToResource(etpAdapter, x)));
            }
            else if (string.IsNullOrWhiteSpace(etpUri.ObjectId) && ObjectTypes.Wellbore.EqualsIgnoreCase(etpUri.ObjectType))
            {
                _wellboreDataProvider.GetAll(parentUri)
                    .ForEach(x => resources.Add(ToResource(etpAdapter, x)));

                serverSortOrder = _wellboreDataProvider.ServerSortOrder;
            }
            else if (ObjectTypes.Well.EqualsIgnoreCase(etpUri.ObjectType))
            {
                _wellboreDataProvider.GetAll(etpUri)
                    .ForEach(x => resources.Add(ToResource(etpAdapter, x)));

                serverSortOrder = _wellboreDataProvider.ServerSortOrder;
            }
            else if (ObjectTypes.Wellbore.EqualsIgnoreCase(etpUri.ObjectType))
            {
                foreach (var adapter in GetWellboreDataAdapters())
                {
                    var type = EtpContentTypes.GetContentType(adapter.DataObjectType);
                    var count = adapter.Count(etpUri);
                    resources.Add(etpAdapter.NewFolder(etpUri, type, type.ObjectType, count));
                }
            }
            else if (ObjectTypes.Log.EqualsIgnoreCase(etpUri.ObjectType))
            {
                var log = _logDataProvider.Get(etpUri);
                log?.LogCurveInfo?.ForEach(x => resources.Add(ToResource(etpAdapter, log, x)));
                serverSortOrder = string.Empty;
            }
        }

        private IEnumerable<IWitsmlDataAdapter> GetDataAdapters<TObject>()
        {
            var objectType = typeof(TObject);

            return Providers
                .OfType<IWitsmlDataAdapter>()
                .Where(x => objectType.IsAssignableFrom(x.DataObjectType))
                .OrderBy(x => x.GetType().Name);
        }

        private IEnumerable<IWitsmlDataAdapter> GetWellDataAdapters()
        {
            return GetDataAdapters<IWellObject>();
        }

        private IEnumerable<IWitsmlDataAdapter> GetWellboreDataAdapters()
        {
            return GetDataAdapters<IWellboreObject>();
        }

        private bool IsValidUri(EtpUri uri)
        {
            if (!uri.IsValid)
                return false;

            if (uri.IsRootUri || uri.IsBaseUri)
                return true;

            var wellboreObjectTypes = new HashSet<string>(GetWellboreDataAdapters().Select(x => ObjectTypes.GetObjectType(x.DataObjectType)), StringComparer.InvariantCultureIgnoreCase);
            var wellObjectTypes = new HashSet<string>(GetWellDataAdapters().Select(x => ObjectTypes.GetObjectType(x.DataObjectType)).Except(wellboreObjectTypes), StringComparer.InvariantCultureIgnoreCase);

            var objectType = uri.ObjectType;
            var objectIds = uri.GetObjectIds().ToList();
            var objectIdGroups = objectIds.GroupBy(x => x.ObjectType, StringComparer.InvariantCultureIgnoreCase);

            // check for repeating groups
            if (objectIdGroups.Any(x => x.Count() > 1))
                return false;

            if (wellObjectTypes.Contains(objectType))
            {
                return objectIds.Count >= 1 && ObjectTypes.Well.EqualsIgnoreCase(objectIds[0].ObjectType);
            }

            if (wellboreObjectTypes.Contains(objectType))
            {
                return objectIds.Count >= 2 && ObjectTypes.Well.EqualsIgnoreCase(objectIds[0].ObjectType) && ObjectTypes.Wellbore.EqualsIgnoreCase(objectIds[1].ObjectType);
            }

            return objectIds.Count == 1 && ObjectTypes.Well.EqualsIgnoreCase(objectIds[0].ObjectType);
        }

        private IResource ToResource(IEtpAdapter etpAdapter, Well entity)
        {
            return etpAdapter.CreateResource(
                uuid: null,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Name,
                count: _wellboreDataProvider.Count(entity.GetUri()),
                lastChanged: entity.GetLastChangedMicroseconds());
        }

        private IResource ToResource(IEtpAdapter etpAdapter, Wellbore entity)
        {
            return etpAdapter.CreateResource(
                uuid: null,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Name,
                count: GetWellboreDataAdapters().Count(),
                lastChanged: entity.GetLastChangedMicroseconds());
        }

        private IResource ToResource(IEtpAdapter etpAdapter, IWellboreObject entity)
        {
            return etpAdapter.CreateResource(
                uuid: null,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Name,
                count: (entity as Log)?.LogCurveInfo?.Count ?? 0,
                lastChanged: (entity as ICommonDataObject).GetLastChangedMicroseconds());
        }

        private IResource ToResource(IEtpAdapter etpAdapter, Log log, LogCurveInfo curve)
        {
            return etpAdapter.CreateResource(
                uuid: null,
                uri: curve.GetUri(log),
                resourceType: ResourceTypes.DataObject,
                name: curve.Mnemonic,
                lastChanged: log.GetLastChangedMicroseconds());
        }
    }
}
