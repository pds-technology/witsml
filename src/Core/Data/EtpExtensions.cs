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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;
using Energistics.Etp.Common.Datatypes.Object;
using PDS.WITSMLstudio.Adapters;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Data
{
    /// <summary>
    /// Defines static helper methods for ETP version independent types.
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
        public static EtpUri CreateAndValidateUri(this IProtocolHandler handler, string uri, long messageId = 0)
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
        public static bool ValidateUriObjectType(this IProtocolHandler handler, EtpUri etpUri, long messageId = 0)
        {
            if (!string.IsNullOrWhiteSpace(etpUri.ObjectType))
                return true;

            handler.UnsupportedObject(null, $"{etpUri.Uri}", messageId);
            return false;
        }

        /// <summary>
        /// Validates URI Parent Hierarchy.
        /// </summary>
        /// <param name="etpUri">The ETP URI.</param>
        /// <returns></returns>
        public static bool ValidateUriParentHierarchy(this EtpUri etpUri)
        {
            if (!etpUri.IsValid)
                return false;

            var parentUri = etpUri.Parent;

            if (parentUri.IsRootUri || parentUri.IsBaseUri)
                return true;

            return parentUri.GetObjectIds().All(segment => !string.IsNullOrWhiteSpace(segment.ObjectId));
        }

        /// <summary>
        /// Validates URI Parent Hierarchy.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="etpUri">The ETP URI.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <returns></returns>
        public static bool ValidateUriParentHierarchy(this IProtocolHandler handler, EtpUri etpUri, long messageId = 0)
        {
            if (etpUri.ValidateUriParentHierarchy())
                return true;

            handler.InvalidUri($"{etpUri} is missing the objectId of a parent.", messageId);

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

            // e.g. eml://witsml20/WellboreGeology{s} or eml://witsml20/WellboreGeology(uid)
            if (ObjectTypes.WellboreGeology.EqualsIgnoreCase(objectType)) return true;

            // e.g. eml://witsml20/CuttingsGeology{s} or eml://witsml20/CuttingsGeology(uid)
            if (ObjectTypes.CuttingsGeology.EqualsIgnoreCase(objectType)) return true;

            // e.g. eml://witsml20/InterpretedGeology{s} or eml://witsml20/InterpretedGeology(uid)
            if (ObjectTypes.InterpretedGeology.EqualsIgnoreCase(objectType)) return true;

            // e.g. eml://witsml20/ShowEvaluation{s} or eml://witsml20/ShowEvaluation(uid)
            if (ObjectTypes.ShowEvaluation.EqualsIgnoreCase(objectType)) return true;

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
        /// Gets the ETP protocols metadata.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <returns>A new <see cref="IEtpProtocols"/> instance.</returns>
        public static IEtpProtocols GetEtpProtocols(this IEtpAdapter etpAdapter)
        {
            return etpAdapter.SupportedVersion == EtpVersion.v11
                ? new Etp11Protocols()
                : new Etp12Protocols() as IEtpProtocols;
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
            if (etpAdapter.SupportedVersion == EtpVersion.v11)
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
                    ? Energistics.Etp.v12.Datatypes.ChannelData.ChannelIndexKind.Time
                    : Energistics.Etp.v12.Datatypes.ChannelData.ChannelIndexKind.Depth,
                Direction = isIncreasing
                    ? Energistics.Etp.v12.Datatypes.ChannelData.IndexDirection.Increasing
                    : Energistics.Etp.v12.Datatypes.ChannelData.IndexDirection.Decreasing,
                StartIndex = null,
                EndIndex = null,
                Uom = string.Empty,
                DepthDatum = string.Empty,
                TimeDatum = string.Empty,
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
            if (etpAdapter.SupportedVersion == EtpVersion.v11)
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
                Status = Energistics.Etp.v12.Datatypes.ChannelData.ChannelStatusKind.Active,
                AttributeMetadata = new List<Energistics.Etp.v12.Datatypes.AttributeMetadataRecord>(),
                CustomData = new Dictionary<string, Energistics.Etp.v12.Datatypes.DataValue>(),
                AxisVectorLengths = new List<int>()
            };
        }

        /// <summary>
        /// Creates a new channel metadata record.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <returns>A new <see cref="IMessageHeader" /> instance.</returns>
        public static IMessageHeader CreateChannelMetadataHeader(this IEtpAdapter etpAdapter, long messageId = 0)
        {
            if (etpAdapter.SupportedVersion == EtpVersion.v11)
            {
                return new Energistics.Etp.v11.Datatypes.MessageHeader
                {
                    MessageId = messageId,
                    Protocol = (int)Energistics.Etp.v11.Protocols.ChannelStreaming,
                    MessageType = (int)Energistics.Etp.v11.MessageTypes.ChannelStreaming.ChannelMetadata,
                    MessageFlags = (int)MessageFlags.None,
                    CorrelationId = 0
                };
            }

            return new Energistics.Etp.v11.Datatypes.MessageHeader
            {
                MessageId = messageId,
                Protocol = (int)Energistics.Etp.v12.Protocols.ChannelStreaming,
                MessageType = (int)Energistics.Etp.v12.MessageTypes.ChannelStreaming.ChannelMetadata,
                MessageFlags = (int)MessageFlags.None,
                CorrelationId = 0
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
            if (etpAdapter.SupportedVersion == EtpVersion.v11)
            {
                return isActive
                    ? (int)Energistics.Etp.v11.Datatypes.ChannelData.ChannelStatuses.Active
                    : (int)Energistics.Etp.v11.Datatypes.ChannelData.ChannelStatuses.Inactive;
            }

            return isActive
                ? (int)Energistics.Etp.v12.Datatypes.ChannelData.ChannelStatusKind.Active
                : (int)Energistics.Etp.v12.Datatypes.ChannelData.ChannelStatusKind.Inactive;
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

            return etpAdapter.SupportedVersion == EtpVersion.v11
                ? index.IndexKind == (int)Energistics.Etp.v11.Datatypes.ChannelData.ChannelIndexTypes.Time
                : index.IndexKind == (int)Energistics.Etp.v12.Datatypes.ChannelData.ChannelIndexKind.Time;
        }

        /// <summary>
        /// Determines whether the specified index is increasing.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="index">The index.</param>
        /// <returns><c>true</c> if the index is increasing; otherwise, <c>false</c>.</returns>
        public static bool IsIncreasing(this IEtpAdapter etpAdapter, IIndexMetadataRecord index)
        {
            if (index == null) return false;

            return etpAdapter.SupportedVersion == EtpVersion.v11
                ? index.Direction == (int)Energistics.Etp.v11.Datatypes.ChannelData.IndexDirections.Increasing
                : index.Direction == (int)Energistics.Etp.v12.Datatypes.ChannelData.IndexDirection.Increasing;
        }

        /// <summary>
        /// Determines the data type of the index.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="index">The index.</param>
        /// <returns><c>true</c> if the index is increasing; otherwise, <c>false</c>.</returns>
        public static string GetDataType(this IEtpAdapter etpAdapter, IIndexMetadataRecord index)
        {
            if (index == null) return string.Empty;

            return etpAdapter.SupportedVersion == EtpVersion.v11
                ? "long"
                : etpAdapter.IsTimeIndex(index) ? "long" : "double";
        }

        /// <summary>
        /// Returns the specified indexValue as an object of the correct type.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="index">The index metadata.</param>
        /// <param name="indexValue">The index value.</param>
        /// <returns>
        ///   <c>true</c> if the index is increasing; otherwise, <c>false</c>.
        /// </returns>
        public static object GetIndexValue(this IEtpAdapter etpAdapter, IIndexMetadataRecord index, long? indexValue)
        {
            if (index == null) return string.Empty;

            var value = indexValue ?? 0;

            if (etpAdapter.SupportedVersion == EtpVersion.v11 || etpAdapter.IsTimeIndex(index))
                return value;

            return (double)value;
        }

        /// <summary>
        /// Creates a new data item instance.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <returns>A new <see cref="IDataItem"/> instance.</returns>
        public static IDataItem CreateDataItem(this IEtpAdapter etpAdapter)
        {
            return etpAdapter.SupportedVersion == EtpVersion.v11
                ? (IDataItem)new Energistics.Etp.v11.Datatypes.ChannelData.DataItem()
                : new Energistics.Etp.v12.Datatypes.ChannelData.DataItem();
        }

        /// <summary>
        /// Creates a new data object instance.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <returns>A new <see cref="IDataValue"/> instance.</returns>
        public static IDataValue CreateDataValue(this IEtpAdapter etpAdapter)
        {
            return etpAdapter.SupportedVersion == EtpVersion.v11
                ? (IDataValue)new Energistics.Etp.v11.Datatypes.DataValue()
                : new Energistics.Etp.v12.Datatypes.DataValue();
        }

        /// <summary>
        /// Creates the channel streaming information instance.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <returns>An <see cref="IChannelStreamingInfo"/> instance</returns>
        public static IChannelStreamingInfo CreateChannelStreamingInfo(this IEtpAdapter etpAdapter)
        {
            return etpAdapter.SupportedVersion == EtpVersion.v11
                ? new Energistics.Etp.v11.Datatypes.ChannelData.ChannelStreamingInfo()
                : null;
        }

        /// <summary>
        /// Creates the streaming start index instance.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <returns>An <see cref="IStreamingStartIndex"/> instance</returns>
        public static IStreamingStartIndex CreateStreamingStartIndex(this IEtpAdapter etpAdapter)
        {
            return etpAdapter.SupportedVersion == EtpVersion.v11
                ? new Energistics.Etp.v11.Datatypes.ChannelData.StreamingStartIndex()
                : null;
        }

        /// <summary>
        /// Casts a list of data attributes to the correct type
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="dataAttributes">The data attributes.</param>
        /// <returns>
        /// A new <see cref="IDataObject" /> instance.
        /// </returns>
        public static IList ToDataAttributes(this IEtpAdapter etpAdapter, IList dataAttributes)
        {
            var castedDataAttribtes = new List<IDataAttribute>();

            foreach (var dataAttribute in dataAttributes)
            {
                if (etpAdapter.SupportedVersion == EtpVersion.v11)
                    castedDataAttribtes.Add(dataAttribute as Energistics.Etp.v11.Datatypes.DataAttribute);
                else
                    castedDataAttribtes.Add(dataAttribute as Energistics.Etp.v12.Datatypes.DataAttribute);
            }

            return castedDataAttribtes;
        }

        /// <summary>
        /// Creates a new data object instance.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <returns>A new <see cref="IDataObject"/> instance.</returns>
        public static IDataObject CreateDataObject(this IEtpAdapter etpAdapter)
        {
            return etpAdapter.SupportedVersion == EtpVersion.v11
                ? new Energistics.Etp.v11.Datatypes.Object.DataObject()
                : new Energistics.Etp.v12.Datatypes.Object.DataObject() as IDataObject;
        }

        /// <summary>
        /// Converts a generic, interface-based list to a non-generic collection of the concrete type.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="indexes">The index metadata.</param>
        /// <returns>A non-generic collection.</returns>
        public static IList ToList(this IEtpAdapter etpAdapter, IList<IIndexMetadataRecord> indexes)
        {
            return etpAdapter.SupportedVersion == EtpVersion.v11
                ? indexes.Cast<Energistics.Etp.v11.Datatypes.ChannelData.IndexMetadataRecord>().ToList()
                : indexes.Cast<Energistics.Etp.v12.Datatypes.ChannelData.IndexMetadataRecord>().ToList() as IList;
        }

        /// <summary>
        /// Converts a generic, interface-based list to a non-generic collection of the concrete type.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="channels">The channel metadata.</param>
        /// <returns>A non-generic collection.</returns>
        public static IList ToList(this IEtpAdapter etpAdapter, IList<IChannelMetadataRecord> channels)
        {
            return etpAdapter.SupportedVersion == EtpVersion.v11
                ? channels.Cast<Energistics.Etp.v11.Datatypes.ChannelData.ChannelMetadataRecord>().ToList()
                : channels.Cast<Energistics.Etp.v12.Datatypes.ChannelData.ChannelMetadataRecord>().ToList() as IList;
        }

        /// <summary>
        /// Converts a generic collection to an ETP array.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="value">The collection of values.</param>
        /// <returns>A new ETP array instance.</returns>
        public static object ToArray(this IEtpAdapter etpAdapter, object value)
        {
            var values = value as IList;
            var type = value?.GetType().GetGenericArguments()[0];

            if (type == typeof(double))
            {
                return etpAdapter.SupportedVersion == EtpVersion.v11
                    ? new Energistics.Etp.v11.Datatypes.ArrayOfDouble { Values = (IList<double>)values }
                    : new Energistics.Etp.v12.Datatypes.ArrayOfDouble { Values = (IList<double>)values } as object;
            }
            if (type == typeof(float))
            {
                return etpAdapter.SupportedVersion == EtpVersion.v11
                    ? new Energistics.Etp.v11.Datatypes.ArrayOfFloat { Values = (IList<float>)values }
                    : new Energistics.Etp.v12.Datatypes.ArrayOfFloat { Values = (IList<float>)values } as object;
            }
            if (type == typeof(long))
            {
                return etpAdapter.SupportedVersion == EtpVersion.v11
                    ? new Energistics.Etp.v11.Datatypes.ArrayOfLong { Values = (IList<long>)values }
                    : new Energistics.Etp.v12.Datatypes.ArrayOfLong { Values = (IList<long>)values } as object;
            }
            if (type == typeof(int))
            {
                return etpAdapter.SupportedVersion == EtpVersion.v11
                    ? new Energistics.Etp.v11.Datatypes.ArrayOfInt { Values = (IList<int>)values }
                    : new Energistics.Etp.v12.Datatypes.ArrayOfInt { Values = (IList<int>)values } as object;
            }
            if (type == typeof(bool))
            {
                return etpAdapter.SupportedVersion == EtpVersion.v11
                    ? new Energistics.Etp.v11.Datatypes.ArrayOfBoolean { Values = (IList<bool>)values }
                    : new Energistics.Etp.v12.Datatypes.ArrayOfBoolean { Values = (IList<bool>)values } as object;
            }

            return etpAdapter.SupportedVersion == EtpVersion.v11
                ? new Energistics.Etp.v11.Datatypes.AnyArray { Item = values }
                : new Energistics.Etp.v12.Datatypes.AnyArray { Item = values } as object;
        }

        /// <summary>
        /// Creates a new <see cref="IDataItem"/> instance using the specified parameters.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="value">The channel data value.</param>
        /// <param name="indexes">The channel index values.</param>
        /// <param name="attributes">The data attributes.</param>
        /// <returns>A new <see cref="IDataItem"/> instance.</returns>
        public static IDataItem CreateDataItem(this IEtpAdapter etpAdapter, long channelId, object value = null, IList<object> indexes = null, IList<object> attributes = null)
        {
            if (etpAdapter.SupportedVersion == EtpVersion.v11)
            {
                return new Energistics.Etp.v11.Datatypes.ChannelData.DataItem
                {
                    ChannelId = channelId,
                    Indexes = indexes?.Cast<long>().ToArray() ?? new long[0],
                    Value = new Energistics.Etp.v11.Datatypes.DataValue { Item = value },
                    ValueAttributes = attributes?
                        .Select((x, i) => new Energistics.Etp.v11.Datatypes.DataAttribute
                        {
                            AttributeId = i,
                            AttributeValue = new Energistics.Etp.v11.Datatypes.DataValue { Item = x }
                        })
                        .ToArray() ?? new Energistics.Etp.v11.Datatypes.DataAttribute[0]
                };
            }

            return new Energistics.Etp.v12.Datatypes.ChannelData.DataItem
            {
                ChannelId = channelId,
                Indexes = indexes?.Select(i => new Energistics.Etp.v12.Datatypes.IndexValue { Item = i }).ToList() ??
                    new List<Energistics.Etp.v12.Datatypes.IndexValue>(),
                Value = new Energistics.Etp.v12.Datatypes.DataValue { Item = value },
                ValueAttributes = attributes?
                    .Select((x, i) => new Energistics.Etp.v12.Datatypes.DataAttribute
                    {
                        AttributeId = i,
                        AttributeValue = new Energistics.Etp.v12.Datatypes.DataValue { Item = x }
                    })
                    .ToArray() ?? new Energistics.Etp.v12.Datatypes.DataAttribute[0]
            };
        }

        /// <summary>
        /// Merges a sequence of channel streaming contexts based on interval ranges.
        /// </summary>
        /// <param name="adapter">The ETP adapter.</param>
        /// <param name="contexts">The channel streaming contexts.</param>
        /// <returns>Sequence of non-overlapping contexts.</returns>
        public static IEnumerable<ChannelStreamingContext> MergeOverlappingContexts(this IEtpAdapter adapter, IEnumerable<ChannelStreamingContext> contexts)
        {
            var intervalList = contexts.ToList();

            if (!intervalList.Any())
                yield break;

            var isIncreasing = adapter.IsIncreasing(intervalList[0].ChannelMetadata.Indexes[0] as IIndexMetadataRecord);

            var orderedIntervals = isIncreasing ? intervalList.OrderBy(x => x.StartIndex) : intervalList.OrderByDescending(x => x.StartIndex);
            var accumulator = orderedIntervals.First();
            contexts = orderedIntervals.Skip(1);

            foreach (var context in contexts)
            {
                var accumulatorRange = new Range<double?>(accumulator.StartIndex, accumulator.EndIndex);
                var intervalRange = new Range<double?>(context.StartIndex, context.EndIndex);

                if (accumulatorRange.Overlaps(intervalRange, isIncreasing))
                {
                    if (!accumulatorRange.End.HasValue || !intervalRange.End.HasValue)
                    {
                        accumulator.EndIndex = null;
                    }
                    else
                    {
                        accumulator.EndIndex = (long?)new List<Range<double?>>() { accumulatorRange, intervalRange }.GetMaxRangeEnd(isIncreasing);
                    }
                }
                else
                {
                    yield return accumulator;
                    accumulator = context;
                }
            }

            yield return accumulator;
        }
    }
}
