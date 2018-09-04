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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Energistics.Etp.Common.Datatypes;
using LinqToQuerystring;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.ChangeLogs;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Transactions
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for a <see cref="MongoDbTransaction"/>
    /// </summary>
    /// <seealso cref="Transactions.DbTransaction" />
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DbTransactionDataAdapter : MongoDbDataAdapter<DbTransaction>
    {
        private const string MongoDbTransaction = "dbTransaction";
        private const string TransactionIdField = "TransactionId";
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DbTransactionDataAdapter" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public DbTransactionDataAdapter(IContainer container, IDatabaseProvider databaseProvider) : base(container, databaseProvider, MongoDbTransaction, ObjectTypes.Uri)
        {
            Logger.Debug("Creating instance.");
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<DbTransaction> GetAll(EtpUri? parentUri = null)
        {            
            var query = GetQuery().AsQueryable();
            var uri = parentUri?.Uri;

            if (!string.IsNullOrWhiteSpace(parentUri?.Query))
            {
                query = query.LinqToQuerystring(parentUri.Value.Query);
                uri = uri.Substring(0, uri.IndexOf('?'));
            }

            query = query.Where(x => x.Uri == uri);

            return query.ToList();
        }

        /// <summary>
        /// Inserts the entities.
        /// </summary>
        /// <param name="entities">The entities.</param>
        public void InsertEntities(List<DbTransaction> entities)
        {
            var collection = GetCollection();
            UploadBinaryFilesAndClearValue(entities);
            collection.InsertMany(entities);
        }

        /// <summary>
        /// Updates the entities.
        /// </summary>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <param name="newTransactionId">The new transaction identifier.</param>
        public void UpdateEntities(string transactionId, string newTransactionId)
        {
            Logger.Debug($"Transferring transactions from Transaction ID: {transactionId} to {newTransactionId}");
            var filter = MongoDbUtility.BuildFilter<DbTransaction>(TransactionIdField, transactionId);
            var update = MongoDbUtility.BuildUpdate<DbTransaction>(null, TransactionIdField, newTransactionId);

            var collection = GetCollection();
            collection.UpdateMany(filter, update);
        }

        /// <summary>
        /// Deletes the transactions.
        /// </summary>
        /// <param name="transactionId">The tid.</param>
        public void DeleteTransactions(string transactionId)
        {
            Logger.Debug($"Deleting transactions for Transaction ID: {transactionId}");

            // Delete the binary files
            DeleteTransactionMongoFiles(transactionId);

            // Delete the document
            var collection = GetCollection();
            var filter = MongoDbUtility.BuildFilter<DbTransaction>(TransactionIdField, transactionId);
            collection.DeleteMany(filter);
        }

        /// <summary>
        /// Gets the transaction value.
        /// </summary>
        /// <param name="fileId">The file identifier.</param>
        /// <returns>The transaction value.</returns>
        public BsonDocument GetTransactionValue(ObjectId fileId)
        {
            var bucket = GetMongoFileBucket();
            var mongoFile = GetMongoFile(bucket, fileId);
            var bytes = bucket.DownloadAsBytes(mongoFile.Id);
            var json = Encoding.UTF8.GetString(bytes);

            return BsonSerializer.Deserialize<BsonDocument>(json);
        }

        /// <summary>
        /// Gets the entity filter for the specified URI.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="idPropertyName">Name of the identifier property.</param>
        /// <returns>The entity filter.</returns>
        protected override FilterDefinition<TObject> GetEntityFilter<TObject>(EtpUri uri, string idPropertyName)
        {
            return MongoDbUtility.BuildFilter<TObject>(idPropertyName, uri.ToString());
        }

        /// <summary>
        /// Audits the entity. Override this method to adjust the audit record
        /// before it is submitted to the database or to prevent the audit.
        /// </summary>
        /// <param name="entity">The changed entity.</param>
        /// <param name="auditHistory">The audit history.</param>
        /// <param name="exists">if set to <c>true</c> the entry exists.</param>
        protected override void AuditEntity(DbTransaction entity, DbAuditHistory auditHistory, bool exists)
        {
            // Excluding DbTransaction from audit history
        }

        private void UploadBinaryFilesAndClearValue(List<DbTransaction> entities)
        {
            var bucket = GetMongoFileBucket();
            foreach (var entity in entities)
            {
                if (entity.Value == null)
                    continue;

                Logger.Debug($"Converting value to binary file: {entity.Uri}");

                var bytes = Encoding.UTF8.GetBytes(entity.Value.ToJson());

                var loadOptions = new GridFSUploadOptions
                {
                    Metadata = new BsonDocument
                    {
                        { "DataBytes", bytes.Length }
                    }
                };

                entity.FileId = bucket.UploadFromBytes(entity.TransactionId, bytes, loadOptions);
                entity.Value = null;
            }
        }

        private IGridFSBucket GetMongoFileBucket()
        {
            var db = DatabaseProvider.GetDatabase();
            return new GridFSBucket(db, new GridFSBucketOptions
            {
                BucketName = DbCollectionName,
                ChunkSizeBytes = WitsmlSettings.ChunkSizeBytes
            });
        }

        private static GridFSFileInfo GetMongoFile(IGridFSBucket bucket, ObjectId fileId)
        {
            var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", fileId);
            return bucket.Find(filter).FirstOrDefault();
        }

        private void DeleteTransactionMongoFiles(string transactionId)
        {
            Logger.Debug($"Deleting mongo files for transaction: {transactionId}");

            var query = GetQuery().AsQueryable();
            query.Where(x => x.TransactionId == transactionId && !ObjectId.Empty.Equals(x.FileId))
                .ForEach(x => DeleteMongoFile(x.FileId));
        }

        private void DeleteMongoFile(ObjectId fileId)
        {
            Logger.Debug($"Deleting dbTransaction Data file: {fileId}");

            var bucket = GetMongoFileBucket();
            var mongoFile = GetMongoFile(bucket, fileId);

            if (mongoFile == null)
                return;

            bucket.Delete(mongoFile.Id);
        }
    }
}
