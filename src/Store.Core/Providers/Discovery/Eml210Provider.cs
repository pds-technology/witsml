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
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.Object;
using Etp11 = Energistics.Etp.v11;
using Etp12 = Energistics.Etp.v12;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Data;

namespace PDS.WITSMLstudio.Store.Providers.Discovery
{
    /// <summary>
    /// Provides information about resources available in a WITSML store for Common version 2.1.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Providers.Discovery.IDiscoveryStoreProvider" />
    [Export(typeof(IDiscoveryStoreProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Eml210Provider : IDiscoveryStoreProvider
    {
        private readonly IContainer _container;
        private IList<EtpContentType> _contentTypes;
        private readonly object _contentTypesLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="Eml210Provider" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        [ImportingConstructor]
        public Eml210Provider(IContainer container)
        {
            _container = container;
            _contentTypes = new List<EtpContentType>();
        }

        /// <summary>
        /// Gets the data schema version supported by the provider.
        /// </summary>
        /// <value>The data schema version.</value>
        public string DataSchemaVersion => OptionsIn.DataVersion.Version200.Value;

        /// <summary>
        /// Gets or sets the collection of <see cref="IEtpDataProvider"/> providers.
        /// </summary>
        /// <value>The collection of providers.</value>
        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<IEtpDataProvider> Providers { get; set; }

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
        /// <param name="args">The <see cref="ProtocolEventArgs{GetTreeResources, IList}"/> instance containing the event data.</param>
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

            if (EtpUris.IsRootUri(uri))
            {
                var childCount = CreateFoldersByObjectType(etpAdapter, EtpUris.Eml210).Count;
                resources.Add(etpAdapter.NewProtocol(EtpUris.Eml210, "EML Common (2.1)", childCount));
                return;
            }

            var etpUri = new EtpUri(uri);
            var parentUri = etpUri.Parent;

            // Append query string, if any
            if (!string.IsNullOrWhiteSpace(etpUri.Query))
                parentUri = new EtpUri(parentUri + etpUri.Query);

            if (!etpUri.IsRelatedTo(EtpUris.Eml210))
            {
                return;
            }
            if (etpUri.IsBaseUri)
            {
                CreateFoldersByObjectType(etpAdapter, etpUri)
                    .ForEach(resources.Add);
            }
            else if (string.IsNullOrWhiteSpace(etpUri.ObjectId))
            {
                var objectType = ObjectTypes.GetObjectType(etpUri.ObjectType, etpUri.Family, etpUri.Version);
                var contentType = EtpContentTypes.GetContentType(objectType);
                var hasChildren = contentType.IsRelatedTo(EtpContentTypes.Eml210) ? 0 : -1;
                var dataProvider = GetDataProvider(etpUri.ObjectType);
                serverSortOrder = dataProvider.ServerSortOrder;

                dataProvider
                    .GetAll(parentUri)
                    .Cast<AbstractObject>()
                    .ForEach(x => resources.Add(ToResource(etpAdapter, x, parentUri, hasChildren)));
            }
            //else
            //{
            //    var propertyName = uri.ObjectType.ToPascalCase();
            //
            //    CreateFoldersByObjectType(uri, propertyName)
            //        .ForEach(resources.Add);
            //}
        }

        private IList<IResource> CreateFoldersByObjectType(IEtpAdapter etpAdapter, EtpUri uri, string propertyName = null, string additionalObjectType = null, int childCount = 0)
        {
            if (!_contentTypes.Any())
            {
                lock (_contentTypesLock)
                {
                    if (!_contentTypes.Any())
                    {
                        var contentTypes = new List<EtpContentType>();
                        Providers.ForEach(x => x.GetSupportedObjects(contentTypes));

                        _contentTypes = contentTypes
                            .Where(x => x.IsRelatedTo(EtpContentTypes.Eml210))
                            .OrderBy(x => x.ObjectType)
                            .ToList();
                    }
                }
            }

            return _contentTypes
                .Select(x => new
                {
                    ContentType = x,
                    DataType = ObjectTypes.GetObjectType(x.ObjectType, x.Family, DataSchemaVersion)
                })
                //.Select(x => new
                //{
                //    x.ContentType,
                //    x.DataType,
                //    PropertyInfo = string.IsNullOrWhiteSpace(propertyName) ? null : x.DataType.GetProperty(propertyName),
                //    ReferenceInfo = x.DataType.GetProperties().FirstOrDefault(p => p.PropertyType == typeof(DataObjectReference))
                //})
                //.Where(x =>
                //{
                //    // Top level folders
                //    if (string.IsNullOrWhiteSpace(uri.ObjectId) || string.IsNullOrWhiteSpace(propertyName))
                //        return x.ContentType.IsRelatedTo(EtpContentTypes.Witsml200) || x.ReferenceInfo == null;
                //
                //    // Data object sub folders, e.g. Well and Wellbore
                //    return (x.ContentType.IsRelatedTo(EtpContentTypes.Eml210) && x.ReferenceInfo != null) ||
                //           x.PropertyInfo?.PropertyType == typeof(DataObjectReference) ||
                //           x.ContentType.ObjectType.EqualsIgnoreCase(additionalObjectType) ||
                //           ObjectTypes.IsDecoratorObject(x.ContentType.ObjectType);
                //})
                .Select(x =>
                {
                    var folderName = ObjectTypes.SingleToPlural(x.ContentType.ObjectType, false).ToPascalCase();
                    var dataProvider = GetDataProvider(x.ContentType.ObjectType);
                    var hasChildren = childCount;

                    // Query for child object count if this is not the specified "additionalObjectType"
                    if (!x.ContentType.ObjectType.EqualsIgnoreCase(additionalObjectType))
                        hasChildren = dataProvider.Count(uri);

                    return etpAdapter.NewFolder(uri, x.ContentType, folderName, hasChildren);
                })
                .ToList();
        }

        private IEtpDataProvider GetDataProvider(string objectType)
        {
            return _container.Resolve<IEtpDataProvider>(new ObjectName(objectType, ObjectFamilies.Witsml, DataSchemaVersion)); // TODO: Update this when EML is handled separately.
        }

        private IResource ToResource(IEtpAdapter etpAdapter, AbstractObject entity, EtpUri parentUri, int hasChildren = -1)
        {
            return etpAdapter.CreateResource(
                uuid: entity.Uuid,
                uri: entity.GetUri(parentUri),
                resourceType: ResourceTypes.DataObject,
                name: entity.Citation.Title,
                count: hasChildren,
                lastChanged: entity.GetLastChangedMicroseconds());
        }
    }
}
