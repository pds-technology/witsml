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
using System.Web;
using Energistics.Etp.Common.Datatypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PDS.WITSMLstudio.Data;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Provides common properties and methods for all strongly typed data object message handlers.
    /// </summary>
    /// <typeparam name="TObject">The data object type.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.DataObjectMessageHandler" />
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.IDataObjectMessageHandler{T}" />
    public class DataObjectMessageHandler<T> : DataObjectMessageHandler, IDataObjectMessageHandler<T>
    {
        /// <summary>
        /// Gets a data object based on the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>A data object retrieved from the data store.</returns>
        public virtual T GetObject(EtpUri uri)
        {
            return default(T);
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public virtual List<T> GetAll(EtpUri? parentUri = null)
        {
            return new List<T>();
        }

        /// <summary>
        /// Gets a collection of data objects based on the specified query template parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <returns>A collection of data objects retrieved from the data store.</returns>
        public virtual List<T> GetAll(WitsmlQueryParser parser)
        {
            return new List<T>();
        }
    }

    /// <summary>
    /// Default data object message handler implementation.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.DataObjectMessageHandler" />
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.IDataObjectMessageHandler{T}" />
    public class DataObjectMessageHandler : IDataObjectMessageHandler
    {
        /// <summary>
        /// Determines whether querying is enabled, e.g. GetFromStore, GetObject, etc.
        /// </summary>
        public virtual bool IsQueryEnabled => false;

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
        /// Gets a value indicating whether validation is enabled for this data object type.
        /// </summary>
        /// <param name="function">The WITSML API method.</param>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object.</param>
        /// <returns><c>true</c> if validation is enabled for this data object type; otherwise, <c>false</c>.</returns>
        public virtual bool IsValidationEnabled(Functions function, WitsmlQueryParser parser, object dataObject) => false;

        /// <summary>
        /// Creates the data object messages.
        /// </summary>
        /// <param name="objectType">The object type.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        public virtual List<DataObjectMessage> CreateMessages(string objectType, EtpUri uri, object dataObject = null)
        {
            var messages = CreateDataObjectMessages(uri, dataObject);
            var witsmlContext = WitsmlOperationContext.Current;
            //var serviceContext = WebOperationContext.Current;
            var httpContext = HttpContext.Current;
            var httpRequest = httpContext?.Request;

            var commonDataObject = dataObject as Energistics.DataAccess.ICommonDataObject;
            var abstractDataObject = dataObject as Energistics.DataAccess.WITSML200.AbstractObject;

            var createdDateTime = commonDataObject?.CommonData?.DateTimeCreation == null
                ? abstractDataObject?.Citation?.Creation?.ToUniversalTime()
                : ((DateTimeOffset) commonDataObject.CommonData.DateTimeCreation.Value).UtcDateTime;

            var lastUpdateDateTime = commonDataObject?.CommonData?.DateTimeLastChange == null
                ? abstractDataObject?.Citation?.LastUpdate?.ToUniversalTime()
                : ((DateTimeOffset) commonDataObject.CommonData.DateTimeLastChange.Value).UtcDateTime;

            foreach (var message in messages)
            {
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
                message.CreatedDateTime = createdDateTime;
                message.LastUpdateDateTime = lastUpdateDateTime;
            }

            return messages;
        }

        /// <summary>
        /// Creates the data object messages.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        public virtual List<DataObjectMessage> CreateDataObjectMessages(EtpUri uri, object dataObject = null)
        {
            return new List<DataObjectMessage>
            {
                new DataObjectMessage(uri, dataObject)
            };
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