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

using System.Collections.Generic;
using Energistics.Etp.Common.Datatypes;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Defines common properties and methods for all strongly typed data object message handlers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDataObjectMessageHandler<T> : IDataObjectMessageHandler
    {
    }

    /// <summary>
    /// Defines common properties and methods for all data object message handlers.
    /// </summary>
    public interface IDataObjectMessageHandler
    {
        /// <summary>
        /// Determines whether querying is enabled, e.g. GetFromStore, GetObject, etc.
        /// </summary>
        bool IsQueryEnabled { get; }

        /// <summary>
        /// Creates the data object messages.
        /// </summary>
        /// <param name="objectType">The object type.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        List<DataObjectMessage> CreateMessages(string objectType, EtpUri uri, object dataObject = null);

        /// <summary>
        /// Formats the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        string FormatMessage(DataObjectMessage message);

        /// <summary>
        /// Determines whether the message is valid to be sent via the specified topic name.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="topicName">The topic name.</param>
        /// <returns></returns>
        bool IsMessageValid(DataObjectMessage message, string topicName);

        /// <summary>
        /// Gets the name of the insert topic.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="defaultTopicName">The default topic name.</param>
        /// <returns></returns>
        string GetInsertTopicName(DataObjectMessage message, string defaultTopicName);

        /// <summary>
        /// Gets the name of the update topic.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="defaultTopicName">The default topic name.</param>
        /// <returns></returns>
        string GetUpdateTopicName(DataObjectMessage message, string defaultTopicName);

        /// <summary>
        /// Gets the name of the replace topic.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="defaultTopicName">The default topic name.</param>
        /// <returns></returns>
        string GetReplaceTopicName(DataObjectMessage message, string defaultTopicName);

        /// <summary>
        /// Gets the name of the delete topic.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="defaultTopicName">The default topic name.</param>
        /// <returns></returns>
        string GetDeleteTopicName(DataObjectMessage message, string defaultTopicName);

        /// <summary>
        /// Gets a data object based on the specified URI.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="uri">The data object URI.</param>
        /// <returns>A data object retrieved from the data store.</returns>
        TObject GetObject<TObject>(EtpUri uri);

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        List<TObject> GetAll<TObject>(EtpUri? parentUri = null);

        /// <summary>
        /// Gets a collection of data objects based on the specified query template parser.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="parser">The query template parser.</param>
        /// <returns>A collection of data objects retrieved from the data store.</returns>
        List<TObject> GetAll<TObject>(WitsmlQueryParser parser);
    }
}