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
using System.Threading.Tasks;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Data;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Message data adapter that encapsulates CRUD functionality for WITSML objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.WitsmlDataAdapter{T}" />
    public abstract class MessageDataAdapter<T> : WitsmlDataAdapter<T> where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageDataAdapter{T}"/> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="objectName">The object name.</param>
        protected MessageDataAdapter(IContainer container, ObjectName objectName) : base(container)
        {
            ObjectName = objectName;
            MessageHandler = ResolveHandler();
            MessageProducer = container.Resolve<IDataObjectMessageProducer>();
        }

        /// <summary>
        /// Gets the name and version of the data object.
        /// </summary>
        protected ObjectName ObjectName { get; }

        /// <summary>
        /// Gets the data object message handler.
        /// </summary>
        protected IDataObjectMessageHandler MessageHandler { get; }

        /// <summary>
        /// Gets the strongly typed data object message handler.
        /// </summary>
        protected IDataObjectMessageHandler<T> TypedMessageHandler => MessageHandler as IDataObjectMessageHandler<T>;

        /// <summary>
        /// Gets the message producer.
        /// </summary>
        protected IDataObjectMessageProducer MessageProducer { get; }

        /// <summary>
        /// Gets a value indicating whether validation is enabled for this data adapter.
        /// </summary>
        /// <param name="function">The WITSML API method.</param>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object.</param>
        /// <returns><c>true</c> if validation is enabled for this data adapter; otherwise, <c>false</c>.</returns>
        public override bool IsValidationEnabled(Functions function, WitsmlQueryParser parser, T dataObject)
        {
            return MessageHandler.IsValidationEnabled(function, parser, dataObject);
        }

        /// <summary>
        /// Gets a data object by the specified UUID.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="fields">The requested fields.</param>
        /// <returns>The data object instance.</returns>
        public override T Get(EtpUri uri, params string[] fields)
        {
            return GetEntity(uri, fields);
        }

        /// <summary>
        /// Retrieves data objects from the data store using the specified parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <param name="context">The response context.</param>
        /// <returns>
        /// A collection of data objects retrieved from the data store.
        /// </returns>
        public override List<T> Query(WitsmlQueryParser parser, ResponseContext context)
        {
            if (WitsmlOperationContext.Current != null)
            {
                WitsmlOperationContext.Current.Response = context;
            }

            return QueryEntities(parser);
        }

        /// <summary>
        /// Adds a data object to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be added.</param>
        public override void Add(WitsmlQueryParser parser, T dataObject)
        {
            InsertEntity(dataObject);
        }

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be updated.</param>
        public override void Update(WitsmlQueryParser parser, T dataObject)
        {
            UpdateEntity(dataObject);
            //ValidateUpdatedEntity(Functions.UpdateInStore, GetUri(dataObject));
        }

        /// <summary>
        /// Replaces a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be replaced.</param>
        public override void Replace(WitsmlQueryParser parser, T dataObject)
        {
            ReplaceEntity(dataObject);
            //ValidateUpdatedEntity(Functions.PutObject, GetUri(dataObject));
        }

        /// <summary>
        /// Deletes or partially updates the specified object by uid.
        /// </summary>
        /// <param name="parser">The query parser that specifies the object.</param>
        public override void Delete(WitsmlQueryParser parser)
        {
            var uri = parser.GetUri<T>();

            if (parser.HasElements())
            {
                // TODO: PartialDeleteEntity(parser, uri);
                //ValidateUpdatedEntity(Functions.DeleteFromStore, uri);
                throw new NotImplementedException();
            }
            else
            {
                Delete(uri);
            }
        }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        public override void Delete(EtpUri uri)
        {
            DeleteEntity(uri);
        }

        /// <summary>
        /// Determines whether the entity exists in the data store.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>true if the entity exists; otherwise, false</returns>
        public override bool Exists(EtpUri uri)
        {
            return GetEntity(uri) != null;
        }

        /// <summary>
        /// Gets the count of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>The count of related data objects.</returns>
        public override int Count(EtpUri? parentUri = null)
        {
            return GetAll(parentUri).Count;
        }

        /// <summary>
        /// Determines if the specified URI has child data objects.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>If there are any related data objects.</returns>
        public override bool Any(EtpUri? parentUri = null)
        {
            return Count(parentUri) > 0;
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<T> GetAll(EtpUri? parentUri = null)
        {
            return MessageHandler.IsQueryEnabled
                ? TypedMessageHandler.GetAll(parentUri)
                : new List<T>();
        }

        /// <summary>
        /// Gets a collection of data objects based on the specified query template parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <returns>A collection of data objects retrieved from the data store.</returns>
        protected virtual List<T> GetAll(WitsmlQueryParser parser)
        {
            return MessageHandler.IsQueryEnabled
                ? TypedMessageHandler.GetAll(parser)
                : new List<T>();
        }

        /// <summary>
        /// Gets a data object based on the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>A data object retrieved from the data store.</returns>
        protected virtual T GetObject(EtpUri uri)
        {
            return MessageHandler.IsQueryEnabled
                ? TypedMessageHandler.GetObject(uri)
                : null;
        }

        /// <summary>
        /// Gets an object from the data store by uid
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="fields">The requested fields.</param>
        /// <returns>The entity represented by the indentifier.</returns>
        protected virtual T GetEntity(EtpUri uri, params string[] fields)
        {
            return GetEntity<T>(uri, ObjectName.Name, fields);
        }

        /// <summary>
        /// Gets an object from the data store by uid
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="uri">The data object URI.</param>
        /// <param name="objectType">The object type.</param>
        /// <param name="fields">The requested fields.</param>
        /// <returns>The entity represented by the indentifier.</returns>
        protected virtual TObject GetEntity<TObject>(EtpUri uri, string objectType, params string[] fields) where TObject : class
        {
            try
            {
                Logger.DebugFormat("Querying {0} data object; uid: {1}", objectType, uri.ObjectId);

                // TODO: Is property/element projection required?
                //var fieldList = fields.Any() ? fields.ToList() : null;

                //var client = GetWebApiServiceClient<TObject>(objectType);
                //return client.Get(uri.ObjectId).Result;

                return GetObject(uri) as TObject;
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Error querying {0} data object:{1}{2}", objectType, Environment.NewLine, ex);
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Queries the data store using the specified <see cref="WitsmlQueryParser"/>.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <returns>The query results collection.</returns>
        protected virtual List<T> QueryEntities(WitsmlQueryParser parser)
        {
            return QueryEntities(parser, ObjectName.Name);
        }

        /// <summary>
        /// Queries the data store using the specified <see cref="WitsmlQueryParser" />.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <param name="objectType">The object type.</param>
        /// <returns>The query results collection.</returns>
        /// <exception cref="WitsmlException"></exception>
        protected virtual List<T> QueryEntities(WitsmlQueryParser parser, string objectType)
        {
            //var mapping = GetMapping(objectType);

            if (OptionsIn.RequestObjectSelectionCapability.True.Equals(parser.RequestObjectSelectionCapability()))
            {
                Logger.DebugFormat("Requesting {0} query template.", objectType);
                var template = CreateQueryTemplate();
                return template.AsList();
            }

            var returnElements = parser.ReturnElements();
            Logger.DebugFormat("Querying with return elements '{0}'", returnElements);

            try
            {
                //var fields = GetProjectionPropertyNames(parser);
                //var ignored = GetIgnoredElementNamesForQuery(parser);

                Logger.DebugFormat("Querying {0} data object.", objectType);

                //var query = new SqlQuery<T>(Container, GetDatabase(), mapping, parser, fields, ignored);
                //return query.Execute();

                return GetAll(parser);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Error querying {0} data object: {1}", objectType, ex);

                if (ex is WitsmlException) throw;
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Inserts a data object into the data store.
        /// </summary>
        /// <param name="entity">The object to be inserted.</param>
        protected virtual void InsertEntity(T entity)
        {
            InsertEntity(entity, GetUri(entity), ObjectName.Name);
        }

        /// <summary>
        /// Inserts a data object into the data store.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="entity">The object to be inserted.</param>
        /// <param name="uri">The data object URI.</param>
        /// <param name="objectType">The object type.</param>
        protected virtual void InsertEntity<TObject>(TObject entity, EtpUri uri, string objectType) where TObject : class
        {
            try
            {
                Logger.DebugFormat("Inserting {0} data object.", objectType);

                foreach (var message in MessageHandler.CreateMessages(objectType, uri, entity))
                {
                    var topicName = MessageHandler.GetInsertTopicName(message, WitsmlSettings.GlobalInsertTopicName);

                    if (MessageHandler.IsMessageValid(message, topicName))
                    {
                        SendMessage(message, topicName).Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Error inserting {0} data object:{1}{2}", objectType, Environment.NewLine, ex);

                if (ex is WitsmlException) throw;
                throw new WitsmlException(ErrorCodes.ErrorAddingToDataStore, ex);
            }
        }

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <param name="entity">The object to be updated.</param>
        private void UpdateEntity(T entity)
        {
            UpdateEntity(entity, GetUri(entity), ObjectName.Name);
        }

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="entity">The object to be updated.</param>
        /// <param name="uri">The data object URI.</param>
        /// <param name="objectType">The object type.</param>
        private void UpdateEntity<TObject>(TObject entity, EtpUri uri, string objectType)
        {
            try
            {
                Logger.DebugFormat("Updating {0} data object.", objectType);

                foreach (var message in MessageHandler.CreateMessages(objectType, uri, entity))
                {
                    var topicName = MessageHandler.GetUpdateTopicName(message, WitsmlSettings.GlobalUpdateTopicName);

                    if (MessageHandler.IsMessageValid(message, topicName))
                    {
                        SendMessage(message, topicName).Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Error updating {0} data object:{1}{2}", objectType, Environment.NewLine, ex);

                if (ex is WitsmlException) throw;
                throw new WitsmlException(ErrorCodes.ErrorUpdatingInDataStore, ex);
            }
        }

        /// <summary>
        /// Replaces a data object in the data store.
        /// </summary>
        /// <param name="entity">The object to be replaced.</param>
        private void ReplaceEntity(T entity)
        {
            ReplaceEntity(entity, GetUri(entity), ObjectName.Name);
        }

        /// <summary>
        /// Replaces a data object in the data store.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="entity">The object to be replaced.</param>
        /// <param name="uri">The data object URI.</param>
        /// <param name="objectType">The object type.</param>
        private void ReplaceEntity<TObject>(TObject entity, EtpUri uri, string objectType) where TObject : class
        {
            try
            {
                Logger.DebugFormat("Replacing {0} data object.", objectType);

                foreach (var message in MessageHandler.CreateMessages(objectType, uri, entity))
                {
                    var topicName = MessageHandler.GetReplaceTopicName(message, WitsmlSettings.GlobalReplaceTopicName);

                    if (MessageHandler.IsMessageValid(message, topicName))
                    {
                        SendMessage(message, topicName).Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Error replacing {0} data object:{1}{2}", objectType, Environment.NewLine, ex);

                if (ex is WitsmlException) throw;
                throw new WitsmlException(ErrorCodes.ErrorReplacingInDataStore, ex);
            }
        }

        /// <summary>
        /// Deletes a data object from the data store.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        protected virtual void DeleteEntity(EtpUri uri)
        {
            DeleteEntity<T>(uri, ObjectName.Name);
        }

        /// <summary>
        /// Deletes a data object from the data store.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="uri">The data object URI.</param>
        /// <param name="objectType">The object type.</param>
        protected virtual void DeleteEntity<TObject>(EtpUri uri, string objectType) where TObject : class
        {
            try
            {
                Logger.DebugFormat("Deleting {0} data object.", objectType);

                foreach (var message in MessageHandler.CreateMessages(objectType, uri))
                {
                    var topicName = MessageHandler.GetDeleteTopicName(message, WitsmlSettings.GlobalDeleteTopicName);

                    if (MessageHandler.IsMessageValid(message, topicName))
                    {
                        SendMessage(message, topicName).Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Error deleting {0} data object:{1}{2}", objectType, Environment.NewLine, ex);

                if (ex is WitsmlException) throw;
                throw new WitsmlException(ErrorCodes.ErrorDeletingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="topicName">The topic name.</param>
        protected virtual async Task SendMessage(DataObjectMessage message, string topicName)
        {
            try
            {
                var payload = MessageHandler.FormatMessage(message);
                await MessageProducer.SendMessageAsync(topicName, message.Uri, payload);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Error sending message for topic: {topicName}", ex);
                throw;
            }
        }

        /// <summary>
        /// Resolves the data object message handler for the current data object type.
        /// </summary>
        /// <returns></returns>
        private IDataObjectMessageHandler ResolveHandler()
        {
            try
            {
                // Try to resolve object/version specific handler
                return Container.Resolve<IDataObjectMessageHandler<T>>();
            }
            catch
            {
                try
                {
                    // Else, ry to resolve object specific handler
                    return Container.Resolve<IDataObjectMessageHandler>(ObjectName.Name);
                }
                catch
                {
                    try
                    {
                        // Else, try to resolve global handler
                        return Container.Resolve<IDataObjectMessageHandler>();
                    }
                    catch
                    {
                        // Otherwise, use default handler
                        return new DataObjectMessageHandler();
                    }
                }
            }
        }
    }
}