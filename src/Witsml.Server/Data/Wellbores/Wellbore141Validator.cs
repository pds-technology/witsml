using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Energistics.DataAccess.WITSML141;
using System.ComponentModel.Composition;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Provides validation for <see cref="Wellbore" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{Energistics.DataAccess.WITSML141.Wellbore}" />
    [Export(typeof(IDataObjectValidator<Wellbore>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Wellbore141Validator : DataObjectValidator<Wellbore>
    {
        private readonly IWitsmlDataAdapter<Wellbore> _wellboreDataAdapter;
        private readonly IWitsmlDataAdapter<Well> _wellDataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wellbore141Validator"/> class.
        /// </summary>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        [ImportingConstructor]
        public Wellbore141Validator(IWitsmlDataAdapter<Wellbore> wellboreDataAdapter, IWitsmlDataAdapter<Well> wellDataAdapter)
        {
            _wellboreDataAdapter = wellboreDataAdapter;
            _wellDataAdapter = wellDataAdapter;
        }

        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected override IEnumerable<ValidationResult> ValidateForInsert()
        {
            var uri = DataObject.GetUri();
            var uriWell = uri.Parent;

            // Validate parent uid property
            if (string.IsNullOrWhiteSpace(DataObject.UidWell))
            {
                yield return new ValidationResult(ErrorCodes.MissingParentUid.ToString(), new[] { "UidWell" });
            }

            // Validate parent exists
            else if (!_wellDataAdapter.Exists(uriWell))
            {
                yield return new ValidationResult(ErrorCodes.MissingParentDataObject.ToString(), new[] { "UidWell" });
            }

            // Validate UID does not exist
            else if (_wellboreDataAdapter.Exists(uri))
            {
                yield return new ValidationResult(ErrorCodes.DataObjectUidAlreadyExists.ToString(), new[] { "Uid" });
            }
        }
    }
}
