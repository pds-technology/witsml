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

using Energistics.DataAccess.WITSML200;
using Energistics.Etp.Common.Datatypes;
using MongoDB.Bson;
using PDS.WITSMLstudio.Store.Data.GrowingObjects;
using PDS.WITSMLstudio.Store.Data.Transactions;

namespace PDS.WITSMLstudio.Store.Data.Wellbores
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Wellbore" />
    /// </summary>
    [Export200(typeof(IWellboreDataAdapter))]
    public partial class Wellbore200DataAdapter : IWellboreDataAdapter
    {
        /// <summary>
        /// Updates the IsActive field of a wellbore.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="isActive">IsActive flag on wellbore is set to the value.</param>
        public void UpdateIsActive(EtpUri uri, bool isActive)
        {
            var wellboreEntity = GetEntity(uri);

            if (wellboreEntity == null)
            {
                Logger.DebugFormat("Wellbore not found with uri '{0}'", uri);
                return;
            }

            if (wellboreEntity.IsActive.GetValueOrDefault() == isActive)
                return;

            Logger.DebugFormat("Updating wellbore isActive for uid '{0}' and name '{1}'.", wellboreEntity.Uuid, wellboreEntity.Citation.Title);

            var filter = MongoDbUtility.GetEntityFilter<Wellbore>(uri);
            var fields = MongoDbUtility.CreateUpdateFields<Wellbore>();

            var wellboreUpdate = MongoDbUtility.BuildUpdate<Wellbore>(null, "IsActive", isActive);
            wellboreUpdate = MongoDbUtility.BuildUpdate(wellboreUpdate, fields);

            var mongoUpdate = new MongoDbUpdate<Wellbore>(Container, GetCollection(), null);
            mongoUpdate.UpdateFields(filter, wellboreUpdate);

            // Join existing Transaction
            var transaction = Transaction;
            transaction.Attach(MongoDbAction.Update, DbCollectionName, IdPropertyName, wellboreEntity.ToBsonDocument(), uri);
            transaction.Save();

            // Audit entity
            AuditEntity(uri, Energistics.DataAccess.WITSML141.ReferenceData.ChangeInfoType.update);
        }
    }
}
