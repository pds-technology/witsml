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
using MongoDB.Bson;

namespace PDS.WITSMLstudio.Store.Data.Transactions
{
    /// <summary>
    /// Class for transaction entry in MongoDb
    /// </summary>
    public class DbTransaction
    {
        /// <summary>
        /// Gets or sets the transaction tid.
        /// </summary>
        /// <value>The transaction tid.</value>
        public string TransactionId { get; set; }

        /// <summary>
        /// Gets or sets the transaction record identifier.
        /// </summary>
        /// <value>The transaction record identifier.</value>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the name of the identifier property.
        /// </summary>
        /// <value>The name of the identifier property.</value>
        public string IdPropertyName { get; set; }

        /// <summary>
        /// Gets or sets the MongoDb collection name.
        /// </summary>
        /// <value>The MongoDb collection name.</value>
        public string Collection { get; set; }

        /// <summary>
        /// Gets or sets the data object operation, e.g. add.
        /// </summary>
        /// <value>The operation on the data object.</value>
        public MongoDbAction Action { get; set; }

        /// <summary>
        /// Gets or sets the status for the transaction record.
        /// </summary>
        /// <value>The status.</value>
        public TransactionStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the data object value in BsonDocumnet format.
        /// </summary>
        /// <value>The data object value in BsonDocumnet format.</value>
        public BsonDocument Value { get; set; }

        /// <summary>
        /// Gets or sets the transaction record created date time.
        /// </summary>
        /// <value>The transaction created date time.</value>
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the binary file identifier.
        /// </summary>
        /// <value>The binary file identifier.</value>
        public ObjectId FileId { get; set; }
    }
}
