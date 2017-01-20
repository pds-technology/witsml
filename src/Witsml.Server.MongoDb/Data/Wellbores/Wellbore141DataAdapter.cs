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
using Energistics.DataAccess.WITSML141;
using Energistics.Datatypes;
using MongoDB.Bson;
using PDS.Witsml.Server.Data.Transactions;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Wellbore" />.
    /// </summary>
    public partial class Wellbore141DataAdapter
    {
        /// <summary>
        /// Gets a list of the element names to ignore during an update.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of element names.</returns>
        protected override List<string> GetIgnoredElementNamesForUpdate(WitsmlQueryParser parser)
        {
            return new List<string> { "isActive" };
        }

        /// <summary>
        /// Updates the IsActive field of a wellbore.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="isActive">if set to <c>true</c> [is active].</param>
        public void UpdateIsActive(EtpUri uri, bool isActive)
        {
            var wellboreEntity = GetEntity(uri);

            if (wellboreEntity.IsActive.GetValueOrDefault() == isActive)
                return;

            Logger.DebugFormat("Updating wellbore isActive for uid '{0}' and name '{1}'.", wellboreEntity.Uid, wellboreEntity.Name);

            wellboreEntity.IsActive = isActive;
            Transaction.Attach(MongoDbAction.Update, DbCollectionName, IdPropertyName, wellboreEntity.ToBsonDocument(), uri);
            Transaction.Save();
            ReplaceEntity(wellboreEntity, uri);
        }
    }
}
