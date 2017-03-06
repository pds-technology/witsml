//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

namespace PDS.Witsml.Server.Data.Transactions
{
    /// <summary>
    /// An enumeration of data object operations, e.g. add
    /// </summary>
    public enum MongoDbAction
    {
        /// <summary>An insert operation.</summary>
        Add,
        /// <summary>An update operation.</summary>
        Update,
        /// <summary>A delete operation.</summary>
        Delete,
        /// <summary>The context for the whole operation.</summary>
        Context
    }

    /// <summary>
    /// An enumeration of transaction record status
    /// </summary>
    public enum TransactionStatus
    {
        /// <summary>A newly created transaction.</summary>
        Created,
        /// <summary>A pending transaction.</summary>
        Pending,
        /// <summary>A committed transaction.</summary>
        Commited,
        /// <summary>A rolled back transaction.</summary>
        RolledBack
    }
}
