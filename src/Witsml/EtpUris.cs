//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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

using System;
using Energistics.DataAccess;
using Energistics.Datatypes;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.Witsml
{
    /// <summary>
    /// Manages Etp Uris for the different WITSML versions.
    /// </summary>
    public static class EtpUris
    {
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
        /// Gets the <see cref="EtpUri"/> for a given type namespace.
        /// </summary>
        /// <param name="type">The type from which the namespace is derived.</param>
        /// <returns>The <see cref="EtpUri"/> instance.</returns>
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
        /// Gets the <see cref="EtpUri"/> for a given <see cref="IDataObject"/> entity.
        /// </summary>
        /// <param name="entity">The <see cref="IDataObject"/> entity.</param>
        /// <returns>The <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUriFamily(this IDataObject entity)
        {
            return GetUriFamily(entity.GetType());
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="IDataObject"/>.
        /// </summary>
        /// <param name="entity">The <see cref="IDataObject"/> entity.</param>
        /// <returns>The <see cref="EtpUri"/> type</returns>
        public static EtpUri GetUri(this IDataObject entity)
        {
            return (entity as IWellboreObject)?.GetUri()
                ?? (entity as IWellObject)?.GetUri()
                ?? entity.GetUriFamily()
                    .Append(ObjectTypes.GetSchemaType(entity), entity.Uid);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="IWellObject"/>.
        /// </summary>
        /// <param name="entity">The <see cref="IWellObject"/> entity.</param>
        /// <returns>The <see cref="EtpUri"/> instance</returns>
        public static EtpUri GetUri(this IWellObject entity)
        {
            return (entity as IWellboreObject)?.GetUri()
                ?? entity.GetUriFamily()
                    .Append(ObjectTypes.Well, entity.UidWell)
                    .Append(ObjectTypes.GetSchemaType(entity), entity.Uid);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="IWellboreObject"/>.
        /// </summary>
        /// <param name="entity">The <see cref="IWellboreObject"/> entity.</param>
        /// <returns>The <see cref="EtpUri"/> instance</returns>
        public static EtpUri GetUri(this IWellboreObject entity)
        {
            return entity.GetUriFamily()
                .Append(ObjectTypes.Well, entity.UidWell)
                .Append(ObjectTypes.Wellbore, entity.UidWellbore)
                .Append(ObjectTypes.GetSchemaType(entity), entity.Uid);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Witsml200.AbstractObject"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Witsml200.AbstractObject"/> entity.</param>
        /// <returns>The <see cref="EtpUri"/> type</returns>
        public static EtpUri GetUri(this Witsml200.AbstractObject entity)
        {
            return Witsml200
                .Append(ObjectTypes.GetSchemaType(entity), entity.Uuid);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Witsml131.ComponentSchemas.LogCurveInfo"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Witsml131.ComponentSchemas.LogCurveInfo"/> entity.</param>
        /// <param name="log">The log.</param>
        /// <returns>A <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Witsml131.ComponentSchemas.LogCurveInfo entity, Witsml131.Log log)
        {
            return log.GetUri()
                .Append(ObjectTypes.GetSchemaType(entity), entity.Mnemonic);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Witsml141.ComponentSchemas.LogCurveInfo"/>
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="log">The log.</param>
        /// <returns>A <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Witsml141.ComponentSchemas.LogCurveInfo entity, Witsml141.Log log)
        {
            return log.GetUri()
                .Append(ObjectTypes.GetSchemaType(entity), entity.Mnemonic.Value);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Witsml200.ChannelSet"/>
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="log">The log.</param>
        /// <returns>A <see cref="EtpUri"/> instance</returns>
        public static EtpUri GetUri(this Witsml200.ChannelSet entity, Witsml200.Log log)
        {
            return log.GetUri()
                .Append(ObjectTypes.GetSchemaType(entity), entity.Uuid);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Witsml200.Channel"/>
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="channelSet">The channel set.</param>
        /// <returns>A <see cref="EtpUri"/> instance</returns>
        public static EtpUri GetUri(this Witsml200.Channel entity, Witsml200.ChannelSet channelSet)
        {
            return channelSet.GetUri()
                .Append(ObjectTypes.GetSchemaType(entity), entity.Mnemonic);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Witsml200.Channel"/>
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="log">The log.</param>
        /// <param name="channelSet">The channel set.</param>
        /// <returns>A <see cref="EtpUri"/> instance</returns>
        public static EtpUri GetUri(this Witsml200.Channel entity, Witsml200.Log log, Witsml200.ChannelSet channelSet)
        {
            return channelSet.GetUri(log)
                .Append(ObjectTypes.GetSchemaType(entity), entity.Mnemonic);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Witsml200.ComponentSchemas.ChannelIndex"/>
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="channelSet">The channel set.</param>
        /// <returns>A <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Witsml200.ComponentSchemas.ChannelIndex entity, Witsml200.ChannelSet channelSet)
        {
            return channelSet.GetUri()
                .Append(ObjectTypes.GetSchemaType(entity), entity.Mnemonic);
        }

        /// <summary>
        /// Gets the <see cref="EtpUri"/> for a given <see cref="Witsml200.ComponentSchemas.ChannelIndex"/>
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="log">The log.</param>
        /// <param name="channelSet">The channel set.</param>
        /// <returns>A <see cref="EtpUri"/> instance.</returns>
        public static EtpUri GetUri(this Witsml200.ComponentSchemas.ChannelIndex entity, Witsml200.Log log, Witsml200.ChannelSet channelSet)
        {
            return channelSet.GetUri(log)
                .Append(ObjectTypes.GetSchemaType(entity), entity.Mnemonic);
        }
    }
}
