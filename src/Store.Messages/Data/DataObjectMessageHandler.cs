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
using System.Web;
using Energistics.Etp.Common.Datatypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Provides common properties and methods for all strongly typed data object message handlers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.DataObjectMessageHandler" />
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.IDataObjectMessageHandler{T}" />
    public class DataObjectMessageHandler<T> : DataObjectMessageHandler, IDataObjectMessageHandler<T>
    {
    }

    /// <summary>
    /// Default data object message handler implementation.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.DataObjectMessageHandler" />
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.IDataObjectMessageHandler{T}" />
    public class DataObjectMessageHandler : IDataObjectMessageHandler
    {
        /// <summary>
        /// Gets the json serializer settings.
        /// </summary>
        protected virtual JsonSerializerSettings JsonSettings { get; } = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new MessageContractResolver(),
            Converters = new JsonConverter[]
            {
                new StringEnumConverter(),
                new TimestampConverter()
            }
        };

        /// <summary>
        /// Creates the message.
        /// </summary>
        /// <param name="objectType">The object type.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        public virtual DataObjectMessage CreateMessage(string objectType, EtpUri uri, object dataObject = null)
        {
            var message = CreateDataObjectMessage(uri, dataObject);
            var witsmlContext = WitsmlOperationContext.Current;
            //var serviceContext = WebOperationContext.Current;
            var httpContext = HttpContext.Current;
            var httpRequest = httpContext?.Request;

            if (httpRequest != null)
            {
                message.UserHost = httpRequest.UserHostAddress;
                message.UserAgent = httpRequest.UserAgent;
                message.Username = httpContext.User?.Identity?.Name;

                if (httpContext.IsWebSocketRequest)
                {
                    // TODO: What is needed for ETP connections?
                }
            }

            message.Function = witsmlContext.Request.Function;
            message.OptionsIn = witsmlContext.Request.Options;
            message.ObjectType = objectType;

            var commonDataObject = dataObject as Energistics.DataAccess.ICommonDataObject;
            var abstractDataObject = dataObject as Energistics.DataAccess.WITSML200.AbstractObject;

            message.CreatedDateTime = commonDataObject?.CommonData?.DateTimeCreation == null
                ? abstractDataObject?.Citation?.Creation?.ToUniversalTime()
                : ((DateTimeOffset) commonDataObject.CommonData.DateTimeCreation.Value).UtcDateTime;

            message.LastUpdateDateTime = commonDataObject?.CommonData?.DateTimeLastChange == null
                ? abstractDataObject?.Citation?.LastUpdate?.ToUniversalTime()
                : ((DateTimeOffset) commonDataObject.CommonData.DateTimeLastChange.Value).UtcDateTime;

            return message;
        }

        /// <summary>
        /// Creates the data object message.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        public virtual DataObjectMessage CreateDataObjectMessage(EtpUri uri, object dataObject = null)
        {
            return new DataObjectMessage(uri, dataObject);
        }

        /// <summary>
        /// Formats the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public virtual string FormatMessage(DataObjectMessage message)
        {
#if DEBUG
            return JsonConvert.SerializeObject(message, Formatting.Indented, JsonSettings);
#else
            return JsonConvert.SerializeObject(message, Formatting.None, JsonSettings);
#endif
        }

        /// <summary>
        /// Determines whether the message is valid to be sent via the specified topic name.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="topicName">The topic name.</param>
        /// <returns></returns>
        public virtual bool IsMessageValid(DataObjectMessage message, string topicName)
        {
            return true;
        }

        /// <summary>
        /// Gets the name of the insert topic.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="defaultTopicName">The default topic name.</param>
        /// <returns></returns>
        public virtual string GetInsertTopicName(DataObjectMessage message, string defaultTopicName)
        {
            return defaultTopicName;
        }

        /// <summary>
        /// Gets the name of the update topic.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="defaultTopicName">The default topic name.</param>
        /// <returns></returns>
        public virtual string GetUpdateTopicName(DataObjectMessage message, string defaultTopicName)
        {
            return defaultTopicName;
        }

        /// <summary>
        /// Gets the name of the replace topic.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="defaultTopicName">The default topic name.</param>
        /// <returns></returns>
        public virtual string GetReplaceTopicName(DataObjectMessage message, string defaultTopicName)
        {
            return defaultTopicName;
        }

        /// <summary>
        /// Gets the name of the delete topic.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="defaultTopicName">The default topic name.</param>
        /// <returns></returns>
        public virtual string GetDeleteTopicName(DataObjectMessage message, string defaultTopicName)
        {
            return defaultTopicName;
        }
    }
}