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
using System.Threading;
using Energistics.Datatypes;
using MongoDB.Bson;
using MongoDB.Driver;
using PDS.Witsml.Server.MongoDb;

namespace PDS.Witsml.Server.Data.Transactions
{
    /// <summary>
    /// Encapsulates transaction-like behavior on MongoDb
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class MongoTransaction : IDisposable
    {
        internal static readonly int DefaultInterval = Settings.Default.DefaultTransactionWaitInterval;
        internal static readonly int MaximumAttempt = Settings.Default.DefaultMaximumTransactionAttempt;

        //private static readonly string _idField = "_id";
        //private static readonly string _uidWell = "UidWell";
        //private static readonly string _uidWellbore = "UidWellbore";

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoTransaction"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="adapter">The transaction adapter.</param>
        [ImportingConstructor]
        public MongoTransaction(IDatabaseProvider databaseProvider, MongoDbTransactionAdapter adapter)
        {
            DatabaseProvider = databaseProvider;
            Adapter = adapter;
            Id = Guid.NewGuid().ToString();
            Transactions = new List<MongoDbTransaction>();
            Committed = false;
        }       

        /// <summary>
        /// Gets or sets the transaction identifier.
        /// </summary>
        /// <value>The tid.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="MongoTransaction"/> is committed.
        /// </summary>
        /// <value><c>true</c> if committed; otherwise, <c>false</c>.</value>
        public bool Committed { get; set; }

        /// <summary>
        /// The list of transaction records. 
        /// </summary>
        public List<MongoDbTransaction> Transactions;

        /// <summary>
        /// Gets the database provider.
        /// </summary>
        /// <value>The database provider.</value>
        public IDatabaseProvider DatabaseProvider { get; }

        /// <summary>
        /// Gets the transaction adapter.
        /// </summary>
        /// <value>The adapter.</value>
        public MongoDbTransactionAdapter Adapter { get; }

        /// <summary>
        /// Commits the transaction in MongoDb.
        /// </summary>
        public void Commit()
        {
            var database = DatabaseProvider.GetDatabase();
            foreach (var transaction in Transactions.Where(t => t.Status == TransactionStatus.Pending && t.Action == MongoDbAction.Delete))
            {
                Delete(database, transaction);
            }

            ClearTransactions();
            Committed = true;
        }

        /// <summary>
        /// Rollbacks the transaction in MongoDb.
        /// </summary>
        public void Rollback()
        {
            var pending = Transactions.Where(t => t.Status == TransactionStatus.Pending).ToList();
            if (!pending.Any())
                return;

            var database = DatabaseProvider.GetDatabase();
            foreach (var transaction in pending)
            {
                var action = transaction.Action;

                if (action == MongoDbAction.Add)
                {
                    Delete(database, transaction);
                }
                else if (action == MongoDbAction.Update)
                {
                    Update(database, transaction);
                }
            }

            ClearTransactions();
        }

        /// <summary>
        /// Creates a transaction record and attach to the transaction.
        /// </summary>
        /// <param name="action">The MongoDb operation, e.g. add.</param>
        /// <param name="collection">The MongoDb collection name.</param>
        /// <param name="document">The data obejct in BsonDocument format.</param>
        /// <param name="uri">The URI.</param>
        public void Attach(MongoDbAction action, string collection, BsonDocument document, EtpUri? uri = null)
        {
            var transaction = new MongoDbTransaction
            {
                TransactionId = Id,
                Collection = collection,
                Action = action,
                Status = TransactionStatus.Created
            };

            if (uri.HasValue)
                transaction.Uid = uri.Value;

            if (document != null)
                transaction.Value = document;          

            Transactions.Add(transaction);
        }

        /// <summary>
        /// Waits this instance if the transaction is attached.
        /// </summary>
        /// <param name="uri">The uri of the data object.</param>
        public void Wait(EtpUri uri)
        {
            var count = MaximumAttempt;

            while (Adapter.Exists(uri))
            {
                Thread.Sleep(DefaultInterval);
                count--;

                if (count == 0)
                {
                    var message = string.Format("Transaction deadlock on data object with Uri: {0}", uri);
                    throw new WitsmlException(ErrorCodes.ErrorTransactionDeadlock, message);
                }
            }
        }

        /// <summary>
        /// Saves the transaction records in MongoDb and change status to pending
        /// </summary>
        public void Save()
        {
            var created = Transactions.Where(t => t.Status == TransactionStatus.Created).ToList();
            if (!created.Any())
                return;

            foreach (var transaction in created)
                transaction.Status = TransactionStatus.Pending;

            Adapter.InsertEntities(created);
        }

        private void ClearTransactions()
        {
            if (!Transactions.Any())
                return;

            Adapter.DeleteTransactions(Id);
            Transactions.Clear();
        }

        private void Update(IMongoDatabase database, MongoDbTransaction transaction)
        {
            var collection = database.GetCollection<BsonDocument>(transaction.Collection);
            var filter = GetDocumentFilter(new EtpUri(transaction.Uid));
            collection.ReplaceOne(filter, transaction.Value);
        }

        private void Delete(IMongoDatabase database, MongoDbTransaction transaction)
        {
            var collection = database.GetCollection<BsonDocument>(transaction.Collection);
            var filter = GetDocumentFilter(new EtpUri(transaction.Uid));
            collection.DeleteOne(filter);
        }

        private FilterDefinition<BsonDocument> GetDocumentFilter(EtpUri uri)
        {
            return uri.Version == EtpUris.Witsml200.Version
                ? MongoDbUtility.GetEntityFilter<BsonDocument>(uri, "Uuid")
                : MongoDbUtility.GetEntityFilter<BsonDocument>(uri);
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // NOTE: dispose managed state (managed objects).

                    if (!Committed)
                        Rollback();
                }

                // NOTE: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // NOTE: set large fields to null.

                _disposedValue = true;
            }
        }

        // NOTE: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MongoTransaction() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // NOTE: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
