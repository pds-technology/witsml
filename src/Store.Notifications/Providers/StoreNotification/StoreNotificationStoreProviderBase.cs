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
    public abstract class StoreNotificationStoreProviderBase : StoreNotificationStoreHandler
    {
        protected override void HandleNotificationRequest(MessageHeader header, NotificationRequest request)
        {
            base.HandleNotificationRequest(header, request);
            EnsureConnection();

            // TODO: Keep track of notification subscriptions
        }

        protected override void HandleCancelNotification(MessageHeader header, CancelNotification request)
        {
            base.HandleCancelNotification(header, request);

            // TODO: Remove notification subscription by UUID
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
            }

            base.Dispose(disposing);
        }

        protected virtual void EnsureConnection()
        {
        }

        protected virtual void Disconnect()
        {
        }

        protected virtual void OnNotifyUpsert(string objectType, object dataObject, DateTime dateTime)
        {
            ChangeNotification(new ObjectChange
            {
                ChangeType = ObjectChangeTypes.Upsert,
                ChangeTime = dateTime.ToUnixTimeMicroseconds(),
                DataObject = GetDataObject(objectType, OptionsIn.DataVersion.Version141.Value, dataObject)
            });
        }

        protected virtual void OnNofityDeleted(string objectType, object dataObject, DateTime dateTime)
        {
            DeleteNotification(new ObjectChange
            {
                ChangeType = ObjectChangeTypes.Delete,
                ChangeTime = dateTime.ToUnixTimeMicroseconds(),
                DataObject = GetDataObject(objectType, OptionsIn.DataVersion.Version141.Value, dataObject)
            });
        }

        protected virtual DataObject GetDataObject(string objectType, string version, object dataObject)
        {
            var jObject = dataObject as JObject;

            if (jObject != null)
            {
                // TODO: Map Web API service type to WITSML object type
                var type = ObjectTypes.GetObjectGroupType(objectType, version) ??
                           ObjectTypes.GetObjectType(objectType, version);

                dataObject = jObject.ToObject(type);
            }

            var collection = dataObject as IEnergisticsCollection;
            var iDataObject = collection?.Items?.OfType<IDataObject>().FirstOrDefault();
            var aDataObject = dataObject as Witsml200.AbstractObject;
            var uri = iDataObject?.GetUri() ?? aDataObject?.GetUri() ?? new EtpUri();
            var name = iDataObject?.Name ?? aDataObject?.Citation?.Title;

            var etpDataObject = new DataObject();
            StoreStoreProvider.SetDataObject(etpDataObject, dataObject, uri, name);

            // TODO: Remove DataObject.Data if not requested in original subscription

            return etpDataObject;
        }
    }
}