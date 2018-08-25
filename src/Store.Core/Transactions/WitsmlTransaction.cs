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
using System.Collections.Concurrent;
using System.Threading;
using Energistics.Etp.Common.Datatypes;
using log4net;

namespace PDS.WITSMLstudio.Store.Transactions
{
    /// <summary>
    /// Wraps a data storage specific transaction implementation.
    /// </summary>
    public abstract class WitsmlTransaction : IWitsmlTransaction
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlTransaction));

        [ThreadStatic]
        private static int _transactionLevel;

        [ThreadStatic]
        private static ConcurrentStack<IWitsmlTransaction> _stack;

        /// <summary>
        /// Gets the URI associated with the transaction.
        /// </summary>
        public EtpUri Uri { get; private set; }

        /// <summary>
        /// Gets or sets a reference to the parent transaction.
        /// </summary>
        public IWitsmlTransaction Parent { get; private set; }

        /// <summary>
        /// Sets the context for the root transaction.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public virtual void SetContext(EtpUri uri)
        {
            Uri = uri;
        }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        public virtual void Commit()
        {
        }

        /// <summary>
        /// Initializes the root transaction.
        /// </summary>
        protected void InitializeRootTransaction()
        {
            var context = WitsmlOperationContext.Current;
            IWitsmlTransaction parent;

            // Keep a stack of nested transactions
            _stack = _stack ?? new ConcurrentStack<IWitsmlTransaction>();

            // Get parent transaction
            if (_stack.TryPeek(out parent))
                Parent = parent;

            // Add current transaction to the stack
            _stack.Push(this);

            // Initialize root transaction
            if (context.Transaction == null)
            {
                context.Transaction = this;
                Initialize();
            }

            _log.Debug("Incrementing transaction level");
            Interlocked.Increment(ref _transactionLevel);
        }

        /// <summary>
        /// Initializes the transaction.
        /// </summary>
        protected virtual void Initialize()
        {
        }

        /// <summary>
        /// Rolls back the transaction.
        /// </summary>
        protected virtual void Rollback()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="WitsmlTransaction"/> is committed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if committed; otherwise, <c>false</c>.
        /// </value>
        protected bool Committed { get; set; }

        #region IDisposable Support

        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; 
        ///   <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            _log.Debug("Decrementing transaction level");
            Interlocked.Decrement(ref _transactionLevel);

            // Remove the current transaction from the stack
            IWitsmlTransaction transaction;
            _stack.TryPop(out transaction);

            if (_transactionLevel < 1 && !Committed)
            {
                Rollback();
            }

            var context = WitsmlOperationContext.Current;
            if (this == context.Transaction)
            {
                context.Transaction = null;
                _stack.Clear();
            }

            if (disposing)
            {
                // NOTE: dispose managed state (managed objects).
            }

            // NOTE: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // NOTE: set large fields to null.

            _disposedValue = true;
        }

        // NOTE: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WitsmlTransaction() {
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
