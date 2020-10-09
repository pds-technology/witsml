//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using System.Linq;
using System.Xml.Serialization;
using Energistics.DataAccess;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Framework;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using Prodml200 = Energistics.DataAccess.PRODML200;
using Resqml210 = Energistics.DataAccess.RESQML210;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Manages Etp Uris for the different WITSML versions.
    /// </summary>
    public static class EtpUris
    {
        private static readonly string[] _rootUris = { "/", "eml:/", "eml://", "eml:///" };

        /// <summary>
        /// The <see cref="EtpUri"/> for witsml13
        /// </summary>
        public static readonly EtpUri Witsml131 = new EtpUri("eml://witsml13");

        /// <summary>
        /// The <see cref="EtpUri"/> for witsml14
        /// </summary>
        public static readonly EtpUri Witsml141 = new EtpUri("eml://witsml14");

        /// <summary>
        /// The <see cref="EtpUri"/> for witsml20
        /// </summary>
        public static readonly EtpUri Witsml200 = new EtpUri("eml://witsml20");

        /// <summary>
        /// The <see cref="EtpUri"/> for prodml20
        /// </summary>
        public static readonly EtpUri Prodml200 = new EtpUri("eml://prodml20");

        /// <summary>
        /// The <see cref="EtpUri"/> for resqml21
        /// </summary>
        public static readonly EtpUri Resqml210 = new EtpUri("eml://resqml21");

        /// <summary>
        /// The <see cref="EtpUri"/> for eml210
        /// </summary>
        public static readonly EtpUri Eml210 = new EtpUri("eml://eml21");

        /// <summary>
        /// Determines whether the left parts of two URIs are equal.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="other">The other URI.</param>
        /// <returns><c>true</c> if the left parts are equal; otherwise, <c>false</c>.</returns>
        public static bool EqualsLeftPart(this EtpUri uri, string other)
        {
            return uri.EqualsLeftPart(new EtpUri(other));
        }

        /// <summary>
        /// Determines whether the left parts of two URIs are equal.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="other">The other URI.</param>
        /// <returns><c>true</c> if the left parts are equal; otherwise, <c>false</c>.</returns>
        public static bool EqualsLeftPart(this EtpUri uri, EtpUri other)
        {
            return uri.Equals(other.WithoutQuery());
        }

        /// <summary>
        /// Gets the URI without any query string parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>A new <see cref="EtpUri"/> instance.</returns>
        public static EtpUri WithoutQuery(this EtpUri uri)
        {
            return new EtpUri(uri.GetLeftPart());
        }

        /// <summary>
        /// Gets the left part of the URI.
        /// </summary>
        /// <param name="uri">The ETP URI.</param>
        public static string GetLeftPart(this EtpUri uri)
        {
            if (uri.IsValid)
            {
                return new Uri(uri).GetLeftPart(UriPartial.Path);
            }

            var value = uri.Uri;
            var index = value.IndexOf("?", StringComparison.InvariantCultureIgnoreCase);

            return index > -1 ? value.Substring(0, index) : value;
        }

        /// <summary>
        /// Determines whether the specified URI is a root URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns><c>true</c> if the specified URI is a root URI; otherwise, <c>false</c>.</returns>
        public static bool IsRootUri(string uri)
        {
            return _rootUris.ContainsIgnoreCase(uri);
        }

        /// <summary>
        /// Gets the data schema version for the specified uri.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>The data schema version.</returns>
        public static string GetDataSchemaVersion(this EtpUri uri)
        {
            return uri.IsRelatedTo(Eml210)
                ? Witsml200.Version
                : uri.Version;
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given type namespace.
        /// </summary>
        /// <param name="type">The type from which the namespace is derived.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUriFamily(Type type)
        {
            var xmlType = XmlAttributeCache<XmlTypeAttribute>.GetCustomAttribute(type);

            if (xmlType?.Namespace?.EndsWith("commonv2") ?? false)
                return Eml210;

            if (type?.Namespace == null)
                return Witsml141;

            if (type.Namespace.Contains("WITSML131"))
                return Witsml131;

            if (type.Namespace.Contains("WITSML200"))
                return Witsml200;

            if (type.Namespace.Contains("PRODML200"))
                return Prodml200;

            if (type.Namespace.Contains("RESQML210"))
                return Resqml210;

            return Witsml141;
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.WITSML200.AbstractObject"/> entity.
        /// </summary>
        /// <param name="entity">The <see cref="Energistics.DataAccess.WITSML200.AbstractObject"/> entity.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUriFamily(this Witsml200.AbstractObject entity)
        {
            return GetUriFamily(entity?.GetType());
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.PRODML200.AbstractObject"/> entity.
        /// </summary>
        /// <param name="entity">The <see cref="Energistics.DataAccess.PRODML200.AbstractObject"/> entity.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUriFamily(this Prodml200.AbstractObject entity)
        {
            return GetUriFamily(entity?.GetType());
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.RESQML210.AbstractObject"/> entity.
        /// </summary>
        /// <param name="entity">The <see cref="Energistics.DataAccess.RESQML210.AbstractObject"/> entity.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUriFamily(this Resqml210.AbstractObject entity)
        {
            return GetUriFamily(entity?.GetType());
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="IDataObject"/> entity.
        /// </summary>
        /// <param name="entity">The <see cref="IDataObject"/> entity.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUriFamily(this IDataObject entity)
        {
            return GetUriFamily(entity?.GetType());
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="IDataObject"/>.
        /// </summary>
        /// <param name="entity">The <see cref="IDataObject"/> entity.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this IDataObject entity)
        {
            return (entity as IWellboreObject)?.GetUri()
                ?? (entity as IWellObject)?.GetUri()
                ?? entity.GetUriFamily()
                    .Append(ObjectTypes.GetObjectType(entity), entity.Uid, true);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="IWellObject"/>.
        /// </summary>
        /// <param name="entity">The <see cref="IWellObject"/> entity.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this IWellObject entity)
        {
            return (entity as IWellboreObject)?.GetUri()
                ?? entity.GetUriFamily()
                    .Append(ObjectTypes.Well, entity.UidWell, true)
                    .Append(ObjectTypes.GetObjectType(entity), entity.Uid, true);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="IWellboreObject"/>.
        /// </summary>
        /// <param name="entity">The <see cref="IWellboreObject"/> entity.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this IWellboreObject entity)
        {
            return entity.GetUriFamily()
                .Append(ObjectTypes.Well, entity.UidWell, true)
                .Append(ObjectTypes.Wellbore, entity.UidWellbore, true)
                .Append(ObjectTypes.GetObjectType(entity), entity.Uid, true);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.WITSML200.AbstractObject"/>  and parentUri.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>An <see cref="EtpUri"/> instance</returns>
        public static EtpUri GetUri(this Witsml200.AbstractObject entity, EtpUri parentUri)
        {
            // Remove query string parameters, if any
            var uri = parentUri.GetLeftPart();

            if (!IsRootUri(uri))
            {
                // Remove trailing separator
                uri = uri.TrimEnd('/');
            }

            return new EtpUri(uri)
                .Append(ObjectTypes.GetObjectType(entity), entity.Uuid);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.WITSML200.AbstractObject"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Energistics.DataAccess.WITSML200.AbstractObject"/> entity.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Witsml200.AbstractObject entity)
        {
            return entity.GetUriFamily()
                .Append(ObjectTypes.GetObjectType(entity), entity.Uuid);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.PRODML200.AbstractObject"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Energistics.DataAccess.PRODML200.AbstractObject"/> entity.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Prodml200.AbstractObject entity)
        {
            return entity.GetUriFamily()
                .Append(ObjectTypes.GetObjectType(entity), entity.Uuid);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.RESQML210.AbstractObject"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Energistics.DataAccess.RESQML210.AbstractObject"/> entity.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Resqml210.AbstractObject entity)
        {
            return entity.GetUriFamily()
                .Append(ObjectTypes.GetObjectType(entity), entity.Uuid);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.WITSML200.ComponentSchemas.DataObjectReference"/>.
        /// </summary>
        /// <param name="reference">The data object reference.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Witsml200.ComponentSchemas.DataObjectReference reference)
        {
            var contentType = new EtpContentType(reference.ContentType);

            return string.IsNullOrWhiteSpace(reference.Uri)
                ? Witsml200.Append(contentType.ObjectType, reference.Uuid)
                : new EtpUri(reference.Uri);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.LogCurveInfo"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.LogCurveInfo"/> entity.</param>
        /// <param name="log">The log.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Witsml131.ComponentSchemas.LogCurveInfo entity, Witsml131.Log log)
        {
            return log.GetUri()
                .Append(ObjectTypes.LogCurveInfo, entity.Mnemonic, true);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.LogCurveInfo"/>
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="log">The log.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Witsml141.ComponentSchemas.LogCurveInfo entity, Witsml141.Log log)
        {
            return log.GetUri()
                .Append(ObjectTypes.LogCurveInfo, entity.Mnemonic.Value, true);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.WITSML200.ChannelSet"/>
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="log">The log.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Witsml200.ChannelSet entity, Witsml200.Log log)
        {
            return log.GetUri()
                .Append(ObjectTypes.ChannelSet, entity.Uuid);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.WITSML200.Channel"/>
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="channelSet">The channel set.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Witsml200.Channel entity, Witsml200.ChannelSet channelSet)
        {
            return channelSet.GetUri()
                .Append(ObjectTypes.Channel, entity.Uuid);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.WITSML200.Channel"/>
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="log">The log.</param>
        /// <param name="channelSet">The channel set.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Witsml200.Channel entity, Witsml200.Log log, Witsml200.ChannelSet channelSet)
        {
            return channelSet.GetUri(log)
                .Append(ObjectTypes.Channel, entity.Uuid);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.WITSML200.ComponentSchemas.ChannelIndex"/>
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Witsml200.ComponentSchemas.ChannelIndex entity, Witsml200.Channel channel)
        {
            return channel.GetUri()
                .Append(ObjectTypes.ChannelIndex, entity.Mnemonic, true);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.WITSML200.ComponentSchemas.ChannelIndex"/>
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="channelSet">The channel set.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Witsml200.ComponentSchemas.ChannelIndex entity, Witsml200.ChannelSet channelSet)
        {
            return channelSet.GetUri()
                .Append(ObjectTypes.ChannelIndex, entity.Mnemonic, true);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Energistics.DataAccess.WITSML200.ComponentSchemas.ChannelIndex"/>
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="log">The log.</param>
        /// <param name="channelSet">The channel set.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Witsml200.ComponentSchemas.ChannelIndex entity, Witsml200.Log log, Witsml200.ChannelSet channelSet)
        {
            return channelSet.GetUri(log)
                .Append(ObjectTypes.ChannelIndex, entity.Mnemonic, true);
        }

        // TODO: Remove this method when the corresponding EtpUriExtension in base submodule has been pushed through        
        /// <summary>
        /// Converts a full path 2.x EtpUri into a top level EtpUri
        /// </summary>
        /// <param name="uri">The specified URI.</param>
        /// <returns>A top level EtpUri</returns>
        public static EtpUri ToTopLevelUri(this EtpUri uri)
        {
            // If this is a root, base, 131 or 141 URI, just return it without any changes
            if (uri.IsRootUri || uri.IsBaseUri || uri.IsRelatedTo(EtpUris.Witsml131) || uri.IsRelatedTo(EtpUris.Witsml141))
                return uri;

            return EtpUris.Witsml200
                .Append(uri.ObjectType, uri.ObjectId);
        }

        /// <summary>
        /// Creates <see cref="ObjectName"/> instance from <see cref="EtpContentType"/> instance.
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static ObjectName ToObjectName(this EtpContentType contentType)
        {
            return new ObjectName(contentType.ObjectType, contentType.Family, contentType.Version);
        }

        /// <summary>
        /// Determines whether this <see cref="EtpUri"/> instance is related to the specified <see cref="EtpContentType"/>.
        /// </summary>
        /// <param name="uri">The uri.</param>
        /// <param name="contentType">The content type.</param>
        /// <returns></returns>
        public static bool IsRelatedTo(this EtpUri uri, EtpContentType contentType)
        {
            return uri.Family.EqualsIgnoreCase(contentType.Family)
                   && uri.Version.EqualsIgnoreCase(contentType.Version)
                   && uri.ObjectType.EqualsIgnoreCase(contentType.ObjectType);
        }

        /// <summary>
        /// Gets the list of potential uris that represent the same object hierarchy.  Assumes URI is well formed.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static List<EtpUri> GetRelatedHierarchyUris(this EtpUri uri)
        {
            // By default WITSML 2.x objects return top-level URIs in their channel metadata
            // Ensure that it is first in the list to speed up any related processing that is
            // looking for those type of URIs...
            // WITSML 1.x object require the full hierarchy regardless.
            var uris = new List<EtpUri>() { uri.ToTopLevelUri() };

            if (!uri.IsValid)
                return uris;

            var uriHierarchy = uri.GetObjectIds().ToList();

            if (!uriHierarchy.Any())
                return uris;

            var uriHierarchyMap = uriHierarchy
                .ToLookup(x => x.ObjectType, x => x.ObjectId, StringComparer.InvariantCultureIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.InvariantCultureIgnoreCase);

            var isWitsml20 = EtpUris.Witsml200.IsRelatedTo(uri);

            if (!isWitsml20)
                return uris;

            if (ObjectTypes.Well.EqualsIgnoreCase(uri.ObjectType))
                return uris;

            var rootUri = EtpUris.Witsml200;

            string wellObjectId;
            uriHierarchyMap.TryGetValue(ObjectTypes.Well, out wellObjectId);

            string wellboreObjectId;
            uriHierarchyMap.TryGetValue(ObjectTypes.Wellbore, out wellboreObjectId);

            string logObjectId;
            uriHierarchyMap.TryGetValue(ObjectTypes.Log, out logObjectId);

            string channelSetObjectId;
            uriHierarchyMap.TryGetValue(ObjectTypes.ChannelSet, out channelSetObjectId);

            if (ObjectTypes.Wellbore.EqualsIgnoreCase(uri.ObjectType))
            {
                var hierarchyUri = rootUri
                    .Append(ObjectTypes.Well, wellObjectId)
                    .Append(ObjectTypes.Wellbore, uri.ObjectId);

                uris.Add(hierarchyUri);
            }
            else
            {
                EtpUri hierarchyUri;

                if (ObjectTypes.Channel.EqualsIgnoreCase(uri.ObjectType))
                {
                    hierarchyUri = rootUri
                        .Append(ObjectTypes.ChannelSet, channelSetObjectId)
                        .Append(ObjectTypes.Channel, uri.ObjectId);

                    uris.Add(hierarchyUri);

                    hierarchyUri = rootUri
                        .Append(ObjectTypes.Log, logObjectId)
                        .Append(ObjectTypes.ChannelSet, channelSetObjectId)
                        .Append(ObjectTypes.Channel, uri.ObjectId);

                    uris.Add(hierarchyUri);

                    hierarchyUri = rootUri
                        .Append(ObjectTypes.Wellbore, wellboreObjectId)
                        .Append(ObjectTypes.Log, logObjectId)
                        .Append(ObjectTypes.ChannelSet, channelSetObjectId)
                        .Append(ObjectTypes.Channel, uri.ObjectId);

                    uris.Add(hierarchyUri);

                    hierarchyUri = rootUri
                        .Append(ObjectTypes.Well, wellObjectId)
                        .Append(ObjectTypes.Wellbore, wellboreObjectId)
                        .Append(ObjectTypes.Log, logObjectId)
                        .Append(ObjectTypes.ChannelSet, channelSetObjectId)
                        .Append(ObjectTypes.Channel, uri.ObjectId);

                    uris.Add(hierarchyUri);
                }
                else if (ObjectTypes.ChannelSet.EqualsIgnoreCase(uri.ObjectType))
                {
                    hierarchyUri = rootUri
                        .Append(ObjectTypes.Log, logObjectId)
                        .Append(ObjectTypes.ChannelSet, uri.ObjectId);

                    uris.Add(hierarchyUri);

                    hierarchyUri = rootUri
                        .Append(ObjectTypes.Wellbore, wellboreObjectId)
                        .Append(ObjectTypes.Log, logObjectId)
                        .Append(ObjectTypes.ChannelSet, uri.ObjectId);

                    uris.Add(hierarchyUri);

                    hierarchyUri = rootUri
                        .Append(ObjectTypes.Well, wellObjectId)
                        .Append(ObjectTypes.Wellbore, wellboreObjectId)
                        .Append(ObjectTypes.Log, logObjectId)
                        .Append(ObjectTypes.ChannelSet, uri.ObjectId);

                    uris.Add(hierarchyUri);
                }

                hierarchyUri = rootUri
                    .Append(ObjectTypes.Wellbore, wellboreObjectId)
                    .Append(uri.ObjectType, uri.ObjectId);

                uris.Add(hierarchyUri);

                hierarchyUri = rootUri
                    .Append(ObjectTypes.Well, wellObjectId)
                    .Append(ObjectTypes.Wellbore, wellboreObjectId)
                    .Append(uri.ObjectType, uri.ObjectId);

                uris.Add(hierarchyUri);
            }

            return uris;
        }

        /// <summary>
        /// If valid, returns the uri that matches the requested query hierarchy.
        /// </summary>
        /// <param name="uri">The uri.</param>
        /// <returns></returns>
        public static EtpUri GetValidHierarchyUri(this EtpUri uri)
        {
            return uri.GetValidHierarchyUri(uri);
        }

        /// <summary>
        /// If valid, returns the uri that matches the requested query hierarchy.
        /// </summary>
        /// <param name="uri">The uri.</param>
        /// <param name="query">The hierarchy query.</param>
        /// <returns></returns>
        public static EtpUri GetValidHierarchyUri(this EtpUri uri, EtpUri query)
        {
            var hierarchyUris = uri.GetRelatedHierarchyUris();

            var queryObjectIds = query.GetObjectIds().ToList();

            return hierarchyUris.FirstOrDefault(x =>
            {
                var uriObjectIds = x.GetObjectIds().ToList();

                if (uriObjectIds.Count != queryObjectIds.Count)
                    return false;

                for (var i = 0; i < uriObjectIds.Count; ++i)
                {
                    if (!uriObjectIds[i].ObjectType.EqualsIgnoreCase(queryObjectIds[i].ObjectType) || !uriObjectIds[i].ObjectId.IsMatch(queryObjectIds[i].ObjectId))
                        return false;
                }

                return true;
            });
        }

        /// <summary>
        /// Get the uri family of the specified uri.
        /// </summary>
        /// <param name="uri">The uri.</param>
        /// <returns></returns>
        public static EtpUri GetUriFamily(this EtpUri uri)
        {
            if (!uri.IsValid || uri.IsBaseUri)
                return new EtpUri();

            return new EtpUri($"{EtpUri.RootUri}{uri.Family}{uri.Version.Replace(".", string.Empty).Substring(0, 2)}");
        }

        /// <summary>
        /// Gets the uri
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static EtpUri GetResolvedHierarchyUri(this EtpUri uri, EtpUri other)
        {
            var resolvedUri = new EtpUri();

            if (other.IsRootUri)
                return uri;

            if (!uri.IsRelatedTo(other))
                return resolvedUri;

            var uriHierarchy = uri.GetObjectIds().ToList();
            var otherHierarchy = other.GetObjectIds().ToList();

            resolvedUri = uri.GetUriFamily();

            var otherHierarchyMap = otherHierarchy
                .ToLookup(x => x.ObjectType, x => x.ObjectId, StringComparer.InvariantCultureIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.InvariantCultureIgnoreCase);

            uriHierarchy.ForEach(segment =>
            {
                var objectType = segment.ObjectType;
                var objectId = segment.ObjectId;

                if (string.IsNullOrWhiteSpace(objectId))
                    otherHierarchyMap.TryGetValue(objectType, out objectId);

                resolvedUri.Append(objectType, objectId);
            });

            return resolvedUri;
        }

        /// <summary>
        /// Gets whether an URI is relative to another URI.
        /// </summary>
        /// <param name="uri">The child URI (E.g. {eml://witsml20/Well(1234)/Wellbore(5678)/Trajectory(91011)}).</param>
        /// <param name="other">The parent URI (E.g. {eml://witsml20/Trajectory}).</param>
        /// <returns></returns>
        public static bool IsRelativeTo(this EtpUri uri, EtpUri other)
        {
            if (!uri.IsValid || !other.IsValid)
                return false;

            if (other.IsRootUri)
                return true;

            if (!uri.IsRelatedTo(other))
                return false;

            var uriHierarchy = uri.GetObjectIds().ToList();
            var otherHierarchy = other.GetObjectIds().ToList();

            var getObjectType = new Func<List<EtpUri.Segment>, int, EtpUri.Segment>((h, i) =>
            {
                return i >= 0 && i < h.Count ? h[i] : new EtpUri.Segment();
            });

            var isBaseUri = new Func<EtpUri.Segment, bool>((o) =>
            {
                return string.IsNullOrWhiteSpace(o.ObjectType) && string.IsNullOrWhiteSpace(o.ObjectId);
            });

            var uriIndex = uriHierarchy.Count - 1;
            var otherIndex = otherHierarchy.Count - 1;

            var uriTemp = getObjectType(uriHierarchy, uriIndex);
            var otherTemp = getObjectType(otherHierarchy, otherIndex);
            var valid = false;
            while (!string.IsNullOrWhiteSpace(uriTemp.ObjectType))
            {
                if (uriTemp.ObjectType.EqualsIgnoreCase(otherTemp.ObjectType))
                {
                    valid = true;
                    break;
                }
                uriTemp = getObjectType(uriHierarchy, --uriIndex);
            }

            if (isBaseUri(uriTemp) && isBaseUri(otherTemp))
                return true;

            if (!valid)
                return false;

            while (!string.IsNullOrWhiteSpace(uriTemp.ObjectType)
                || !string.IsNullOrWhiteSpace(otherTemp.ObjectType))
            {
                var objectTypeMatch = uriTemp.ObjectType.EqualsIgnoreCase(otherTemp.ObjectType);
                var objectIdMatch = objectTypeMatch && (uriTemp.ObjectId?.IsMatch(otherTemp.ObjectId) ?? string.IsNullOrWhiteSpace(otherTemp.ObjectId));
                if (!objectIdMatch)
                {
                    valid = false;
                    break;
                }
                uriTemp = getObjectType(uriHierarchy, --uriIndex);
                otherTemp = getObjectType(otherHierarchy, --otherIndex);
            }

            return valid || isBaseUri(uriTemp) && isBaseUri(otherTemp);
        }

        /// <summary>
        /// Returns whether the provided URIs reference the same object.
        /// Two uri are the "same" if their hierarchies match (1.3.1.1/1.4.1.1) or if their object ids are equal (2.0).
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static bool AreSame(EtpUri u, EtpUri v)
        {
            if (!u.IsRelatedTo(v))
                return false;

            if (u.IsRelatedTo(EtpUris.Witsml200) && u.ObjectType.EqualsIgnoreCase(v.ObjectType) && !string.IsNullOrEmpty(u.ObjectId) && u.ObjectId.EqualsIgnoreCase(v.ObjectId))
                return true;

            return u.Equals(v);
        }

        /// <summary>
        /// Gets whether an URI is the parent of another URI.
        /// </summary>
        /// <param name="uri">The parent URI.</param>
        /// <param name="other">The child URI.</param>
        /// <returns></returns>
        public static bool IsParentOf(this EtpUri uri, EtpUri other)
        {
            return !AreSame(uri, other) && other.IsRelativeTo(uri);
        }

        /// <summary>
        /// Gets whether an URI is the parent of another URI.
        /// </summary>
        /// <param name="uri">The parent URI.</param>
        /// <param name="other">The child URI.</param>
        /// <returns></returns>
        public static bool IsChildOf(this EtpUri uri, EtpUri other)
        {
            return !AreSame(uri, other) && uri.IsRelativeTo(other);
        }

        /// <summary>
        /// Tries to get the segment in the ETP URI corresponding to the specified object type.
        /// </summary>
        /// <param name="uri">The ETP URI to get the segment from.</param>
        /// <param name="objectType">The object type of the segment to get.</param>
        /// <param name="success"><c>true</c> if the object segment was found; <c>false</c> otherwise.</param>
        /// <returns>The found segment or default if not found.</returns>
        public static EtpUri.Segment TryGetObjectTypeSegment(this EtpUri uri, string objectType, out bool success)
        {
            success = false;
            if (!uri.IsValid || string.IsNullOrEmpty(objectType))
                return default(EtpUri.Segment);

            foreach (var s in uri.GetObjectIds())
            {
                if (!string.IsNullOrEmpty(s.ObjectType) && objectType.EqualsIgnoreCase(s.ObjectType))
                {
                    success = true;
                    return s;
                }
            }


            return default(EtpUri.Segment);
        }

        /// <summary>
        /// Tries to replace the segment in the ETP URI of the same object type with the specified segment.
        /// </summary>
        /// <param name="uri">The URI to create a new URI from with the segment replaced.</param>
        /// <param name="segment">The segment to replace the current URI segment with.</param>
        /// <param name="success"><c>true</c> if successful; <c>false</c> otherwise.</param>
        /// <returns>The input URI with the segment replaced or the original URI if not successful.</returns>
        public static EtpUri TryReplaceObjectTypeSegment(this EtpUri uri, EtpUri.Segment segment, out bool success)
        {
            success = false;
            if (!uri.IsValid || string.IsNullOrEmpty(segment.ObjectType))
                return uri;

            var constructedUri = uri.GetUriFamily();
            foreach (var s in uri.GetObjectIds())
            {
                if (!string.IsNullOrEmpty(s.ObjectType) && segment.ObjectType.EqualsIgnoreCase(s.ObjectType))
                {
                    constructedUri = constructedUri.Append(segment.ObjectType, segment.ObjectId);
                    success = true;
                }
                else
                {
                    constructedUri = constructedUri.Append(s.ObjectType, s.ObjectId);
                }
            }

            if (success)
                return new EtpUri(constructedUri.ToString() + uri.Query);
            else
                return uri;
        }

        /// <summary>
        /// Tries to get the ETP URI prefix up to the specified object type.
        /// </summary>
        /// <param name="uri">The URI to create the prefix from.</param>
        /// <param name="objecType">The object type to end the prefix with.</param>
        /// <param name="success"><c>true</c> if successful; <c>false</c> otherwise.</param>
        /// <returns>The prefix of the input URI or the original URI without its query if not successful.</returns>
        public static EtpUri TryGetObjectTypePrefix(this EtpUri uri, string objecType, out bool success)
        {
            success = false;
            uri = new EtpUri(uri.GetLeftPart());

            if (!uri.IsValid || string.IsNullOrEmpty(objecType))
                return uri;

            var constructedUri = uri.GetUriFamily();
            foreach (var s in uri.GetObjectIds())
            {
                if (!string.IsNullOrEmpty(s.ObjectType) && objecType.EqualsIgnoreCase(s.ObjectType))
                {
                    constructedUri = constructedUri.Append(s.ObjectType, s.ObjectId);
                    success = true;
                    break;
                }
                else
                {
                    constructedUri = constructedUri.Append(s.ObjectType, s.ObjectId);
                }
            }

            if (success)
                return constructedUri;
            else
                return uri;
        }

        /// <summary>
        /// Tries to replace the segment in the ETP URI of the same object type with the segment of the same type in the specified URI.
        /// </summary>
        /// <param name="uri">The URI to create a new URI from with the segment replaced.</param>
        /// <param name="uriWithSegment">The URI with the segment to replace in the URI.</param>
        /// <param name="objectType">The object type of the segment to replace.</param>
        /// <param name="success"><c>true</c> if successful; <c>false</c> otherwise.</param>
        /// <returns>The input URI with the segment replaced or the original URI if not successful.</returns>
        public static EtpUri TryReplaceObjectTypeSegment(this EtpUri uri, EtpUri uriWithSegment, string objectType, out bool success)
        {
            var segment = uriWithSegment.TryGetObjectTypeSegment(objectType, out success);
            if (!success)
                return uri;

            return uri.TryReplaceObjectTypeSegment(segment, out success);
        }
    }
}
