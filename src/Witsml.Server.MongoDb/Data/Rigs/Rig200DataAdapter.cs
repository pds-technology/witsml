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
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.Datatypes;

namespace PDS.Witsml.Server.Data.Rigs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Rig" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Rig}" />
    [Export(typeof(IWitsmlDataAdapter<Rig>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Rig200DataAdapter : MongoDbDataAdapter<Rig>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rig200DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Rig200DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Rig200, ObjectTypes.Uuid)
        {
            Logger.Debug("Instance created.");
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<Rig> GetAll(EtpUri? parentUri = null)
        {
            Logger.Debug("Fetching all Rigs.");

            var query = GetQuery().AsQueryable();

            //if (parentUri != null)
            //{
            //    var uidWellbore = parentUri.Value.ObjectId;
            //    query = query.Where(x => x.Wellbore.Uuid == uidWellbore);
            //}

            return query
                .OrderBy(x => x.Citation.Title)
                .ToList();
        }
    }
}
