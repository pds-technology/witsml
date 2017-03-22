//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.1
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
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using Witsml200 = Energistics.DataAccess.WITSML200;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.StoreNotification;
using Newtonsoft.Json.Linq;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Providers.Store;

namespace PDS.WITSMLstudio.Store.Providers.StoreNotification
{
    /// <summary>
    /// Provides a common framework for all Store Notification Store provider implementations.
    /// </summary>
    /// <seealso cref="Energistics.Protocol.StoreNotification.StoreNotificationStoreHandler" />
    public abstract class StoreNotificationStoreProviderBase : StoreNotificationStoreHandler
    {
        private List<NotificationRequest> _requests;

        /// <summary>
        /// Gets the collection of notification requests.
        /// </summary>
        /// <value>The collection of notification requests.</value>
        protected List<NotificationRequest> Requests
            => _requests ?? (_requests = new List<NotificationRequest>());

        /// <summary>
        /// Handles the notification request.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="request">The request.</param>
        protected override void HandleNotificationRequest(MessageHeader header, NotificationRequest request)
        {
            base.HandleNotificationRequest(header, request);
            EnsureConnection();

            if (Requests.Any(x => x.Request.Uuid.EqualsIgnoreCase(request.Request.Uuid)))
            {
                // TODO: Should this be an error?
            }
            else
            {
                Requests.Add(request);
            }
        }

        /// <summary>
        /// Handles the cancel notification.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="request">The request.</param>
        protected override void HandleCancelNotification(MessageHeader header, CancelNotification request)
        {
            base.HandleCancelNotification(header, request);

            var message = Requests.FirstOrDefault(x => x.Request.Uuid.EqualsIgnoreCase(request.RequestUuid));

            if (message == null)
            {
                // TODO: Should this be an error?
            }
            else
            {
                Requests.Remove(message);
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Ensures the connection to the message broker.
        /// </summary>
        protected virtual void EnsureConnection()
        {
        }

        /// <summary>
        /// Disconnects from the message broker.
        /// </summary>
        protected virtual void Disconnect()
        {
        }

        protected virtual void OnNotifyUpserted(string uri, object dataObject, DateTime dateTime)
        {
            var etpUri = new EtpUri(uri);

            ChangeNotification(new ObjectChange
            {
                ChangeType = ObjectChangeTypes.Upsert,
                ChangeTime = dateTime.ToUnixTimeMicroseconds(),
                DataObject = GetDataObject(etpUri.ObjectType, etpUri.Version, dataObject)
            });
        }

        protected virtual void OnNotifyDeleted(string uri, object dataObject, DateTime dateTime)
        {
            var etpUri = new EtpUri(uri);

            DeleteNotification(new ObjectChange
            {
                ChangeType = ObjectChangeTypes.Delete,
                ChangeTime = dateTime.ToUnixTimeMicroseconds(),
                DataObject = GetDataObject(etpUri.ObjectType, etpUri.Version, dataObject)
            });
        }

        protected virtual DataObject GetDataObject(string objectType, string version, object dataObject)
        {
            var jObject = dataObject as JObject;

            if (jObject != null || dataObject is string)
            {
                var type = ObjectTypes.GetObjectGroupType(objectType, version) ??
                           ObjectTypes.GetObjectType(objectType, version);

                dataObject = jObject?.ToObject(type) ??
                    WitsmlParser.Parse(type, WitsmlParser.Parse(dataObject.ToString()).Root);
            }

            var collection = dataObject as IEnergisticsCollection;
            var iDataObject = collection?.Items?.OfType<IDataObject>().FirstOrDefault();
            var cDataObject = iDataObject as ICommonDataObject;
            var aDataObject = dataObject as Witsml200.AbstractObject;

            var uri = iDataObject?.GetUri() ?? aDataObject?.GetUri() ?? new EtpUri();
            var name = iDataObject?.Name ?? aDataObject?.Citation?.Title;
            var lastChanged = cDataObject?.CommonData?.DateTimeLastChange?.ToUnixTimeMicroseconds() ??
                              aDataObject?.Citation?.LastUpdate?.ToUnixTimeMicroseconds();

            var etpDataObject = new DataObject();
            StoreStoreProvider.SetDataObject(etpDataObject, dataObject, uri, name, -1, lastChanged.GetValueOrDefault());

            // TODO: Remove DataObject.Data if not requested in original subscription

            return etpDataObject;
        }
    }
}