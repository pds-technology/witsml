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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ReferenceData;
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
        /// Gets or creates the audit history for the changed entity.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="uri">The URI of the changed entity.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="changeType">Type of the change.</param>
        /// <returns>A new or existing DbAuditHistory for the entity.</returns>
        public DbAuditHistory GetAuditHistory<TObject>(EtpUri uri, TObject entity, ChangeInfoType changeType)
        {
            var uriLower = uri.Uri.ToLowerInvariant();
            var auditHistory = GetQuery().FirstOrDefault(x => x.Uri == uriLower);
            var changeInfo = $"{changeType:G} {uri.ObjectType}";


            // Creating audit history entry
            if (auditHistory == null)
            {
                var dataObject = entity as IDataObject;
                var wellObject = entity as IWellObject;
                var wellboreObject = entity as IWellboreObject;
                var abstractObject = entity as Energistics.DataAccess.WITSML200.AbstractObject;

                auditHistory = new DbAuditHistory
                {
                    ObjectType = uri.ObjectType,
                    LastChangeInfo = changeInfo,
                    LastChangeType = changeType,
                    ChangeHistory = new List<Energistics.DataAccess.WITSML141.ComponentSchemas.ChangeHistory>(),
                    NameWellbore = wellboreObject?.NameWellbore,
                    UidWellbore = wellboreObject?.UidWellbore,
                    NameWell = wellObject?.NameWell ?? wellboreObject?.NameWell,
                    UidWell = wellObject?.UidWell ?? wellboreObject?.UidWell,
                    NameObject = dataObject?.Name ?? abstractObject?.Citation?.Title,
                    UidObject = uri.ObjectId,
                    Uri = uriLower
                };

                auditHistory.CommonData = auditHistory.CommonData.Create();
            }
            else // Updating existing entry
            {
                auditHistory.CommonData.DateTimeLastChange = DateTimeOffset.UtcNow;
                auditHistory.LastChangeInfo = changeInfo;
                auditHistory.LastChangeType = changeType;
            }

            // Append current change entry
            auditHistory.ChangeHistory.Add(new Energistics.DataAccess.WITSML141.ComponentSchemas.ChangeHistory
            {
                Uid = Guid.NewGuid().ToString(),
                ChangeInfo = auditHistory.LastChangeInfo,
                ChangeType = auditHistory.LastChangeType,
                // TODO: Set to entity's CommonData.DateTimeLastChange when CommonData interface is added to DevKit
                DateTimeChange = auditHistory.CommonData.DateTimeLastChange
                // TODO: Set to the entity's CommonData.SourceName when CommonData interface is added to DevKit
                // SourceName = entity.CommonData.SourceName
            });

            return auditHistory;
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

        /// <summary>
        /// Audits the entity. Override this method to adjust the audit record
        /// before it is submitted to the database or to prevent the audit.
        /// </summary>
        /// <param name="auditHistory">The audit history.</param>
        /// <param name="exists">if set to <c>true</c> the entry exists.</param>
        protected override void AuditEntity(DbAuditHistory auditHistory, bool exists)
        {
            // Excluding DbAuditHistory from audit history
        }
    }
}