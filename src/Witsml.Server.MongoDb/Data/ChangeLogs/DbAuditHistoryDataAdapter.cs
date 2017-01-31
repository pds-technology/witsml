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
using LinqToQuerystring;
using PDS.Framework;
using PDS.Witsml.Data.ChangeLogs;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.ChangeLogs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="DbAuditHistory" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{DbAuditHistory}" />
    [Export(typeof(IWitsmlDataAdapter<DbAuditHistory>))]
    [Export(typeof(IWitsml141Configuration))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class DbAuditHistoryDataAdapter : MongoDbDataAdapter<DbAuditHistory>, IWitsml141Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbAuditHistoryDataAdapter" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public DbAuditHistoryDataAdapter(IContainer container, IDatabaseProvider databaseProvider)
            : base(container, databaseProvider, "dbAuditHistory")
        {
            Logger.Debug("Instance created.");
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="DbAuditHistory"/> object.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        public void GetCapabilities(CapServer capServer)
        {
            Logger.DebugFormat("Getting the supported capabilities for ChangeLog data version {0}.", capServer.Version);

            capServer.Add(Functions.GetFromStore, ObjectTypes.ChangeLog);
        }

        /// <summary>
        /// Retrieves data objects from the data store using the specified parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <param name="context">The response context.</param>
        /// <returns>A collection of data objects retrieved from the data store.</returns>
        public override List<DbAuditHistory> Query(WitsmlQueryParser parser, ResponseContext context)
        {
            var entities = base.Query(parser, context);
            var returnElements = parser.ReturnElements();

            // Only return full changeHistory if requested
            if (!OptionsIn.ReturnElements.All.Equals(returnElements) && !parser.Contains("changeHistory"))
            {
                entities.ForEach(x => x.ChangeHistory = null);
            }

            return entities;
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<DbAuditHistory> GetAll(EtpUri? parentUri)
        {
            Logger.DebugFormat("Fetching all ChangeLogs; Parent URI: {0}", parentUri);

            return GetAllQuery(parentUri)
                .OrderBy(x => x.Name)
                .ToList();
        }

        /// <summary>
        /// Gets an <see cref="IQueryable{DbAuditHistory}" /> instance to by used by the GetAll method.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>An executable query.</returns>
        protected override IQueryable<DbAuditHistory> GetAllQuery(EtpUri? parentUri)
        {
            var query = GetQuery().AsQueryable();

            if (parentUri != null)
            {
                //var uidWellbore = parentUri.Value.ObjectId;
                //query = query.Where(x => x.Wellbore.Uuid == uidWellbore);

                if (!string.IsNullOrWhiteSpace(parentUri.Value.Query))
                    query = query.LinqToQuerystring(parentUri.Value.Query);
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
            var returnElements = parser.ReturnElements();

            return OptionsIn.ReturnElements.IdOnly.Equals(returnElements)
                ? new List<string> { IdPropertyName, NamePropertyName, "UidWell", "NameWell", "UidWellbore", "NameWellbore", "UidObject", "NameObject" }
                : null;
        }
    }
}