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
using System.Collections.Generic;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.Object;
using MongoDB.Bson;
using MongoDB.Driver;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Data.Transactions;
using Energistics.Etp.Common;

namespace PDS.WITSMLstudio.Store.Data.GrowingObjects
{
    /// <summary>
    /// Provides properties and methods common to all growing data objects.
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.MongoDbDataAdapter{T}" />
    public abstract class GrowingObjectDataAdapterBase<T> : MongoDbDataAdapter<T>, IGrowingObjectDataAdapter
    {
        private IGrowingObjectDataProvider _dbGrowingObjectDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="GrowingObjectDataAdapterBase{T}"/> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="dbCollectionName">The database collection name.</param>
        /// <param name="idPropertyName">The name of the identifier property.</param>
        /// <param name="namePropertyName">The name of the object name property</param>
        protected GrowingObjectDataAdapterBase(IContainer container, IDatabaseProvider databaseProvider, string dbCollectionName, string idPropertyName = ObjectTypes.Uid, string namePropertyName = ObjectTypes.NameProperty)
            : base(container, databaseProvider, dbCollectionName, idPropertyName, namePropertyName)
        {
        }

        /// <summary>
        /// Gets a reference to the growing object data adapter.
        /// </summary>
        /// <value>The growing object data adapter.</value>
        protected IGrowingObjectDataProvider DbGrowingObjectAdapter
        {
            get { return _dbGrowingObjectDataAdapter ?? (_dbGrowingObjectDataAdapter = Container.Resolve<IGrowingObjectDataProvider>()); }
        }

        /// <summary>
        /// Updates the objectGrowing flag for a growing object.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="isObjectGrowing">Is the object currently growing.</param>
        public virtual void UpdateObjectGrowing(EtpUri uri, bool isObjectGrowing)
        {
            var current = GetEntity(uri);

            if (current == null)
            {
                Logger.DebugFormat("Growing object not found with uri '{0}'", uri);
                return;
            }

            UpdateGrowingObject(current, null, isObjectGrowing);
        }

        /// <summary>
        /// Determines whether this instance can save the data portion of the growing object.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this instance can save the data portion of the growing object; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanSaveData()
        {
            return true;
        }

        /// <summary>
        /// Gets the growing part having the specified UID for a growing object.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="uid">The growing part's uid.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual IDataObject GetGrowingPart(IEtpAdapter etpAdapter, EtpUri uri, string uid)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the growing parts for a growing object within the specified index range.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual List<IDataObject> GetGrowingParts(IEtpAdapter etpAdapter, EtpUri uri, object startIndex, object endIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Puts the growing part for a growing object.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual void PutGrowingPart(IEtpAdapter etpAdapter, EtpUri uri, string contentType, byte[] data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes the growing part having the specified UID for a growing object.
        /// </summary>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="uid">The growing part's uid.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual void DeleteGrowingPart(EtpUri uri, string uid)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes the growing parts for a growing object within the specified index range.
        /// </summary>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual void DeleteGrowingParts(EtpUri uri, object startIndex, object endIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the wellbore URI from the specified childUri
        /// </summary>
        /// <param name="childUri">The child URI.</param>
        /// <returns>The wellbore uri from a specified childUri</returns>
        protected virtual EtpUri GetWellboreUri(EtpUri childUri)
        {
            return childUri.Parent;
        }

        /// <summary>
        /// Determines whether the objectGrowing flag is true for the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        ///   <c>true</c> if the objectGrowing flag is true for the specified entity; otherwise, <c>false</c>.
        /// </returns>
        protected abstract bool IsObjectGrowing(T entity);

