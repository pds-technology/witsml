//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.1
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

using Energistics.Common;
using Energistics.Datatypes;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Providers
{
    /// <summary>
    /// Defines static helper methods that can be used from any protocol handler.
    /// </summary>
    public static class EtpExtensions
    {
        /// <summary>
        /// Creates and validates the specified URI.
        /// </summary>
        /// <param name="handler">The protocol handler.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <returns>A new <see cref="EtpUri" /> instance.</returns>
        public static EtpUri CreateAndValidateUri(this EtpProtocolHandler handler, string uri, long messageId = 0)
        {
            var etpUri = new EtpUri(uri);

            if (!etpUri.IsValid)
            {
                handler.InvalidUri(uri, messageId);
            }

            return etpUri;
        }

        /// <summary>
        /// Validates URI Object Type.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="etpUri">The ETP URI.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <returns></returns>
        public static bool ValidateUriObjectType(this EtpProtocolHandler handler, EtpUri etpUri, long messageId = 0)
        {
            if (!string.IsNullOrWhiteSpace(etpUri.ObjectType))
                return true;

            handler.UnsupportedObject(null, $"{etpUri.Uri}", messageId);
            return false;
        }

        /// <summary>
        /// Determines whether this URI can be used for for resolving channel metadata for the purpose of streaming via protocol 1.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>
        ///   <c>true</c> if this URI can be used to resolve channel metadata; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsChannelSubscribable(this EtpUri uri)
        {
            // eml://eml21 does not need to be subscribable as there are no growing/channel objects
            if (!uri.IsValid || EtpUris.Eml210.Equals(uri)) return false;

            // e.g. "/" or eml://witsml20 or eml://witsml14 or eml://witsml13
            if (EtpUri.RootUri.Equals(uri) || uri.IsBaseUri) return true;

            var objectType = ObjectTypes.PluralToSingle(uri.ObjectType);

            // e.g. eml://witsml14/well{s} or eml://witsml14/well(uid)
            if (ObjectTypes.Well.EqualsIgnoreCase(objectType)) return true;

            // e.g. eml://witsml14/well(uid_well)/wellbore{s} or eml://witsml14/well(uid_well)/wellbore(uid)
            if (ObjectTypes.Wellbore.EqualsIgnoreCase(objectType)) return true;

            // e.g. eml://witsml14/well(uid_well)/wellbore(uid_wellbore/log{s} or eml://witsml14/well(uid_well)/wellbore(uid_wellbore/log(uid)
            if (ObjectTypes.Log.EqualsIgnoreCase(objectType)) return true;

            // e.g. eml://witsml14/well(uid_well)/wellbore(uid_wellbore/log(uid)/logCurveInfo{s} or eml://witsml14/well(uid_well)/wellbore(uid_wellbore/log(uid)/logCurveInfo(mnemonic)
            if (ObjectTypes.LogCurveInfo.EqualsIgnoreCase(objectType)) return true;

            // e.g. eml://witsml20/ChannelSet{s} or eml://witsml20/ChannelSet(uid)
            if (ObjectTypes.ChannelSet.EqualsIgnoreCase(objectType)) return true;

            // e.g. eml://witsml20/Channel{s} or eml://witsml20/Channel(uid)
            if (ObjectTypes.Channel.EqualsIgnoreCase(objectType)) return true;

            // e.g. eml://witsml20/Trajectory{s} or eml://witsml20/Trajectory(uid)
            if (ObjectTypes.Trajectory.EqualsIgnoreCase(objectType)) return true;

            return false;
        }

        /// <summary>
        /// Determines whether this URI can be used to subscribe to change notifications via protocol 5.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>
        ///   <c>true</c> if this URI can be used to subscribe to change notifications; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsObjectNotifiable(this EtpUri uri)
        {
            return uri.IsValid && !string.IsNullOrWhiteSpace(uri.ObjectId);
        }
    }
}
