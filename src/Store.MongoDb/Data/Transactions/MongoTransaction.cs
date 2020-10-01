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
using System.Threading;
using Energistics.Etp.Common.Datatypes;
using log4net;
using MongoDB.Bson;
using MongoDB.Driver;
using PDS.WITSMLstudio.Store.MongoDb;
using PDS.WITSMLstudio.Store.Transactions;

namespace PDS.WITSMLstudio.Store.Data.Transactions
{
    /// <summary>
    /// Encapsulates transaction-like behavior on MongoDb
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    [Export(typeof(IWitsmlTransaction))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class MongoTransaction : WitsmlTransaction
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MongoTransaction));
        internal static readonly int DefaultInterval = Settings.Default.DefaultTransactionWaitInterval;
        internal static readonly int MaximumAttempt = Settings.Default.DefaultMaximumTransactionAttempt;
        internal static readonly int ServerTimeoutMinutes = Settings.Default.DefaultServerTimeoutMinutes;

        private IDatabaseProvider _databaseProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoTransaction"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="adapter">The transaction adapter.</param>
        [ImportingConstructor]
        public MongoTransaction(IDatabaseProvider databaseProvider, DbTransactionDataAdapter adapter)
        {
            _databaseProvider = databaseProvider;
            Adapter = adapter;
            Id = Guid.NewGuid().ToString();
            Transactions = new List<DbTransaction>();
            InitializeRootTransaction();
        }       

        /// <summary>
        /// Gets or sets the transaction identifier.
        /// </summary>
        /// <value>The tid.</value>
        public string Id { get; }

        /// <summary>
        /// The list of transaction records. 
        /// </summary>
        public List<DbTransaction> Transactions { get; }

        /// <summary>
        /// Gets the <see cref="IMongoDatabase"/> instance associated with the current transaction.
        /// </summary>
        public IMongoDatabase Database { get; private set; }

        /// <summary>
        /// Gets the transaction adapter.
        /// </summary>
        /// <value>The adapter.</value>
        public DbTransactionDataAdapter Adapter { get; }

        /// <summary>
        /// Sets the context for the root transaction.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public override void SetContext(EtpUri uri)
        {
            base.SetContext(uri);
            Wait(uri);
        }

        /// <summary>
        /// Creates a transaction record and attach to the transaction.
        /// </summary>
        /// <param name="action">The MongoDb operation, e.g. add.</param>
        /// <param name="collection">The MongoDb collection name.</param>
        /// <param name="idPropertyName">Name of the identifier property.</param>
        /// <param name="document">The data obejct in BsonDocument format.</param>
        /// <param name="uri">The URI.</param>
        public void Attach(MongoDbAction action, string collection, string idPropertyName, BsonDocument document, EtpUri? uri = null)
        {
            _log.Debug($"Attaching '{action}' transaction for MongoDb collection {collection} with URI: {uri}");

            var transaction = new DbTransaction
            {
                TransactionId = Id,
                Collection = collection,
                IdPropertyName = idPropertyName,
                Action = action,
                Status = TransactionStatus.Created,
                CreatedDateTime = DateTime.UtcNow
            };

            if (uri.HasValue)
                transaction.Uri = uri.Value;

            if (document != null)
                transaction.Value = document;          

            Transactions.Add(transaction);
        }

        /// <summary>
        /// Saves the transaction records in MongoDb and change status to pending
        /// </summary>
        public void Save()
        {
            var created = Transactions.Where(t => t.Status == TransactionStatus.Created).ToList();
            if (!created.Any()) return;

            _log.Debug($"Saving attached transactions for URI: {Uri}");

            foreach (var transaction in created)
                transaction.Status = TransactionStatus.Pending;

            Adapter.InsertEntities(created);
        }

        /// <summary>
        /// Commits the transaction in MongoDb.
        /// </summary>
        public override void Commit()
        {
            _log.Debug($"Committing transaction for URI: {Uri}");
            var parent = Parent as MongoTransaction;

            if (parent != null)
            {
                // Transfer transactions to parent transaction
                Adapter.UpdateEntities(Id, parent.Id);
                parent.Transactions.AddRange(Transactions);

                // Prevent deletion of transactions
                Transactions.Clear();
            }
            else
            {
                var pending = Transactions.Where(t => t.Status == TransactionStatus.Pending && t.Action == MongoDbAction.Delete);

                foreach (var transaction in pending)
                {
                    Delete(transaction);
                }
            }

            ClearTransactions();
            Committed = true;
        }

        /// <summary>
        /// Initializes the transaction.
        /// </summary>
        protected override void Initialize()
        {
            _log.Debug($"Initializing transaction for URI: {Uri}");
            Database = _databaseProvider.GetDatabase();
        }

        /// <summary>
        /// Rollbacks the transaction in MongoDb.
        /// </summary>
        protected override void Rollback()
        {
            _log.Debug($"Rolling back transaction for URI: {Uri}");

            // Rollback transactions in reverse order
            var pending = Transactions.Where(t => t.Status == TransactionStatus.Pending).Reverse();

            foreach (var transaction in pending)
            {
                var action = transaction.Action;

                if (action == MongoDbAction.Add)
                {
                    Delete(transaction);
                }
                else if (action == MongoDbAction.Update)
                {
                    Update(transaction);
                }
            }

            ClearTransactions();
        }

        private void ClearTransactions()
        {
            if (Transactions.Any())
            {
                Adapter.DeleteTransactions(Id);
                Transactions.Clear();
            }

            _databaseProvider = null;
            Database = null;
        }

        private void Update(DbTransaction transaction)
        {
            var collection = Database.GetCollection<BsonDocument>(transaction.Collection);
            var filter = GetDocumentFilter(new EtpUri(transaction.Uri), transaction.IdPropertyName);
            collection.ReplaceOne(filter, Adapter.GetTransactionValue(transaction.FileId));
        }

        private void Delete(DbTransaction transaction)
        {
            var collection = Database.GetCollection<BsonDocument>(transaction.Collection);
            var filter = GetDocumentFilter(new EtpUri(transaction.Uri), transaction.IdPropertyName);
            collection.DeleteOne(filter);
        }

        private FilterDefinition<BsonDocument> GetDocumentFilter(EtpUri uri, string idPropertyName)
        {
            return MongoDbUtility.GetEntityFilter<BsonDocument>(uri, idPropertyName);
        }

        /// <summary>
        /// Blocks the current thread if there is a pending transaction for the specified URI.
        /// </summary>
        /// <param name="uri">The uri of the data object.</param>
        private void Wait(EtpUri uri)
        {
            var message = $"Transaction Id: {Id}; URI: {uri}; Thread Id: {Thread.CurrentThread.ManagedThreadId};";
            object locker = string.Intern($"{GetType().FullName}-{uri.Uri}");
            var count = MaximumAttempt;
            
            var transaction = GetActiveTransaction(uri);
            if (transaction == null)
            {
                _log.Debug($"{message} waiting for lock");
                lock (locker)
                {
                    _log.Debug($"{message} acquired lock");
                    transaction = GetActiveTransaction(uri);
                    if (transaction == null)
                    {
                        _log.Debug($"{message} created context");
                        Attach(MongoDbAction.Context, "dbTransaction", "Uri", null, uri);
                        Save();
                        return;
                    }
                }
            }

            // No need to wait if part of the same transaction
            if (IsSameTransaction(transaction.TransactionId))
            {
                _log.Debug($"{message} joining transaction: {transaction.TransactionId}");
                return;
            }

            while (GetActiveTransaction(uri) != null)
            {
                _log.Debug($"{message} waiting for pending transaction");

                Thread.Sleep(DefaultInterval);
                count--;

                if (count > 0) continue;
                message = $"Transaction timeout on data object with URI: {uri}";
                throw new WitsmlException(ErrorCodes.ErrorTransactionTimeout, message);
            }

            // Call Wait again to ensure new Context entry is created
            Wait(uri);
        }

        /// <summary>
        /// Determines whether the current transaction has the specified transaction id or
        /// if the current transaction is nested within a transaction with the specified id.
        /// </summary>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <returns><c>true</c> if this instance is part of the same transaction; otherwise <c>false</c>.</returns>
        private bool IsSameTransaction(string transactionId)
        {
            var transaction = this;

            while (transaction != null)
            {
                if (transactionId.Equals(transaction.Id))
                    return true;

                transaction = transaction.Parent as MongoTransaction;
            }

            return false;
        }

        /// <summary>
        /// Gets the active transaction.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>Returns an <see cref="DbTransaction" /> instance.</returns>
        private DbTransaction GetActiveTransaction(EtpUri uri)
        {
            var timeout = DateTime.UtcNow.AddMinutes(-1 * ServerTimeoutMinutes);

            var queryUri = new EtpUri($"{uri}?$filter=CreatedDateTime gt datetime'{timeout:s}Z'");

            return Adapter.GetAll(queryUri).FirstOrDefault();
        }
    }
}
