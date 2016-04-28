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
using PDS.Witsml.Server.Models;

namespace PDS.Witsml.Server.Data.Transactions
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for a <see cref="MongoDbTransaction"/>
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{PDS.Witsml.Server.Models.MongoDbTransaction}" />
    [Export]
    public class MongoDbTransactionAdapter : MongoDbDataAdapter<MongoDbTransaction>
    {
        private readonly string _idField = "TransactionId";

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbTransactionAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public MongoDbTransactionAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectTypes.MongoDbTransaction, ObjectTypes.Uid)
        {
            Logger.Debug("Creating instance.");
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
            var filter = MongoDbUtility.BuildFilter<MongoDbTransaction>(_idField, transactionId);
            collection.DeleteMany(filter);
        }
    }
}
