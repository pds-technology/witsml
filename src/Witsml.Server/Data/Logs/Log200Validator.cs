using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Energistics.DataAccess.WITSML200;
using System.ComponentModel.Composition;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Provides validation for <see cref="Log" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{Energistics.DataAccess.WITSML200.Log}" />
    [Export(typeof(IDataObjectValidator<Log>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Log200Validator : DataObjectValidator<Log>
    {
        private readonly IEtpDataAdapter<Log> _logDataAdapter;
        private readonly IEtpDataAdapter<Wellbore> _wellboreDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log200Validator" /> class.
        /// </summary>
        /// <param name="logDataAdapter">The log data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        [ImportingConstructor]
        public Log200Validator(IEtpDataAdapter<Log> logDataAdapter, IEtpDataAdapter<Wellbore> wellboreDataAdapter)
        {
            _logDataAdapter = logDataAdapter;
            _wellboreDataAdapter = wellboreDataAdapter;
        }

        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForInsert()
        {
            // Validate parent uid property
            //if (string.IsNullOrWhiteSpace(DataObject.UidWellbore))
            //{
            //    yield return new ValidationResult(ErrorCodes.MissingParentUid.ToString(), new[] { "UidWellbore" });
            //}

            // Validate parent exists
            //else if (_wellboreDataAdapter.Exists(DataObject.UidWellbore))
            //{
            //    yield return new ValidationResult(ErrorCodes.MissingParentDataObject.ToString(), new[] { "UidWellbore" });
            //}

            // Validate UID does not exist
            //else if (_logDataAdapter.Exists(DataObject.Uid))
            //{
            //    yield return new ValidationResult(ErrorCodes.DataObjectUidAlreadyExists.ToString(), new[] { "Uid" });
            //}

            yield break;
        }
    }
}
