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
using Energistics.DataAccess.WITSML141;
using Energistics.Datatypes;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Messages
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Message" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Message}" />
    [Export(typeof(IWitsmlDataAdapter<Message>))]
    [Export(typeof(IWitsml141Configuration))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Message141DataAdapter : MongoDbDataAdapter<Message>, IWitsml141Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Message141DataAdapter" /> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Message141DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Message141)
        {
            Logger.Debug("Instance created.");
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Message"/> object.
        /// </summary>
        /// <param name="capServer">The capServer object.</param>
        public void GetCapabilities(CapServer capServer)
        {
            Logger.DebugFormat("Getting the supported capabilities for Message data version {0}.", capServer.Version);

            capServer.Add(Functions.GetFromStore, ObjectTypes.Message);
            capServer.Add(Functions.AddToStore, ObjectTypes.Message);
            capServer.Add(Functions.UpdateInStore, ObjectTypes.Message);
            capServer.Add(Functions.DeleteFromStore, ObjectTypes.Message);
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<Message> GetAll(EtpUri? parentUri = null)
        {
            Logger.DebugFormat("Fetching all Messages; Parent URI: {0}", parentUri);

            return GetAllQuery(parentUri)
                .OrderBy(x => x.Name)
                .ToList();
        }

        /// <summary>
        /// Gets an <see cref="IQueryable{Message}" /> instance to by used by the GetAll method.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>An executable query.</returns>
        protected override IQueryable<Message> GetAllQuery(EtpUri? parentUri)
        {
            var query = GetQuery().AsQueryable();

            if (parentUri != null)
            {
                var ids = parentUri.Value.GetObjectIds().ToDictionary(x => x.ObjectType, y => y.ObjectId);
                var uidWellbore = ids[ObjectTypes.Wellbore];
                var uidWell = ids[ObjectTypes.Well];

                query = query.Where(x => x.UidWell == uidWell && x.UidWellbore == uidWellbore);
            }

            return query;
        }

        /// <summary>
        /// Gets a list of the property names to project during a query.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of property names.</returns>
        protected override List<string> GetProjectionPropertyNames(WitsmlQueryParser parser)
        {
            return OptionsIn.ReturnElements.IdOnly.Equals(parser.ReturnElements())
                ? new List<string> { IdPropertyName, NamePropertyName, "UidWell", "NameWell", "UidWellbore", "NameWellbore" }
                : null;
        }
    }
}
