//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.2
//
// Copyright 2017 PDS Americas LLC
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
using Energistics.Common;
using Energistics.DataAccess.WITSML200;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Discovery;
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
        private readonly IList<EtpContentType> _contentTypes;

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
        /// <param name="args">The <see cref="ProtocolEventArgs{GetResources, IList}"/> instance containing the event data.</param>
        public void GetResources(ProtocolEventArgs<GetResources, IList<Resource>> args)
        {
            if (EtpUris.IsRootUri(args.Message.Uri))
            {
                // NOTE: This entry added by the Witsml200Provider so that it appears at the end of the list
                //args.Context.Add(DiscoveryStoreProvider.NewProtocol(EtpUris.Eml210, "EML Common (2.1)"));
                return;
            }

            var uri = new EtpUri(args.Message.Uri);
            var parentUri = uri.Parent;

            // Append query string, if any
            if (!string.IsNullOrWhiteSpace(uri.Query))
                parentUri = new EtpUri(parentUri + uri.Query);

            if (!uri.IsRelatedTo(EtpUris.Eml210))
            {
                return;
            }
            if (uri.IsBaseUri)
            {
                CreateFoldersByObjectType(uri)
                    .ForEach(args.Context.Add);
            }
            else if (string.IsNullOrWhiteSpace(uri.ObjectId))
            {
                var objectType = ObjectTypes.GetObjectType(uri.ObjectType, uri.Version);
                var contentType = EtpContentTypes.GetContentType(objectType);
                var hasChildren = contentType.IsRelatedTo(EtpContentTypes.Eml210) ? 0 : -1;
                var dataProvider = GetDataProvider(uri.ObjectType);

                dataProvider
                    .GetAll(parentUri)
                    .Cast<AbstractObject>()
                    .ForEach(x => args.Context.Add(ToResource(x, hasChildren)));
            }
            //else
            //{
            //    var propertyName = uri.ObjectType.ToPascalCase();
            //
            //    CreateFoldersByObjectType(uri, propertyName)
            //        .ForEach(args.Context.Add);
            //}
        }

        private IList<Resource> CreateFoldersByObjectType(EtpUri uri, string propertyName = null, string additionalObjectType = null, int childCount = 0)
        {
            if (!_contentTypes.Any())
            {
                var contentTypes = new List<EtpContentType>();
                Providers.ForEach(x => x.GetSupportedObjects(contentTypes));

                contentTypes
                    .Where(x => x.IsRelatedTo(EtpContentTypes.Eml210))
                    .OrderBy(x => x.ObjectType)
                    .ForEach(_contentTypes.Add);
            }

            return _contentTypes
                .Select(x => new
                {
                    ContentType = x,
                    DataType = ObjectTypes.GetObjectType(x.ObjectType, DataSchemaVersion)
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

                    return DiscoveryStoreProvider.NewFolder(uri, x.ContentType, folderName, hasChildren);
                })
                .ToList();
        }

        private IEtpDataProvider GetDataProvider(string objectType)
        {
            return _container.Resolve<IEtpDataProvider>(new ObjectName(objectType, DataSchemaVersion));
        }

        private Resource ToResource(AbstractObject entity, int hasChildren = -1)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Citation.Title,
                count: hasChildren,
                lastChanged: GetLastChanged(entity));
        }

        private long GetLastChanged(AbstractObject entity)
        {
            return entity?.Citation?.LastUpdate?.ToUnixTimeMicroseconds() ?? 0;
        }
    }
}
