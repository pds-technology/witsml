using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Server.Data.Wells
{
    /// <summary>
    /// Provides validation for <see cref="Well" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{Energistics.DataAccess.WITSML131.Well}" />
    [Export(typeof(IDataObjectValidator<Well>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Well131Validator : DataObjectValidator<Well>
    {
        private readonly IWitsmlDataAdapter<Well> _wellDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Well141Validator"/> class.
        /// </summary>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        [ImportingConstructor]
        public Well131Validator(IWitsmlDataAdapter<Well> wellDataAdapter)
        {
            _wellDataAdapter = wellDataAdapter;
        }

        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForInsert()
        {
            // Validate UID does not exist
            if (_wellDataAdapter.Exists(DataObject.GetObjectId()))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidAlreadyExists.ToString(), new[] { "Uid" });
            }
        }
    }
}
