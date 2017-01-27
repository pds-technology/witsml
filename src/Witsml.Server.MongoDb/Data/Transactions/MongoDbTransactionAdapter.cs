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
using Energistics.Datatypes;
using LinqToQuerystring;
using MongoDB.Driver;
using PDS.Framework;

namespace PDS.Witsml.Server.Data.Transactions
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for a <see cref="MongoDbTransaction"/>
    /// </summary>
    /// <seealso cref="Transactions.MongoDbTransaction" />
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MongoDbTransactionAdapter : MongoDbDataAdapter<MongoDbTransaction>
    {
        private const string MongoDbTransaction = "dbTransaction";
        private const string TransactionIdField = "TransactionId";

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbTransactionAdapter" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public MongoDbTransactionAdapter(IContainer container, IDatabaseProvider databaseProvider) : base(container, databaseProvider, MongoDbTransaction, ObjectTypes.Uri)
        {
            Logger.Debug("Creating instance.");
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<MongoDbTransaction> GetAll(EtpUri? parentUri = null)
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
        public void InsertEntities(List<MongoDbTransaction> entities)
        {
            var collection = GetCollection();
            collection.InsertMany(entities);
        }

        /// <summary>
        /// Deletes the transactions.
        /// </summary>
        /// <param name="transactionId">The tid.</param>
        public void DeleteTransactions(string transactionId)
        {
            var collection = GetCollection();
            var filter = MongoDbUtility.BuildFilter<MongoDbTransaction>(TransactionIdField, transactionId);
            collection.DeleteMany(filter);
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
    }
}
