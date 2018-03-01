//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.1
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
using System.Reflection;
using System.Xml.Serialization;
using Energistics.DataAccess;
using Energistics.Datatypes;
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
                    .Append(ObjectTypes.GetObjectType(entity), entity.Uid);
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
                    .Append(ObjectTypes.Well, entity.UidWell)
                    .Append(ObjectTypes.GetObjectType(entity), entity.Uid);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="IWellboreObject"/>.
        /// </summary>
        /// <param name="entity">The <see cref="IWellboreObject"/> entity.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this IWellboreObject entity)
        {
            return entity.GetUriFamily()
                .Append(ObjectTypes.Well, entity.UidWell)
                .Append(ObjectTypes.Wellbore, entity.UidWellbore)
                .Append(ObjectTypes.GetObjectType(entity), entity.Uid);
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
                .Append(ObjectTypes.LogCurveInfo, entity.Mnemonic);
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
                .Append(ObjectTypes.LogCurveInfo, entity.Mnemonic.Value);
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
                .Append(ObjectTypes.ChannelIndex, entity.Mnemonic);
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
                .Append(ObjectTypes.ChannelIndex, entity.Mnemonic);
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
                .Append(ObjectTypes.ChannelIndex, entity.Mnemonic);
        }
    }
}
