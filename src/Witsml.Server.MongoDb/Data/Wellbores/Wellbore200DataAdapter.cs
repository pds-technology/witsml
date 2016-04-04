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

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Wellbore" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML200.Wellbore}" />
    [Export(typeof(IEtpDataAdapter))]
    [Export(typeof(IEtpDataAdapter<Wellbore>))]
    [Export200(ObjectTypes.Wellbore, typeof(IEtpDataAdapter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Wellbore200DataAdapter : MongoDbDataAdapter<Wellbore>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Wellbore200DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Wellbore200DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Wellbore200, ObjectTypes.Uuid)
        {
        }

        /// <summary>
        /// Adds a <see cref="Wellbore"/> to the data store.
        /// </summary>
        /// <param name="entity">The <see cref="Wellbore"/> to be added.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Add(Wellbore entity)
        {
            entity.Uuid = NewUid(entity.Uuid);
            entity.Citation = entity.Citation.Create();
            Logger.DebugFormat("Adding Wellbore with uid '{0}' and name '{1}'.", entity.Uuid, entity.Citation.Title);

            Validate(Functions.AddToStore, entity);
            Logger.DebugFormat("Validated Wellbore with uid '{0}' and name '{1}' for Add", entity.Uuid, entity.Citation.Title);

            InsertEntity(entity);

            return new WitsmlResult(ErrorCodes.Success, entity.Uuid);
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<Wellbore> GetAll(EtpUri? parentUri = null)
        {
            var query = GetQuery().AsQueryable();

            if (parentUri != null)
            {
                var uidWell = parentUri.Value.ObjectId;
                query = query.Where(x => x.ReferenceWell.Uuid == uidWell);
            }

            return query
                .OrderBy(x => x.Citation.Title)
                .ToList();
        }
    }
}
