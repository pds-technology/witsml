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

using Energistics.DataAccess.WITSML200;
using Energistics.Datatypes;
using MongoDB.Bson;
using PDS.Witsml.Server.Data.GrowingObjects;
using PDS.Witsml.Server.Data.Transactions;

namespace PDS.Witsml.Server.Data.Wellbores
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

            if (wellboreEntity.IsActive.GetValueOrDefault() == isActive)
                return;

            Logger.DebugFormat("Updating wellbore isActive for uid '{0}' and name '{1}'.", wellboreEntity.Uuid, wellboreEntity.Citation.Title);

            Transaction.Attach(MongoDbAction.Update, DbCollectionName, IdPropertyName, wellboreEntity.ToBsonDocument(), uri);
            Transaction.Save();

            var filter = MongoDbUtility.GetEntityFilter<Wellbore>(uri);
            var wellboreUpdate = MongoDbUtility.BuildUpdate<Wellbore>(null, "IsActive", isActive);
            var mongoUpdate = new MongoDbUpdate<Wellbore>(Container, GetCollection(), null);
            mongoUpdate.UpdateFields(filter, wellboreUpdate);
        }
    }
}
