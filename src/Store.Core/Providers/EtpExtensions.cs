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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;
using Energistics.Etp.Common.Datatypes.Object;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Providers.Discovery;
using PDS.WITSMLstudio.Store.Providers.Store;

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
 
        /// <summary>
        /// Creates a new index metadata record.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> [is time index].</param>
        /// <param name="isIncreasing">if set to <c>true</c> [is increasing].</param>
        /// <returns>A new <see cref="IIndexMetadataRecord" /> instance.</returns>
        public static IIndexMetadataRecord CreateIndexMetadata(this IEtpAdapter etpAdapter, EtpUri? uri = null, bool isTimeIndex = true, bool isIncreasing = true)
        {
            if (etpAdapter is Energistics.Etp.v11.Etp11Adapter)
            {
                return new Energistics.Etp.v11.Datatypes.ChannelData.IndexMetadataRecord
                {
                    Uri = uri?.ToString(),
                    IndexType = isTimeIndex
                        ? Energistics.Etp.v11.Datatypes.ChannelData.ChannelIndexTypes.Time
                        : Energistics.Etp.v11.Datatypes.ChannelData.ChannelIndexTypes.Depth,
                    Direction = isIncreasing
                        ? Energistics.Etp.v11.Datatypes.ChannelData.IndexDirections.Increasing
                        : Energistics.Etp.v11.Datatypes.ChannelData.IndexDirections.Decreasing,
                    CustomData = new Dictionary<string, Energistics.Etp.v11.Datatypes.DataValue>()
                };
            }

            return new Energistics.Etp.v12.Datatypes.ChannelData.IndexMetadataRecord
            {
                Uri = uri?.ToString(),
                IndexKind = isTimeIndex
                    ? Energistics.Etp.v12.Datatypes.ChannelData.ChannelIndexKinds.Time
                    : Energistics.Etp.v12.Datatypes.ChannelData.ChannelIndexKinds.Depth,
                Direction = isIncreasing
                    ? Energistics.Etp.v12.Datatypes.ChannelData.IndexDirections.Increasing
                    : Energistics.Etp.v12.Datatypes.ChannelData.IndexDirections.Decreasing,
                CustomData = new Dictionary<string, Energistics.Etp.v12.Datatypes.DataValue>()
            };
        }

        /// <summary>
        /// Creates a new channel metadata record.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>A new <see cref="IChannelMetadataRecord"/> instance.</returns>
        public static IChannelMetadataRecord CreateChannelMetadata(this IEtpAdapter etpAdapter, EtpUri? uri = null)
        {
            if (etpAdapter is Energistics.Etp.v11.Etp11Adapter)
            {
                return new Energistics.Etp.v11.Datatypes.ChannelData.ChannelMetadataRecord
                {
                    ChannelUri = uri?.ToString(),
                    ContentType = uri?.ContentType.ToString(),
                    Status = Energistics.Etp.v11.Datatypes.ChannelData.ChannelStatuses.Active,
                    CustomData = new Dictionary<string, Energistics.Etp.v11.Datatypes.DataValue>()
                };
            }

            return new Energistics.Etp.v12.Datatypes.ChannelData.ChannelMetadataRecord
            {
                ChannelUri = uri?.ToString(),
                ContentType = uri?.ContentType.ToString(),
                Status = Energistics.Etp.v12.Datatypes.ChannelData.ChannelStatuses.Active,
                CustomData = new Dictionary<string, Energistics.Etp.v12.Datatypes.DataValue>()
            };
        }

        /// <summary>
        /// Gets the channel status indicator.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="isActive">if set to <c>true</c> is active.</param>
        /// <returns>A channel status indicator.</returns>
        public static int GetChannelStatus(this IEtpAdapter etpAdapter, bool isActive)
        {
            if (etpAdapter is Energistics.Etp.v11.Etp11Adapter)
            {
                return isActive
                    ? (int) Energistics.Etp.v11.Datatypes.ChannelData.ChannelStatuses.Active
                    : (int) Energistics.Etp.v11.Datatypes.ChannelData.ChannelStatuses.Inactive;
            }

            return isActive
                ? (int)Energistics.Etp.v12.Datatypes.ChannelData.ChannelStatuses.Active
                : (int)Energistics.Etp.v12.Datatypes.ChannelData.ChannelStatuses.Inactive;
        }

        /// <summary>
        /// Determines whether the specified index is time.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="index">The index.</param>
        /// <returns><c>true</c> if the index is time-based; otherwise, <c>false</c>.</returns>
        public static bool IsTimeIndex(this IEtpAdapter etpAdapter, IIndexMetadataRecord index)
        {
            if (index == null) return false;

            return etpAdapter is Energistics.Etp.v11.Etp11Adapter
                ? index.IndexKind == (int) Energistics.Etp.v11.Datatypes.ChannelData.ChannelIndexTypes.Time
                : index.IndexKind == (int) Energistics.Etp.v12.Datatypes.ChannelData.ChannelIndexKinds.Time;
        }

        /// <summary>
        /// Creates a new data object instance.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <returns>A new <see cref="IDataObject"/> instance.</returns>
        public static IDataObject CreateDataObject(this IEtpAdapter etpAdapter)
        {
            return etpAdapter is Energistics.Etp.v11.Etp11Adapter
                ? new Energistics.Etp.v11.Datatypes.Object.DataObject()
                : new Energistics.Etp.v12.Datatypes.Object.DataObject() as IDataObject;
        }

        /// <summary>
        /// Creates the server capabilities object for the ETP session.
        /// </summary>
        /// <param name="etpSession">The ETP session.</param>
        /// <param name="supportedObjects">The supported objects.</param>
        /// <param name="supportedEncodings">The supported encodings.</param>
        /// <returns>A new server capabilities instance.</returns>
        public static object CreateServerCapabilities(this IEtpSession etpSession, IList<string> supportedObjects, IList<string> supportedEncodings)
        {
            if (etpSession.Adapter is Energistics.Etp.v11.Etp11Adapter)
            {
                return new Energistics.Etp.v11.Datatypes.ServerCapabilities
                {
                    ApplicationName = etpSession.ApplicationName,
                    ApplicationVersion = etpSession.ApplicationVersion,
                    SupportedProtocols = etpSession.GetSupportedProtocols()
                        .Cast<Energistics.Etp.v11.Datatypes.SupportedProtocol>()
                        .ToList(),
                    SupportedObjects = supportedObjects,
                    SupportedEncodings = string.Join(";", supportedEncodings),
                    ContactInformation = new Energistics.Etp.v11.Datatypes.Contact
                    {
                        OrganizationName = WitsmlSettings.DefaultVendorName,
                        ContactName = WitsmlSettings.DefaultContactName,
                        ContactEmail = WitsmlSettings.DefaultContactEmail,
                        ContactPhone = WitsmlSettings.DefaultContactPhone
                    }
                };
            }

            return new Energistics.Etp.v12.Datatypes.ServerCapabilities
            {
                ApplicationName = etpSession.ApplicationName,
                ApplicationVersion = etpSession.ApplicationVersion,
                SupportedProtocols = etpSession.GetSupportedProtocols()
                    .Cast<Energistics.Etp.v12.Datatypes.SupportedProtocol>()
                    .ToList(),
                SupportedObjects = supportedObjects,
                SupportedEncodings = string.Join(";", supportedEncodings),
                SupportedCompression = new List<string>(),
                ContactInformation = new Energistics.Etp.v12.Datatypes.Contact
                {
                    OrganizationName = WitsmlSettings.DefaultVendorName,
                    ContactName = WitsmlSettings.DefaultContactName,
                    ContactEmail = WitsmlSettings.DefaultContactEmail,
                    ContactPhone = WitsmlSettings.DefaultContactPhone
                }
            };
        }

        /// <summary>
        /// Initializes a new instance of <see cref="IResource" /> using the specified parameters.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uuid">The UUID.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="resourceType">The resource type.</param>
        /// <param name="name">The name.</param>
        /// <param name="count">The count.</param>
        /// <param name="lastChanged">The last changed in microseconds.</param>
        /// <returns>The resource instance.</returns>
        public static IResource CreateResource(this IEtpAdapter etpAdapter, string uuid, EtpUri uri, ResourceTypes resourceType, string name, int count = 0, long lastChanged = 0)
        {
            if (etpAdapter is Energistics.Etp.v11.Etp11Adapter)
                return Discovery11StoreProvider.New(uuid, uri, resourceType, name, count, lastChanged);

            return Discovery12StoreProvider.New(uuid, uri, resourceType, name, count, lastChanged);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="IResource" /> using the specified parameters.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="protocolUri">The protocol URI.</param>
        /// <param name="folderName">The folder name.</param>
        /// <returns>The resource instance.</returns>
        public static IResource NewProtocol(this IEtpAdapter etpAdapter, EtpUri protocolUri, string folderName)
        {
            if (etpAdapter is Energistics.Etp.v11.Etp11Adapter)
                return Discovery11StoreProvider.NewProtocol(protocolUri, folderName);

            return Discovery12StoreProvider.NewProtocol(protocolUri, folderName);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="IResource" /> using the specified parameters.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <param name="contentType">The content type.</param>
        /// <param name="folderName">The folder name.</param>
        /// <param name="childCount">The child count.</param>
        /// <param name="appendFolderName">if set to <c>true</c> append folder name.</param>
        /// <returns>A new <see cref="IResource"/> instance.</returns>
        public static IResource NewFolder(this IEtpAdapter etpAdapter, EtpUri parentUri, EtpContentType contentType, string folderName, int childCount = -1, bool appendFolderName = false)
        {
            if (etpAdapter is Energistics.Etp.v11.Etp11Adapter)
                return Discovery11StoreProvider.NewFolder(parentUri, contentType, folderName, childCount, appendFolderName);

            return Discovery12StoreProvider.NewFolder(parentUri, contentType, folderName, childCount, appendFolderName);
        }

        /// <summary>
        /// Sets the properties of the <see cref="IDataObject" /> instance.
        /// </summary>
        /// <typeparam name="T">The type of entity.</typeparam>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="dataObject">The data object.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="name">The name.</param>
        /// <param name="childCount">The child count.</param>
        /// <param name="lastChanged">The last changed in microseconds.</param>
        /// <param name="compress">if set to <c>true</c> compress the data object.</param>
        public static void SetDataObject<T>(this IEtpAdapter etpAdapter, IDataObject dataObject, T entity, EtpUri uri, string name, int childCount = -1, long lastChanged = 0, bool compress = true)
        {
            if (etpAdapter is Energistics.Etp.v11.Etp11Adapter)
            {
                Store11StoreProvider.SetDataObject(dataObject, entity, uri, name, childCount, lastChanged, compress);
            }
            else
            {
                Store12StoreProvider.SetDataObject(dataObject, entity, uri, name, childCount, lastChanged);
            }
        }

        /// <summary>
        /// Converts a generic, interface-based list to a non-generic collection of the concrete type.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="channels">The channel metadata.</param>
        /// <returns>A non-generic collection.</returns>
        public static IList ToList(this IEtpAdapter etpAdapter, IList<IChannelMetadataRecord> channels)
        {
            return etpAdapter is Energistics.Etp.v11.Etp11Adapter
                ? channels.Cast<Energistics.Etp.v11.Datatypes.ChannelData.ChannelMetadataRecord>().ToList()
                : channels.Cast<Energistics.Etp.v12.Datatypes.ChannelData.ChannelMetadataRecord>().ToList() as IList;
        }

        /// <summary>
        /// Converts a generic, interface-based list to a non-generic collection of the concrete type.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="indexes">The index metadata.</param>
        /// <returns>A non-generic collection.</returns>
        public static IList ToList(this IEtpAdapter etpAdapter, IList<IIndexMetadataRecord> indexes)
        {
            return etpAdapter is Energistics.Etp.v11.Etp11Adapter
                ? indexes.Cast<Energistics.Etp.v11.Datatypes.ChannelData.IndexMetadataRecord>().ToList()
                : indexes.Cast<Energistics.Etp.v12.Datatypes.ChannelData.IndexMetadataRecord>().ToList() as IList;
        }
    }
}