        /// <summary>
        /// Updates the growing status for the specified growing object. If isObjectGrowing has a value it will update
        /// the objectGrowing flag for the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="updates">The header update definition.</param>
        /// <param name="isObjectGrowing">Is the object currently growing.</param>
        protected virtual void UpdateGrowingObject(T entity, UpdateDefinition<T> updates, bool? isObjectGrowing = null)
        {
            var uri = GetUri(entity);
            var isCurrentObjectGrowing = IsObjectGrowing(entity);
            var isAuditUpdate = !isObjectGrowing.GetValueOrDefault();

            // Set change history object growing flag
            var changeHistory = AuditHistoryAdapter.GetCurrentChangeHistory();
            changeHistory.ObjectGrowingState = isCurrentObjectGrowing;

            // Check to see if the object growing flag needs to be toggled
            if (isObjectGrowing.HasValue && isCurrentObjectGrowing != isObjectGrowing)
            {
                // Only allow DbGrowingObjectDataAdapter to set flag to false
                Logger.Debug($"Updating object growing flag for URI: {uri}; Value: {isObjectGrowing.Value}");
                var flag = MongoDbUtility.CreateObjectGrowingFields<T>(isObjectGrowing.Value);
                updates = MongoDbUtility.BuildUpdate(updates, flag);

                // Only audit an append of data when first toggling object growing flag
                changeHistory.ObjectGrowingState = isObjectGrowing;
                isAuditUpdate = true;
            }

            UpdateGrowingObject(uri, updates, isAuditUpdate);

            // If the object is not currently growing do not update wellbore isActive
            if (!isObjectGrowing.GetValueOrDefault()) return;

            // Update dbGrowingObject timestamp
            DbGrowingObjectAdapter.UpdateLastAppendDateTime(uri, GetWellboreUri(uri));
            // Update Wellbore isActive
            UpdateWellboreIsActive(uri, true);
        }

        /// <summary>
        /// Updates the growing object's header and change history.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="updates">The header update definition.</param>
        /// <param name="isAuditUpdate">if set to <c>true</c> audit the update.</param>
        protected virtual void UpdateGrowingObject(EtpUri uri, UpdateDefinition<T> updates = null, bool isAuditUpdate = true)
        {
            var current = GetEntity(uri);

            // Update the growing object's header
            var filter = MongoDbUtility.GetEntityFilter<T>(uri);
            var fields = MongoDbUtility.CreateUpdateFields<T>();

            Logger.Debug($"Updating date time last change for URI: {uri}");
            updates = MongoDbUtility.BuildUpdate(updates, fields);

            var mongoUpdate = new MongoDbUpdate<T>(Container, GetCollection(), null, IdPropertyName);
            mongoUpdate.UpdateFields(filter, updates);

            // Join existing Transaction
            var transaction = Transaction;
            transaction.Attach(MongoDbAction.Update, DbCollectionName, IdPropertyName, current.ToBsonDocument(), uri);
            transaction.Save();

            if (!isAuditUpdate) return;

            var changeType = WitsmlOperationContext.Current.Request.Function == Functions.AddToStore
                ? Witsml141.ReferenceData.ChangeInfoType.add
                : Witsml141.ReferenceData.ChangeInfoType.update;

            // Audit entity
            AuditEntity(uri, changeType);
        }

        /// <summary>
        /// Updates the IsActive field of a wellbore.
        /// </summary>
        /// <param name="uri">The growing object's URI.</param>
        /// <param name="isActive">IsActive flag on wellbore is set to the value.</param>
        protected virtual void UpdateWellboreIsActive(EtpUri uri, bool isActive)
        {
            var dataAdapter = Container.Resolve<IWellboreDataAdapter>(new ObjectName(uri.Version));
            dataAdapter.UpdateIsActive(GetWellboreUri(uri), isActive);
        }

        /// <summary>
        /// Audits the insert operation.
        /// </summary>
        /// <param name="uri">The URI.</param>
        protected override void AuditInsert(EtpUri uri)
        {
            // Overriding default behavior for growing objects
        }

        /// <summary>
        /// Audits the update operation.
        /// </summary>
        /// <param name="uri">The URI.</param>
        protected override void AuditUpdate(EtpUri uri)
        {
            // Overriding default behavior for growing objects
        }

        /// <summary>
        /// Audits the partial delete operation.
        /// </summary>
        /// <param name="uri">The URI.</param>
        protected override void AuditPartialDelete(EtpUri uri)
        {
            // Overriding default behavior for growing objects
        }
    }
}
