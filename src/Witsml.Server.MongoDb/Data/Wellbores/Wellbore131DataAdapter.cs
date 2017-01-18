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

using Energistics.DataAccess.WITSML131;
using Energistics.Datatypes;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Wellbore" />.
    /// </summary>
    public partial class Wellbore131DataAdapter
    {
        /// <summary>
        /// Updates the IsActive field of a wellbore.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="isActive">if set to <c>true</c> [is active].</param>
        public void UpdateIsActive(EtpUri uri, bool isActive)
        {
            var wellboreEntity = GetEntity(uri);
            Logger.DebugFormat("Updating wellbore isActive for uid '{0}' and name '{1}'.", wellboreEntity.Uid, wellboreEntity.Name);

            // Update isActive
            var mongoUpdate = new MongoDbUpdate<Wellbore>(Container, GetCollection(), null);
            var filter = MongoDbUtility.GetEntityFilter<Wellbore>(uri);
            var wellboreUpdate = MongoDbUtility.BuildUpdate<Wellbore>(null, "IsActive", isActive);

            mongoUpdate.UpdateFields(filter, wellboreUpdate);
        }
    }
}
