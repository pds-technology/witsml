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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Web.Services.Protocols;
using Energistics.DataAccess;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Properties;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Provides extension methods that can be used with common WITSML types and interfaces.
    /// </summary>
    public static class Extensions
    {
        private static readonly string _defaulWmlstUserAgent = Settings.Default.DefaultWmlsUserAgent;

        /// <summary>
        /// Initializes a new UID value if one was not specified.
        /// </summary>
        /// <typeparam name="T">The type of data object.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <returns>The supplied UID if not null; otherwise, a generated UID.</returns>
        public static string NewUid<T>(this T dataObject) where T : IUniqueId
        {
            return string.IsNullOrEmpty(dataObject.Uid)
                ? Guid.NewGuid().ToString()
                : dataObject.Uid;
        }

        /// <summary>
        /// Initializes a new UUID value if one was not specified.
        /// </summary>
        /// <typeparam name="T">The type of data object.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <returns>The supplied UUID if not null; otherwise, a generated UID.</returns>
        public static string NewUuid<T>(this T dataObject) where T : Witsml200.AbstractObject
        {
            return string.IsNullOrEmpty(dataObject.Uuid)
                ? Guid.NewGuid().ToString()
                : dataObject.Uuid;
        }

        /// <summary>
        /// Gets the description associated with the specified WITSML error code.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <returns>The description for the error code.</returns>
        public static string GetDescription(this ErrorCodes errorCode)
        {
            return Resources.ResourceManager.GetString(errorCode.ToString(), Resources.Culture);
        }

        /// <summary>
        /// Gets the value of the Version property for specified container object.
        /// </summary>
        /// <typeparam name="T">The data object type.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <returns>The value of the Version property.</returns>
        public static string GetVersion<T>(this T dataObject) where T : IEnergisticsCollection
        {
            return (string)dataObject.GetType().GetProperty("Version")?.GetValue(dataObject, null);
        }

        /// <summary>
        /// Sets the value of the Version property for the specified container object.
        /// </summary>
        /// <typeparam name="T">The data object type.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <param name="version">The version.</param>
        /// <returns>The data object instance.</returns>
        public static T SetVersion<T>(this T dataObject, string version) where T : IEnergisticsCollection
        {
            dataObject.GetType().GetProperty("Version")?.SetValue(dataObject, version);
            return dataObject;
        }

        /// <summary>
        /// Sets the User-Agent header sent by the SOAP client proxy.
        /// </summary>
        /// <param name="proxy">The SOAP client proxy.</param>
        /// <param name="userAgent">The user agent.</param>
        /// <returns>The <see cref="SoapHttpClientProtocol"/> instance.</returns>
        public static SoapHttpClientProtocol WithUserAgent(this SoapHttpClientProtocol proxy, string userAgent = null)
        {
            if (proxy == null) return null;

            proxy.UserAgent = userAgent ?? _defaulWmlstUserAgent;

            return proxy;
        }

        /// <summary>
        /// Builds an emtpy WITSML query for the specified data object type and data schema version.
        /// </summary>
        /// <param name="connection">The WITSML connection.</param>
        /// <param name="type">The data object type.</param>
        /// <param name="version">The data schema version.</param>
        /// <returns>An <see cref="IEnergisticsCollection"/> instance.</returns>
        public static IEnergisticsCollection BuildEmptyQuery(this WITSMLWebServiceConnection connection, Type type, string version)
        {
            var method = connection?.GetType()
                .GetMethod("BuildEmptyQuery", BindingFlags.Static | BindingFlags.Public)?
                .MakeGenericMethod(type);

            var query = method?.Invoke(null, null) as IEnergisticsCollection;
            query?.SetVersion(version);

            return query;
        }

        /// <summary>
        /// Creates an <see cref="IEnergisticsCollection"/> container and wraps the current entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>A <see cref="IEnergisticsCollection"/> instance.</returns>
        public static IEnergisticsCollection CreateCollection(this IDataObject entity)
        {
            if (entity == null)
                return null;

            var type = entity.GetType();
            var objectType = ObjectTypes.GetObjectType(type);
            var family = ObjectTypes.GetFamily(type);
            var version = ObjectTypes.GetVersion(type);

            var groupType = ObjectTypes.GetObjectGroupType(objectType, family, version);
            var property = ObjectTypes.GetObjectTypeListPropertyInfo(objectType, family, version);
            var group = Activator.CreateInstance(groupType) as IEnergisticsCollection;
            var list = Activator.CreateInstance(property.PropertyType) as IList;
            if (list == null) return group;

            list.Add(entity);
            property.SetValue(group, list);

            return group;
        }

        /// <summary>
        /// Wraps the specified data object in a <see cref="List{TObject}"/>.
        /// </summary>
        /// <typeparam name="TObject">The type of data object.</typeparam>
        /// <param name="instance">The data object instance.</param>
        /// <returns>A <see cref="List{TObject}"/> instance containing a single item.</returns>
        public static List<TObject> AsList<TObject>(this TObject instance) where TObject : IUniqueId
        {
            return new List<TObject>() { instance };
        }

        /// <summary>
        /// Adds the item to the collection.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="item">The item.</param>
        public static void Add<T>(this ICollection<T> collection, object item)
        {
            collection?.Add((T)item);
        }

        /// <summary>
        /// Adds the item to the collection.
        /// </summary>
        /// <typeparam name="T1">The item type.</typeparam>
        /// <typeparam name="T2">The base type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="item">The item.</param>
        public static void Add<T1, T2>(this ICollection<T1> collection, T2 item) where T1 : T2
        {
            collection?.Add((T1)item);
        }

        /// <summary>
        /// Converts a nullable scaled index to an index nullable double value.
        /// </summary>
        /// <param name="index">The nullable scaled index.</param>
        /// <param name="scale">The scale factor value.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> the index value is passed through, otherwise it is converted.</param>
        /// <returns>The converted index value</returns>
        public static double? IndexFromScale(this long? index, int scale, bool isTimeIndex = false)
        {
            return index?.IndexFromScale(scale, isTimeIndex);
        }

        /// <summary>
        /// Converts a scaled index to an index double value.
        /// </summary>
        /// <param name="index">The scaled index.</param>
        /// <param name="scale">The scale factor value.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> the index value is passed through, otherwise it is converted.</param>
        /// <returns>The converted index value</returns>
        public static double IndexFromScale(this long index, int scale, bool isTimeIndex = false)
        {
            return isTimeIndex
                ? index
                : index * Math.Pow(10, -scale);
        }

        /// <summary>
        /// Converts a nullable index value to a nullable scaled long value.
        /// </summary>
        /// <param name="index">The nullable index value to be scaled.</param>
        /// <param name="scale">The scale factor value.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> the index value is passed through, otherwise it is converted.</param>
        /// <returns>The converted, scaled index value</returns>
        public static long? IndexToScale(this double? index, int scale, bool isTimeIndex = false)
        {
            return index?.IndexToScale(scale, isTimeIndex);
        }

        /// <summary>
        /// Converts an index value to a scaled long value.
        /// </summary>
        /// <param name="index">The index value to be scaled.</param>
        /// <param name="scale">The scale factor value.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> the index value is passed through, otherwise it is converted.</param>
        /// <returns>The converted, scaled index value</returns>
        public static long IndexToScale(this double index, int scale, bool isTimeIndex = false)
        {
            return Convert.ToInt64(
                (isTimeIndex
                    ? index
                    : index * Math.Pow(10, scale))
            );
        }

        /// <summary>
        /// Applies a time zone offset to the current <see cref="Timestamp"/> instance.
        /// </summary>
        /// <param name="value">The timestamp value.</param>
        /// <param name="offset">The offset time span.</param>
        /// <returns>A <see cref="DateTimeOffset"/> instance, or null.</returns>
        public static DateTimeOffset? ToOffsetTime(this Timestamp? value, TimeSpan? offset)
        {
            if (!value.HasValue || !offset.HasValue)
                return value;

            return ((DateTimeOffset)value.Value).ToOffsetTime(offset);
        }

        /// <summary>
        /// Converts the <see cref="Timestamp"/> to unix time microseconds.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns>The timestamp in unix time microseconds</returns>
        public static long ToUnixTimeMicroseconds(this Timestamp timestamp)
        {
            return ((DateTimeOffset)timestamp).ToUnixTimeMicroseconds();
        }

        /// <summary>
        /// Converts the <see cref="Timestamp"/> to unix time microseconds.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns>The timestamp in unix time microseconds</returns>
        public static long? ToUnixTimeMicroseconds(this Timestamp? timestamp)
        {
            return timestamp?.ToUnixTimeMicroseconds();
        }

        /// <summary>
        /// Gets the last changed date time in microseconds.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The last changed date time in microseconds.</returns>
        public static long GetLastChangedMicroseconds(this ICommonDataObject entity)
        {
            return entity?.CommonData?.DateTimeLastChange?.ToUnixTimeMicroseconds() ?? 0;
        }

        /// <summary>
        /// Gets the last changed date time in microseconds.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The last changed date time in microseconds.</returns>
        public static long GetLastChangedMicroseconds(this Witsml200.AbstractObject entity)
        {
            return entity?.Citation?.LastUpdate?.ToUnixTimeMicroseconds() ?? 0;
        }

        /// <summary>
        /// Gets the non conforming error code.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns>The proper error code based on the specified function.</returns>
        public static ErrorCodes GetNonConformingErrorCode(this Functions function)
        {
            return function == Functions.UpdateInStore
                ? ErrorCodes.UpdateTemplateNonConforming
                : ErrorCodes.InputTemplateNonConforming;
        }

        /// <summary>
        /// Converts input template non-conforming error codes to the correct error code depending on whether the request is compressed or not.
        /// </summary>
        /// <param name="errorCode">The initial error code.</param>
        /// <param name="requestCompressed">Whether or not the input request was compressed.</param>
        /// <returns><see cref="ErrorCodes.CompressedInputNonConforming"/> if <paramref name="requestCompressed"/> is
        /// <see cref="ErrorCodes.InputTemplateNonConforming"/> and <paramref name="errorCode"/> is <c>true</c>; otherwise
        /// the input errror code.</returns>
        public static ErrorCodes CorrectNonConformingErrorCodes(this ErrorCodes errorCode, bool requestCompressed)
        {
            if (requestCompressed && errorCode == ErrorCodes.InputTemplateNonConforming)
                return ErrorCodes.CompressedInputNonConforming;

            return errorCode;
        }

        /// <summary>
        /// Gets the missing element uid error code.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns>The proper error code based on the specified function.</returns>
        public static ErrorCodes GetMissingElementUidErrorCode(this Functions function)
        {
            return function == Functions.AddToStore
                ? ErrorCodes.MissingElementUidForAdd
                : function == Functions.DeleteFromStore
                    ? ErrorCodes.MissingElementUidForDelete
                    : ErrorCodes.MissingElementUidForUpdate;
        }

        /// <summary>
        /// Gets the missing uom value error code.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns>The proper error code based on the specified function.</returns>
        public static ErrorCodes GetMissingUomValueErrorCode(this Functions function)
        {
            return (function == Functions.AddToStore || function == Functions.UpdateInStore)
                ? ErrorCodes.MissingUnitForMeasureData
                : ErrorCodes.EmptyUomSpecified;
        }

        /// <summary>
        /// Gets the object growing status.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        public static bool? GetObjectGrowingStatus(this IDataObject dataObject)
        {
            var log131 = dataObject as Witsml131.Log;
            var log141 = dataObject as Witsml141.Log;
            var trajectory131 = dataObject as Witsml131.Trajectory;
            var trajectory141 = dataObject as Witsml141.Trajectory;
            var mudLog131 = dataObject as Witsml131.MudLog;
            var mudLog141 = dataObject as Witsml141.MudLog;

            return log131?.ObjectGrowing ?? log141?.ObjectGrowing
                   ?? trajectory131?.ObjectGrowing ?? trajectory141?.ObjectGrowing
                   ?? mudLog131?.ObjectGrowing ?? mudLog141?.ObjectGrowing;
        }

        /// <summary>
        /// Gets the wellbore status.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        public static bool? GetWellboreStatus(this IDataObject dataObject)
        {
            var wellbore141 = dataObject as Witsml141.Wellbore;

            return wellbore141?.IsActive;
        }

        /// <summary>
        /// Gets the start index.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="displayTimeOffset">The display time offset.</param>
        /// <returns></returns>
        public static string GetStartIndex(this IWellboreObject dataObject, TimeSpan? displayTimeOffset = null)
        {
            var startIndex = string.Empty;

            var log131 = dataObject as Witsml131.Log;
            var log141 = dataObject as Witsml141.Log;
            var mudLog131 = dataObject as Witsml131.MudLog;
            var mudLog141 = dataObject as Witsml141.MudLog;
            var trajectory131 = dataObject as Witsml131.Trajectory;
            var trajectory141 = dataObject as Witsml141.Trajectory;

            if (log131 == null && log141 == null && mudLog131 == null && mudLog141 == null && trajectory131 == null && trajectory141 == null)
                return null;

            if (log131 != null || log141 != null)
            {
                if (log131?.IndexType == Witsml131.ReferenceData.LogIndexType.datetime ||
                    log141?.IndexType == Witsml141.ReferenceData.LogIndexType.datetime)
                {
                    var isStartIndexSpecified = log131?.StartDateTimeIndexSpecified ?? log141.StartDateTimeIndexSpecified;

                    if (isStartIndexSpecified)
                    {
                        if (displayTimeOffset != null)
                            startIndex =
                                log131?.StartDateTimeIndex.ToDisplayDateTime(displayTimeOffset.Value) ??
                                log141?.StartDateTimeIndex.ToDisplayDateTime(displayTimeOffset.Value);
                        else
                            startIndex =
                                log131?.StartDateTimeIndex?.ToString() ??
                                log141?.StartDateTimeIndex?.ToString();
                    }
                }
                else
                {
                    startIndex =
                        log131?.StartIndex?.ToString() ??
                        log141?.StartIndex?.ToString();
                }
            }

            if (mudLog131 != null || mudLog141 != null)
            {
                startIndex =
                    mudLog131?.StartMD?.Value.ToString(CultureInfo.InvariantCulture) ??
                    mudLog141?.StartMD?.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (trajectory131 != null || trajectory141 != null)
            {
                startIndex =
                    trajectory131?.MDMin?.Value.ToString(CultureInfo.InvariantCulture) ??
                    trajectory141?.MDMin?.Value.ToString(CultureInfo.InvariantCulture);
            }

            return startIndex;
        }

        /// <summary>
        /// Gets the end index.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="displayTimeOffset">The display time offset.</param>
        /// <returns></returns>
        public static string GetEndIndex(this IWellboreObject dataObject, TimeSpan? displayTimeOffset = null)
        {
            var endIndex = string.Empty;

            var log131 = dataObject as Witsml131.Log;
            var log141 = dataObject as Witsml141.Log;
            var mudLog131 = dataObject as Witsml131.MudLog;
            var mudLog141 = dataObject as Witsml141.MudLog;
            var trajectory131 = dataObject as Witsml131.Trajectory;
            var trajectory141 = dataObject as Witsml141.Trajectory;

            if (log131 == null && log141 == null && mudLog131 == null && mudLog141 == null && trajectory131 == null && trajectory141 == null)
                return null;

            if (log131 != null || log141 != null)
            {
                if (log131?.IndexType == Witsml131.ReferenceData.LogIndexType.datetime ||
                    log141?.IndexType == Witsml141.ReferenceData.LogIndexType.datetime)
                {
                    var isEndIndexSpecified = log131?.EndDateTimeIndexSpecified ?? log141.EndDateTimeIndexSpecified;

                    if (isEndIndexSpecified)
                    {
                        if (displayTimeOffset != null)
                            endIndex =
                                log131?.EndDateTimeIndex.ToDisplayDateTime(displayTimeOffset.Value) ??
                                log141?.EndDateTimeIndex.ToDisplayDateTime(displayTimeOffset.Value);
                        else
                            endIndex =
                                log131?.EndDateTimeIndex?.ToString() ??
                                log141?.EndDateTimeIndex?.ToString();
                    }
                }
                else
                {
                    endIndex =
                        log131?.EndIndex?.ToString() ??
                        log141?.EndIndex?.ToString();
                }
            }

            if (mudLog131 != null || mudLog141 != null)
            {
                endIndex =
                    mudLog131?.EndMD?.Value.ToString(CultureInfo.InvariantCulture) ??
                    mudLog141?.EndMD?.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (trajectory131 != null || trajectory141 != null)
            {
                endIndex =
                    trajectory131?.MDMax?.Value.ToString(CultureInfo.InvariantCulture) ??
                    trajectory141?.MDMax?.Value.ToString(CultureInfo.InvariantCulture);
            }

            return endIndex;
        }

        /// <summary>
        /// Determines if the growing object object is empty or not.
        /// </summary>
        /// <param name="dataObject">The growing data object.</param>
        /// <returns>true if the object is empty; false otherwise</returns>
        public static bool? IsGrowingObjectEmpty(this IWellboreObject dataObject)
        {
            var startIndex = dataObject.GetStartIndex();
            var endIndex = dataObject.GetEndIndex();

            if (startIndex == null || endIndex == null) return null;

            return string.IsNullOrWhiteSpace(startIndex) && string.IsNullOrWhiteSpace(endIndex);
        }

        /// <summary>
        /// Determines whether the log has the specified index type.
        /// </summary>
        /// <param name="log">The data object.</param>
        /// <param name="indexType">The index type.</param>
        /// <returns></returns>
        public static bool HasSpecifiedIndexType(this IWellboreObject log, string indexType)
        {
            var log131 = log as Witsml131.Log;
            var log141 = log as Witsml141.Log;

            if (log131 != null && log131.IndexType.ToString().EqualsIgnoreCase(indexType))
                return true;

            return log141 != null && log141.IndexType.ToString().EqualsIgnoreCase(indexType);
        }
    }
}
