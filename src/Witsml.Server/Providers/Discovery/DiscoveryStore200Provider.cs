//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
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
using Energistics.DataAccess.WITSML200;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Discovery;
using PDS.Framework;
using PDS.Witsml.Server.Data;

namespace PDS.Witsml.Server.Providers.Discovery
{
    /// <summary>
    /// Provides information about resources available in a WITSML store for version 1.4.1.1.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Providers.Discovery.IDiscoveryStoreProvider" />
    [Export(typeof(IDiscoveryStoreProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DiscoveryStore200Provider : IDiscoveryStoreProvider
    {
        private readonly IContainer _container;
        private readonly IEtpDataProvider<Well> _wellDataProvider;
        private readonly IEtpDataProvider<Wellbore> _wellboreDataProvider;
        private readonly IEtpDataProvider<Log> _logDataProvider;
        private readonly IEtpDataProvider<ChannelSet> _channelSetDataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryStore200Provider" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="wellDataProvider">The well data Provider.</param>
        /// <param name="wellboreDataProvider">The wellbore data Provider.</param>
        /// <param name="logDataProvider">The log data Provider.</param>
        /// <param name="channelSetDataProvider">The channel set data Provider.</param>
        [ImportingConstructor]
        public DiscoveryStore200Provider(
            IContainer container,
            IEtpDataProvider<Well> wellDataProvider,
            IEtpDataProvider<Wellbore> wellboreDataProvider,
            IEtpDataProvider<Log> logDataProvider,
            IEtpDataProvider<ChannelSet> channelSetDataProvider)
        {
            _container = container;
            _wellDataProvider = wellDataProvider;
            _wellboreDataProvider = wellboreDataProvider;
            _logDataProvider = logDataProvider;
            _channelSetDataProvider = channelSetDataProvider;
        }

        /// <summary>
        /// Gets the data schema version supported by the provider.
        /// </summary>
        /// <value>The data schema version.</value>
        public string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version200.Value; }
        }

        /// <summary>
        /// Gets or sets the collection of <see cref="IEtpDataProvider"/> providers.
        /// </summary>
        /// <value>The collection of providers.</value>
        [ImportMany]
        public IEnumerable<IEtpDataProvider> Providers { get; set; }

        /// <summary>
        /// Gets a collection of resources associated to the specified URI.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetResources, IList}"/> instance containing the event data.</param>
        public void GetResources(ProtocolEventArgs<GetResources, IList<Resource>> args)
        {
            if (EtpUri.IsRoot(args.Message.Uri))
            {
                args.Context.Add(DiscoveryStoreProvider.NewProtocol(EtpUris.Witsml200, "WITSML Store (2.0)"));
                return;
            }

            var uri = new EtpUri(args.Message.Uri);
            var parentUri = uri.Parent;

            // Append query string, if any
            if (!string.IsNullOrWhiteSpace(uri.Query))
                parentUri = new EtpUri(parentUri + uri.Query);

            if (!uri.IsRelatedTo(EtpUris.Witsml200))
            {
                return;
            }
            if (uri.IsBaseUri)
            {
                _wellDataProvider.GetAll(uri)
                    .ForEach(x => args.Context.Add(ToResource(x)));
            }
            else if (string.IsNullOrWhiteSpace(uri.ObjectId))
            {
                if (ObjectFolders.Logs.EqualsIgnoreCase(uri.ObjectType))
                {
                    args.Context.Add(DiscoveryStoreProvider.NewFolder(uri, ObjectTypes.Log, ObjectFolders.Time));
                    args.Context.Add(DiscoveryStoreProvider.NewFolder(uri, ObjectTypes.Log, ObjectFolders.Depth));
                }
                else if (ObjectFolders.Logs.EqualsIgnoreCase(parentUri.ObjectType) &&
                    (ObjectFolders.Time.EqualsIgnoreCase(uri.ObjectType) || ObjectFolders.Depth.EqualsIgnoreCase(uri.ObjectType)))
                {
                    var wellboreUri = parentUri.Parent;

                    // Append query string, if any
                    if (!string.IsNullOrWhiteSpace(uri.Query))
                        wellboreUri = new EtpUri(wellboreUri + uri.Query);

                    _logDataProvider.GetAll(wellboreUri)
                        .Where(x => x.TimeDepth.EqualsIgnoreCase(uri.ObjectType))
                        .ForEach(x => args.Context.Add(ToResource(x)));
                }
                else
                {
                    var objectType = ObjectTypes.PluralToSingle(uri.ObjectType);
                    var dataProvider = _container.Resolve<IEtpDataProvider>(new ObjectName(objectType, uri.Version));

                    dataProvider
                        .GetAll(parentUri)
                        .Cast<AbstractObject>()
                        .ForEach(x => args.Context.Add(ToResource(x)));
                }
            }
            else if (ObjectTypes.Well.EqualsIgnoreCase(uri.ObjectType))
            {
                _wellboreDataProvider.GetAll(uri)
                    .ForEach(x => args.Context.Add(ToResource(x)));
            }
            else if (ObjectTypes.Wellbore.EqualsIgnoreCase(uri.ObjectType))
            {
                var contentTypes = new List<EtpContentType>();
                Providers.ForEach(x => x.GetSupportedObjects(contentTypes));

                contentTypes
                    .Where(x => x.Version == DataSchemaVersion && ObjectTypes.GetObjectType(x.ObjectType, DataSchemaVersion)?.GetProperty("Wellbore") != null)
                    .OrderBy(x => x.ObjectType)
                    .ForEach(x => args.Context.Add(DiscoveryStoreProvider.NewFolder(uri, x.ObjectType, ObjectTypes.SingleToPlural(x.ObjectType, false))));
            }
            else if (ObjectTypes.Log.EqualsIgnoreCase(uri.ObjectType))
            {
                var log = _logDataProvider.Get(uri);
                log.ChannelSet.ForEach(x => args.Context.Add(ToResource(x)));
            }
            else if (ObjectTypes.ChannelSet.EqualsIgnoreCase(uri.ObjectType))
            {
                //var uid = uri.GetObjectIds()
                //    .Where(x => x.Key == ObjectTypes.Log)
                //    .Select(x => x.Value)
                //    .FirstOrDefault();
                //
                //var set = log.ChannelSet.FirstOrDefault(x => x.Uuid == uri.ObjectId);

                var set = _channelSetDataProvider.Get(uri);
                set.Channel.ForEach(x => args.Context.Add(ToResource(set, x)));
            }
        }

        private Resource ToResource(Well entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Citation.Title,
                count: -1);
        }

        private Resource ToResource(Wellbore entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Citation.Title,
                count: -1);
        }

        private Resource ToResource(Log entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Citation.Title,
                count: -1);
        }

        private Resource ToResource(ChannelSet entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Citation.Title,
                count: -1);
        }

        private Resource ToResource(ChannelSet channelSet, Channel entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.GetUri(channelSet),
                resourceType: ResourceTypes.DataObject,
                name: entity.Mnemonic);
        }

        private Resource ToResource(AbstractObject entity)
        {
            return DiscoveryStoreProvider.New(
                uuid: entity.Uuid,
                uri: entity.GetUri(),
                resourceType: ResourceTypes.DataObject,
                name: entity.Citation.Title);
        }
    }
}
