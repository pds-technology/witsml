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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.Etp.Common.Datatypes;
using MongoDB.Driver;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.ChangeLogs;

namespace PDS.WITSMLstudio.Store.Data.GrowingObjects
{
    /// <summary>
    /// Manages storage of DbGrowingDataObject in the Mongo Db
    /// </summary>
    [Export(typeof(IGrowingObjectDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DbGrowingObjectDataAdapter : MongoDbDataAdapter<DbGrowingObject>, IGrowingObjectDataProvider
    {
        private readonly IWellboreDataAdapter _wellbore141DataAdapter;
        private readonly IWellboreDataAdapter _wellbore200DataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbGrowingObjectDataAdapter"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public DbGrowingObjectDataAdapter(IContainer container, IDatabaseProvider databaseProvider) : 
            base(container, databaseProvider, "dbGrowingObject", ObjectTypes.Uri)
        {
            _wellbore141DataAdapter = Container.Resolve<IWellboreDataAdapter>(new ObjectName(OptionsIn.DataVersion.Version141.Value));
            _wellbore200DataAdapter = Container.Resolve<IWellboreDataAdapter>(new ObjectName(OptionsIn.DataVersion.Version200.Value));
        }

        /// <summary>
        /// Growings the object append.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="wellboreUri">The wellbore URI.</param>
        public void UpdateLastAppendDateTime(EtpUri uri, EtpUri wellboreUri)
        {
            uri = new EtpUri(uri.ToString().ToLowerInvariant());

            var growingObject = GetEntity(uri);
            var lastAppendDateTime = DateTime.UtcNow;

            if (growingObject == null)
            {
                growingObject = new DbGrowingObject()
                {
                    Uri = uri,
                    ObjectType = uri.ObjectType,
                    WellboreUri = wellboreUri,
                    LastAppendDateTime = lastAppendDateTime
                };

                InsertEntity(growingObject);
            }
            else
            {
                growingObject.LastAppendDateTime = lastAppendDateTime;
                ReplaceEntity(growingObject, uri);
            }
        }

        /// <summary>
        /// Expires the growing objects for the specified objectType and expiredDateTime.
        /// Any growing object of the specified type will have its objectGrowing flag set
        /// to false if its lastAppendDateTime is older than the expireDateTime.
        /// </summary>
        /// <param name="objectType">Type of the groing object.</param>
        /// <param name="expiredDateTime">The expired date time.</param>
        /// <returns>A list of wellbore uris of expired growing objects.</returns>
        public List<string> ExpireGrowingObjects(string objectType, DateTime expiredDateTime)
        {
            var wellboreUris = new List<string>();

            // Get dbGrowingObject for object type that are expired.
            var dataByVersion = GetExpiredGrowingObjects(objectType, expiredDateTime)
                .Select(x => new { Uri = new EtpUri(x.Uri), DataObject = x })
                .GroupBy(g => g.Uri.Version);

            foreach (var group in dataByVersion)
            {
                var firstItem = group.FirstOrDefault();
                var objectName = new ObjectName(objectType, firstItem?.Uri.Family, group.Key);
                var dataAdapter = Container.Resolve<IGrowingObjectDataAdapter>(objectName);

                foreach (var item in group)
                {
                    using (var transaction = GetTransaction())
                    {
                        transaction.SetContext(item.Uri);

                        // Set expired growing object to objectGrowing = false;
                        dataAdapter.UpdateObjectGrowing(item.Uri, false);                        

                        // Delete the dbGrowingObject record
                        DeleteEntity(item.Uri);

                        // Add wellbore uri of expired object to the list
                        if (!wellboreUris.Contains(item.DataObject.WellboreUri))
                            wellboreUris.Add(item.DataObject.WellboreUri);

                        // Commit transaction
                        transaction.Commit();
                    }
                }
            }

            return wellboreUris;
        }

        /// <summary>
        /// Sets isActive flag of wellbore to false if none of its children are growing
        /// </summary>
        /// <param name="wellboreUris">List of wellbore uris of expired growing objects</param>
        public void ExpireWellboreObjects(List<string> wellboreUris)
        {            
            foreach (var uri in wellboreUris)
            {
                using (var transaction = GetTransaction())
                {
                    var etpUri = new EtpUri(uri);

                    transaction.SetContext(etpUri);

                    // Check for other growing objects of wellbore
                    if (GetActiveWellboreCount(uri) != 0)
                        continue;

                    if (OptionsIn.DataVersion.Version141.Equals(etpUri.Version))
                        _wellbore141DataAdapter.UpdateIsActive(etpUri, false);

                    else if (OptionsIn.DataVersion.Version200.Equals(etpUri.Version))
                        _wellbore200DataAdapter.UpdateIsActive(etpUri, false);

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Audits the entity. Override this method to adjust the audit record
        /// before it is submitted to the database or to prevent the audit.
        /// </summary>
        /// <param name="entity">The changed entity.</param>
        /// <param name="auditHistory">The audit history.</param>
        /// <param name="exists">if set to <c>true</c> the entry exists.</param>
        protected override void AuditEntity(DbGrowingObject entity, DbAuditHistory auditHistory, bool exists)
        {
            // Excluding DbGrowingObject from audit history
        }

        /// <summary>
        /// Gets the URI for the specified data object.
        /// </summary>
        /// <param name="instance">The data object.</param>
        /// <returns>The URI representing the data object.</returns>
        protected override EtpUri GetUri(DbGrowingObject instance)
        {
            return new EtpUri(instance.Uri);
        }

        private FilterDefinition<DbGrowingObject> BuildDataFilter(string objectType, DateTime expiredDateTime)
        {
            var builder = Builders<DbGrowingObject>.Filter;
            var filters = new List<FilterDefinition<DbGrowingObject>>
            {
                builder.Eq("ObjectType", objectType),
                builder.Lt("LastAppendDateTime", expiredDateTime)
            };

            return builder.And(filters);
        }

        private List<DbGrowingObject> GetExpiredGrowingObjects(string objectType, DateTime expiredDateTime)
        {
            return GetCollection()
                .Find(BuildDataFilter(objectType, expiredDateTime) ?? "{}")
                .ToList();
        }

        private long GetActiveWellboreCount(string wellboreUri)
        {
            var filter = MongoDbUtility.BuildFilter<DbGrowingObject>("WellboreUri", wellboreUri);
            return GetCollection().CountDocuments(filter);
        }
    }
}
