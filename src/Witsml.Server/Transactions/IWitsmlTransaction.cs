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
using Energistics.Datatypes;

namespace PDS.Witsml.Server.Transactions
{
    /// <summary>
    /// Defines the wrapper that can be used for abstracting a data storage specific transaction implementation.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface IWitsmlTransaction : IDisposable
    {
        /// <summary>
        /// Gets the URI associated with the transaction.
        /// </summary>
        EtpUri Uri { get; }

        /// <summary>
        /// Sets the context for the root transaction.
        /// </summary>
        /// <param name="uri">The URI.</param>
        void SetContext(EtpUri uri);

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        void Commit();
    }
}
