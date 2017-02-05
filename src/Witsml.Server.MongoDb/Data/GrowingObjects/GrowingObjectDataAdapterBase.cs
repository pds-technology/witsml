//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
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

using System.Collections.Generic;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Energistics.Datatypes;
using MongoDB.Bson;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Server.Data.Transactions;

namespace PDS.Witsml.Server.Data.GrowingObjects
{
    /// <summary>
    /// Provides properties and methods common to all growing data objects.
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{T}" />
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
        /// Determines whether the objectGrowing flag is true for the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        ///   <c>true</c> if the objectGrowing flag is true for the specified entity; otherwise, <c>false</c>.
        /// </returns>
        protected abstract bool IsObjectGrowing(T entity);

        /// <summary>
        /// Updates the growing status for the specified growing object.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="updates">The header update definition.</param>
        /// <param name="isObjectGrowing">Is the object currently growing.</param>
        protected virtual void UpdateGrowingObject(T entity, UpdateDefinition<T> updates, bool isObjectGrowing)
        {
            var uri = GetUri(entity);

            // Check to see if the object growing flag needs to be toggled
            if (IsObjectGrowing(entity) != isObjectGrowing)
            {
                Logger.Debug($"Updating object growing flag for URI: {uri}");
                var flag = MongoDbUtility.CreateObjectGrowingFields<T>(isObjectGrowing);
                updates = MongoDbUtility.BuildUpdate(updates, flag);
            }

            UpdateGrowingObject(uri, updates);

            // If the object is not currently growing do not update wellbore isActive
            if (!isObjectGrowing) return;

            // Update dbGrowingObject timestamp
            DbGrowingObjectAdapter.UpdateLastAppendDateTime(uri, uri.Parent);
            // Update Wellbore isActive
            UpdateWellboreIsActive(uri, true);
        }

        /// <summary>
        /// Updates the growing object's header and change history.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="updates">The header update definition.</param>
        protected virtual void UpdateGrowingObject(EtpUri uri, UpdateDefinition<T> updates = null)
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

            // Audit entity
            AuditEntity(uri, current, Witsml141.ReferenceData.ChangeInfoType.update, fields);
        }

        /// <summary>
        /// Updates the IsActive field of a wellbore.
        /// </summary>
        /// <param name="uri">The growing object's URI.</param>
        /// <param name="isActive">IsActive flag on wellbore is set to the value.</param>
        protected virtual void UpdateWellboreIsActive(EtpUri uri, bool isActive)
        {
            var dataAdapter = Container.Resolve<IWellboreDataAdapter>(new ObjectName(uri.Version));
            dataAdapter.UpdateIsActive(uri.Parent, isActive);
        }

        /// <summary>
        /// Audits the update operation.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="updateFields">Update fields not yet modified in the entity object.</param>
        protected override void AuditUpdate<TObject>(EtpUri uri, TObject entity, Dictionary<string, object> updateFields = null)
        {
            // Overriding default behavior for growing objects
        }
    }
}
