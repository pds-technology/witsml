//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2020 PDS Americas LLC
//-----------------------------------------------------------------------

using System.ComponentModel.Composition;
using Energistics.Etp.Common.Datatypes;

namespace PDS.WITSMLstudio.Store.Transactions
{
    /// <summary>
    /// Defines the wrapper that can be used for managing the lifecycle of a specific transaction implementation.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Transactions.IWitsmlTransaction" />
    public class TransactionWrapper : IWitsmlTransaction
    {
        private readonly ExportLifetimeContext<IWitsmlTransaction> _exportLifetimeContext;
        private IWitsmlTransaction WitsmlTransaction => _exportLifetimeContext.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionWrapper"/> class.
        /// </summary>
        /// <param name="exportLifetimeContext">The export lifetime context.</param>
        public TransactionWrapper(ExportLifetimeContext<IWitsmlTransaction> exportLifetimeContext)
        {
            _exportLifetimeContext = exportLifetimeContext;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _exportLifetimeContext.Dispose();
        }

        /// <summary>
        /// Gets the URI associated with the transaction.
        /// </summary>
        public EtpUri Uri => WitsmlTransaction.Uri;

        /// <summary>
        /// Gets or sets a reference to the parent transaction.
        /// </summary>
        public IWitsmlTransaction Parent => WitsmlTransaction.Parent;

        /// <summary>
        /// Sets the context for the root transaction.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void SetContext(EtpUri uri)
        {
            WitsmlTransaction.SetContext(uri);
        }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        public void Commit()
        {
            WitsmlTransaction.Commit();
        }
    }
}