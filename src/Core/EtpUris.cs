//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2017.1
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

using System;
using Energistics.DataAccess;
using Energistics.Datatypes;
using PDS.WITSMLstudio.Framework;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Manages Etp Uris for the different WITSML versions.
    /// </summary>
    public static class EtpUris
    {
        private static readonly string[] _rootUris = { "/", "eml:/", "eml://", "eml://" };

        /// <summary>
        /// The <see cref="EtpUri"/> for witsml131
        /// </summary>
        public static readonly EtpUri Witsml131 = new EtpUri("eml://witsml13");

        /// <summary>
        /// The <see cref="EtpUri"/> for witsml141
        /// </summary>
        public static readonly EtpUri Witsml141 = new EtpUri("eml://witsml14");

        /// <summary>
        /// The <see cref="EtpUri"/> for witsml200
        /// </summary>
        public static readonly EtpUri Witsml200 = new EtpUri("eml://witsml20");

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
        /// Gets the <see cref="EtpUri"/> for a given type namespace.
        /// </summary>
        /// <param name="type">The type from which the namespace is derived.</param>
        /// <returns>An <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUriFamily(Type type)
        {
            if (type?.Namespace == null)
                return Witsml141;

            if (type.Namespace.Contains("WITSML131"))
                return Witsml131;

            if (type.Namespace.Contains("WITSML200"))
                return Witsml200;

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
                .Append(ObjectTypes.Channel, entity.Mnemonic);
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
                .Append(ObjectTypes.Channel, entity.Mnemonic);
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
